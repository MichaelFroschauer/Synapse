using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt.PromptTypes;

public class JspHitlChatPrompt : IChatPrompt
{
    public string? Message { get; set; }
    public string? UserPrompt { get; init; }
    public string Name => "Job Shop Scheduling Problem (JSP) HITL operations automation";
    public IEnumerable<string>? GeneratedProblemSpecifics { get; set; }

    public IEnumerable<string> ProblemSpecifics =>
        GeneratedProblemSpecifics ?? FallbackProblemSpecifics;

    private static readonly string[] FallbackProblemSpecifics =
    [
        "Solutions are encoded as permutations of operation indices. Each job has NumMachines operations.",
        "An operation index is computed as: jobId * numMachines + operationIndex.",
        "",
        "You can add constraints with the IHitlController. You have these constraints available:",
        "    - class JobStartTimeConstraint(JspProblem problem, int jobId, long desiredStart, int opIdxToCheck = 0, long tolerance = 0) : IConstraint  // Ensures that a specific operation of a job starts at a desired time (within an optional tolerance).",
        "    - class JobFinishRankConstraint(JspProblem problem, int jobId, int desiredPlace, long tolerance = 0) : IConstraint  // Ensures that a job finishes at a specific rank among all jobs (e.g., 1 = finishes first).",
    ];

    public IEnumerable<string> Examples =>
    [
        """
        # Example 1: Job 0 must start at time 0 and Job 2 should finish first among all jobs.
        // BEGIN NAME
        Job 0 Start at 0, Job 2 Finishes First
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            var problem = (JspProblem)g.Problem;
            // Job 0 first operation must start at time 0
            g.HitlController.AddConstraint(new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 0, opIdxToCheck: 0, tolerance: 0));
            // Job 2 should finish first among all jobs (rank 1)
            g.HitlController.AddConstraint(new JobFinishRankConstraint(problem, jobId: 2, desiredPlace: 1));
            await Task.CompletedTask;
        }
        // END SCRIPT
        """
    ];

    public IEnumerable<ProblemProperties> ProblemProperties =>
    [
        Prompt.ProblemProperties.NeedsPermutation
    ];
}
