using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.JobManagement;

public interface IJobManager
{
    Guid QueueJob(IAlgorithmConfig config, IProblem problem, IProblemInstance problemInstance, bool startNow = false);
    Task StartQueuedJobAsync(Guid jobId);
    Task PauseJobAsync(Guid jobId);
    Task ResumeJobAsync(Guid jobId);
    Task StopJobAsync(Guid jobId);
    JobInfo? GetJobInfo(Guid? jobId);
    IReadOnlyCollection<JobInfo> ListJobs();
    bool JobExists(Guid? jobId);
    Task DeleteJobAsync(Guid jobId, bool force = false);
    event EventHandler<(Guid JobId, ProgressEventArgs Progress)>? JobProgress;
    event EventHandler<(Guid JobId, JobStatus Status)>? JobStatusChanged;
}
