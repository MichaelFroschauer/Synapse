namespace Synapse.JobManagement;

public enum JobStatus
{
    Created,
    Queued,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed,
    Stopped,
    Removed
}
