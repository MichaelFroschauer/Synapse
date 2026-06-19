using GeneticSharp;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm.HITL;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Algorithms.GeneticAlgorithm;

// Full adapter for GeneticSharp -> IMetaheuristic
[Algorithm(
    Name = "Genetic Algorithm",
    Description = "Genetic Algorithm with Crossover/Mutation",
    Category = AlgorithmCategory.PopulationBased,
    AlgorithmType = AlgorithmType.Genetic)]
public class GeneticAlgorithm : IMetaheuristic
{
    private GeneticAlgorithmConfig _config = null!;
    private IAlgorithmController? _algCtrl;
    private IHitlController? _hitlCtrl;
    
    private GeneticSharp.GeneticAlgorithm _gsGeneticAlgorithm = null!;
    private ISelection? _selection;
    private ICrossover? _crossover;
    private IMutation? _mutation;
    private Guid _selection1Id;
    private Guid? _selection2Id;
    private Guid _crossoverId;
    private Guid _mutationId;
    
    private readonly ILogger<GeneticAlgorithm>? _logger;

    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    
    public GeneticAlgorithm(IAlgorithmConfig config, ILogger<GeneticAlgorithm>? logger = null)
    {
        SetConfig(config);
        _logger = logger;
    }

    public void SetConfig(IAlgorithmConfig config)
    {
        _config = config as GeneticAlgorithmConfig ?? throw new ArgumentException($"Must be of type {nameof(GeneticAlgorithmConfig)}", nameof(config));
        _algCtrl = _config.AlgorithmController;
        _hitlCtrl = _config.HitlController;
    }

    public Task<ISolution> SolveAsync(IProblem problem, CancellationToken ct = default)
    {
        var prototype = problem.CreateRandomSolution().ToChromosome();
        var population = new HitlPopulation(_hitlCtrl, _config, prototype);
        var fitnessEvaluator = problem.GetFitnessEvaluator();
        var fitness = new HitlFitness(_hitlCtrl, fitnessEvaluator);
        
        SetAlgorithmOperators(initialize: true);
        if (_config.Seed.HasValue)
        {
            RandomProvider.SetSeed(_config.Seed.Value);
            FastRandomRandomization.ResetSeed(_config.Seed);
        }
        
        _logger?.LogInformation("{Algorithm} called for problem type {ProblemType} with config {ConfigType}.\n" +
                                "Population size={PopulationSize}, Selection={Selection}\n" +
                                "Crossover={Crossover}, CrossoverProb={CrossoverProb}\n" +
                                "Mutation={Mutation}, MutationProb={MutationProb}\n" +
                                "MaxGenerations={MaxGen}, TimeoutSeconds={Timeout}",
            nameof(GeneticAlgorithm), problem.GetType().Name, _config.GetType().Name,
            _config.PopulationSize, _config.Selection.GetType().Name,
            _config.Crossover.GetType().Name, _config.CrossoverProbability,
            _config.Mutation.GetType().Name, _config.MutationProbability,
            _config.MaxIterations, _config.TimeoutSeconds);

        var taskExecutor = new ParallelTaskExecutor()
        {
            MinThreads = 16,
            MaxThreads = 16
        };
        _gsGeneticAlgorithm = new GeneticSharp.GeneticAlgorithm(population, fitness, _selection, _crossover, _mutation)
        {
            CrossoverProbability = (float)_config.CrossoverProbability,
            MutationProbability = (float)_config.MutationProbability,
            Termination = new GenerationNumberTermination(_config.MaxIterations),
            TaskExecutor = taskExecutor
        };

        ISolution? globalBestSolution = null;
        var tcs = new TaskCompletionSource<ISolution>(TaskCreationOptions.RunContinuationsAsynchronously);
        // timeout/cancellation handling
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var tokenReg = linkedCts.Token.Register(() =>
        {
            ISolution solution = globalBestSolution ??
                                 throw new InvalidOperationException("GA finished without a best chromosome.");
            tcs.TrySetResult(solution);
            _gsGeneticAlgorithm.Stop();
            _logger?.LogInformation("Stopping GA via CancellationToken.");
        });
        if (_config.TimeoutSeconds.HasValue)
        {
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds.Value));
        }

        // Subscribe to generation events to forward progress and allow external Stop/Pause via context
        EventHandler? generationHandler = null;
        generationHandler += (_, _) =>
        {
            try
            {
                // Set updated configuration values for the GA
                SetAlgorithmOperators();
                
                var gen = _gsGeneticAlgorithm.GenerationsNumber;
                var currentBestChromosome = _gsGeneticAlgorithm.BestChromosome;
                if (currentBestChromosome == null) return;
                var currentBestSolution = currentBestChromosome.ToSolution();
                if (globalBestSolution is null || 
                    fitnessEvaluator.Minimize && currentBestSolution.Fitness > globalBestSolution.Fitness ||
                    !fitnessEvaluator.Minimize && currentBestSolution.Fitness < globalBestSolution.Fitness)
                {
                    globalBestSolution = currentBestSolution;
                }
                
                _hitlCtrl?.GenerationFinished(gen,
                    globalBestSolution?.Fitness ?? 0.0,
                    currentBestSolution.Fitness ?? 0.0);
                
                if (gen % Math.Max(1, _config.ProgressInterval) == 0 || gen == _config.MaxIterations || gen == 1)
                {
                    var bestFitness = globalBestSolution?.Fitness ?? 0.0;
                    var msgPrefix = string.IsNullOrEmpty(_config.Name) ? "" : $"{_config.Name}: ";
                    
                    // Compute population diversity from current generation
                    double? diversity = null;
                    try
                    {
                        var chromosomes = _gsGeneticAlgorithm.Population?.CurrentGeneration?.Chromosomes;
                        if (chromosomes is { Count: >= 2 })
                        {
                            var popSolutions = chromosomes.Select(c => c.ToSolution()).ToList();
                            var simMeasure = _config.GetAlgorithmSimilarity(popSolutions[0]);
                            diversity = PopulationDiversityCalculator.ComputeDiversity(popSolutions, simMeasure);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "Could not compute diversity at gen {Gen}.", gen);
                    }

                    var meanFitness = 0.0;
                    var medianFitness = 0.0;
                    var worstFitness = 0.0;
                    var fitnessValues = _gsGeneticAlgorithm.Population?.CurrentGeneration?.Chromosomes.Select(c => c.Fitness!.Value).ToList();
                    if (fitnessValues != null)
                    {
                        meanFitness = fitnessValues.Sum() / fitnessValues.Count;
                        medianFitness = fitnessValues.OrderBy(f => f).ElementAt(fitnessValues.Count / 2);
                        worstFitness = fitnessValues.Min();
                    }

                    var evt = new ProgressEventArgs
                    {
                        Problem = problem,
                        Config = _config,
                        AlgorithmController = _algCtrl,
                        HitlController = _hitlCtrl,
                        Iteration = gen,
                        BestSolution = globalBestSolution?.Clone(),
                        BestFitness = bestFitness,
                        BestRawFitness = globalBestSolution != null ? fitnessEvaluator.Evaluate(globalBestSolution) : 0.0,
                        CurrentBestSolution = currentBestSolution.Clone(),
                        CurrentBestFitness = currentBestSolution.Fitness ?? 0.0,
                        CurrentBestRawFitness = fitnessEvaluator.Evaluate(currentBestSolution),
                        MeanFitness = meanFitness,
                        MedianFitness = medianFitness,
                        WorstFitness = worstFitness,
                        Diversity = diversity,
                        Message = $"{msgPrefix}Gen {gen} | Fitness: {Math.Abs(bestFitness):F2} | Solution: {globalBestSolution}"
                    };
                    
                    ProgressChanged?.Invoke(this, evt);
                    _algCtrl?.SignalProgress(evt);
                }
        
                // external stop
                if (_algCtrl?.StopRequested ?? linkedCts.Token.IsCancellationRequested)
                {
                    _gsGeneticAlgorithm.Stop();
                    return;
                }
        
                // pause: busy-wait until resumed (GeneticSharp has Pause/Resume but to keep this adapter lightweight we simply spin-sleep here)
                if (_algCtrl?.PauseRequested ?? false)
                {
                    _gsGeneticAlgorithm.Stop();
                    _algCtrl.SignalPaused();
                    
                    while (_algCtrl.PauseRequested && !linkedCts.Token.IsCancellationRequested)
                    {
                        Task.Delay(50, linkedCts.Token).Wait(linkedCts.Token);
                    }
        
                    if (!linkedCts.Token.IsCancellationRequested)
                    {
                        _gsGeneticAlgorithm.Resume();
                        _algCtrl.SignalResumed();
                    }
                }
                
                _hitlCtrl?.ExecuteScripts(problem, fitnessEvaluator, globalBestSolution, globalBestSolution, _algCtrl, gen);
                _hitlCtrl?.NextGenerationStarted();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception in GenerationRan handler.");
            }
        };

        _gsGeneticAlgorithm.GenerationRan += generationHandler;

        // Run GA on a background thread (GeneticSharp's Start blocks)
        Task.Run(() =>
        {
            try
            {
                _gsGeneticAlgorithm.Start(); // blocks until finished or _gsGeneticAlgorithm.Stop() is called
                _logger?.LogInformation("Time Elapsed: {TimeSpan}", _gsGeneticAlgorithm.TimeEvolving);
                if (globalBestSolution is null)
                {
                    tcs.TrySetException(new InvalidOperationException("GA finished without a best chromosome."));
                    return;
                }
                
                var endFitness = globalBestSolution.Fitness ?? 0.0;
                var msgPrefix = string.IsNullOrEmpty(_config.Name) ? "" : $"{_config.Name}: ";
                var evt = new ProgressEventArgs
                {
                    Problem = problem,
                    Config = _config,
                    AlgorithmController = _algCtrl,
                    HitlController = _hitlCtrl,
                    Iteration = _gsGeneticAlgorithm.GenerationsNumber,
                    BestSolution = globalBestSolution.Clone(),
                    BestFitness = endFitness,
                    Message = $"{msgPrefix}Gen {_gsGeneticAlgorithm.GenerationsNumber} | Fitness: {Math.Abs(endFitness):F2} | Solution: {globalBestSolution}"
                };
                ProgressChanged?.Invoke(this, evt);
                _algCtrl?.SignalProgress(evt);

                tcs.TrySetResult(globalBestSolution);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception thrown while running GA.");
                tcs.TrySetException(ex);
            }
        }, linkedCts.Token);
        
        _ = tcs.Task.ContinueWith(t =>
        {
            try
            {
                if (generationHandler != null)
                {
                    _gsGeneticAlgorithm.GenerationRan -= generationHandler;
                    generationHandler = null;
                }
                linkedCts.Dispose();
                tokenReg.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed during cleanup.");
            }
            
            if (t.IsCanceled) _logger?.LogInformation("SolveAsync Task completed: Canceled.");
            else if (t.IsFaulted) _logger?.LogError(t.Exception, "SolveAsync Task completed: Faulted.");
            else _logger?.LogInformation("SolveAsync Task completed: Success.");
        }, TaskScheduler.Default);

        return tcs.Task;
    }
    
    private void SetAlgorithmOperators(bool initialize = false)
    {
        if (_config.Selection.Id != _selection1Id || _config.Selection2?.Id != _selection2Id)
        {
            var gsSelection1 = _config.Selection.ToGsSelection()
                               ?? throw new InvalidOperationException("Invalid Selection 1 mapping.");
            var gsSelection2 = _config.Selection2?.ToGsSelection();
            _selection = new HitlSelection(_hitlCtrl, gsSelection1, gsSelection2);
            _selection1Id = _config.Selection.Id;
            _selection2Id = _config.Selection2?.Id;
            if (!initialize) _gsGeneticAlgorithm.Selection = _selection;
        }
        
        if (_config.Crossover.Id != _crossoverId)
        {
            var gsCrossover = _config.Crossover.ToGsCrossover()
                              ?? throw new InvalidOperationException("Invalid Crossover mapping.");
            _crossover = new HitlCrossover(_hitlCtrl, gsCrossover);
            _crossoverId = _config.Crossover.Id;
            if (!initialize) _gsGeneticAlgorithm.Crossover = _crossover;
        }

        if (_config.Mutation.Id != _mutationId)
        {
            var gsMutation = _config.Mutation.ToGsMutation()
                             ?? throw new InvalidOperationException("Invalid Mutation mapping.");
            _mutation = new HitlMutation(_hitlCtrl, gsMutation);
            _mutationId = _config.Mutation.Id;
            if (!initialize) _gsGeneticAlgorithm.Mutation = _mutation;
        }

        if (!initialize)
        {
            _gsGeneticAlgorithm.CrossoverProbability = (float)_config.CrossoverProbability;
            _gsGeneticAlgorithm.MutationProbability = (float)_config.MutationProbability;   
        }
    }
}
