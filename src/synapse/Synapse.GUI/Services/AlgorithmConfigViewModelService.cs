using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.Services;

public class AlgorithmConfigViewModelService(IAlgorithmConfigViewModelFactory factory) : IAlgorithmConfigViewModelService
{
    public void SetAlgorithmType(AlgorithmType algorithmType)
    {
        var vm = factory.GetViewModel(algorithmType);
        CurrentAlgorithmConfigViewModel = vm;
    }

    private BaseAlgorithmConfigViewModel? _currentAlgorithmTypeViewModel;
    public BaseAlgorithmConfigViewModel? CurrentAlgorithmConfigViewModel
    {
        get => _currentAlgorithmTypeViewModel;
        private set
        {
            _currentAlgorithmTypeViewModel = value;
            NotifyCurrentAlgorithmTypeChanged();
        }
    }
    
    public BaseAlgorithmConfigViewModel? GetViewModelCloned()
    {
        var vm = factory.GetViewModel(CurrentAlgorithmConfigViewModel?.AlgorithmType ?? AlgorithmType.Unknown);
        vm?.SetConfig(CurrentAlgorithmConfigViewModel);
        CurrentAlgorithmConfigViewModel = vm;
        return vm;
    }
    
    public event Action? CurrentAlgorithmTypeChanged;
    private void NotifyCurrentAlgorithmTypeChanged() => CurrentAlgorithmTypeChanged?.Invoke();
}
