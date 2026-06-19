using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.Scripting;
using Synapse.GUI.Models;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.Prompt;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.Reflection;

namespace Synapse.GUI.ViewModels.Pages;

public partial class AiConfiguratorPageViewModel : PageViewModel
{
    private readonly IProblemResultViewModelService _problemResultViewModelService;
    private readonly IScriptProvider _scriptProvider;
    private readonly IJobSelectorService _jobSelector;
    private readonly IJobManager _jobManager;
    private readonly IScriptManager _scriptManager;

    // ── Solution view ───────────────────────────────────────────────────
    [ObservableProperty] private ProblemResultViewModel? _currentProblemResults;
    [ObservableProperty] private ProblemResultViewModel? _bestProblemResults;

    // ── Job context info ────────────────────────────────────────────────
    [ObservableProperty] private string _algorithmName = "—";
    [ObservableProperty] private string _problemName = "—";
    [ObservableProperty] private string _jobStatus = "—";
    [ObservableProperty] private double? _bestFitness;
    [ObservableProperty] private int _iteration;
    [ObservableProperty] private int _aiScriptCount;

    // ── Chat / prompt state ─────────────────────────────────────────────
    [ObservableProperty] private string _chatPrompt = "";
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = "";
    
    // ── AI models ───────────────────────────────────────────────────────
    [ObservableProperty] private string _currentAiModel = "openai/gpt-4o";
    [ObservableProperty] private int _aiTokens = 10000;
    [ObservableProperty] private double _aiTemperature = 0.05;
    public ObservableCollection<string> AvailableAiModels
        => [ "openai/gpt-5.2-codex", "openai/gpt-5-mini", "openai/gpt-4o", "openai/gpt-3.5-turbo",
             "anthropic/claude-opus-4.5", "google/gemini-3.1-pro-preview" ];
    
    public bool CanSend => !IsGenerating && !string.IsNullOrWhiteSpace(ChatPrompt);
    public bool NoAiScripts => AiScripts.Count == 0;
    public bool HasOpenRouterKey
    {
        get
        {
            HasError = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
            if (HasError)
            {
                ErrorMessage = "OPENROUTER_API_KEY environment variable is not set. AI features will be unavailable.";
            }
            return !HasError;
        }
    }

    /// <summary>AI-generated scripts only (not manually created scripts).</summary>
    public ObservableCollection<ExpanderItem> AiScripts { get; } = new();
    
    private EventHandler<(Guid, ProgressEventArgs)>? _progressHandler;
    private EventHandler<(Guid, JobStatus)>? _statusHandler;

    private const string AiScriptConfigPrefix = "// SYNAPSE-AI-CONFIG:";

    public AiConfiguratorPageViewModel(
        IProblemResultViewModelService problemResultViewModelService,
        IScriptProvider scriptProvider,
        IJobSelectorService jobSelector,
        IJobManager jobManager,
        IScriptManager scriptManager)
    {
        PageName = ApplicationPageNames.AiConfigurator;

        _problemResultViewModelService = problemResultViewModelService;
        _scriptProvider = scriptProvider;
        _jobSelector = jobSelector;
        _jobManager = jobManager;
        _scriptManager = scriptManager;

        _problemResultViewModelService.CurrentProblemTypeChanged += OnCurrentProblemResultChanged;
        
        // Ensure the problem type is set from the current job so visualizations
        // (e.g. TSP cities) are available even before the algorithm starts.
        var currentJob = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (currentJob is not null)
            _problemResultViewModelService.SetProblemType(currentJob.Problem.ProblemType);

        CurrentProblemResults = _problemResultViewModelService.CurrentProblemResultViewModel;
        BestProblemResults = _problemResultViewModelService.BestProblemResultViewModel;
        if (CurrentProblemResults is not null) CurrentProblemResults.ShowCurrentNonOptimalResults = true;

        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        
        _progressHandler = (_, e) =>
        {
            if (e.Item1 != _jobSelector.SelectedJobId) return;
            Dispatcher.UIThread.Post(() => RefreshJobContext());
        };
        _jobManager.JobProgress += _progressHandler;
        
        // When a job starts/pauses/stops, refresh the solution views so the user
        // doesn't have to navigate away and back.
        _statusHandler = (_, e) =>
        {
            if (e.Item1 != _jobSelector.SelectedJobId) return;
            Dispatcher.UIThread.Post(() =>
            {
                RefreshJobContext();
                RefreshSolutionViews();
            });
        };
        _jobManager.JobStatusChanged += _statusHandler;
        
        OnSelectedJobChanged(null, null);
    }

    // ── Property change hooks ───────────────────────────────────────────

    partial void OnChatPromptChanged(string value) => OnPropertyChanged(nameof(CanSend));
    partial void OnIsGeneratingChanged(bool value) => OnPropertyChanged(nameof(CanSend));
    partial void OnAiScriptCountChanged(int value) => OnPropertyChanged(nameof(NoAiScripts));

    // ── Event handlers ──────────────────────────────────────────────────

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (jobInfo is not null)
        {
            // Set the problem type so the correct visualization VM is created
            _problemResultViewModelService.SetProblemType(jobInfo.Problem.ProblemType);
        }
        
        RefreshJobContext();
        RefreshSolutionViews();
        LoadAiScripts();
    }

    private void OnCurrentProblemResultChanged()
    {
        RefreshSolutionViews();
    }

    /// <summary>
    /// Re-fetches the solution view models from the service and assigns them.
    /// This ensures the view always shows the latest VM instance after problem type
    /// changes, job selection changes, or algorithm start/stop events.
    /// </summary>
    private void RefreshSolutionViews()
    {
        CurrentProblemResults = _problemResultViewModelService.CurrentProblemResultViewModel;
        BestProblemResults = _problemResultViewModelService.BestProblemResultViewModel;
        if (CurrentProblemResults is not null) CurrentProblemResults.ShowCurrentNonOptimalResults = true;
    }

    private void RefreshJobContext()
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (jobInfo is null)
        {
            AlgorithmName = "—";
            ProblemName = "—";
            JobStatus = "—";
            BestFitness = null;
            Iteration = 0;
            return;
        }

        AlgorithmName = jobInfo.Config.AlgorithmType.ToString();
        ProblemName = jobInfo.Problem.ProblemType.ToString();
        JobStatus = jobInfo.Status.ToString();
        BestFitness = jobInfo.BestFitness.HasValue ? Math.Abs(jobInfo.BestFitness.Value) : null;
        Iteration = jobInfo.IterationCount;
    }

    /// <summary>
    /// Loads only AI-generated scripts that are attached to the current job's HITL controller.
    /// Scripts are identified by being registered in the ScriptManager AND attached to the job.
    /// </summary>
    private void LoadAiScripts()
    {
        AiScripts.Clear();
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        var registeredScripts = jobInfo?.Config.HitlController?.GetScripts().ToList();
        if (registeredScripts is null) return;

        foreach (var script in registeredScripts.Where(s => IsAiGeneratedScriptText(s.ScriptText)))
        {
            var display = ParseAiScriptText(script.ScriptText);
            AiScripts.Add(new ExpanderItem(script.Name, display.Code, display.ConfigSummary));
        }

        AiScriptCount = AiScripts.Count;
    }

    private sealed record AiScriptDisplayData(string Code, string ConfigSummary);

    private static string BuildAiScriptTextWithConfig(string code, string model, int tokens, double temperature)
    {
        var meta = $"model={model};tokens={tokens};temperature={temperature.ToString("0.00", CultureInfo.InvariantCulture)}";
        return $"{AiScriptConfigPrefix} {meta}{Environment.NewLine}{code}";
    }

    private static bool IsAiGeneratedScriptText(string? scriptText)
    {
        if (string.IsNullOrWhiteSpace(scriptText)) return false;

        var firstNewline = scriptText.IndexOf('\n');
        var firstLine = firstNewline >= 0 ? scriptText[..firstNewline].TrimEnd('\r') : scriptText.Trim();
        return firstLine.StartsWith(AiScriptConfigPrefix, StringComparison.Ordinal);
    }

    private static AiScriptDisplayData ParseAiScriptText(string scriptText)
    {
        if (string.IsNullOrWhiteSpace(scriptText))
            return new AiScriptDisplayData(string.Empty, "Generation config: n/a");

        var firstNewline = scriptText.IndexOf('\n');
        var firstLine = firstNewline >= 0 ? scriptText[..firstNewline].TrimEnd('\r') : scriptText.Trim();

        if (!firstLine.StartsWith(AiScriptConfigPrefix, StringComparison.Ordinal))
            return new AiScriptDisplayData(scriptText, "Generation config: unknown (legacy script)");

        var metaPart = firstLine.Substring(AiScriptConfigPrefix.Length).Trim();
        var values = ParseMetaValues(metaPart);

        var code = firstNewline >= 0 ? scriptText[(firstNewline + 1)..] : string.Empty;
        var model = values.TryGetValue("model", out var modelValue) ? modelValue : "unknown";
        var tokens = values.TryGetValue("tokens", out var tokensValue) ? tokensValue : "unknown";
        var temp = values.TryGetValue("temperature", out var tempValue) ? tempValue : "unknown";
        var summary = $"Model: {model} | Tokens: {tokens} | Temperature: {temp}";

        return new AiScriptDisplayData(code, summary);
    }

    private static Dictionary<string, string> ParseMetaValues(string meta)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pairs = meta.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0 || idx >= pair.Length - 1) continue;

            var key = pair[..idx].Trim();
            var value = pair[(idx + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }

    // ── Send prompt ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SendPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatPrompt)) return;

        HasError = false;
        ErrorMessage = "";
        IsGenerating = true;

        var userPrompt = ChatPrompt;
        ChatPrompt = "";

        try
        {
            var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
            var promptType = ResolvePromptType(jobInfo);

            IEnumerable<string>? generatedSpecifics = null;
            if (jobInfo is not null)
            {
                try
                {
                    var solutionType = jobInfo.Problem.CreateRandomSolution().GetType();
                    generatedSpecifics = HitlPromptGenerator.GenerateProblemSpecifics(solutionType);
                }
                catch
                {
                    // Fall back to hardcoded specifics if reflection fails
                }
            }

            var prompt = ChatPromptFactory.Get(promptType, userPrompt, generatedSpecifics);
            var result = await _scriptProvider.GenerateScriptAsync(prompt, CurrentAiModel, AiTokens, AiTemperature);
            if (result.ErrorGeneratingCode)
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "AI returned an error without details.";
                return;
            }

            if (result.Name is null || result.Code is null)
            {
                HasError = true;
                ErrorMessage = "AI returned an empty response.";
                return;
            }

            var scriptId = await _scriptManager.CompileAndRegisterAsync(result.Code, result.Name, true);
            var persistedScriptText = BuildAiScriptTextWithConfig(result.Code, CurrentAiModel, AiTokens, AiTemperature);
            var registeredScript = _scriptManager.GetRegisteredScript(scriptId);
            if (registeredScript is not null)
                registeredScript.ScriptText = persistedScriptText;

            var currentJobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
            currentJobInfo?.Config.HitlController?.AddScript(scriptId);

            var display = ParseAiScriptText(persistedScriptText);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiScripts.Add(new ExpanderItem(result.Name, display.Code, display.ConfigSummary));
                AiScriptCount = AiScripts.Count;
            });
        }
        catch (CompilationErrorException ex)
        {
            HasError = true;
            ErrorMessage = $"Script compilation failed:\n{string.Join("\n", ex.Diagnostics)}";
            Debug.WriteLine($"[AI] Compilation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to generate script: {ex.Message}";
            Debug.WriteLine($"[AI] Unexpected error: {ex}");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// Determines the appropriate prompt type based on the current job's problem type.
    /// </summary>
    private static PromptType ResolvePromptType(JobInfo? jobInfo)
    {
        if (jobInfo is null) return PromptType.HitlTsp;

        return jobInfo.Problem.ProblemType switch
        {
            ProblemType.Tsp => PromptType.HitlTsp,
            ProblemType.Qap => PromptType.HitlQap,
            ProblemType.Jsp => PromptType.HitlJsp,
            ProblemType.Function => PromptType.HitlFunction,
            _ => PromptType.HitlTsp
        };
    }

    protected override void DisposeManaged()
    {
        _problemResultViewModelService.CurrentProblemTypeChanged -= OnCurrentProblemResultChanged;
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        if (_progressHandler is not null)
            _jobManager.JobProgress -= _progressHandler;
        if (_statusHandler is not null)
            _jobManager.JobStatusChanged -= _statusHandler;
        base.DisposeManaged();
    }
}
