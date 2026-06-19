//#define USE_PARALLEL

using GeneticSharp;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;

namespace Synapse.Algorithms.RAPGA;

[Algorithm(
    Name = "RAPGA",
    Description = "Relevant Alleles Preserving Genetic Algorithm",
    Category = AlgorithmCategory.PopulationBased,
    AlgorithmType = AlgorithmType.RAPGA)]
public class RAPGA : IMetaheuristic
{
    private RAPGAConfig _config = null!;
    private IAlgorithmController? _algCtrl;
    private IHitlController? _hitlCtrl;
    private HitlFitness _hitlFitnessEval = null!;
    
    private ISelection _selection1 = null!;
    private ISelection _selection2 = null!;
    private ICrossover _crossover = null!;
    private IMutation _mutation = null!;
    private Guid _selection1Id;
    private Guid? _selection2Id;
    private Guid _crossoverId;
    private Guid _mutationId;
    
    private readonly ILogger<RAPGA>? _logger;
    
    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    
    public RAPGA(IAlgorithmConfig config, ILogger<RAPGA>? logger = null)
    {
        SetConfig(config);
        _logger = logger;
    }

    public void SetConfig(IAlgorithmConfig config)
    {
        _config = config as RAPGAConfig ?? throw new ArgumentException($"Must be of type {nameof(RAPGAConfig)}", nameof(config));
        _algCtrl = _config.AlgorithmController;
        _hitlCtrl = _config.HitlController;
    }

    public async Task<ISolution> SolveAsync(IProblem problem, CancellationToken ct = default)
    {
        if (_config.Seed.HasValue)
        {
            RandomProvider.SetSeed(_config.Seed.Value);
            FastRandomRandomization.ResetSeed(_config.Seed);
        }
        _logger?.LogInformation("{Algorithm} called for problem type {ProblemType} with config {ConfigType}.\n" +
                                "Population size={PopulationSize}, Selection1={Selection1}, Selection2={Selection2}\n" +
                                "Crossover={Crossover}\n" +
                                "Mutation={Mutation}, MutationProb={MutationProb}\n" +
                                "MaxGenerations={MaxGen}, TimeoutSeconds={Timeout}",
            nameof(RAPGA), problem.GetType().Name, _config.GetType().Name,
            _config.PopulationSize, _config.Selection1.GetType().Name, _config.Selection2.GetType().Name,
            _config.Crossover.GetType().Name,
            _config.Mutation.GetType().Name, _config.MutationProbability,
            _config.MaxIterations, _config.TimeoutSeconds);

        var baseFitnessEval = problem.GetFitnessEvaluator();
        _hitlFitnessEval = new HitlFitness(_hitlCtrl, baseFitnessEval);
        
        // ------------------------------------------------------------
        // Initial Population
        // ------------------------------------------------------------
        List<ISolution> population = new();
        for (int i = 0; i < _config.PopulationSize; i++)
            population.Add(CreateRandomConstraintSolution(problem, _hitlCtrl));
        
        // Use HitlFitness to get preference score
        EvaluateAll(population);

        ISolution globalBest = GetBest(population);
        
        int iteration = 0;

        // Timeout handling
        var timeoutCts = _config.TimeoutSeconds.HasValue 
            ? new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds.Value))
            : null;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts?.Token ?? CancellationToken.None);
        var token = linkedCts.Token;

        while (iteration < _config.MaxIterations && !token.IsCancellationRequested)
        {
            iteration++;
            
            SetAlgorithmOperators();
            
            // Resolve the algorithm-level similarity measure (may change between iterations via GUI)
            ISolutionSimilarity? algSimilarity = null;
            try { if (population.Count > 0) algSimilarity = _config.GetAlgorithmSimilarity(population[0]); }
            catch { /* fallback: null means exact comparison */ }

            _hitlCtrl?.NextGenerationStarted();

            // Prepare next generation
            List<ISolution> nextPop = new();

            // ------------------------------------------------------------
            // ELITISM
            // ------------------------------------------------------------
            if (_config.Elites > 0)
            {
                foreach (var elite in population
                             .OrderBy(s => s.Fitness)
                             .Take(_config.Elites))
                    nextPop.Add(elite.Clone());
            }

            // ------------------------------------------------------------
            // Generate offspring with RAPGA rules
            // ------------------------------------------------------------
            int effort = 0;
            var maxEffort = _config.Effort;

            var chromosomes = population.Select(s => s.ToChromosome()).ToList();
            var generation = new Generation(population.Count, chromosomes);
            
#if USE_PARALLEL

            ParallelOptions options = new ParallelOptions()
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            };
            int remainingSpots = cfg.MaximumPopulationSize - cfg.Elites;
            ConcurrentBag<ISolution> acceptedChildren = new();
            var localIteration = iteration;
            Parallel.For(0, maxEffort, options, (index, state) =>
            {
                if (acceptedChildren.Count >= remainingSpots)
                {
                    state.Stop();
                }
                
                if (!state.ShouldExitCurrentIteration) {
                    // if (cfg.Seed.HasValue)
                    // {
                    //     RandomProvider.SetSeed(cfg.Seed.Value * localIteration * index);
                    //     FastRandomRandomization.ResetSeed(config.Seed * localIteration * index);
                    // }
                    
                    // Select parents
                    IChromosome parentA, parentB;
                    if (_selection2 is null)
                    {
                        var parents = _selection1.SelectChromosomes(2, generation).ToList();
                        parentA = parents[0];
                        parentB = parents[1];
                    }
                    else
                    {
                        parentA = _selection1.SelectChromosomes(2, generation).First();
                        parentB = _selection2.SelectChromosomes(2, generation).First();
                    }
                    
                    // Perform crossover
                    var childChrom = _crossover.Cross(new[] { parentA, parentB }).First();

                    // Perform mutation
                    _mutation.Mutate(childChrom, (float)cfg.MutationProbability);
                    
                    var child = childChrom.ToSolution();
                    child.Fitness = fitnessEval.Evaluate(child);
                    
                    // Check acceptance rule
                    bool betterThanParents =
                        IsBetter(child, parentA.ToSolution(), fitnessEval, cfg.ComparisonFactor) &&
                        IsBetter(child, parentB.ToSolution(), fitnessEval, cfg.ComparisonFactor);

                    if (betterThanParents)
                    {
                        acceptedChildren.Add(child);
                    }
                }
            });
            
            foreach (var child in acceptedChildren)
            {
                if (nextPop.Count >= cfg.MaximumPopulationSize) break;
                if (ContainsGenome(nextPop, child, algSimilarity, cfg.SimilarityDiversityThreshold)) continue;
                nextPop.Add(child);
            }
#else
            while (effort < maxEffort &&
                   nextPop.Count < _config.MaximumPopulationSize)
            {
                effort++;
            
                // Select parents
                IChromosome parentA, parentB;
                if (_selection2 is null)
                {
                     var parents = _selection1.SelectChromosomes(2, generation).ToList();
                     parentA = parents[0];
                     parentB = parents[1];
                }
                else
                {
                    parentA = _selection1.SelectChromosomes(2, generation).First();
                    parentB = _selection2.SelectChromosomes(2, generation).First();
                }
            
                // Perform crossover
                var childChrom = _crossover.Cross(new[] { parentA, parentB }).First();
            
                // Perform mutation
                _mutation.Mutate(childChrom, (float)_config.MutationProbability);
                
                var child = childChrom.ToSolution();
                //child.Fitness = fitnessEval.Evaluate(child);
                child.Fitness = _hitlFitnessEval.EvaluateSolution(child);
            
                // Check if constraint is met
                if (_hitlCtrl is not null && _hitlCtrl.ConstraintsExist())
                {
                    var constraintChild = _hitlCtrl.EnforceConstraints(child);
                    if (constraintChild is null) continue;
                    child = constraintChild;
                }
                
                // Check acceptance rule
                bool betterThanParents =
                    IsBetter(child, parentA.ToSolution(), _config.ComparisonFactor) &&
                    IsBetter(child, parentB.ToSolution(), _config.ComparisonFactor);
            
                if (!betterThanParents)
                    continue;
            
                // Check if new genotype
                if (ContainsGenome(nextPop, child, algSimilarity, _config.SimilarityDiversityThreshold))
                    continue;
            
                nextPop.Add(child);
            }
#endif

            // ------------------------------------------------------------
            // If population too small, refill with random solutions
            // ------------------------------------------------------------
            while (nextPop.Count < _config.MinimumPopulationSize)
            {
                var rnd = CreateRandomConstraintSolution(problem, _hitlCtrl);
                //rnd.Fitness = fitnessEval.Evaluate(rnd);
                rnd.Fitness = _hitlFitnessEval.EvaluateSolution(rnd);
                if (!ContainsGenome(nextPop, rnd))
                    nextPop.Add(rnd);
            }

            // ------------------------------------------------------------
            // Progress reporting
            // ------------------------------------------------------------
            var currentBest = GetBest(nextPop);

            if (IsBetter(currentBest, globalBest, 0))
                globalBest = currentBest.Clone();

            if (iteration % Math.Max(1, _config.ProgressInterval) == 0 || iteration == _config.MaxIterations || iteration == 1)
            {
                // Compute population diversity
                double? diversity = null;
                try
                {
                    if (nextPop.Count >= 2)
                    {
                        var simMeasure = _config.GetAlgorithmSimilarity(nextPop[0]);
                        diversity = PopulationDiversityCalculator.ComputeDiversity(nextPop, simMeasure);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not compute diversity at iteration {Iter}.", iteration);
                }
                
                var evt = new ProgressEventArgs
                {
                    Problem = problem,
                    Config = _config,
                    AlgorithmController = _algCtrl,
                    HitlController = _hitlCtrl,
                    Iteration = iteration,
                    BestSolution = globalBest.Clone(),
                    BestFitness = globalBest.Fitness ?? 0,
                    BestRawFitness = baseFitnessEval.Evaluate(globalBest),
                    CurrentBestSolution = currentBest.Clone(),
                    CurrentBestFitness = currentBest.Fitness ?? 0,
                    CurrentBestRawFitness = baseFitnessEval.Evaluate(currentBest),
                    Diversity = diversity,
                    Message = $"{_config.Name} Gen {iteration}, Best={globalBest.Fitness}"
                };

                ProgressChanged?.Invoke(this, evt);
                _algCtrl?.SignalProgress(evt);
            }
            
            _hitlCtrl?.GenerationFinished(iteration,
                globalBest.Fitness ?? 0,
                currentBest.Fitness ?? 0);

            // External STOP
            if (_algCtrl?.StopRequested ?? false)
                break;

            // PAUSE handling
            if (_algCtrl?.PauseRequested ?? false)
            {
                _algCtrl.SignalPaused();
                while (_algCtrl.PauseRequested && !token.IsCancellationRequested)
                    await Task.Delay(50, token);
                _algCtrl.SignalResumed();
            }

            population = nextPop;
            
            _hitlCtrl?.ExecuteScripts(problem, baseFitnessEval, currentBest, globalBest, _algCtrl, iteration);
        }

        return globalBest;
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

    
    private void SetAlgorithmOperators()
    {
        if (_config.Selection1.Id != _selection1Id)
        {
            var gsSelection1 = _config.Selection1.ToGsSelection()
                               ?? throw new InvalidOperationException("Invalid Selection 1 mapping.");
            _selection1 = new HitlSelection(_hitlCtrl, gsSelection1);
            _selection1Id = _config.Selection1.Id;
        }

        if (_config.Selection2.Id != _selection2Id)
        {
            var gsSelection2 = _config.Selection2.ToGsSelection();
            if (gsSelection2 is not null) _selection2 = new HitlSelection(_hitlCtrl, gsSelection2);
            _selection2Id = _config.Selection2.Id;
        }
        
        if (_config.Crossover.Id != _crossoverId)
        {
            var gsCrossover = _config.Crossover.ToGsCrossover()
                              ?? throw new InvalidOperationException("Invalid Crossover mapping.");
            _crossover = new HitlCrossover(_hitlCtrl, gsCrossover);
            _crossoverId = _config.Crossover.Id;
        }

        if (_config.Mutation.Id != _mutationId)
        {
            var gsMutation = _config.Mutation.ToGsMutation()
                             ?? throw new InvalidOperationException("Invalid Mutation mapping.");
            _mutation = new HitlMutation(_hitlCtrl, gsMutation);
            _mutationId = _config.Mutation.Id;
        }
    }
    
    private void EvaluateAll(IEnumerable<ISolution> pop)
    {
        foreach (var s in pop)
            s.Fitness = _hitlFitnessEval.EvaluateSolution(s);
    }

    private static ISolution GetBest(IList<ISolution> pop)
    {
        return pop.MaxBy(s => s.Fitness ?? double.MinValue)!;
    }
    
    private static bool IsBetter(ISolution a, ISolution b, double comparisonFactor)
    {
        if (a.Fitness is null || b.Fitness is null)
            return false;

        return a.Fitness >= b.Fitness * (1.0 + comparisonFactor);
    }

    private static bool ContainsGenome(IList<ISolution> pop, ISolution candidate, ISolutionSimilarity? similarityEvaluator = null, double threshold = 0.0)
    {
        foreach (var s in pop)
        {
            // Use similarity-based comparison when a similarity measure is available.
            // Two solutions are treated as the same genome if their similarity exceeds the threshold.
            if (similarityEvaluator != null && threshold > 0.0)
            {
                try
                {
                    if (similarityEvaluator.GetSimilarity(s, candidate) >= threshold)
                        return true;
                }
                catch
                {
                    // Fall back to exact comparison if similarity evaluation fails
                    if (SameGenome(s, candidate)) return true;
                }
            }
            else if (SameGenome(s, candidate))
            {
                return true;
            }
        }
        return false;
    }

    private static bool SameGenome(ISolution a, ISolution b)
    {
        var pa = a.GetParameters();
        var pb = b.GetParameters();
        if (pa.Length != pb.Length)
            return false;

        for (int i = 0; i < pa.Length; i++)
        {
            if (!Equals(pa[i].Value, pb[i].Value))
                return false;
        }

        return true;
    }
}
