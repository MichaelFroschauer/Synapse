using System.Collections.Concurrent;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.Script;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.HITL;

/// <summary>
/// Default implementation of <see cref="IHitlController"/>.
/// Manages user preferences, manual edits, constraints, scripting, and interaction triggers
/// for human-in-the-loop optimization.
/// </summary>
public class HitlController : IHitlController
{
    private readonly ConcurrentDictionary<string, object> _parameters = new();
    private readonly ConcurrentDictionary<string, SolutionPreference> _preferences = new();
    private readonly ConcurrentDictionary<string, SolutionEditor> _manualEdit = new();
    private readonly List<IConstraint> _constraints = new();
    private readonly ConcurrentDictionary<Guid, ScriptEntry> _registeredScripts = new();
    
    private readonly IScriptManager _scriptManager;
    private readonly IRandom _rnd = RandomProvider.Value;
    
    public event Action? ScriptError;

    public HitlController(IScriptManager? scriptManager = null)
    {
        _scriptManager = scriptManager ?? new ScriptManager();
    }

    // ── Arbitrary Parameters ────────────────────────────────────────────

    /// <inheritdoc />
    public void SetParameter(string key, object value)
        => _parameters[key] = value;

    /// <inheritdoc />
    public bool TryGetParameter<T>(string key, out T value)
    {
        if (_parameters.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool ParametersExist() => !_parameters.IsEmpty;

    /// <summary>
    /// Gets a parameter value or returns <paramref name="defaultValue"/> if not found or wrong type.
    /// </summary>
    public T? GetParameter<T>(string key, T? defaultValue = default)
    {
        if (_parameters.TryGetValue(key, out var v) && v is T t) return t;
        return defaultValue;
    }

    // ── Solution Preferences ────────────────────────────────────────────

    /// <inheritdoc />
    public void AddSolutionPreference(Func<ISolution, bool> matcher, double weight = 1.0, string? name = null)
    {
        string n = name ?? Guid.NewGuid().ToString();
        _preferences[n] = new SolutionPreference
        {
            Name = n,
            Weight = weight,
            MatcherFunction = matcher
        };
    }
    
    /// <inheritdoc />
    public void AddSolutionPreference(ISolutionSimilarity similarityEvaluator, ISolution referenceSolution,
        double weight = 1.0, string? name = null)
    {
        string n = name ?? Guid.NewGuid().ToString();
        _preferences[n] = new SolutionPreference
        {
            Name = n,
            Weight = weight,
            HasSolutionSimilarity = true,
            SimilarityEvaluator = similarityEvaluator,
            ReferenceSolution = referenceSolution
        };
    }
    
    /// <inheritdoc />
    public void ClearPreference(string name)
        => _preferences.TryRemove(name, out _);

    /// <inheritdoc />
    public void ClearPreferences() 
        => _preferences.Clear();
    
    /// <inheritdoc />
    public double GetPreferenceFactor(ISolution s)
    {
        double factor = 1.0;
        foreach (var pref in _preferences.Values)
        {
            try
            {
                if (pref.HasSolutionSimilarity && pref.SimilarityEvaluator is not null && pref.ReferenceSolution is not null)
                {
                    factor += pref.SimilarityEvaluator.GetSimilarity(s, pref.ReferenceSolution) * pref.Weight;
                }
                else if (pref.MatcherFunction is not null && pref.MatcherFunction(s))
                {
                    factor *= pref.Weight;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{nameof(GetPreferenceFactor)} failed for preference '{pref.Name}'.", ex);
            }
        }

        return factor;
    }

    /// <inheritdoc />
    public IEnumerable<SolutionPreference> GetPreferences() => _preferences.Values.ToList();

    /// <inheritdoc />
    public bool PreferencesExist() => !_preferences.IsEmpty;

    /// <inheritdoc />
    public double GetPreferenceAdjustedFitness(ISolution solution, IFitnessEvaluator fitnessEvaluator)
    {
        const double maxMultiplier = 5.0;
        double rawFitness = solution.Fitness ?? fitnessEvaluator.Evaluate(solution);
        if (PreferencesExist())
        {
            double prefFactor = GetPreferenceFactor(solution);
            prefFactor = Math.Max(0.0, Math.Min(5.0, prefFactor)); // clamp prefFactor to 0-5

            double multiplier = Math.Max(0.0, prefFactor);
            if (multiplier > maxMultiplier) multiplier = maxMultiplier;
            if (multiplier < 1.0 / maxMultiplier) multiplier = 1.0 / maxMultiplier;
            
            return fitnessEvaluator.Minimize ? rawFitness / multiplier : rawFitness * multiplier;
        }

        return rawFitness;
    }

    private ISolutionSimilarity? _solutionSimilarity;

    private double _solutionSimilarityPreferenceWeight = 0.2;
    
    /// <inheritdoc />
    public double SolutionSimilarityPreferenceWeight
    {
        get => _solutionSimilarityPreferenceWeight;
        set => _solutionSimilarityPreferenceWeight = Math.Clamp(value, 0.0, 2.0);
    }

    /// <inheritdoc />
    public void SetSolutionSimilarity(ISolutionSimilarity similarityEvaluator)
    {
        _solutionSimilarity = similarityEvaluator;
    }

    /// <inheritdoc />
    public ISolutionSimilarity GetSolutionSimilarity(ISolution solution)
    {
        return _solutionSimilarity ??= solution.GetDefaultSolutionSimilarityClass();
    }

    // ── User Solution Choices (Ask Preference) ──────────────────────────

    /// <inheritdoc />
    public Action<IEnumerable<ISolution>, IEnumerable<ISolution>>? AskPreference { get; set; }
    
    /// <inheritdoc />
    public int AskPreferenceInterval { get; set; }
    
    private TaskCompletionSource<int>? _preferenceTcs;
    
    /// <inheritdoc />
    public void SetUserPreference(int userSelection)
    {
        _preferenceTcs?.TrySetResult(userSelection);
        _preferenceTcs = null;
    }
    
    /// <inheritdoc />
    public async Task<int> GetUserResponseAsync()
    {
        _preferenceTcs = new TaskCompletionSource<int>();
        return await _preferenceTcs.Task;
    }

    // ── Manual Edits ────────────────────────────────────────────────────

    /// <inheritdoc />
    public Guid AddManualEdit(ISolution manualSolution, IManualEditApplier applier, double probability = 1.0,
        int executeForNrOfIterations = 0, string? name = null)
    {
        if (probability is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "Must be between 0.0 and 1.0.");
        
        var id = Guid.NewGuid();
        string key = name ?? id.ToString();
        _manualEdit[key] = new SolutionEditor
        {
            Name = key,
            SolutionPrototype = manualSolution,
            EditProbability = probability,
            EditApplier = applier,
            ExecuteForNrOfIterations = executeForNrOfIterations,
            ExecutedForNrOfIterations = 0,
        };
        return id;
    }

    /// <inheritdoc />
    public void ClearManualEdit() => _manualEdit.Clear();
    
    /// <inheritdoc />
    public void ClearManualEdit(string name) => _manualEdit.TryRemove(name, out _);
    
    /// <inheritdoc />
    public ISolution? MaybeApplyManualEdit(ISolution candidate)
    {
        if (_manualEdit.IsEmpty) return candidate;
        
        var newSolution = candidate.Clone();
        foreach (var edit in _manualEdit.Values)
        {
            // Skip edits that have exceeded their iteration budget
            if (edit.ExecuteForNrOfIterations > 0 && edit.ExecutedForNrOfIterations >= edit.ExecuteForNrOfIterations)
                continue;
            
            if (_rnd.GetDouble() <= edit.EditProbability)
            {
                newSolution = edit.EditApplier.Apply(newSolution, edit.SolutionPrototype);
            }
        }

        return newSolution;
    }

    /// <inheritdoc />
    public bool ManualEditsExist() => !_manualEdit.IsEmpty;

    /// <inheritdoc />
    public SolutionEditor GetManualEdit(Guid id) => _manualEdit[id.ToString()];
    
    /// <inheritdoc />
    public IEnumerable<SolutionEditor> GetManualEdits() => _manualEdit.Values.ToList();

    // ── Constraints ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public void AddConstraint(IConstraint c)
    {
        lock (_constraints) { _constraints.Add(c); }
    }

    /// <inheritdoc />
    public void RemoveConstraint(IConstraint c)
    {
        lock (_constraints) { _constraints.Remove(c); }
    }

    /// <inheritdoc />
    public IEnumerable<IConstraint> GetConstraints()
    {
        lock (_constraints) { return _constraints.ToList(); }
    }
    
    /// <inheritdoc />
    public ISolution? EnforceConstraints(ISolution candidate, bool repairSolution = false)
    {
        var s = candidate;
        foreach (var c in GetConstraints())
        {
            try
            {
                if (c.IsSatisfied(s))
                    continue;

                if (c.RepairSolution || repairSolution)
                {
                    var repaired = c.Repair(s);
                    if (repaired is null) return null;
                    s = repaired;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null; // constraint evaluation/repair threw — reject the candidate
            }
        }

        return s;
    }

    /// <inheritdoc />
    public bool ConstraintsExist()
    {
        lock (_constraints) { return _constraints.Count > 0; }
    }

    // ── Scripting ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public void AddScript(Guid scriptId)
    {
        var registeredScript = _scriptManager.GetRegisteredScript(scriptId);
        if (registeredScript is null) return;
        
        var entry = new ScriptEntry { Script = registeredScript };
        _registeredScripts.AddOrUpdate(scriptId, entry, (_, existing) =>
        {
            entry.Executed = existing.Executed;
            entry.ExecutionCount = existing.ExecutionCount;
            return entry;
        });
    }

    /// <inheritdoc />
    public void RemoveScript(Guid scriptId) => _registeredScripts.TryRemove(scriptId, out _);
    
    /// <inheritdoc />
    public void RemoveScripts() => _registeredScripts.Clear();

    /// <inheritdoc />
    public void ExecuteScripts(ScriptGlobals globals)
    {
        var pendingEntries = _registeredScripts.Values
            .Where(s => !s.Script.ExecuteOnce || !s.Executed)
            .ToList();
        
        var scriptIds = pendingEntries.Select(s => s.Script.Id);
        _scriptManager.ExecuteSetAsync(globals, scriptIds).GetAwaiter().GetResult();
        
        foreach (var entry in pendingEntries)
        {
            entry.Executed = true;
            entry.ExecutionCount++;
        }
    }

    /// <inheritdoc />
    public void AddAndExecuteScript(Guid scriptId, ScriptGlobals globals)
    {
        AddScript(scriptId);
        var entry = _registeredScripts[scriptId];
        _scriptManager.ExecuteSetAsync(globals, [scriptId]).GetAwaiter().GetResult();
        entry.Executed = true;
        entry.ExecutionCount++;
    }
    
    /// <inheritdoc />
    public void ExecuteScripts(IProblem? problem, IFitnessEvaluator? evaluator, ISolution? current, ISolution? best,
        IAlgorithmController? algorithmController, int iteration)
    {
        // execute any registered scripts (user-injected code)
        try
        {
            if (ScriptsExist())
            {
                ExecuteScripts(new ScriptGlobals
                {
                    Problem = problem,
                    Evaluator = evaluator,
                    AlgorithmController = algorithmController,
                    HitlController = this,
                    Current = current,
                    Best = best,
                    Iteration = iteration,
                    Random = _rnd
                });
            }
        }
        catch (Exception ex)
        {
            SignalScriptError();
        }
    }

    /// <inheritdoc />
    public IEnumerable<Script> GetScripts() => _scriptManager.GetRegisteredScripts(_registeredScripts.Keys);
    
    /// <inheritdoc />
    public IEnumerable<ScriptEntry> GetScriptEntries() => _registeredScripts.Values;
    
    /// <inheritdoc />
    public void SignalScriptError() => ScriptError?.Invoke();
    
    /// <inheritdoc />
    public bool ScriptsExist() => _scriptManager.ScriptsExist();

    // ── Algorithm Lifecycle ─────────────────────────────────────────────

    /// <inheritdoc />
    public void NextGenerationStarted()
    {
        // Clear trigger reason so it doesn't show on subsequent manual pauses
        LastTriggerReason = null;
    }

    /// <inheritdoc />
    public void GenerationFinished()
    {
        _manualEdit.Values.ToList().ForEach(me => me.ExecutedForNrOfIterations++);
    }
    
    /// <inheritdoc />
    public void GenerationFinished(int iteration, double bestFitness, double currentBestFitness)
    {
        GenerationFinished();
        
        _fitnessHistory.Add(bestFitness);
        if (_fitnessHistory.Count > MaxHistorySize)
            _fitnessHistory.RemoveAt(0);
        
        if (InteractionTrigger is null) return;
        
        var context = new HitlTriggerContext
        {
            Iteration = iteration,
            BestFitness = bestFitness,
            CurrentBestFitness = currentBestFitness,
            Diversity = _diversityHistory.Count > 0 ? _diversityHistory[^1] : null,
            FitnessHistory = _fitnessHistory.AsReadOnly(),
            DiversityHistory = _diversityHistory.AsReadOnly()
        };
        
        if (InteractionTrigger.ShouldTrigger(context))
        {
            LastTriggerReason = InteractionTrigger.LastTriggerReason;
            InteractionTriggered?.Invoke(LastTriggerReason ?? "Interaction trigger fired");
        }
    }
    
    // ── Interaction Triggers ────────────────────────────────────────────
    
    private const int MaxHistorySize = 500;
    
    private readonly List<double> _fitnessHistory = new();
    private readonly List<double> _diversityHistory = new();
    
    /// <inheritdoc />
    public IInteractionTrigger? InteractionTrigger { get; set; }
    
    /// <inheritdoc />
    public string? LastTriggerReason { get; private set; }
    
    /// <inheritdoc />
    public event Action<string>? InteractionTriggered;
    
    /// <inheritdoc />
    public bool EvaluateInteractionTrigger(ProgressEventArgs progress)
    {
        if (progress.Diversity.HasValue)
        {
            _diversityHistory.Add(progress.Diversity.Value);
            if (_diversityHistory.Count > MaxHistorySize)
                _diversityHistory.RemoveAt(0);
        }
        
        return false;
    }
}
