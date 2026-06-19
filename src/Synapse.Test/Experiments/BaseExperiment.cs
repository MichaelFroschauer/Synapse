using Synapse.JobManagement;
using Synapse.OptimizationCore.HITL;

namespace Synapse.Test.Experiments;

public abstract class BaseExperiment : IExperiment
{
    protected readonly IJobManager JobManager;
    protected readonly HitlInterventionRunner InterventionRunner;

    public BaseExperiment(IJobManager jobManager)
    {
        JobManager = jobManager;
        InterventionRunner = new HitlInterventionRunner(JobManager);
    }
    
    public virtual void Setup()
    {
        
    }
    
    public abstract Task RunAsync();

    protected static string ResolveProblemInstancesBasePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        var envPath = Environment.GetEnvironmentVariable("SYNAPSE_PROBLEM_INSTANCES_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
            return Path.GetFullPath(envPath);

        // Walk upwards from the app base directory until we find the repo folder containing ProblemInstances.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "ProblemInstances");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        // Last fallback for local dev/test runs started from repo root.
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ProblemInstances"));
    }
}

public static class GuidExtensions
{
    public static IHitlController HitlController(this Guid guid, IJobManager jobManager)
    {
        return jobManager.GetJobInfo(guid)?.Config.HitlController
               ?? throw new InvalidOperationException($"Job with ID {guid} not found or has no HITL controller.");   
    }
}