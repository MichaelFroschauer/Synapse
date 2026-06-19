using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synapse.JobManagement.Factory;
using Synapse.JobManagement.Persistence;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.JobManagement;

public class JobManager : IJobManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobManager>? _logger;
    private readonly IJobSnapshotStore? _snapshotStore;

    // Stores running job metadata
    private readonly ConcurrentDictionary<Guid, JobEntry> _jobs = new();

    private class JobEntry
    {
        public required JobInfo Info { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public Task? RunningTask { get; set; }
        public readonly Lock StartLock = new();
        // Guard to prevent multiple concurrent PauseJobAsync calls from interaction triggers
        public int TriggerPauseRequested;
    }

    public event EventHandler<(Guid JobId, ProgressEventArgs Progress)>? JobProgress;
    public event EventHandler<(Guid JobId, JobStatus Status)>? JobStatusChanged;

    public JobManager(IServiceProvider serviceProvider, ILogger<JobManager>? logger = null,
        IJobSnapshotStore? snapshotStore = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _snapshotStore = snapshotStore;
    }

    /// <summary>
    /// Creates a job and enqueues it. If startNow = true, it starts immediately.
    /// If startNow = false, it remains in Queued status until StartQueuedJobAsync is called.
    /// </summary>
    public Guid QueueJob(IAlgorithmConfig config, IProblem problem, IProblemInstance problemInstance, bool startNow = false)
    {
        var id = Guid.NewGuid();
        var info = new JobInfo { Id = id, Status = JobStatus.Created, Config = config, Problem = problem, ProblemInstance = problemInstance };
        var cts = new CancellationTokenSource();
        var entry = new JobEntry { Info = info, Cancellation = cts };
        
        // If Config has no controllers, try to resolve them from DI 
        entry.Info.Config.AlgorithmController ??= _serviceProvider.GetService<IAlgorithmController>();
        entry.Info.Config.HitlController ??= _serviceProvider.GetService<IHitlController>();
        
        if (!_jobs.TryAdd(id, entry)) throw new InvalidOperationException("Could not add job.");
        SetAndPublishStatus(info, JobStatus.Queued);
        if (startNow)
        {
            _ = StartQueuedJobAsync(id);
        }

        return id;
    }

    /// <summary>
    /// Starts a previously enqueued job (if not already started).
    /// </summary>
    public async Task StartQueuedJobAsync(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var entry)) throw new KeyNotFoundException($"Job {jobId} not found.");

        // prevent the same job from being started twice
        lock (entry.StartLock)
        {
            if (entry.Info.Status == JobStatus.Running || entry.Info.Status == JobStatus.Completed ||
                entry.Info.Status == JobStatus.Cancelled || entry.Info.Status == JobStatus.Failed ||
                entry.Info.Status == JobStatus.Stopped)
            {
                throw new InvalidOperationException($"Job {jobId} cannot be started in status {entry.Info.Status}.");
            }

            // Set status to Running and start background task
            entry.Info.StartedAt = DateTimeOffset.UtcNow;
            SetAndPublishStatus(entry.Info, JobStatus.Running);
            
            // If Config has no controllers, try to resolve them from DI 
            entry.Info.Config.AlgorithmController ??= _serviceProvider.GetService<IAlgorithmController>();
            entry.Info.Config.HitlController ??= _serviceProvider.GetService<IHitlController>();
            entry.RunningTask = Task.Run(() => RunJobAsync(entry), entry.Cancellation.Token);
        }

        await entry.RunningTask;
    }

    /// <summary>
    /// Core logic for executing a job.
    /// </summary>
    private async Task RunJobAsync(JobEntry entry)
    {
        var id = entry.Info.Id;
        var info = entry.Info;
        var cts = entry.Cancellation;
        
        // Declare outside try so we can unsubscribe in finally
        var hitlController = info.Config.HitlController;
        Action<string>? triggerHandler = null;
        
        try
        {
            // Resolve algorithm factory if you have one, otherwise attempt to resolve IMetaheuristic by name/type
            using var scope = _serviceProvider.CreateScope();
            IMetaheuristic algorithm = MetaheuristicAlgorithmFactory.Create(info.Config, scope.ServiceProvider);

            // Subscribe to interaction trigger events - triggers are evaluated every iteration
            // inside HitlController.GenerationFinished(). When a trigger fires, pause the job
            // via PauseJobAsync which properly sets JobStatus and notifies the GUI.
            if (hitlController is not null)
            {
                triggerHandler = reason =>
                {
                    if (info.Status != JobStatus.Running) return;
                    // Ensure only one PauseJobAsync is in flight at a time
                    if (Interlocked.CompareExchange(ref entry.TriggerPauseRequested, 1, 0) != 0) return;
                    _logger?.LogInformation(
                        "Interaction trigger fired for job {JobId}: {Reason}", id, reason);
                    _ = PauseJobAsync(id);
                };
                hitlController.InteractionTriggered += triggerHandler;
            }

            // Subscribe to AlgorithmController progress events (preferred) and re-publish
            algorithm.ProgressChanged += (s, progressArgs) =>
            {
                info.BestFitness = progressArgs.BestFitness;
                info.BestSolution = progressArgs.BestSolution;
                info.CurrentSolution = progressArgs.CurrentBestSolution;
                info.LastMessage = progressArgs.Message;
                info.IterationCount = progressArgs.Iteration;
                
                info.FitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.BestFitness)));
                info.RawFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.BestRawFitness)));
                info.PopulationFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.CurrentBestFitness)));
                info.PopulationRawFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.CurrentBestRawFitness)));
                
                if (progressArgs.MeanFitness.HasValue)  info.MeanFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.MeanFitness.Value)));
                if (progressArgs.MedianFitness.HasValue) info.MedianFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.MedianFitness.Value)));
                if (progressArgs.WorstFitness.HasValue) info.WorstFitnessPerIteration.Add(new IterationFitness(progressArgs.Iteration, Math.Abs(progressArgs.WorstFitness.Value)));
                if (progressArgs.PopulationSize.HasValue) info.PopulationSizePerIteration.Add(new IterationPopulationSize(progressArgs.Iteration, Math.Abs(progressArgs.PopulationSize.Value)));
                if (progressArgs.Diversity.HasValue) info.DiversityPerIteration.Add(new IterationDiversity(progressArgs.Iteration, progressArgs.Diversity.Value));

                PublishProgress(id, progressArgs);
                
                if (progressArgs.Iteration % (progressArgs.Config?.ProgressInterval ?? 100) == 0)
                {
                    //_ = SaveSnapshotIfConfigured(id, info);
                }
                
                if (progressArgs.Iteration == progressArgs.Config?.MaxIterations)
                {
                    _logger?.LogInformation("Job {JobId} reached max iterations ({MaxIterations}) - Saving Job Snapshot.", id, progressArgs.Config.MaxIterations);
                    _ = SaveSnapshotIfConfigured(id, info);
                }
                
                // Update diversity history in HitlController (called at ProgressInterval).
                // Trigger evaluation itself happens every iteration via GenerationFinished().
                try
                {
                    progressArgs.HitlController?.EvaluateInteractionTrigger(progressArgs);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Diversity history update failed for job {JobId}.", id);
                }
            };
            
            // Start solving the algorithm
            info.LastMessage = "Running";
            algorithm.SetConfig(info.Config);
            var result = await algorithm.SolveAsync(info.Problem, cts.Token);

            // On completion
            info.BestSolution = result;
            info.BestFitness = result.Fitness;
            info.FinishedAt = DateTimeOffset.UtcNow;
            SetAndPublishStatus(info, JobStatus.Completed);
            _logger?.LogInformation("Job {JobId} completed.", id);
        }
        catch (OperationCanceledException)
        {
            info.FinishedAt = DateTimeOffset.UtcNow;
            SetAndPublishStatus(info, JobStatus.Cancelled);
            _logger?.LogInformation("Job {JobId} was cancelled.", id);
        }
        catch (Exception ex)
        {
            info.FinishedAt = DateTimeOffset.UtcNow;
            info.LastMessage = ex.Message;
            SetAndPublishStatus(info, JobStatus.Failed);
            _logger?.LogError(ex, "Job {JobId} failed.", id);
        }
        finally
        {
            // Unsubscribe from trigger events
            if (hitlController is not null && triggerHandler is not null)
                hitlController.InteractionTriggered -= triggerHandler;
            
            // on finish keep JobInfo in dictionary (so UI can query finished jobs) but we might remove running task reference
            entry.RunningTask = null;
            // final snapshot
            //_ = SaveSnapshotIfConfigured(id, entry.Info);
        }
    }

    public bool JobExists(Guid? jobId) => jobId is not null && _jobs.ContainsKey(jobId.Value);

    public async Task PauseJobAsync(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var entry))
        {
            var ctrl = entry.Info.Config.AlgorithmController;
            if (ctrl != null) await ctrl.RequestPauseAsync();
            SetAndPublishStatus(entry.Info, JobStatus.Paused);
            await Task.CompletedTask;
        }
        else
        {
            throw new KeyNotFoundException($"Job {jobId} not found.");
        }
    }

    public async Task ResumeJobAsync(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var entry))
        {
            if (entry.Info.Status == JobStatus.Paused)
            {
                // Clear any stale trigger reason so it doesn't show on the next manual pause
                entry.Info.Config.HitlController?.NextGenerationStarted();
                
                // Reset the trigger pause guard so triggers can fire again
                Interlocked.Exchange(ref entry.TriggerPauseRequested, 0);
                
                entry.Info.Config.AlgorithmController?.Resume();
                SetAndPublishStatus(entry.Info, JobStatus.Running);
            }

            await Task.CompletedTask;
        }
        else
        {
            throw new KeyNotFoundException($"Job {jobId} not found.");
        }
    }

    public async Task StopJobAsync(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var entry))
        {
            if (entry.Info.Status == JobStatus.Completed ||
                entry.Info.Status == JobStatus.Cancelled ||
                entry.Info.Status == JobStatus.Failed) return;

            // cancel token -> algorithm should observe and stop
            try
            {
                await entry.Cancellation.CancelAsync();
            }
            catch { /* ignore */ }

            // wait for running task to finish
            if (entry.RunningTask != null)
            {
                try
                {
                    await entry.RunningTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch { /* ignore */ }
            }

            entry.Info.Status = JobStatus.Stopped;
            SetAndPublishStatus(entry.Info, JobStatus.Stopped);
        }
        else
        {
            throw new KeyNotFoundException($"Job {jobId} not found.");
        }
    }

    public JobInfo? GetJobInfo(Guid? jobId)
    {
        if (jobId is null) return null;
        return _jobs.TryGetValue(jobId.Value, out var entry) ? entry.Info : null;
    }

    public IReadOnlyCollection<JobInfo> ListJobs()
    {
        return _jobs.Values.Select(j => j.Info).ToList();
    }
    
    public async Task DeleteJobAsync(Guid jobId, bool force = false)
    {
        if (!_jobs.TryGetValue(jobId, out var entry))
            throw new KeyNotFoundException($"Job {jobId} not found.");
        
        if (entry.Info.Status == JobStatus.Running)
        {
            if (!force)
                throw new InvalidOperationException("Job is running. Use force=true to cancel and delete.");

            try
            {
                await entry.Cancellation.CancelAsync();
                if (entry.RunningTask != null)
                {
                    await entry.RunningTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
            } 
            catch { /* ignore */ }
        }
        
        if (_jobs.TryRemove(jobId, out var removed))
        {
            SetAndPublishStatus(removed.Info, JobStatus.Removed);
            _logger?.LogInformation("Job {JobId} deleted (force={Force}).", jobId, force);
            removed.Cancellation.Dispose();
            removed.RunningTask?.Dispose();
        }
        else
        {
            throw new InvalidOperationException($"Failed to remove job {jobId} from internal store.");
        }
    }

    public void Dispose()
    {
        foreach (var e in _jobs.Values)
        {
            try
            {
                e.Cancellation.Cancel();
                e.Cancellation.Dispose();
                e.RunningTask?.Dispose();
            }
            catch { /* ignored */ }
        }
    }

    private void PublishProgress(Guid jobId, ProgressEventArgs progress)
    {
        try
        {
            JobProgress?.Invoke(this, (jobId, progress));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Exception while publishing progress for {JobId}", jobId);
        }
    }

    private void SetAndPublishStatus(JobInfo jobInfo, JobStatus status)
    {
        try
        {
            jobInfo.Status = status;
            JobStatusChanged?.Invoke(this, (jobInfo.Id, status));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Exception while publishing status for {JobId}", jobInfo.Id);
        }
    }
    
    private async Task SaveSnapshotIfConfigured(Guid jobId, JobInfo info)
    {
        if (_snapshotStore == null) return;
        try
        {
            var filePath = await _snapshotStore.SaveSnapshotAsync(jobId, info);
            if (!string.IsNullOrEmpty(filePath)) _logger?.LogInformation("Snapshot saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save snapshot for job {JobId}", jobId);
        }
    }
}