using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Synapse.GUI.Models;
using Synapse.GUI.Models.Messages;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls;

public partial class AlgorithmExecutionControlViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IJobManager _jobManager;
    private readonly IJobSelectorService _jobSelector;
    private readonly IAlgorithmConfigViewModelService _configViewModelService;
    private IAlgorithmConfig? _algorithmConfig;
    private IProblem? _problem;
    private IProblemInstance? _problemInstance;
    
    [ObservableProperty] private bool _queueingEnabled = true;
    [ObservableProperty] private bool _startingEnabled = true;

    private bool _currentJobCloned = false;
    
    public AlgorithmExecutionControlViewModel(INavigationService navigationService, IJobManager jobManager, IJobSelectorService jobSelector, IAlgorithmConfigViewModelService configViewModelService)
    {
        _navigationService = navigationService;
        _jobManager = jobManager;
        _jobSelector = jobSelector;
        _configViewModelService = configViewModelService;

        WeakReferenceMessenger.Default.Register<AlgorithmConfigChangedMessage>(this, (r, msg) =>
        {
            _algorithmConfig = msg.Config;
        });
        
        WeakReferenceMessenger.Default.Register<ProblemSelectedMessage>(this, (r, msg) =>
        {
            _problem = msg.Problem;
            _problemInstance = msg.ProblemInstance;
        });

        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        
        WeakReferenceMessenger.Default.Register<AlgorithmConfigCloned>(this, (r, msg) =>
        {
            QueueingEnabled = true;
            StartingEnabled = true;
            _currentJobCloned = true;
        });
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs e)
    {
        QueueingEnabled = e.SelectedJobInfo?.Id == null;
        StartingEnabled = e.SelectedJobInfo?.Status == JobStatus.Queued;
    }

    [RelayCommand]
    private void CreateNewConfig()
    {
        _jobSelector.SelectedJobId = null;
        _configViewModelService.SetAlgorithmType(AlgorithmType.Unknown);
    }
    
    [RelayCommand]
    private void CloneCurrentJob()
    {
        if (_jobSelector.SelectedJobInfo?.Problem is not null)
            _problem = _jobSelector.SelectedJobInfo?.Problem;
        
        if (_jobSelector.SelectedJobInfo?.ProblemInstance is not null)
            _problemInstance = _jobSelector.SelectedJobInfo?.ProblemInstance;
        
        WeakReferenceMessenger.Default.Send(new AlgorithmConfigCloned());
    }
    

    [RelayCommand]
    private void QueueJob()
    {
        if (_algorithmConfig is null || _problem is null || _problemInstance is null) return;
        var guid = _jobManager.QueueJob(_algorithmConfig, _problem, _problemInstance);
        
        WeakReferenceMessenger.Default.Send(new AlgorithmStartedMessage(guid));
        _jobSelector.SelectedJobId = guid;
        QueueingEnabled = false;
        _currentJobCloned = false;
    }
    
    [RelayCommand]
    private void StartAlgorithmExecution()
    {
        if (!_currentJobCloned && _jobSelector.SelectedJobInfo?.Status == JobStatus.Queued)
        {
            _ = _jobManager.StartQueuedJobAsync(_jobSelector.SelectedJobInfo.Id);
        }
        else
        {
            if (_algorithmConfig is null || _problem is null || _problemInstance is null) return;
            var guid = _jobManager.QueueJob(_algorithmConfig, _problem, _problemInstance, true);
            
            WeakReferenceMessenger.Default.Send(new AlgorithmStartedMessage(guid));
            _jobSelector.SelectedJobId = guid;
            QueueingEnabled = false;
        }

        StartingEnabled = false;
        _currentJobCloned = false;
    }

    [RelayCommand]
    private void ConfigureHitlActions()
    {
        QueueJob();
        _navigationService.Navigate(ApplicationPageNames.HitlConfigurator);
    }

    protected override void DisposeManaged()
    {
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.DisposeManaged();
    }
}
