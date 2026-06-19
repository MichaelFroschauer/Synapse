using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.Services;

public interface IAlgorithmConfigViewModelService
{
    void SetAlgorithmType(AlgorithmType algorithmType);
    BaseAlgorithmConfigViewModel? GetViewModelCloned();
    BaseAlgorithmConfigViewModel? CurrentAlgorithmConfigViewModel { get; }
    event Action? CurrentAlgorithmTypeChanged;
}
