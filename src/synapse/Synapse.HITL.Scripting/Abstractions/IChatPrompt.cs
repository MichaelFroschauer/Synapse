using Synapse.HITL.Scripting.Prompt;

namespace Synapse.HITL.Scripting.Abstractions;

public interface IChatPrompt
{
    string? Message { get; set; }
    string? UserPrompt { get; init; }
    string Name { get; }
    
    /// <summary>
    /// The solution type this prompt is applicable to.
    /// Used to filter constraints and manual edit appliers shown to the AI.
    /// Return null if no filtering is desired.
    /// </summary>
    Type? SolutionType => null;

    /// <summary>
    /// When set, overrides the default <see cref="ProblemSpecifics"/> with auto-generated content.
    /// This is typically populated by a factory that uses reflection to discover
    /// available constraints and manual edit appliers for the current solution type.
    /// </summary>
    IEnumerable<string>? GeneratedProblemSpecifics { get; set; }

    IEnumerable<string> Header =>
    [
        "SYSTEM:",
        "You are an assistant that writes safe, small C# scripts for a metaheuristic human in the loop optimization system.",
        "Constraints:",
        "- Output a fitting (max 50 character) name between markers: // BEGIN NAME and // END NAME.",
        "- Output ONLY valid C# source between markers: // BEGIN SCRIPT and // END SCRIPT.",
        "- If you can not create a script begin with the word ERROR.",
        "- The script must implement: Func<ScriptGlobals, Task>",
        "  where ScriptGlobals exposes only:",
        "    - ISolution Current                                    // current solution",
        "    - ISolution Best                                       // best solution",
        "    - int Iteration                                        // current iteration",
        "    - IHitlController HitlController                       // human in the loop controller",
        "  and IHitlController exposes only:",
        "    -  Guid AddManualEdit(ISolution s, IManualEditApplier a, double probability = 1.0, int executeForNrOfIterations = 0, string? name = null)",
        "    -  void ClearManualEdit(string name);",
        "    -  void AddConstraint(IConstraint c);",
        "    -  void RemoveConstraint(IConstraint c);",
        "    -  void AddSolutionPreference(ISolutionSimilarity similarityEvaluator, ISolution referenceSolution, double weight = 1.0, string? name = null);",
        "    -  void AddSolutionPreference(Func<ISolution, bool> matcher, double weight = 1.0, string? name = null);",
        "    -  void ClearPreference(string name);",
        "    -  void ClearPreferences();",
        "    -  void SetParameter(string key, object value);",
        "  and ISolution exposes only:",
        "    - double? Fitness                                      // get fitness of solution",
        "    - int Length                                           // get solution length (count of parameters)",
        "    - Parameter[] GetParameters()                          // get all parameters",
        "    - void SetParameters(Parameter[] parameters)           // set all parameters",
        "    - Parameter GetParameter(int index)                    // get a specific parameter",
        "    - void SetParameter(int index, Parameter parameter)    // Set a specific parameter",
        "    - ISolution CreateNew()                                // create a new random solution",
        "    - ISolution Clone()",
        "    - ISolutionSimilarity GetDefaultSolutionSimilarityClass() // gets the default solution similarity class",
        "  and Parameter exposes only:",
        "    - public struct Parameter(object value) : IEquatable<Parameter>",
        "        public object Value => this.m_value;                // object is casted to the specific problem type (int, double, ...)",
        "",
        "The script MAY NOT:",
        "  - use System.IO, System.Net, reflection, Threading that escapes the runner",
        "  - declare additional public types, P/Invoke, or spawn processes",
        "",
        "The script MUST:",
        "  - Keep runtime logic simple and deterministic. Use only operations on the provided API.",
        "",
        "Valid example:",
        "// BEGIN NAME",
        "Name of the script",
        "// END NAME",
        "// BEGIN SCRIPT",
        "System.Console.WriteLine('Valid C# code');",
        "// END SCRIPT",
        "",
        "Error example:",
        "ERROR: Invalid input was given, could not create script!"
    ];
    
    IEnumerable<string> ProblemSpecifics { get; }
    
    IEnumerable<string> Examples { get; }

    IEnumerable<string> Footer =>
    [
        "Now produce a script that implements the user preference above. Output only the script between the markers."
    ];
    
    IEnumerable<ProblemProperties> ProblemProperties { get; }
}
