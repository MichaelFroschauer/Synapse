using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.ParticleSwarm.Mapper;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Algorithms.ParticleSwarm;

[Algorithm(
    Name = "Particle Swarm Algorithm",
    Description = "Particle Swarm Algorithm simulates bird or fish swarms",
    Category = AlgorithmCategory.PopulationBased,
    AlgorithmType = AlgorithmType.ParticleSwarm)]
public class ParticleSwarmAlgorithm : IMetaheuristic
{
    private ParticleSwarmConfig _config = null!;
    private IAlgorithmController? _algCtrl;
    private IHitlController? _hitlCtrl;
    private IFitnessEvaluator _fitnessEvaluator = null!;
    
    private readonly ILogger<ParticleSwarmAlgorithm>? _logger;
    public event EventHandler<ProgressEventArgs>? ProgressChanged;


    public ParticleSwarmAlgorithm(IAlgorithmConfig config, ILogger<ParticleSwarmAlgorithm>? logger = null)
    {
        SetConfig(config);
        _logger = logger;
    }
    
    public void SetConfig(IAlgorithmConfig config)
    {
        if (config is not ParticleSwarmConfig psoConfig)
        {
            _logger?.LogError("Config is not of type ParticleSwarmConfig (actual: {Type}).", config.GetType().Name);
            throw new ArgumentException($"Must be of type {nameof(ParticleSwarmConfig)}", nameof(config));
        }

        _config = psoConfig;
        _algCtrl = _config.AlgorithmController;
        _hitlCtrl = _config.HitlController;
    }

    public async Task<ISolution> SolveAsync(IProblem problem, CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting PSO SolveAsync. Name={Name}, SwarmSize={Size}, MaxIter={MaxIter}",
            _config.Name, _config.SwarmSize, _config.MaxIterations);

        // Acquire evaluator from problem (assume exists like in GA)
        _fitnessEvaluator = problem.GetFitnessEvaluator();

        // Dimensions and bounds
        var solution = problem.CreateRandomSolution();
        IParticleMapper mapper = solution.GetMapper();
        var dim = mapper.Dimensions;
        if (dim <= 0)
        {
            _logger?.LogError("PSO configuration lacks dimension information. Provide Mapper or CreateRandomPosition or set Dimensions.");
            throw new ArgumentException("Dimensions not specified in ParticleSwarmConfig.");
        }

        // validate bounds
        var (minBound, maxBound) = GetBounds(_config, mapper);
        if ((minBound != null && minBound.Length != dim) || (maxBound != null && maxBound.Length != dim))
        {
            _logger?.LogError("PositionMin/PositionMax must either be null or arrays of length Dimensions ({Dim}).", dim);
            throw new ArgumentException("Bounds length mismatch.");
        }

        var velClamp = VelocityClamp(_config, mapper);

        // Prepare cancellation & timeout
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var tokenReg = linkedCts.Token.Register(() => _logger?.LogInformation("Stopping PSO via CancellationToken."));
        if (_config.TimeoutSeconds.HasValue)
        {
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds.Value));
        }

        // Particle state containers
        int swarm = Math.Max(1, _config.SwarmSize);
        var positions = new double[swarm][];
        var velocities = new double[swarm][];
        var personalBestPositions = new double[swarm][];
        var personalBestFitness = new double[swarm]; // stores _adjusted_ fitness values (HITL preference factored)
        var currentBestPosition = new double[dim];
        double currentBestFitness = _fitnessEvaluator.Minimize ? double.PositiveInfinity : double.NegativeInfinity;
        var globalBestPosition = new double[dim];
        double globalBestFitness = _fitnessEvaluator.Minimize ? double.PositiveInfinity : double.NegativeInfinity;
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        if (_config.Seed.HasValue) RandomProvider.SetSeed(_config.Seed.Value);
        IRandom rand = RandomProvider.Value;

        for (int i = 0; i < swarm; i++)
            personalBestFitness[i] = _fitnessEvaluator.Minimize ? double.PositiveInfinity : double.NegativeInfinity;

        // Initialize
        for (int i = 0; i < swarm; i++)
        {
            positions[i] = CreateRandomPos(_config, mapper, rand);
            velocities[i] = new double[dim]; // zero initial velocities
            for (int j = 0; j < dim; j++) velocities[i][j] = rand.GetDouble() * 0.1 * (rand.GetDouble() > 0.5 ? -1 : 1);
            personalBestPositions[i] = (double[])positions[i].Clone();

            // Evaluate initial fitness with HITL hooks (manual edits + constraints + preferences)
            var sol = PositionToSolution(positions[i], mapper);
            sol = ApplyHitlHooks(sol, true);
            if (sol != null)
            {
                double effective = AdjustedFitness(sol);
                personalBestFitness[i] = effective;
            
                // update global best based on effective fitness
                if (i == 0 || (_fitnessEvaluator.Minimize ? effective < globalBestFitness : effective > globalBestFitness))
                {
                    globalBestFitness = effective;
                    Array.Copy(positions[i], globalBestPosition, dim);
                }
            }
            // TODO: What to do here?
            // else
            // {
            //     var sol2 = PositionToSolution(positions[i], mapper);
            //     sol2 = ApplyHitlHooks(sol2, true);
            // }
        }

        // Ensure current-best starts from a valid initialized global best.
        currentBestFitness = globalBestFitness;
        Array.Copy(globalBestPosition, currentBestPosition, dim);

        // If all initial candidates were rejected (e.g., strict constraints), recover with a bounded random seed point.
        if ((_fitnessEvaluator.Minimize && double.IsPositiveInfinity(globalBestFitness)) ||
            (!_fitnessEvaluator.Minimize && double.IsNegativeInfinity(globalBestFitness)))
        {
            var fallback = CreateRandomPos(_config, mapper, rand);
            Array.Copy(fallback, globalBestPosition, dim);
            Array.Copy(fallback, currentBestPosition, dim);
            var fallbackSolution = PositionToSolution(fallback, mapper);
            var fallbackFitness = AdjustedFitness(fallbackSolution);
            globalBestFitness = fallbackFitness;
            currentBestFitness = fallbackFitness;
        }

        _logger?.LogInformation("Initialized swarm: size={Size}, dimension={Dim}. Initial global fitness={GBest}.",
            swarm, dim, globalBestFitness);

        var cbestLock = new Lock();
        var gbestLock = new Lock();
        var positionsLock = new Lock(); // used when sampling positions for HITL AskPreference
        // var parallelOptions = new ParallelOptions
        // {
        //     MaxDegreeOfParallelism = Environment.ProcessorCount,
        //     CancellationToken = linkedCts.Token
        // };

        // Run PSO loop on background thread
        try
        {
            await Task.Run(async () =>
            {
                int iter = 0;
                while (iter < _config.MaxIterations)
                {
                    _hitlCtrl?.NextGenerationStarted();
                    iter++;

                    // check external stop
                    if (_algCtrl?.StopRequested == true || linkedCts.IsCancellationRequested)
                    {
                        _logger?.LogInformation("AlgorithmController requested Stop at iteration {Iter}. Stopping PSO.",
                            iter);
                        break;
                    }

                    // compute particle updates
                    // Parallel.ForEach(Enumerable.Range(0, swarm), parallelOptions, p =>
                    // {
                    for (int p = 0; p < swarm; p++)
                    {
                        var pos = positions[p];
                        var vel = velocities[p];
                        var pbest = personalBestPositions[p];

                        for (int d = 0; d < dim; d++)
                        {
                            // update velocity
                            // velocity_new = intertia * velocity + rand1 * cognitive * (personal_best - pos) + rand2 * social * (global_best - pos) 
                            // 𝑉𝑖(𝑡+1) = 𝑤*𝑉𝑖(𝑡) + 𝑐1*𝑟1*(𝑝𝑏𝑒𝑠𝑡𝑖–𝑋𝑖(𝑡)) + 𝑐2*𝑟2*(𝑔𝑏𝑒𝑠𝑡–𝑋𝑖(𝑡))
                            double r1 = rand.GetDouble();
                            double r2 = rand.GetDouble();
                            double cognitiveTerm = _config.Cognitive * r1 * (pbest[d] - pos[d]);
                            double socialTerm = _config.Social * r2 * (globalBestPosition[d] - pos[d]);
                            vel[d] = _config.Inertia * vel[d] + cognitiveTerm + socialTerm;

                            // clamp velocity
                            double vmax = velClamp[d];
                            if (vmax > 0)
                            {
                                if (vel[d] > vmax) vel[d] = vmax;
                                else if (vel[d] < -vmax) vel[d] = -vmax;
                            }

                            // update position
                            pos[d] += vel[d];
                            // clamp to bounds if present
                            if (minBound != null && maxBound != null)
                            {
                                if (pos[d] < minBound[d]) pos[d] = minBound[d];
                                else if (pos[d] > maxBound[d]) pos[d] = maxBound[d];
                            }
                        }

                        // Evaluate new solution
                        var candidateSol = PositionToSolution(pos, mapper);

                        // Apply HITL manual edits and constraints; if constraints reject (return null) -> skip
                        candidateSol = ApplyHitlHooks(candidateSol);
                        if (candidateSol == null)
                        {
                            // hitl decided to reject candidate (constraint cannot be satisfied) -> skip updates for this particle
                            continue;
                        }

                        // compute effective fitness with preferences
                        double effectiveFitness = AdjustedFitness(candidateSol);

                        // Update personal best if improved (compare using effective fitness)
                        bool pbetter = _fitnessEvaluator.Minimize
                            ? effectiveFitness < personalBestFitness[p]
                            : effectiveFitness > personalBestFitness[p];
                        if (pbetter)
                        {
                            personalBestFitness[p] = effectiveFitness;
                            personalBestPositions[p] = (double[])pos.Clone();
                        }

                        // Update current best (compare using effective fitness)
                        lock (cbestLock)
                        {
                            bool cbetter = _fitnessEvaluator.Minimize ? effectiveFitness < currentBestFitness : effectiveFitness > currentBestFitness;
                            if (cbetter)
                            {
                                currentBestFitness = effectiveFitness;
                                Array.Copy(pos, currentBestPosition, dim);
                            }
                        }
                        
                        // Update global best (compare using effective fitness)
                        lock (gbestLock)
                        {
                            bool gbetter = _fitnessEvaluator.Minimize ? effectiveFitness < globalBestFitness : effectiveFitness > globalBestFitness;
                            if (gbetter)
                            {
                                globalBestFitness = effectiveFitness;
                                Array.Copy(pos, globalBestPosition, dim);
                            }
                        }
                    }
                    //); // end particles loop

                    var currentBestSolution = PositionToSolution(currentBestPosition, mapper);
                    var globalBestSolution = PositionToSolution(globalBestPosition, mapper);

                    // Compute population diversity from all particle positions
                    double? diversity = null;
                    try
                    {
                        if (iter % Math.Max(1, _config.ProgressInterval) == 0 || _config.MaxIterations == iter || iter == 1)
                        {
                            var popSolutions = new List<ISolution>(swarm);
                            for (int p = 0; p < swarm; p++)
                                popSolutions.Add(PositionToSolution(positions[p], mapper));
                            var simMeasure = _config.GetAlgorithmSimilarity(popSolutions[0]);
                            diversity = PopulationDiversityCalculator.ComputeDiversity(popSolutions, simMeasure);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "Could not compute diversity at iter {Iter}.", iter);
                    }

                    ReportProgress(problem, _config, globalBestSolution, currentBestSolution, iter, diversity);
                    _hitlCtrl?.ExecuteScripts(problem, _fitnessEvaluator, currentBestSolution, globalBestSolution, _algCtrl, iter);

                    // Ask human for preference occasionally (non-blocking; controller may show UI and handle results)
                    try
                    {
                        if (_hitlCtrl?.AskPreference != null && _hitlCtrl.AskPreferenceInterval > 0 && iter % Math.Max(1, _hitlCtrl.AskPreferenceInterval) == 0)
                        {
                            // Sample a small set of candidate solutions to show to the human.
                            // When a similarity measure is configured, use it to build a maximally diverse
                            // sample: greedily pick particles that are least similar to those already selected.
                            List<ISolution> sample = new List<ISolution>();
                            int sampleCount = Math.Min(8, swarm);
                            lock (positionsLock)
                            {
                                // Build a pool of candidate solutions from all particles
                                var pool = new List<ISolution>();
                                for (int s = 0; s < swarm; s++)
                                {
                                    var sol = PositionToSolution(positions[s], mapper);
                                    sol = ApplyHitlHooks(sol) ?? sol;
                                    pool.Add(sol);
                                }

                                // Try diversity-based sampling using the configured similarity measure
                                bool diverseSamplingSucceeded = false;
                                try
                                {
                                    ISolutionSimilarity? similarityEvaluator = pool.Count > 0
                                        ? _hitlCtrl.GetSolutionSimilarity(pool[0])
                                        : null;

                                    if (similarityEvaluator != null)
                                    {
                                        // Greedy max-diversity selection: start with the particle nearest to global best,
                                        // then iteratively add the particle that is least similar to all already-selected ones.
                                        sample.Add(pool[0]); // seed with the first particle
                                        while (sample.Count < sampleCount && pool.Count > sample.Count)
                                        {
                                            ISolution? mostDiverse = null;
                                            double lowestMaxSimilarity = double.MaxValue;
                                            foreach (var candidate in pool)
                                            {
                                                if (sample.Contains(candidate)) continue;
                                                // Find the maximum similarity of this candidate to any already-selected solution
                                                double maxSim = 0.0;
                                                foreach (var selected in sample)
                                                {
                                                    double sim = similarityEvaluator.GetSimilarity(candidate, selected);
                                                    if (sim > maxSim) maxSim = sim;
                                                }
                                                // Pick the candidate with the smallest max-similarity (most diverse)
                                                if (maxSim < lowestMaxSimilarity)
                                                {
                                                    lowestMaxSimilarity = maxSim;
                                                    mostDiverse = candidate;
                                                }
                                            }
                                            if (mostDiverse != null) sample.Add(mostDiverse);
                                            else break;
                                        }
                                        diverseSamplingSucceeded = sample.Count > 0;
                                    }
                                }
                                catch
                                {
                                    // Diversity sampling failed; fall back to evenly-spaced below
                                    sample.Clear();
                                }

                                // Fallback: evenly-spaced sampling when similarity-based selection is unavailable
                                if (!diverseSamplingSucceeded)
                                {
                                    sample.Clear();
                                    for (int s = 0; s < sampleCount; s++)
                                    {
                                        int idx = (int)Math.Floor((double)s * swarm / sampleCount);
                                        sample.Add(pool[idx]);
                                    }
                                }
                            }

                            // notify controller — controller implementation may show a UI and set preferences internally
                            // We do not assume a particular return value semantics here; controller can call AddSolutionPreference itself.
                            try
                            {
                                _hitlCtrl.AskPreference(sample, new[] { globalBestSolution });
                                var userSelection = _hitlCtrl.GetUserResponseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                                if (userSelection < 1 || userSelection > 2)
                                {
                                    Console.WriteLine($"User selection out of range, choosing selection 1");
                                }
                                var selection = userSelection switch
                                {
                                    1 => sample.First(),
                                    2 => globalBestSolution,
                                    _ => sample.First()
                                };
                                
                                // Prefer similar solutions in the future
                                _hitlCtrl.AddSolutionPreference(_hitlCtrl.GetSolutionSimilarity(selection), selection,
                                    _hitlCtrl.SolutionSimilarityPreferenceWeight, "ParticleSwarmAskPreferenceSolution");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "AskPreference callback threw an exception.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error while invoking AskPreference.");
                    }

                    // Pause handling: if algCtrl requests pause, pause without blocking core PSO thread
                    var stop = await CheckForPauseAndStop(iter, linkedCts);
                    if (stop) break;
                    
                    _hitlCtrl?.GenerationFinished(iter, globalBestFitness, currentBestFitness);
                } // end outer loop

                _logger?.LogInformation(
                    "PSO finished at iteration {Iter}, BestFitness={Fitness}, Time Elapsed: {TimeSpan}",
                    iter, globalBestFitness, stopwatch.Elapsed);

            }, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("PSO run cancelled by cancellation token.");
            //throw; // do not rethrow
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unhandled exception inside PSO run.");
            //throw; // do not rethrow
        }

        // Final: return the raw solution at gbest position (HITL may have preferences but we return the actual position translation)
        return PositionToSolution(globalBestPosition, mapper);
    }

    private async Task<bool> CheckForPauseAndStop(int iter, CancellationTokenSource linkedCts)
    {
        // Pause handling: if algCtrl requests pause, pause without blocking core PSO thread
        if (_algCtrl?.PauseRequested ?? false)
        {
            _logger?.LogInformation(
                "Pause requested at iteration {Iter}. Entering pause-wait (non-blocking).", iter);
            // wait until resumed or cancelled or stop requested
            _algCtrl.SignalPaused();
            while (_algCtrl.PauseRequested && !linkedCts.IsCancellationRequested && !(_algCtrl.StopRequested))
            {
                await Task.Delay(50, linkedCts.Token).ConfigureAwait(false);
            }

            if (_algCtrl.StopRequested)
            {
                _logger?.LogInformation(
                    "Stop requested while paused at iteration {Iter}. Exiting PSO loop.", iter);
                return true;
            }

            if (linkedCts.IsCancellationRequested)
            {
                _logger?.LogInformation(
                    "Cancellation requested while paused at iteration {Iter}. Exiting PSO loop.", iter);
                return true;
            }

            _algCtrl.SignalResumed();
            _logger?.LogInformation("Resumed after pause at iteration {Iter}.", iter);
        }

        return false;
    }

    private static (double[]?, double[]?) GetBounds(ParticleSwarmConfig psoConfig, IParticleMapper mapper)
    {
        var dimensions = mapper.Dimensions;
        if (psoConfig.PositionMin is not null && psoConfig.PositionMin.Length != dimensions)
            throw new ArgumentException($"PositionMin length ({psoConfig.PositionMin.Length}) must match mapper dimensions ({dimensions}).");
        if (psoConfig.PositionMax is not null && psoConfig.PositionMax.Length != dimensions)
            throw new ArgumentException($"PositionMax length ({psoConfig.PositionMax.Length}) must match mapper dimensions ({dimensions}).");

        double[]? minBound = null;
        double[]? maxBound = null;

        var mapperBounds = mapper.Bounds;
        if (mapperBounds.HasValue)
        {
            minBound = (double[])mapperBounds.Value.Min.Clone();
            maxBound = (double[])mapperBounds.Value.Max.Clone();
        }

        if (psoConfig.PositionMin is not null)
        {
            minBound ??= new double[dimensions];
            for (var i = 0; i < dimensions; i++)
            {
                minBound[i] = mapperBounds.HasValue ? Math.Max(minBound[i], psoConfig.PositionMin[i]) : psoConfig.PositionMin[i];
            }
        }

        if (psoConfig.PositionMax is not null)
        {
            maxBound ??= new double[dimensions];
            for (var i = 0; i < dimensions; i++)
            {
                maxBound[i] = mapperBounds.HasValue ? Math.Min(maxBound[i], psoConfig.PositionMax[i]) : psoConfig.PositionMax[i];
            }
        }

        if (minBound is not null && maxBound is not null)
        {
            for (var i = 0; i < dimensions; i++)
            {
                if (minBound[i] <= maxBound[i]) continue;
                minBound[i] = maxBound[i];
            }
        }

        return (minBound, maxBound);
    }

    private static double[] VelocityClamp(ParticleSwarmConfig psoConfig, IParticleMapper mapper)
    {
        // Velocity clamp limits
        var velClamp = new double[mapper.Dimensions];
        var (minBound, maxBound) = GetBounds(psoConfig, mapper);
        for (int i = 0; i < mapper.Dimensions; i++)
        {
            double range = 0.0;
            if (minBound != null && maxBound != null)
            {
                range = Math.Abs(maxBound[i] - minBound[i]);
            }

            // default fallback range is 1.0
            if (range <= 0) range = 1.0;
            velClamp[i] = range * psoConfig.VelocityClampFactor;
        }

        return velClamp;
    }

    // helper to turn position into solution
    private ISolution PositionToSolution(double[] pos, IParticleMapper? mapper)
    {
        if (mapper != null)
        {
            var sol = mapper.PositionToSolution(pos);
            sol.Fitness = _fitnessEvaluator.Evaluate(sol);
            return sol;
        }
        throw new InvalidOperationException("No mapper or PositionToSolution delegate available in config.");
    }

    // Apply HITL manual edits and constraints. Returns possibly modified solution or null if candidate rejected by constraints.
    private ISolution? ApplyHitlHooks(ISolution candidate, bool initialization = false)
    {
        if (_hitlCtrl == null) return candidate;

        try
        {
            // Maybe apply manual edit (controller may return a new solution or null)
            var edited = _hitlCtrl.MaybeApplyManualEdit(candidate);
            if (edited != null)
            {
                edited.Fitness = _fitnessEvaluator.Evaluate(edited);
                candidate = edited;
            }

            // Enforce constraints (controller may adjust solution or return null to indicate infeasible)
            var repairSolution = initialization;
            var constrained = _hitlCtrl.EnforceConstraints(candidate, repairSolution);
            if (constrained == null)
            {
                // candidate rejected
                return null;
            }

            // If constraints changed the solution, re-evaluate
            if (!ReferenceEquals(constrained, candidate))
            {
                constrained.Fitness = _fitnessEvaluator.Evaluate(constrained);
            }

            return constrained;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while applying HITL hooks (manual edits/constraints). Returning original candidate.");
            return candidate;
        }
    }
    
    private double AdjustedFitness(ISolution s)
    {
        return _hitlCtrl?.GetPreferenceAdjustedFitness(s, _fitnessEvaluator) ?? _fitnessEvaluator.Evaluate(s);
    }

    // helper to create random position
    private double[] CreateRandomPos(ParticleSwarmConfig psoConfig, IParticleMapper mapper, IRandom rand)
    {
        var (minBound, maxBound) = GetBounds(psoConfig, mapper);
        var mp = mapper.CreateRandomPosition();
        if (mp != null)
        {
            if (mp.Length != mapper.Dimensions)
                throw new ArgumentException($"Mapper random position length ({mp.Length}) must match mapper dimensions ({mapper.Dimensions}).");

            if (minBound != null && maxBound != null)
            {
                for (int d = 0; d < mapper.Dimensions; d++)
                {
                    if (mp[d] < minBound[d]) mp[d] = minBound[d];
                    else if (mp[d] > maxBound[d]) mp[d] = maxBound[d];
                }
            }

            return mp;
        }

        // fallback: uniform within provided bounds or [0,1]
        var pos = new double[mapper.Dimensions];
        for (int d = 0; d < mapper.Dimensions; d++)
        {
            double lo = minBound != null ? minBound[d] : 0.0;
            double hi = maxBound != null ? maxBound[d] : 1.0;
            pos[d] = lo + rand.GetDouble() * (hi - lo);
        }

        return pos;
    }

    private void ReportProgress(IProblem problem, ParticleSwarmConfig psoConfig, ISolution globalBestSolution, ISolution currentBestSolution, int iter, double? diversity = null)
    {
        var algCtrl = psoConfig.AlgorithmController;
        var hitlCtrl = psoConfig.HitlController;
        var fitnessEvaluator = problem.GetFitnessEvaluator();

        // progress reporting
        if (iter % Math.Max(1, psoConfig.ProgressInterval) == 0 || psoConfig.MaxIterations == iter || iter == 1)
        {
            // build solution from gbest
            var globalBestFitness = fitnessEvaluator.Evaluate(globalBestSolution);
            var currentBestFitness = fitnessEvaluator.Evaluate(currentBestSolution);
            var msgPrefix = string.IsNullOrEmpty(psoConfig.Name) ? "" : $"{psoConfig.Name}: ";
            var evt = new ProgressEventArgs
            {
                Problem = problem,
                Config = psoConfig,
                AlgorithmController = algCtrl,
                HitlController = hitlCtrl,
                Iteration = iter,
                BestSolution = globalBestSolution,
                BestFitness = AdjustedFitness(globalBestSolution),
                BestRawFitness =  globalBestFitness,
                CurrentBestSolution = currentBestSolution,
                CurrentBestFitness = AdjustedFitness(currentBestSolution),
                CurrentBestRawFitness = currentBestFitness,
                Diversity = diversity,
                Message =
                    $"{msgPrefix}PSO Iter {iter} | BestFitness: {Math.Abs(globalBestFitness):F6} | Solution: {globalBestSolution}"
            };
            ProgressChanged?.Invoke(this, evt);
            algCtrl?.SignalProgress(evt);
        }
    }
}
