using System.Text;
using Synapse.HITL.Scripting.Abstractions;

namespace Synapse.HITL.Scripting.Prompt;

public class PromptBuilder<TPrompt> where TPrompt : class, IChatPrompt, new()
{
    private readonly StringBuilder _sb = new();
    private readonly TPrompt _prompt;

    public PromptBuilder(string? userPrompt = null, IEnumerable<string>? generatedProblemSpecifics = null)
    {
        _prompt = new TPrompt
        {
            UserPrompt = userPrompt,
            GeneratedProblemSpecifics = generatedProblemSpecifics
        };
    }

    private void AddSystemHeader()
    {
        _prompt.Header.ToList().ForEach(l => _sb.AppendLine(l));
    }

    private void AddProblemSpecifics()
    {
        _sb.AppendLine();
        _sb.AppendLine($"NAME: {_prompt.Name}");
        
        _prompt.ProblemSpecifics.ToList().ForEach(l => _sb.AppendLine($"  - {l}"));
        
        foreach (var pp in _prompt.ProblemProperties)
        {
            switch (pp)
            {
                case ProblemProperties.NeedsPermutation:
                    _sb.AppendLine("  - IMPORTANT: This algorithm requires permutation-preserving operators (do not duplicate/remove indices).");
                    break;
                case ProblemProperties.ContinuousValues:
                    _sb.AppendLine("  - IMPORTANT: Solutions consist of continuous (double) values. Each parameter represents one dimension of the search space.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void AddUserPreferences()
    {
        if (!string.IsNullOrWhiteSpace(_prompt.UserPrompt))
        {
            _sb.AppendLine();
            _sb.AppendLine("USER:");
            // sanitize user text to avoid breaking the message structure (e.g. escape triple quotes)
            var safe = (_prompt.UserPrompt ?? string.Empty).Replace("```", "'`'`'");
            _sb.AppendLine($"User prompt text: \"{safe}\"");
        }
    }

    private void AddExamples()
    {
        if (_prompt.Examples.Any())
        {
            _sb.AppendLine();
            _sb.AppendLine("EXAMPLES:");
            foreach (var ex in _prompt.Examples)
            {
                _sb.AppendLine(ex);
                _sb.AppendLine();
            }
        }
        
        // TODO probably add default examples
    }
    
    private void AddFooter() => _prompt.Footer.ToList().ForEach(l => _sb.AppendLine(l));

    public IChatPrompt Build()
    {
        AddSystemHeader();
        AddProblemSpecifics();
        AddUserPreferences();
        AddExamples();
        AddFooter();
        _prompt.Message = _sb.ToString();
        return _prompt;
    }
}

// """
// SYSTEM:
// You are an assistant that writes safe, small C# scripts for a metaheuristic human in the loop optimization system.
// Constraints:
// - Output ONLY valid C# source between markers: // BEGIN SCRIPT and // END SCRIPT.
// - The script must implement the function:
//     Func<ScriptGlobals, Task>
//   where ScriptGlobals exposes only:
//     - ISolution Current    // current solution
//     - ISolution Best       // best solution
//     - int Iteration        // current iteration
//   and ISolution exposes only:  
//     - double? Fitness                                      // get fitness of solution
//     - int Length                                           // get solution length (count of parameters)
//     - Parameter[] GetParameters()                          // get all parameters
//     - void SetParameters(Parameter[] parameters)           // set all parameters
//     - Parameter GetParameter(int index)                    // get a specific parameter
//     - void SetParameter(int index, Parameter parameter)    // Set a specific parameter
//     - ISolution CreateNew()                                // create a new random solution
//     - ISolution Clone()
//   and Parameter exposes only:
//     - public struct Parameter(object value) : IEquatable<Parameter>
//         public object Value => this.m_value;                // object is casted to the specific problem type (int, double, ...)
//     
//   The script MAY NOT:
//     - use System.IO, System.Net, reflection, Threading that escapes the runner
//     - declare additional public types, P/Invoke, or spawn processes
//   The script must return by calling SetParameters(...) if it proposes a change.
//   Keep runtime logic simple and deterministic. Use only operations on the provided API.
//
// USER:
// User preference text: "{USER_TEXT}"
//
// EXAMPLES:
// # Example 1: prefer tours that include node 5 early
// // BEGIN SCRIPT
// async (g) => {
//     Parameter[] sol = g.Current.GetParameters();
//     // try to move node 5 to position 1 if not already
//     int idx = Array.IndexOf(sol, 5);
//     if (idx > 1)
//     {
//         var list = sol.ToList();
//         list.RemoveAt(idx);
//         list.Insert(1, 5);
//         g.Current.SetParameters(list.ToArray());
//     }
//     await Task.CompletedTask;
// }
// // END SCRIPT
//
// Now produce a script that implements the user preference above. Output only the script between the markers.
// """;