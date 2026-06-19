namespace Synapse.Problems.JSP;

public class Operation
{
    public int Machine { get; }
    public int ProcessingTime { get; }

    public Operation(int machine, int processingTime)
    {
        Machine = machine;
        ProcessingTime = processingTime;
    }
}
