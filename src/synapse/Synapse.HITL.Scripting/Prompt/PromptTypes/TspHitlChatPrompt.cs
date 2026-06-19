using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt.PromptTypes;

public class TspHitlChatPrompt : IChatPrompt
{
    public string? Message { get; set; }
    public string? UserPrompt { get; init; }
    public string Name => "Traveling Sales Man Problem HITL operations automation";
    public IEnumerable<string>? GeneratedProblemSpecifics { get; set; }
    
    public IEnumerable<string> ProblemSpecifics =>
        GeneratedProblemSpecifics ?? FallbackProblemSpecifics;
    
    private static readonly string[] FallbackProblemSpecifics =
    [
        "You can add constraints with the IHitlController. You have these constraints available:",
        "    - class TspStartPositionConstraint(int start) : IConstraint  // Forces the tour to begin at a specific city.",
        "    - class TspPrecedenceConstraint(int reference, int mustComeAfter) : IConstraint  // Ensures that a specific city is visited after another city in the tour.",
        "    - class TspClusterConstraint(IEnumerable<int> clusterCities) : IConstraint  // Enforces that the specified cities appear consecutively as a single block in the TSP tour.",
        "    - class TspFixedEdgeConstraint(IEnumerable<(int, int)> mustHaveEdges) : IConstraint  // Requires that one or more specific edges must appear in the tour.",
        "    - class TspForbiddenEdgeConstraint(int node1, int node2) : IConstraint  // Prevents specific edges from appearing in the tour.",
        "",
        "You can add human edit suggestions with the IHitlController this works by adding solution segments as suggestions.",
        "    - class TspPermutationSequenceManualEditApplier() : IManualEditApplier  // Allows manual editing of a TSP tour by providing a new city sequence.",
        "    - class Swap2ManualEditApplier() : IManualEditApplier  // Swaps two values in a permutation-based solution.",
        "    - class ReverseSegmentByValuesManualEditApplier() : IManualEditApplier  // Reverses a segment of a permutation-based solution."
    ];

    public IEnumerable<string> Examples =>
    [
        """
        # Example 1: it must be started at city 1 and city 10 must be visited anytime after city 17. City 11 must come after city 17.
        // BEGIN NAME
        Start at City 1, 17 Before 10 and 11
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            g.HitlController.AddConstraint(new TspStartPositionConstraint(start: 1));   // tsp must start at city 1
            g.HitlController.AddConstraint(new TspPrecedenceConstraint(reference: 17, mustComeAfter: 10));  // tsp city 10 must come after city 17 at some point (there may be other cities in between)
            g.HitlController.AddConstraint(new TspFixedEdgeConstraint(mustHaveEdges: [(17,11)])); // tsp city 11 must come after city 17
            await Task.CompletedTask;
        }
        // END SCRIPT
        """,
        
        """
        # Example 2: The user suggests a route that may be better than the current one. The probability (0.0–1.0) is selected based on how confident the user is an on the type of the operation.
        // BEGIN NAME
        User-Suggested TSP Routes with Confidence
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            // Insert permutation sequence
            ISolution userSuggestion1 = new TspSolution(new int[]{ 30,20,16,6,1,41 });
            g.HitlController.AddManualEdit(userSuggestion1, applier: new TspPermutationSequenceManualEditApplier(), probability: 0.1, executeForNrOfIterations: 20, name: "user suggestion 1");
            
            // Reverse permutation segment between city 7 and 5
            ISolution userSuggestion2 = new TspSolution(new int[]{ 7,5 });
            g.HitlController.AddManualEdit(userSuggestion2, applier: new ReverseSegmentByValuesManualEditApplier(), probability: 0.9, executeForNrOfIterations: 10, name: "user suggestion 2");
            await Task.CompletedTask;
        }
        // END SCRIPT
        """,
        
        """
        # Example 3: The user has a preference against a solution with sequence "21, 48, 38"
        // BEGIN NAME
        User-Suggested partially TSP Solution
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            var solSim = g.Best.GetDefaultSolutionSimilarityClass();
            var refSol = new TspSolution([21, 48, 38]);
            g.HitlController.AddSolutionPreference(solSim, refSol, weight: 0.2);
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
