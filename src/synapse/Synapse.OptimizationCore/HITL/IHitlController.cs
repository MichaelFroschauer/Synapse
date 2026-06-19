using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Controller for Human-in-the-Loop (HITL) interaction with running optimization algorithms.
/// Manages user preferences, manual edits, constraints, scripting, and interaction triggers.
/// </summary>
public interface IHitlController
{
    // ── Arbitrary Parameters ────────────────────────────────────────────

    /// <summary>
    /// Sets an arbitrary parameter that algorithms can optionally read each iteration.
    /// Unknown keys are silently ignored by algorithms that do not use them.
    /// </summary>
    void SetParameter(string key, object value);
    
    /// <summary>Tries to retrieve a previously set parameter, casting to <typeparamref name="T"/>.</summary>
    bool TryGetParameter<T>(string key, out T value);
    
    /// <summary>Returns <c>true</c> when at least one parameter has been set.</summary>
    bool ParametersExist();
    
    // ── Solution Preferences ────────────────────────────────────────────

    /// <summary>Adds a preference based on a boolean matcher function.</summary>
    void AddSolutionPreference(Func<ISolution, bool> matcher, double weight = 1.0, string? name = null);
    
    /// <summary>Adds a preference based on similarity to a reference solution.</summary>
    void AddSolutionPreference(ISolutionSimilarity similarityEvaluator, ISolution referenceSolution,
        double weight = 1.0, string? name = null);
    
    /// <summary>Removes a named preference.</summary>
    void ClearPreference(string name);
    
    /// <summary>Removes all preferences.</summary>
    void ClearPreferences();
    
    /// <summary>Computes the aggregated preference factor for a solution (higher = more preferred).</summary>
    double GetPreferenceFactor(ISolution s);
    
    /// <summary>Returns all registered preferences.</summary>
    IEnumerable<SolutionPreference> GetPreferences();
    
    /// <summary>Returns <c>true</c> when at least one preference has been registered.</summary>
    bool PreferencesExist();

    /// <summary>Convenience method that applies the preference factor to a raw fitness value, returning an adjusted fitness.</summary>
    double GetPreferenceAdjustedFitness(ISolution solution, IFitnessEvaluator fitnessEvaluator);
    
    /// <summary>Sets the similarity measure used for user solution preference choices.</summary>
    void SetSolutionSimilarity(ISolutionSimilarity similarityEvaluator);
    
    /// <summary>
    /// Returns the configured similarity measure for user solution choices,
    /// falling back to the solution's default similarity class.
    /// </summary>
    ISolutionSimilarity GetSolutionSimilarity(ISolution solution);
    
    /// <summary>Weight applied when computing preference score from solution similarity (0–2).</summary>
    double SolutionSimilarityPreferenceWeight { get; set; }

    // ── User Solution Choices (Ask Preference) ──────────────────────────

    /// <summary>
    /// Callback invoked by the algorithm to present candidate solutions to the user.
    /// First parameter: candidate solutions. Second parameter: reference/best solutions.
    /// </summary>
    Action<IEnumerable<ISolution>, IEnumerable<ISolution>>? AskPreference { get; set; }
    
    /// <summary>How often (in iterations) to ask the user for a solution preference.</summary>
    int AskPreferenceInterval { get; set; }
    
    /// <summary>Records the user's selection (1-based index).</summary>
    void SetUserPreference(int userSelection);
    
    /// <summary>Awaits the user's selection asynchronously.</summary>
    Task<int> GetUserResponseAsync();

    // ── Manual Edits ────────────────────────────────────────────────────

    /// <summary>Registers a manual edit that modifies candidate solutions with a given probability.</summary>
    Guid AddManualEdit(ISolution manualSolution, IManualEditApplier applier, double probability = 1.0,
        int executeForNrOfIterations = 0, string? name = null);
    
    /// <summary>Removes a named manual edit.</summary>
    void ClearManualEdit(string name);
    
    /// <summary>Removes all manual edits.</summary>
    void ClearManualEdit();
    
    /// <summary>Probabilistically applies registered manual edits to a candidate solution.</summary>
    ISolution? MaybeApplyManualEdit(ISolution candidate);
    
    /// <summary>Returns all registered manual edits.</summary>
    IEnumerable<SolutionEditor> GetManualEdits();
    
    /// <summary>Returns a specific manual edit by its ID.</summary>
    SolutionEditor GetManualEdit(Guid id);
    
    /// <summary>Returns <c>true</c> when at least one manual edit is registered.</summary>
    bool ManualEditsExist();

    // ── Constraints ─────────────────────────────────────────────────────

    /// <summary>Adds a constraint that candidate solutions must satisfy.</summary>
    void AddConstraint(IConstraint c);
    
    /// <summary>Removes a specific constraint.</summary>
    void RemoveConstraint(IConstraint c);
    
    /// <summary>Returns all registered constraints.</summary>
    IEnumerable<IConstraint> GetConstraints();
    
    /// <summary>
    /// Checks all constraints and optionally repairs the solution.
    /// Returns the (possibly repaired) solution, or <c>null</c> if constraints cannot be satisfied.
    /// </summary>
    ISolution? EnforceConstraints(ISolution candidate, bool repairSolution = false);
    
    /// <summary>Returns <c>true</c> when at least one constraint has been registered.</summary>
    bool ConstraintsExist();

    // ── Scripting ───────────────────────────────────────────────────────

    /// <summary>Registers a script by its ID for execution during the optimization loop.</summary>
    void AddScript(Guid scriptId);
    
    /// <summary>Unregisters a script.</summary>
    void RemoveScript(Guid scriptId);
    
    /// <summary>Unregisters all scripts.</summary>
    void RemoveScripts();
    
    /// <summary>Registers and immediately executes a script with the given globals.</summary>
    void AddAndExecuteScript(Guid scriptId, ScriptGlobals globals);
    
    /// <summary>Executes all registered scripts. Must be called by the algorithm each iteration.</summary>
    void ExecuteScripts(ScriptGlobals globals);
    
    /// <summary>Convenience overload that builds <see cref="ScriptGlobals"/> from individual parameters.</summary>
    void ExecuteScripts(IProblem? problem, IFitnessEvaluator? evaluator, ISolution? current, ISolution? best,
        IAlgorithmController? algorithmController, int iteration);
    
    /// <summary>Returns all registered script definitions.</summary>
    IEnumerable<Script> GetScripts();
    
    /// <summary>Returns all registered script entries (including execution metadata).</summary>
    IEnumerable<ScriptEntry> GetScriptEntries();
    
    /// <summary>Signals that a script execution error has occurred.</summary>
    void SignalScriptError();
    
    /// <summary>Returns <c>true</c> when at least one script is registered.</summary>
    bool ScriptsExist();

    // ── Algorithm Lifecycle ─────────────────────────────────────────────

    /// <summary>Called by the algorithm at the start of each new generation/iteration.</summary>
    void NextGenerationStarted();
    
    /// <summary>Called by the algorithm at the end of each generation (bookkeeping only, no trigger evaluation).</summary>
    void GenerationFinished();
    
    /// <summary>
    /// Called by the algorithm at the end of every iteration with fitness values.
    /// Tracks fitness history and evaluates interaction triggers.
    /// This is lightweight — diversity is updated separately via <see cref="EvaluateInteractionTrigger"/>.
    /// </summary>
    void GenerationFinished(int iteration, double bestFitness, double currentBestFitness);
    
    // ── Interaction Triggers ────────────────────────────────────────────
    
    /// <summary>
    /// The active interaction trigger (may be a <see cref="CompositeTrigger"/>).
    /// <c>null</c> means manual-only (no automatic pausing).
    /// </summary>
    IInteractionTrigger? InteractionTrigger { get; set; }
    
    /// <summary>
    /// Called at <c>ProgressInterval</c> with the full <see cref="ProgressEventArgs"/>.
    /// Updates diversity history only — trigger evaluation happens every iteration
    /// in <see cref="GenerationFinished(int,double,double)"/>.
    /// </summary>
    bool EvaluateInteractionTrigger(ProgressEventArgs progress);
    
    /// <summary>
    /// Human-readable reason the last trigger fired. <c>null</c> if no trigger has fired
    /// or the reason was cleared on resume.
    /// </summary>
    string? LastTriggerReason { get; }
    
    /// <summary>
    /// Raised when an interaction trigger fires and requests a pause.
    /// The string parameter contains the human-readable reason.
    /// </summary>
    event Action<string>? InteractionTriggered;
}
