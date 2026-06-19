namespace Synapse.JobManagement.Persistence;

public interface IJobSnapshotStore
{
    Task SaveSnapshotAsync(Guid jobId, JobInfo snapshot, CancellationToken ct = default);
}