namespace Synapse.Problems.JSP;

public class Job
{
    public Operation[] Operations { get; }

    public Job(Operation[] operations)
    {
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }
}
