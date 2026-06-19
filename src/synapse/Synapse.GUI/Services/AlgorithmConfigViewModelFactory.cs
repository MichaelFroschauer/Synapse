using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.Services;

public class AlgorithmConfigViewModelFactory(Func<AlgorithmType, BaseAlgorithmConfigViewModel?> factory) : IAlgorithmConfigViewModelFactory
{
    public BaseAlgorithmConfigViewModel? GetViewModel(AlgorithmType algorithmType) => factory(algorithmType);
}
