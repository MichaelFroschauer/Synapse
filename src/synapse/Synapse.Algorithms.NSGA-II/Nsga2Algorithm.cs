using GeneticSharp;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm.HITL;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.TSP;

namespace Synapse.Algorithms.NSGA_II;

internal sealed class MultiObjectiveSolution : TspSolution
{
    public MultiObjectiveSolution(int length) : base(length) { }
    public MultiObjectiveSolution(Parameter[] parameters) : base(parameters) { }
    public MultiObjectiveSolution(int[] tour) : base(tour) { }

    public List<double> ObjectiveValues { get; } = new();
    public int Rank { get; set; }
    public double CrowdingDistance { get; set; }

    public MultiObjectiveSolution CloneAsMultiObjective()
    {
        var clone = new MultiObjectiveSolution(GetParametersWithType());
        clone.ObjectiveValues.AddRange(ObjectiveValues);
        clone.Rank = Rank;
        clone.CrowdingDistance = CrowdingDistance;
        clone.Fitness = Fitness;
        return clone;
    }
}

[Algorithm(
    Name = "NSGA-II",
    Description = "Non-dominated Sorting Genetic Algorithm II",
    Category = AlgorithmCategory.PopulationBased,
    AlgorithmType = AlgorithmType.NSGA_II)]
public class Nsga2Algorithm : IMetaheuristic
{
    private Nsga2AlgorithmConfig _config = null!;
    private IAlgorithmController? _algCtrl;
    private IHitlController? _hitlCtrl;

    private GeneticSharp.ICrossover _crossover = null!;
    private GeneticSharp.IMutation _mutation = null!;
    private Guid _crossoverId;
    private Guid _mutationId;

    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    private readonly ILogger<Nsga2Algorithm>? _logger;

    public Nsga2Algorithm(IAlgorithmConfig config, ILogger<Nsga2Algorithm>? logger = null)
    {
        SetConfig(config);
        _logger = logger;
    }

    public void SetConfig(IAlgorithmConfig config)
    {
        _config = config as Nsga2AlgorithmConfig ?? throw new ArgumentException($"Must be of type {nameof(Nsga2AlgorithmConfig)}", nameof(config));
        _algCtrl = _config.AlgorithmController;
        _hitlCtrl = _config.HitlController;
    }

    private const string ObjectiveWeightsParamKey = "nsga2.objectiveWeights";
    private const string PairwisePreferenceName = "Nsga2PairwisePreference";

    public async Task<ISolution> SolveAsync(IProblem problem, CancellationToken ct = default)
    {
        if (problem is not TspProblem)
            throw new ArgumentException($"Expecting {nameof(TspProblem)}", nameof(problem));

        if (_config.Seed.HasValue)
        {
            RandomProvider.SetSeed(_config.Seed.Value);
            FastRandomRandomization.ResetSeed(_config.Seed);
        }

        SetAlgorithmOperators();
        var objectiveEvaluators = ResolveObjectiveEvaluators(problem);
        if (objectiveEvaluators.Count == 0)
            throw new InvalidOperationException("NSGA-II requires at least one objective evaluator.");

        var minimizeObjectives = objectiveEvaluators.Select(e => e.Minimize).ToArray();
        var objectiveWeights = ResolveObjectiveWeights(minimizeObjectives.Length);

        var population = CreateInitialPopulation(problem);
        EvaluatePopulation(population, objectiveEvaluators);

        var globalBest = CreateRepresentativeSolution(
            FastNonDominatedSort(population, minimizeObjectives),
            minimizeObjectives,
            objectiveWeights,
            _hitlCtrl);
        globalBest = globalBest.CloneAsMultiObjective();

        int iteration = 0;
        var timeoutCts = _config.TimeoutSeconds.HasValue
            ? new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds.Value))
            : null;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts?.Token ?? CancellationToken.None);
        var token = linkedCts.Token;

        while (iteration < _config.MaxIterations && !token.IsCancellationRequested)
        {
            iteration++;
            _hitlCtrl?.NextGenerationStarted();
            SetAlgorithmOperators();
            objectiveWeights = ResolveObjectiveWeights(minimizeObjectives.Length);

            var offspring = CreateOffspring(population, objectiveEvaluators, minimizeObjectives, objectiveWeights, token);
            var combined = population.Concat(offspring).ToList();
            var combinedFronts = FastNonDominatedSort(combined, minimizeObjectives);
            population = EnvironmentalSelection(combinedFronts, _config.PopulationSize, minimizeObjectives, objectiveWeights, _hitlCtrl);

            var currentFronts = FastNonDominatedSort(population, minimizeObjectives);
            var currentBest = CreateRepresentativeSolution(currentFronts, minimizeObjectives, objectiveWeights, _hitlCtrl);
            if (IsWeightedBetter(currentBest, globalBest, population, minimizeObjectives, objectiveWeights, _hitlCtrl))
                globalBest = currentBest.CloneAsMultiObjective();

            await MaybeAskUserObjectivePreferenceAsync(currentFronts, minimizeObjectives, objectiveWeights, iteration, token);

            if (iteration % Math.Max(1, _config.ProgressInterval) == 0 || iteration == _config.MaxIterations || iteration == 1)
            {
                double? diversity = null;
                try
                {
                    if (population.Count >= 2)
                    {
                        var simMeasure = _config.GetAlgorithmSimilarity(population[0]);
                        diversity = PopulationDiversityCalculator.ComputeDiversity(population.Cast<ISolution>().ToList(), simMeasure);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not compute diversity at iteration {Iter}.", iteration);
                }

                var prefBaseBestFitness = GetPreferenceAwareWeightedScore(globalBest, population, minimizeObjectives, objectiveWeights, _hitlCtrl);
                // var prefBaseCurrentBestFitness = GetPreferenceAwareWeightedScore(currentBest, population, minimizeObjectives, objectiveWeights, _hitlCtrl);
                // var evt = new ProgressEventArgs
                // {
                //     Problem = problem,
                //     Config = _config,
                //     AlgorithmController = _algCtrl,
                //     HitlController = _hitlCtrl,
                //     Iteration = iteration,
                //     BestSolution = globalBest.Clone(),
                //     BestFitness = prefBaseBestFitness,
                //     BestRawFitness = globalBest.Fitness ?? 0,
                //     CurrentBestSolution = currentBest.Clone(),
                //     CurrentBestFitness = prefBaseCurrentBestFitness,
                //     CurrentBestRawFitness = currentBest.Fitness ?? 0,
                //     Diversity = diversity,
                //     Message = $"{_config.Name} Gen {iteration}, WeightedScore={prefBaseBestFitness:F4}"
                // };
                
                var evt = new ProgressEventArgs
                {
                    Problem = problem,
                    Config = _config,
                    AlgorithmController = _algCtrl,
                    HitlController = _hitlCtrl,
                    Iteration = iteration,
                    BestSolution = globalBest.Clone(),
                    BestFitness = globalBest.ObjectiveValues[0],
                    BestRawFitness = globalBest.ObjectiveValues[1],
                    CurrentBestSolution = currentBest.Clone(),
                    CurrentBestFitness = currentBest.ObjectiveValues[0],
                    CurrentBestRawFitness = currentBest.ObjectiveValues[1],
                    Diversity = diversity,
                    Message = $"{_config.Name} Gen {iteration}, WeightedScore={prefBaseBestFitness:F4}, Distance={globalBest.ObjectiveValues[0]:F4}, Crossing={globalBest.ObjectiveValues[1]}"
                };

                ProgressChanged?.Invoke(this, evt);
                _algCtrl?.SignalProgress(evt);
            }

            _hitlCtrl?.GenerationFinished(iteration, globalBest.Fitness ?? 0, currentBest.Fitness ?? 0);

            if (_algCtrl?.StopRequested ?? false)
                break;

            if (_algCtrl?.PauseRequested ?? false)
            {
                _algCtrl.SignalPaused();
                while (_algCtrl.PauseRequested && !token.IsCancellationRequested)
                    await Task.Delay(50, token);
                _algCtrl.SignalResumed();
            }

            _hitlCtrl?.ExecuteScripts(problem, objectiveEvaluators[0], currentBest, globalBest, _algCtrl, iteration);
        }

        return globalBest;
    }

    private List<IFitnessEvaluator> ResolveObjectiveEvaluators(IProblem problem)
    {
        if (_hitlCtrl?.TryGetParameter<List<IFitnessEvaluator>>("nsga2.objectiveEvaluators", out var hitlEvaluators) == true &&
            hitlEvaluators is { Count: > 0 })
        {
            return hitlEvaluators;
        }

        if (problem is TspProblem tspProblem)
        {
            return
            [
                new TspFitnessEvaluator(tspProblem),
                new TspCrossingFitnessEvaluator(tspProblem)
            ];
        }

        return [problem.GetFitnessEvaluator()];
    }

    private double[] ResolveObjectiveWeights(int objectiveCount)
    {
        List<double>? source = null;
        
        if (_config.ObjectiveImportanceWeights.Count > 0)
        {
            source = _config.ObjectiveImportanceWeights;
        }
        
        if (_hitlCtrl?.TryGetParameter<double[]>(ObjectiveWeightsParamKey, out var runtimeWeightsArray) == true)
        {
            if (source == null)
            {
                source = runtimeWeightsArray.ToList();
            }
            else
            {
                int maxIdx = Math.Min(runtimeWeightsArray.Length, source.Count);
                for (int i = 0; i < maxIdx; i++)
                {
                    source[i] = (source[i] + runtimeWeightsArray[i]) / 2.0;
                }
            }
        }
        else if (_hitlCtrl?.TryGetParameter<List<double>>(ObjectiveWeightsParamKey, out var runtimeWeightsList) == true)
        {
            if (source == null)
            {
                source = runtimeWeightsList;
            }
            else
            {
                int maxIdx = Math.Min(runtimeWeightsList.Count, source.Count);
                for (int i = 0; i < maxIdx; i++)
                {
                    source[i] = (source[i] + runtimeWeightsList[i]) / 2.0;
                }
            }
        }

        var weights = new double[objectiveCount];
        for (int i = 0; i < objectiveCount; i++)
            weights[i] = 1.0;

        if (source is not null)
        {
            int idx = 0;
            foreach (var w in source)
            {
                if (idx >= objectiveCount) break;
                weights[idx] = w > 0 ? w : 0.0001;
                idx++;
            }
        }

        var sum = weights.Sum();
        if (sum <= 0)
        {
            for (int i = 0; i < objectiveCount; i++)
                weights[i] = 1.0 / objectiveCount;
            return weights;
        }

        for (int i = 0; i < objectiveCount; i++)
            weights[i] /= sum;

        return weights;
    }

    private List<MultiObjectiveSolution> CreateInitialPopulation(IProblem problem)
    {
        var population = new List<MultiObjectiveSolution>(_config.PopulationSize);
        while (population.Count < _config.PopulationSize)
        {
            var solution = CreateRandomConstraintSolution(problem, _hitlCtrl) as TspSolution
                           ?? throw new ArgumentException($"Must be of type {nameof(TspSolution)}");

            var adjusted = ApplyHitlAdjustments(new MultiObjectiveSolution(solution.GetParametersWithType()));
            if (adjusted is not null)
                population.Add(adjusted);
        }

        return population;
    }

    private void EvaluatePopulation(IEnumerable<MultiObjectiveSolution> population, IReadOnlyList<IFitnessEvaluator> objectiveEvaluators)
    {
        foreach (var solution in population)
        {
            solution.ObjectiveValues.Clear();
            foreach (var evaluator in objectiveEvaluators)
                solution.ObjectiveValues.Add(evaluator.Evaluate(solution));

            // IMetaheuristic uses a scalar Fitness field; for NSGA-II we expose objective-0 as the representative scalar.
            solution.Fitness = solution.ObjectiveValues[0];
        }
    }

    private List<MultiObjectiveSolution> CreateOffspring(
        IReadOnlyList<MultiObjectiveSolution> population,
        IReadOnlyList<IFitnessEvaluator> objectiveEvaluators,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        CancellationToken token)
    {
        var offspring = new List<MultiObjectiveSolution>(_config.PopulationSize);

        while (offspring.Count < _config.PopulationSize && !token.IsCancellationRequested)
        {
            var parentA = TournamentSelect(population, population, minimizeObjectives, objectiveWeights);
            var parentB = TournamentSelect(population, population, minimizeObjectives, objectiveWeights);

            foreach (var child in Recombine(parentA, parentB))
            {
                if (token.IsCancellationRequested || offspring.Count >= _config.PopulationSize)
                    break;

                var constrained = ApplyConstraints(child);
                if (constrained is null)
                    continue;

                constrained.ObjectiveValues.Clear();
                foreach (var evaluator in objectiveEvaluators)
                    constrained.ObjectiveValues.Add(evaluator.Evaluate(constrained));
                constrained.Fitness = constrained.ObjectiveValues[0];
                offspring.Add(constrained);
            }
        }

        return offspring;
    }

    private IEnumerable<MultiObjectiveSolution> Recombine(MultiObjectiveSolution parentA, MultiObjectiveSolution parentB)
    {
        var pAChrom = new TspSolution(parentA.GetParametersWithType()).ToChromosome();
        var pBChrom = new TspSolution(parentB.GetParametersWithType()).ToChromosome();

        IList<IChromosome> childrenChromosomes;
        if (RandomProvider.Value.GetDouble() <= _config.CrossoverProbability)
        {
            childrenChromosomes = _crossover.Cross([pAChrom, pBChrom]);
        }
        else
        {
            childrenChromosomes = [pAChrom.Clone(), pBChrom.Clone()];
        }

        foreach (var chromosome in childrenChromosomes)
        {
            _mutation.Mutate(chromosome, (float)_config.MutationProbability);
            var tspSolution = chromosome.ToSolution() as TspSolution
                              ?? throw new InvalidOperationException($"Mapped solution must be {nameof(TspSolution)}.");
            yield return new MultiObjectiveSolution(tspSolution.GetParametersWithType());
        }
    }

    private MultiObjectiveSolution? ApplyConstraints(MultiObjectiveSolution candidate)
    {
        return ApplyHitlAdjustments(candidate);
    }

    private MultiObjectiveSolution? ApplyHitlAdjustments(MultiObjectiveSolution candidate)
    {
        ISolution current = candidate;

        if (_hitlCtrl is not null && _hitlCtrl.ManualEditsExist())
        {
            current = _hitlCtrl.MaybeApplyManualEdit(current) ?? current;
        }

        if (_hitlCtrl is null || !_hitlCtrl.ConstraintsExist())
        {
            return current as TspSolution is { } tspNoConstraint
                ? new MultiObjectiveSolution(tspNoConstraint.GetParametersWithType())
                : null;
        }

        var constrained = _hitlCtrl.EnforceConstraints(current)
                         ?? _hitlCtrl.EnforceConstraints(current, true);
        if (constrained is not TspSolution tsp)
            return null;

        return new MultiObjectiveSolution(tsp.GetParametersWithType());
    }

    private MultiObjectiveSolution TournamentSelect(
        IReadOnlyList<MultiObjectiveSolution> population,
        IReadOnlyList<MultiObjectiveSolution> scoreReferencePopulation,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights)
    {
        var tournamentSize = Math.Max(2, _config.TournamentSize);
        var winner = population[RandomProvider.Value.GetInt(0, population.Count)];

        for (int i = 1; i < tournamentSize; i++)
        {
            var contender = population[RandomProvider.Value.GetInt(0, population.Count)];
            if (CrowdedComparisonOperator(contender, winner, scoreReferencePopulation, minimizeObjectives, objectiveWeights, _hitlCtrl) < 0)
                winner = contender;
        }

        return winner;
    }

    private static int CrowdedComparisonOperator(
        MultiObjectiveSolution a,
        MultiObjectiveSolution b,
        IReadOnlyList<MultiObjectiveSolution> scoreReferencePopulation,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        IHitlController? hitlCtrl)
    {
        if (a.Rank < b.Rank) return -1;
        if (a.Rank > b.Rank) return 1;

        if (a.CrowdingDistance > b.CrowdingDistance) return -1;
        if (a.CrowdingDistance < b.CrowdingDistance) return 1;

        var scoreA = GetPreferenceAwareWeightedScore(a, scoreReferencePopulation, minimizeObjectives, objectiveWeights, hitlCtrl);
        var scoreB = GetPreferenceAwareWeightedScore(b, scoreReferencePopulation, minimizeObjectives, objectiveWeights, hitlCtrl);
        return scoreA.CompareTo(scoreB);
    }

    private List<List<MultiObjectiveSolution>> FastNonDominatedSort(List<MultiObjectiveSolution> population, IReadOnlyList<bool> minimizeObjectives)
    {
        var dominationCounts = new Dictionary<MultiObjectiveSolution, int>(population.Count);
        var dominates = new Dictionary<MultiObjectiveSolution, List<MultiObjectiveSolution>>(population.Count);
        var fronts = new List<List<MultiObjectiveSolution>>();

        var firstFront = new List<MultiObjectiveSolution>();
        foreach (var p in population)
        {
            dominates[p] = new List<MultiObjectiveSolution>();
            dominationCounts[p] = 0;

            foreach (var q in population)
            {
                if (ReferenceEquals(p, q))
                    continue;

                if (Dominates(p, q, minimizeObjectives))
                {
                    dominates[p].Add(q);
                }
                else if (Dominates(q, p, minimizeObjectives))
                {
                    dominationCounts[p]++;
                }
            }

            if (dominationCounts[p] == 0)
            {
                p.Rank = 1;
                firstFront.Add(p);
            }
        }

        fronts.Add(firstFront);
        int frontIndex = 0;

        while (frontIndex < fronts.Count && fronts[frontIndex].Count > 0)
        {
            var nextFront = new List<MultiObjectiveSolution>();
            foreach (var p in fronts[frontIndex])
            {
                foreach (var q in dominates[p])
                {
                    dominationCounts[q]--;
                    if (dominationCounts[q] == 0)
                    {
                        q.Rank = frontIndex + 2;
                        nextFront.Add(q);
                    }
                }
            }

            if (nextFront.Count > 0)
                fronts.Add(nextFront);

            frontIndex++;
        }

        foreach (var front in fronts)
            CrowdingDistanceAssignment(front);

        return fronts;
    }

    private bool Dominates(MultiObjectiveSolution a, MultiObjectiveSolution b, IReadOnlyList<bool> minimizeObjectives)
    {
        bool betterInAtLeastOne = false;
        for (int i = 0; i < a.ObjectiveValues.Count; i++)
        {
            var aVal = a.ObjectiveValues[i];
            var bVal = b.ObjectiveValues[i];
            var minimize = minimizeObjectives[i];

            if (minimize)
            {
                if (aVal > bVal)
                    return false;
                if (aVal < bVal)
                    betterInAtLeastOne = true;
            }
            else
            {
                if (aVal < bVal)
                    return false;
                if (aVal > bVal)
                    betterInAtLeastOne = true;
            }
        }

        return betterInAtLeastOne;
    }

    private void CrowdingDistanceAssignment(List<MultiObjectiveSolution> front)
    {
        if (front.Count == 0)
            return;

        foreach (var solution in front)
            solution.CrowdingDistance = 0;

        if (front.Count <= 2)
        {
            foreach (var solution in front)
                solution.CrowdingDistance = double.PositiveInfinity;
            return;
        }

        int objectiveCount = front[0].ObjectiveValues.Count;
        for (int objectiveIndex = 0; objectiveIndex < objectiveCount; objectiveIndex++)
        {
            var sorted = front.OrderBy(s => s.ObjectiveValues[objectiveIndex]).ToList();
            sorted[0].CrowdingDistance = double.PositiveInfinity;
            sorted[^1].CrowdingDistance = double.PositiveInfinity;

            double min = sorted[0].ObjectiveValues[objectiveIndex];
            double max = sorted[^1].ObjectiveValues[objectiveIndex];
            if (Math.Abs(max - min) < double.Epsilon)
                continue;

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                if (double.IsPositiveInfinity(sorted[i].CrowdingDistance))
                    continue;

                var prev = sorted[i - 1].ObjectiveValues[objectiveIndex];
                var next = sorted[i + 1].ObjectiveValues[objectiveIndex];
                sorted[i].CrowdingDistance += (next - prev) / (max - min);
            }
        }
    }

    private static List<MultiObjectiveSolution> EnvironmentalSelection(
        List<List<MultiObjectiveSolution>> fronts,
        int populationSize,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        IHitlController? hitlCtrl)
    {
        var nextPopulation = new List<MultiObjectiveSolution>(populationSize);

        foreach (var front in fronts)
        {
            if (nextPopulation.Count + front.Count <= populationSize)
            {
                nextPopulation.AddRange(front.Select(s => s.CloneAsMultiObjective()));
                continue;
            }

            var slotsLeft = populationSize - nextPopulation.Count;
            if (slotsLeft <= 0)
                break;

            var selectedFromFront = front
                .OrderByDescending(s => s.CrowdingDistance)
                .ThenBy(s => GetPreferenceAwareWeightedScore(s, front, minimizeObjectives, objectiveWeights, hitlCtrl))
                .Take(slotsLeft)
                .Select(s => s.CloneAsMultiObjective());

            nextPopulation.AddRange(selectedFromFront);
            break;
        }

        return nextPopulation;
    }

    private static MultiObjectiveSolution CreateRepresentativeSolution(
        List<List<MultiObjectiveSolution>> fronts,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        IHitlController? hitlCtrl)
    {
        var firstFront = fronts.FirstOrDefault(f => f.Count > 0)
                         ?? throw new InvalidOperationException("Population has no non-empty Pareto front.");

        var representative = firstFront
            .OrderByDescending(s => s.CrowdingDistance)
            .ThenBy(s => GetPreferenceAwareWeightedScore(s, firstFront, minimizeObjectives, objectiveWeights, hitlCtrl))
            .First();

        return representative.CloneAsMultiObjective();
    }

    private static bool IsWeightedBetter(
        MultiObjectiveSolution candidate,
        MultiObjectiveSolution currentBest,
        IReadOnlyList<MultiObjectiveSolution> referencePopulation,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        IHitlController? hitlCtrl)
    {
        var candidateScore = GetPreferenceAwareWeightedScore(candidate, referencePopulation, minimizeObjectives, objectiveWeights, hitlCtrl);
        var bestScore = GetPreferenceAwareWeightedScore(currentBest, referencePopulation, minimizeObjectives, objectiveWeights, hitlCtrl);
        return candidateScore < bestScore;
    }

    private static double GetPreferenceAwareWeightedScore(
        MultiObjectiveSolution solution,
        IReadOnlyList<MultiObjectiveSolution> referencePopulation,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        IHitlController? hitlCtrl)
    {
        var objectiveCount = solution.ObjectiveValues.Count;
        double weighted = 0.0;

        for (int i = 0; i < objectiveCount; i++)
        {
            var values = referencePopulation.Select(s => s.ObjectiveValues[i]);
            var min = values.Min();
            var max = values.Max();
            var value = solution.ObjectiveValues[i];

            double normalized;
            if (Math.Abs(max - min) < double.Epsilon)
            {
                normalized = 0.0;
            }
            else if (minimizeObjectives[i])
            {
                normalized = (value - min) / (max - min);
            }
            else
            {
                normalized = (max - value) / (max - min);
            }

            weighted += objectiveWeights[i] * normalized;
        }

        var preferenceFactor = hitlCtrl?.GetPreferenceFactor(solution) ?? 1.0;
        if (preferenceFactor <= 0.0) preferenceFactor = 1.0;

        // Lower score remains better; preference factor <-> stronger user preference lowers effective score.
        return weighted / preferenceFactor;
    }

    private async Task MaybeAskUserObjectivePreferenceAsync(
        IReadOnlyList<List<MultiObjectiveSolution>> fronts,
        IReadOnlyList<bool> minimizeObjectives,
        IReadOnlyList<double> objectiveWeights,
        int iteration,
        CancellationToken token)
    {
        if (_hitlCtrl is null || _hitlCtrl.AskPreference is null || _hitlCtrl.AskPreferenceInterval <= 0)
            return;

        if (iteration % _hitlCtrl.AskPreferenceInterval != 0)
            return;

        var firstFront = fronts.FirstOrDefault(f => f.Count >= 2);
        if (firstFront is null)
            return;

        if (firstFront.Count < 2)
            return;

        if ((_config.MaxIterations <= 0) || token.IsCancellationRequested)
            return;

        // Pick a pair with strong trade-off distance in normalized objective space.
        var pair = SelectMostDiversePair(firstFront, minimizeObjectives);
        if (pair is null)
            return;

        var (left, right) = pair.Value;

        // Just for experiment tests:
        // var leftDistance = left.ObjectiveValues[0];
        // var leftCrossing = left.ObjectiveValues[1];
        // var rightDistance = right.ObjectiveValues[0];
        // var rightCrossing = right.ObjectiveValues[1];
        // Console.WriteLine($"Left: {leftDistance} {leftCrossing} | Right: {rightDistance} {rightCrossing}");
        // var userSelection = leftDistance < rightDistance ? 1 : 2;
        //var userSelection = leftCrossing > rightCrossing ? 1 : 2;
        
        _hitlCtrl.AskPreference([left], [right]);
         var userSelection = await _hitlCtrl.GetUserResponseAsync();
         if (userSelection is < 1 or > 2)
             return;

        var selected = userSelection == 1 ? left : right;
        var rejected = userSelection == 1 ? right : left;

        var similarity = _hitlCtrl.GetSolutionSimilarity(selected);
        _hitlCtrl.AddSolutionPreference(similarity, selected, _hitlCtrl.SolutionSimilarityPreferenceWeight, PairwisePreferenceName);

        // Learn objective importance from the user's pairwise choice and push it as runtime override.
        var learnedWeights = LearnWeightsFromPreference(selected, rejected, objectiveWeights, minimizeObjectives);
        _hitlCtrl.SetParameter(ObjectiveWeightsParamKey, learnedWeights);
    }

    private static (MultiObjectiveSolution Left, MultiObjectiveSolution Right)? SelectMostDiversePair(
        IReadOnlyList<MultiObjectiveSolution> front,
        IReadOnlyList<bool> minimizeObjectives)
    {
        if (front.Count < 2)
            return null;

        double bestDistance = double.NegativeInfinity;
        MultiObjectiveSolution? bestLeft = null;
        MultiObjectiveSolution? bestRight = null;

        for (int i = 0; i < front.Count - 1; i++)
        {
            for (int j = i + 1; j < front.Count; j++)
            {
                var d = GetNormalizedObjectiveDistance(front[i], front[j], front, minimizeObjectives);
                if (d > bestDistance)
                {
                    bestDistance = d;
                    bestLeft = front[i];
                    bestRight = front[j];
                }
            }
        }

        return bestLeft is not null && bestRight is not null ? (bestLeft, bestRight) : null;
    }

    private static double GetNormalizedObjectiveDistance(
        MultiObjectiveSolution a,
        MultiObjectiveSolution b,
        IReadOnlyList<MultiObjectiveSolution> reference,
        IReadOnlyList<bool> minimizeObjectives)
    {
        double sumSq = 0.0;
        for (int i = 0; i < a.ObjectiveValues.Count; i++)
        {
            var values = reference.Select(s => s.ObjectiveValues[i]);
            var min = values.Min();
            var max = values.Max();
            if (Math.Abs(max - min) < double.Epsilon)
                continue;

            var an = minimizeObjectives[i]
                ? (a.ObjectiveValues[i] - min) / (max - min)
                : (max - a.ObjectiveValues[i]) / (max - min);
            var bn = minimizeObjectives[i]
                ? (b.ObjectiveValues[i] - min) / (max - min)
                : (max - b.ObjectiveValues[i]) / (max - min);

            var diff = an - bn;
            sumSq += diff * diff;
        }

        return Math.Sqrt(sumSq);
    }

    private static double[] LearnWeightsFromPreference(
        MultiObjectiveSolution selected,
        MultiObjectiveSolution rejected,
        IReadOnlyList<double> currentWeights,
        IReadOnlyList<bool> minimizeObjectives)
    {
        var learned = currentWeights.ToArray();

        for (int i = 0; i < selected.ObjectiveValues.Count; i++)
        {
            var s = selected.ObjectiveValues[i];
            var r = rejected.ObjectiveValues[i];

            var selectedBetter = minimizeObjectives[i] ? s < r : s > r;
            var selectedWorse = minimizeObjectives[i] ? s > r : s < r;

            if (selectedBetter)
                learned[i] *= 1.15;
            else if (selectedWorse)
                learned[i] *= 0.9;
        }

        var sum = learned.Sum();
        if (sum <= 0)
            return Enumerable.Repeat(1.0 / learned.Length, learned.Length).ToArray();

        for (int i = 0; i < learned.Length; i++)
            learned[i] /= sum;

        return learned;
    }

    private void SetAlgorithmOperators()
    {
        if (_config.Crossover.Id != _crossoverId)
        {
            var gsCrossover = _config.Crossover.ToGsCrossover()
                              ?? throw new InvalidOperationException("Invalid crossover mapping.");
            _crossover = new HitlCrossover(_hitlCtrl, gsCrossover);
            _crossoverId = _config.Crossover.Id;
        }

        if (_config.Mutation.Id != _mutationId)
        {
            var gsMutation = _config.Mutation.ToGsMutation()
                             ?? throw new InvalidOperationException("Invalid mutation mapping.");
            _mutation = new HitlMutation(_hitlCtrl, gsMutation);
            _mutationId = _config.Mutation.Id;
        }
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    private ISolution CreateRandomConstraintSolution(IProblem problem, IHitlController? hitlCtrl)
    {
        const int maxAttempts = 100;
        var solution = problem.CreateRandomSolution();

        if (hitlCtrl is null || !hitlCtrl.ConstraintsExist())
            return solution;

        ISolution? constrainedSolution = null;
        var attemptsLeft = maxAttempts;

        while (constrainedSolution is null && attemptsLeft > 0)
        {
            solution = problem.CreateRandomSolution();
            constrainedSolution = hitlCtrl.EnforceConstraints(solution);
            attemptsLeft--;
        }

        constrainedSolution ??= hitlCtrl.EnforceConstraints(solution, true);
        return constrainedSolution ?? solution;
    }
}
