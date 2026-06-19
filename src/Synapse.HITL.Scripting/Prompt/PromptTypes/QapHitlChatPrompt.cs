using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt.PromptTypes;

public class QapHitlChatPrompt : IChatPrompt
{
    public string? Message { get; set; }
    public string? UserPrompt { get; init; }
    public string Name => "Quadratic Assignment Problem (QAP) HITL operations automation";
    public IEnumerable<string>? GeneratedProblemSpecifics { get; set; }

    public IEnumerable<string> ProblemSpecifics =>
        GeneratedProblemSpecifics ?? FallbackProblemSpecifics;
    
    private static readonly string[] FallbackProblemSpecifics =
    [
        "You can add constraints with the IHitlController. You have these constraints available:",
        "    - class QapFixAssignmentConstraint(int facility, int location) : IConstraint  // Pins a specific facility to an exact location.",
        "    - class QapForbiddenAssignmentConstraint(int facility, int location) : IConstraint  // Prevents a specific facility from being placed at a certain location.",
        "    - class QapPairwiseAssignmentConstraint(int facilityA, int facilityB, double maxDistance) : IConstraint  // Ensures two facilities are within a given maximum distance.",
        "    - class QapClusterConstraint(IEnumerable<int> facilities, IEnumerable<int> allowedLocations) : IConstraint  // Restricts a group of facilities to specific allowed locations.",
        "    - class QapForbiddenPairConstraint(int facilityA, int facilityB) : IConstraint  // Prevents two facilities from being placed at adjacent locations.",
        "",
        "You can add human edit suggestions with the IHitlController by adding solution mappings as suggestions.",
        "    - class QapManualEditApplier() : IManualEditApplier  // Allows manual editing of a QAP facility-to-location assignment.",
        "    - class Swap2ManualEditApplier() : IManualEditApplier  // Swaps two values in a permutation-based solution.",
        "    - class ReverseSegmentByValuesManualEditApplier() : IManualEditApplier  // Reverses a segment of a permutation-based solution."
    ];

    public IEnumerable<string> Examples =>
        new[]
        {
            @"# Example 1: Facility 0 must be at location 3 and facility 5 should not be at 7. Also the facilities 2,4 and 6 should be anywhere at 10, 11 and 12. Also facility 1 and 8 should be 15m apart at maximum.
// BEGIN NAME
Fix and Forbid Assignments with Constraints
// END NAME
// BEGIN SCRIPT
async (g) => {
    // facility 0 must be at location 3
    g.HitlController.AddConstraint(new QapFixAssignmentConstraint(facility: 0, location: 3));

    // facility 5 must not be at location 7
    g.HitlController.AddConstraint(new QapForbiddenAssignmentConstraint(facility: 5, location: 7));

    // facilities {2,4,6} must be placed within locations {10,11,12} (in any order)
    g.HitlController.AddConstraint(new QapClusterConstraint(facilities: new[]{2,4,6}, allowedLocations: new[]{10,11,12}));

    // make sure facility 1 and 8 are within distance 15 according to the instance D-matrix
    g.HitlController.AddConstraint(new QapPairwiseAssignmentConstraint(facilityA:1, facilityB:8, maxDistance:15.0, distanceMatrix: g.Problem is Synapse.Problems.QAP.QapProblem ? ((QapProblem)g.Problem).DistanceMatrix : null));

    await Task.CompletedTask;
}
// END SCRIPT",
            
            
            @"# Example 2: I think it would be good if the facilities are mapped to the locations like 3,0,2,5,1,4. It could also be pretty good if facility 10 is on 2 and 1 is on 7.
// BEGIN NAME
User Full and Partial Mapping Suggestions
// END NAME
// BEGIN SCRIPT
async (g) => {
    // full mapping: a complete new assignment (facility -> location for each facility)
    int[] fullMapping = new int[]{ 3, 0, 2, 5, 1, 4 }; // example for N=6 (must be a permutation)
    ISolution userSuggestionFull = new QapSolution(fullMapping);
    g.HitlController.AddManualEdit(userSuggestionFull, applier: new QapManualEditApplier(), probability: 0.7, executeForNrOfIterations: 5, name: ""user full mapping"");

    // partial mapping: only some facilities suggested (use Parameter[]; null entries mean 'no suggestion' for that facility)
    int N = ((QapProblem)g.Problem).N;
    Parameter[] partial = new Parameter[N];
    // leave all null initially; suggest facility 2 -> location 10 and facility 7 -> location 1
    partial[2] = new Parameter(10);
    partial[7] = new Parameter(1);
    ISolution userSuggestionPartial = new QapSolution(partial);
    g.HitlController.AddManualEdit(userSuggestionPartial, applier: new QapManualEditApplier(), probability: 0.9, executeForNrOfIterations: 10, name: ""user partial mapping"");

    await Task.CompletedTask;
}
// END SCRIPT"
        };

    public IEnumerable<ProblemProperties> ProblemProperties => new[] { Prompt.ProblemProperties.NeedsPermutation };
}
