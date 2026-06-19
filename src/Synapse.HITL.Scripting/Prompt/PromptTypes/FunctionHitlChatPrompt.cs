using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt.PromptTypes;

public class FunctionHitlChatPrompt : IChatPrompt
{
    public string? Message { get; set; }
    public string? UserPrompt { get; init; }
    public string Name => "Continuous Function Optimization HITL operations automation";
    public IEnumerable<string>? GeneratedProblemSpecifics { get; set; }

    public IEnumerable<string> ProblemSpecifics =>
        GeneratedProblemSpecifics ?? FallbackProblemSpecifics;

    private static readonly string[] FallbackProblemSpecifics =
    [
        "Solutions are real-valued vectors. Each Parameter.Value is a double representing one dimension.",
        "The solution length equals the number of dimensions of the function being optimized.",
        "",
        "There are currently no problem-specific constraints for function optimization.",
        "However, you can use the IHitlController to influence the search:",
        "    - Use SetParameter(key, value) to pass custom parameters to the algorithm.",
        "    - Use AddSolutionPreference(...) to bias the search towards specific regions.",
        "",
        "To suggest a specific solution vector, create a FunctionSolution with the desired values:",
        "    new FunctionSolution(new double[]{ 0.5, 0.3, ... })"
    ];

    public IEnumerable<string> Examples =>
    [
        """
        # Example 1: The user thinks the optimum is near a specific point and wants to bias the search.
        // BEGIN NAME
        Bias Search Towards Region
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            // Create a reference solution near where the user thinks the optimum is
            ISolution reference = new FunctionSolution(new double[]{ 0.5, 0.5, 0.5 });
            g.HitlController.AddSolutionPreference(
                new RealValueSolutionEuclideanSimilarity(),
                reference,
                weight: 2.0,
                name: "user region bias"
            );
            await Task.CompletedTask;
        }
        // END SCRIPT
        """,

        """
        # Example 2: The user wants to directly set a candidate solution for evaluation.
        // BEGIN NAME
        Inject User Solution Candidate
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            // Overwrite the current solution with specific values
            double[] values = new double[]{ 0.1, 0.9, 0.45 };
            for (int i = 0; i < Math.Min(values.Length, g.Current.Length); i++)
            {
                g.Current.SetParameter(i, new Parameter(values[i]));
            }
            g.Current.Fitness = null; // reset fitness so it gets re-evaluated
            await Task.CompletedTask;
        }
        // END SCRIPT
        """
    ];

    public IEnumerable<ProblemProperties> ProblemProperties =>
    [
        Prompt.ProblemProperties.ContinuousValues
    ];
}
