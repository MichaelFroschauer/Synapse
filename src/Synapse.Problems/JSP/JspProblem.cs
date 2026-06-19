using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.JSP;

[Problem(
    Name = "JSP Problem",
    ProblemType = ProblemType.Jsp)]
public class JspProblem : IProblem
{
    private readonly Job[] _jobs;
    private readonly int _machinesCount;
    public string Name { get; }
    public string Description { get; }
    public Job[] Jobs => _jobs;
    public int MachinesCount => _machinesCount;
    public int TotalOperations { get; }
    public int NumJobs => _jobs.Length;
    public int NumMachines => _machinesCount;
    public int[] OpMachine { get; }          // length = TotalOperations
    public int[] OpProcessingTime { get; }   // length = TotalOperations
    
    public JspProblem(Job[] jobs, int machinesCount, string? name = null, string? description = null)
    {
        this._jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        this._machinesCount = machinesCount;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;

        TotalOperations = jobs.Length * machinesCount;
        OpMachine = new int[TotalOperations];
        OpProcessingTime = new int[TotalOperations];

        for (int j = 0; j < jobs.Length; ++j)
        {
            var ops = jobs[j].Operations;
            if (ops.Length != machinesCount)
                throw new ArgumentException("Each job must contain exactly NumMachines operations.");

            for (int k = 0; k < machinesCount; ++k)
            {
                int opId = j * machinesCount + k;
                OpMachine[opId] = ops[k].Machine;
                OpProcessingTime[opId] = ops[k].ProcessingTime;
            }
        }
    }
    
    public ISolution CreateRandomSolution() => new JspSolution(_jobs.Length, _machinesCount);

    public IFitnessEvaluator GetFitnessEvaluator() => new JspFitnessEvaluator(this);

    
    public string Describe() => $"JSP '{Name ?? "unnamed"}' with {_machinesCount} machines and {_jobs.Length} jobs.";

    public ProblemType ProblemType => ProblemType.Jsp;
}
