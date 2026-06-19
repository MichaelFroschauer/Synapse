namespace Synapse.JobManagement.Persistence;

public interface IJobSnapshotStore
{
    Task<string> SaveSnapshotAsync(Guid jobId, JobInfo snapshot, CancellationToken ct = default);
}