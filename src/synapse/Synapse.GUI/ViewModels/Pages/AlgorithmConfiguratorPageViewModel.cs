using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Synapse.GUI.Models;
using Synapse.GUI.Models.Messages;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

namespace Synapse.GUI.ViewModels.Pages;

public partial class AlgorithmConfiguratorPageViewModel : PageViewModel
{
    public AlgorithmSelectionViewModel AlgorithmSelectionVm { get; }
    public AlgorithmExecutionControlViewModel AlgorithmExecutionControlVm { get; }

    [ObservableProperty]
    private BaseAlgorithmConfigViewModel? _currentAlgorithmParameters;

    private readonly IAlgorithmConfigViewModelService _configViewModelService;
    private readonly IJobSelectorService _jobSelectorService;

    public AlgorithmConfiguratorPageViewModel(
        IAlgorithmConfigViewModelService configViewModelService,
        IJobSelectorService jobSelectorService,
        AlgorithmSelectionViewModel algorithmSelectionVm,
        AlgorithmExecutionControlViewModel algorithmExecutionControlVm)
    {
        PageName = ApplicationPageNames.AlgorithmConfigurator;
        
        AlgorithmSelectionVm = algorithmSelectionVm;
        AlgorithmExecutionControlVm = algorithmExecutionControlVm;
        
        _configViewModelService = configViewModelService;
        _configViewModelService.CurrentAlgorithmTypeChanged += OnCurrentAlgorithmTypeChanged;
        CurrentAlgorithmParameters = _configViewModelService.CurrentAlgorithmConfigViewModel;

        _jobSelectorService = jobSelectorService;
        _jobSelectorService.SelectedJobChanged += OnSelectedJobChanged;
        
        WeakReferenceMessenger.Default.Register<AlgorithmConfigCloned>(this, void (r, msg)
            => CurrentAlgorithmParameters = _configViewModelService.GetViewModelCloned());
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        CurrentAlgorithmParameters = _configViewModelService.CurrentAlgorithmConfigViewModel;
    }

    private void OnCurrentAlgorithmTypeChanged()
    {
        CurrentAlgorithmParameters = _configViewModelService.CurrentAlgorithmConfigViewModel;
    }

    protected override void DisposeManaged()
    {
        _configViewModelService.CurrentAlgorithmTypeChanged -= OnCurrentAlgorithmTypeChanged;
        _jobSelectorService.SelectedJobChanged -= OnSelectedJobChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.DisposeManaged();
    }
}
