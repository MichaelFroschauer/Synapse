using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt.PromptTypes;


public class TspChatPrompt : IChatPrompt
{
    public string? Message { get; set; }
    public string? UserPrompt { get; init; }
    public string Name => "Traveling Sales Man Problem direct solution manipulator";
    public IEnumerable<string>? GeneratedProblemSpecifics { get; set; }

    public IEnumerable<string> ProblemSpecifics =>
        GeneratedProblemSpecifics ??
    [
        "IMPORTANT: Solutions are permutations of node indices. Do not change element types.",
        "Representation: Parameter.Value is an int representing node id."
    ];

    public IEnumerable<string> Examples =>
    [
        """
        # Example 1: prefer tours that include node 5 early
        // BEGIN NAME
        City 5 preferably early in the tour
        // END NAME
        // BEGIN SCRIPT
        async (g) => {
            Parameter[] sol = g.Current.GetParameters();
            // find index of node 5 (Parameter.Value is boxed int)
            int idx = Array.FindIndex(sol, p => (int)p.Value == 5);
            if (idx > 1)
            {
                var list = sol.ToList();
                var val = list[idx];
                list.RemoveAt(idx);
                list.Insert(1, val);
                g.Current.SetParameters(list.ToArray());
            }
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
