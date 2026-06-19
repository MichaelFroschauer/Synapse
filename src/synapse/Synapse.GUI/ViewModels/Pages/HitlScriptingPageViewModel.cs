using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ActiproSoftware.Extensions;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.Scripting;
using Synapse.GUI.Models;
using Synapse.GUI.Services;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Random;
using Script = Synapse.OptimizationCore.HITL.Script;

namespace Synapse.GUI.ViewModels.Pages;

public partial class HitlScriptingPageViewModel : PageViewModel
{
    private readonly IJobSelectorService _jobSelector;
    private readonly IJobManager _jobManager;
    private readonly IScriptManager _scriptManager;
    
    public ObservableCollection<Script> Scripts { get; } = new();
    public ObservableCollection<ScriptEntry> ScriptsOfCurrentJob { get; } = new();


    [ObservableProperty] private string _compileErrorText = string.Empty;
    [ObservableProperty] private bool _showCompileErrorText;
    [ObservableProperty] private string _scriptName = string.Empty;
    [ObservableProperty] private bool _scriptExecuteOnce = true;
    [ObservableProperty] private string _codeEditorText = string.Empty;
    [ObservableProperty] private bool _isCompiling = false;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanCompile))] private bool _isNotCompiling = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanCompile))] private bool _isJobSelected = false;
    public bool CanCompile => IsNotCompiling && IsJobSelected;
    
    public TextDocument EditorDocument { get; set; } = new(InitialCodeEditorText);
    public TextEditorOptions TextEditorOptions { get; set; } = new()
    {
        AllowScrollBelowDocument = false,
        HighlightCurrentLine = true,
        AllowToggleOverstrikeMode = true
    };

    private const string InitialCodeEditorText = @"
async (globals) =>
{
    // Add your code here. You can use the following properties through the global variable
    /*
    public IProblem Problem { get; init; }
    public IFitnessEvaluator Evaluator { get; init; }
    public IAlgorithmController? AlgorithmController { get; init; }
    public IHitlController HitlController { get; init; }
    public ISolution Current { get; set; }
    public ISolution Best { get; init; }
    public int Iteration { get; init; }
    public IRandom Random { get; init; }
    */
}
";

    public HitlScriptingPageViewModel(IJobSelectorService jobSelector, IJobManager jobManager, IScriptManager scriptManager)
    {
        PageName = ApplicationPageNames.HitlScripting;
        
        _jobSelector = jobSelector;
        _jobManager = jobManager;
        _scriptManager = scriptManager;
        
        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        OnSelectedJobChanged(null, null);
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        IsJobSelected = _jobSelector.SelectedJobId != null;
        UpdateScripts();
    }

    private void UpdateScripts()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateScripts);
            return;
        }
        
        Scripts.Clear();
        ScriptsOfCurrentJob.Clear();
        
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        var jobScripts = jobInfo?.Config.HitlController?.GetScripts().Select(s => s.Id);
        if (jobScripts != null)
        {
            ScriptsOfCurrentJob.AddRange(jobInfo?.Config.HitlController?.GetScriptEntries() ?? []);
        }
        Scripts.AddRange(_scriptManager.GetRegisteredScripts().Where(s => !ScriptsOfCurrentJob.Select(se => se.Script.Id).Contains(s.Id)));
    }

    [RelayCommand]
    private void ExecuteScriptForJob(ScriptEntry? script)
    {
        if (script is null) return;
        
        try
        {
            var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
            if (jobInfo is null) return;
            var globals = GetScriptGlobalsOfCurrentJob(jobInfo);
            jobInfo.Config.HitlController?.AddAndExecuteScript(script.Script.Id, globals);
            UpdateScripts();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void AddScriptToCurrentJob(Script? script)
    {
        if (script is null) return;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        jobInfo?.Config.HitlController?.AddScript(script.Id);
        UpdateScripts();
    }

    [RelayCommand]
    private void RemoveScriptFromCurrentJob(ScriptEntry? script)
    {
        if (script is null) return;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        jobInfo?.Config.HitlController?.RemoveScript(script.Script.Id);
        UpdateScripts();
    }

    [RelayCommand]
    private Task CompileRegister()
    {
        return CompileAndProcessAsync(
            executeOnce: ScriptExecuteOnce,
            onCompiled: async (scriptId, jobInfo) =>
            {
                if (jobInfo is null) return;
                jobInfo.Config.HitlController?.AddScript(scriptId);
                await Task.CompletedTask;
            });
    }
    
    [RelayCommand]
    private Task CompileExecute()
    {
        return CompileAndProcessAsync(
            executeOnce: ScriptExecuteOnce,
            onCompiled: async (scriptId, jobInfo) =>
            {
                if (jobInfo is null) return;
                var globals = GetScriptGlobalsOfCurrentJob(jobInfo);
                jobInfo.Config.HitlController?.AddAndExecuteScript(scriptId, globals);
                await Task.CompletedTask;
            });
    }
    
    private async Task CompileAndProcessAsync(bool executeOnce, Func<Guid, JobInfo?, Task>? onCompiled)
    {
        CompileErrorText = string.Empty;
        IsCompiling = true;
        
        var code = EditorDocument.Text;
        var scriptName = ScriptName;

        try
        {
            var scriptId = await _scriptManager.CompileAndRegisterAsync(code, scriptName, executeOnce);
            var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
            
            if (onCompiled != null)
                await onCompiled(scriptId, jobInfo);
            
            ScriptName = string.Empty;
            ScriptExecuteOnce = true;
        }
        catch (CompilationErrorException ex)
        {
            CompileErrorText = ex.Message;
        }
        finally
        {
            IsCompiling = false;
            UpdateScripts();
        }
    }

    private ScriptGlobals GetScriptGlobalsOfCurrentJob(JobInfo jobInfo)
    {
        return new ScriptGlobals
        {
            Problem = jobInfo.Problem,
            Evaluator = jobInfo.Problem.GetFitnessEvaluator(),
            AlgorithmController = jobInfo.Config.AlgorithmController,
            HitlController = jobInfo.Config.HitlController,
            Current = null,
            Best = null,
            Iteration = jobInfo.IterationCount,
            Random = RandomProvider.Value
        };
    }

    partial void OnCompileErrorTextChanged(string value)
    {
        ShowCompileErrorText = value.Length > 0;
    }

    partial void OnIsCompilingChanged(bool value) => IsNotCompiling = !value;
    
    [RelayCommand]
    private void ClearCompileError()
    {
        CompileErrorText = string.Empty;
    }

    protected override void DisposeManaged()
    {
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        base.DisposeManaged();
    }
}