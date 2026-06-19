using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Synapse.GUI.Models;
using Synapse.GUI.Models.Messages;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;
using Synapse.Reflection.Models;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

public abstract partial class BaseAlgorithmConfigViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    protected readonly IJobManager _jobManager;
    protected readonly IJobSelectorService _jobSelector;
    protected readonly ITypeRegistry? _typeRegistry;
    
    [ObservableProperty] private AlgorithmType _algorithmType;
    [ObservableProperty] private int _maxGenerations;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _randomSeed;
    [ObservableProperty] private int? _timeoutSeconds;
    [ObservableProperty] private int _progressInterval;
    [ObservableProperty] private bool _jobActive;
    [ObservableProperty] private bool _enableBaseParameter = true;
    
    /// <summary>
    /// Algorithm-level similarity measure used for diversity calculation, duplicate detection, etc.
    /// </summary>
    public ObservableCollection<UserSettableClass> AvailableAlgorithmSimilarities { get; } = new();
    [ObservableProperty] private UserSettableClass? _selectedAlgorithmSimilarity;
    
    /// <summary>
    /// Tracks the current solution type so available similarities can be filtered.
    /// Updated when a problem is selected or a job is selected.
    /// </summary>
    private Type? _currentSolutionType;
    
    protected abstract IAlgorithmConfig Config { get; set; }
    private readonly Action _pageChangedHandler;
    private readonly EventHandler<SelectedJobChangedEventArgs>? _jobChangedHandler;
    
    protected BaseAlgorithmConfigViewModel(INavigationService navigationService, IJobManager jobManager,
        IJobSelectorService jobSelector, ITypeRegistry? typeRegistry = null)
    {
        _navigationService = navigationService;
        _jobManager = jobManager;
        _jobSelector = jobSelector;
        _typeRegistry = typeRegistry;
        
        _pageChangedHandler = () => OnJobActiveChanged(false);
        _navigationService.CurrentPageChanged += _pageChangedHandler;
        
        WeakReferenceMessenger.Default.Register<AlgorithmStartedMessage>(this, void (r, msg) =>
        {
            JobActive = true;
        });
        
        // When the user selects a problem type/instance → update solution type and reload similarities
        WeakReferenceMessenger.Default.Register<ProblemSelectedMessage>(this, (r, msg) =>
        {
            try
            {
                _currentSolutionType = msg.Problem.CreateRandomSolution().GetType();
            }
            catch
            {
                _currentSolutionType = null;
            }
            ReloadAvailableSimilarities();
            OnProblemContextChanged(msg.Problem);
        });
        
        // When the user selects a queued/running job → update solution type from that job's problem
        _jobChangedHandler = (_, e) =>
        {
            if (e.SelectedJobInfo is not null)
            {
                try
                {
                    _currentSolutionType = e.SelectedJobInfo.Problem.CreateRandomSolution().GetType();
                }
                catch
                {
                    _currentSolutionType = null;
                }
            }
            else
            {
                _currentSolutionType = null;
            }
            ReloadAvailableSimilarities();
            OnProblemContextChanged(e.SelectedJobInfo?.Problem);
        };
        _jobSelector.SelectedJobChanged += _jobChangedHandler;
        
        // Initialize solution type from the currently selected job (if any)
        var currentJob = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (currentJob is not null)
        {
            try { _currentSolutionType = currentJob.Problem.CreateRandomSolution().GetType(); }
            catch { /* ignore */ }
        }
    }
    
    partial void OnJobActiveChanged(bool value)
    {
        if (_navigationService.CurrentPageName == ApplicationPageNames.HitlConfigurator)
        {
            JobActive = false;
            EnableBaseParameter = false;
        }
        else EnableBaseParameter = !JobActive;
    }

    protected virtual void SetValuesFromConfig()
    {
        RandomSeed = Config.Seed ?? Random.Shared.Next();
        MaxGenerations = Config.MaxIterations;
        Name = Config.Name;
        TimeoutSeconds = Config.TimeoutSeconds;
        ProgressInterval = Config.ProgressInterval;
        
        ReloadAvailableSimilarities();
    }
    
    /// <summary>
    /// Reloads the available similarity measures, filtered to the current solution type.
    /// Preserves the current selection if it is still applicable, otherwise falls back to
    /// the config's measure, the solution's default, or the first available.
    /// </summary>
    protected void ReloadAvailableSimilarities()
    {
        if (_typeRegistry is null) return;
        
        // Remember what was selected before (by type) so we can restore it
        var previousSelectionType = SelectedAlgorithmSimilarity?.BaseType
                                    ?? Config?.SimilarityMeasure?.GetType();
        
        AvailableAlgorithmSimilarities.Clear();
        
        var allSimilarities = _typeRegistry
            .GetAssignableTypes(typeof(ISolutionSimilarity))
            .BuildUserSettableClasses();
        
        var applicable = _currentSolutionType is not null
            ? allSimilarities.ApplicableToSolution(_currentSolutionType).ToList()
            : allSimilarities.ToList();
        
        foreach (var sim in applicable)
            AvailableAlgorithmSimilarities.Add(sim);
        
        // 1) Try to restore previous selection
        if (previousSelectionType is not null)
        {
            var match = applicable.FirstOrDefault(s => s.BaseType == previousSelectionType);
            if (match is not null)
            {
                SelectedAlgorithmSimilarity = match;
                return;
            }
        }
        
        // 2) Try to match the config's current similarity measure
        if (Config?.SimilarityMeasure is not null)
        {
            var match = applicable.FirstOrDefault(s => s.BaseType == Config.SimilarityMeasure.GetType());
            if (match is not null)
            {
                SelectedAlgorithmSimilarity = match;
                return;
            }
        }
        
        // 3) Try to use the solution's default similarity
        if (_currentSolutionType is not null)
        {
            try
            {
                var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
                var defaultSim = jobInfo?.Problem.CreateRandomSolution().GetDefaultSolutionSimilarityClass();
                if (defaultSim is not null)
                {
                    var match = applicable.FirstOrDefault(s => s.BaseType == defaultSim.GetType());
                    if (match is not null)
                    {
                        SelectedAlgorithmSimilarity = match;
                        return;
                    }
                }
            }
            catch { /* ignore */ }
        }
        
        // 4) Fall back to first available
        SelectedAlgorithmSimilarity = applicable.FirstOrDefault();
    }

    /// <summary>
    /// Called when the active problem context changes either through problem selection
    /// or selected job changes. Derived classes can override this to keep UI state in sync.
    /// </summary>
    protected virtual void OnProblemContextChanged(IProblem? problem)
    {
    }
    
    partial void OnSelectedAlgorithmSimilarityChanged(UserSettableClass? value)
    {
        if (value is not null &&
            value.TryGetInstance<ISolutionSimilarity>(out var similarity, out _) &&
            similarity is not null)
        {
            Config.SimilarityMeasure = similarity;
            UpdateConfig();
        }
    }
    
    partial void OnMaxGenerationsChanged(int value) 
    {
        Config.MaxIterations = value;
        UpdateConfig();
    }

    partial void OnNameChanged(string value) 
    {
        Config.Name = value;
        UpdateConfig();
    }

    partial void OnRandomSeedChanged(int value)
    { 
        Config.Seed = value;
        UpdateConfig();
    }
    
    partial void OnTimeoutSecondsChanged(int? value) 
    {
        Config.TimeoutSeconds = value;
        UpdateConfig();
    }
    
    partial void OnProgressIntervalChanged(int value) 
    {
        Config.ProgressInterval = value;
        UpdateConfig();
    }
    
    protected void UpdateConfig()
    {
        WeakReferenceMessenger.Default.Send(new AlgorithmConfigChangedMessage(Config));
    }
    
    [RelayCommand]
    private void SaveConfig()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public abstract void SetConfig(BaseAlgorithmConfigViewModel? currentAlgorithmConfigViewModel);

    protected override void DisposeManaged()
    {
        _navigationService.CurrentPageChanged -= _pageChangedHandler;
        if (_jobChangedHandler is not null)
            _jobSelector.SelectedJobChanged -= _jobChangedHandler;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.DisposeManaged();
    }
}
