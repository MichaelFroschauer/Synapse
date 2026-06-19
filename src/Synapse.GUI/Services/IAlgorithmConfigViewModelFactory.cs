using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.Services;

public interface IAlgorithmConfigViewModelFactory
{
    BaseAlgorithmConfigViewModel? GetViewModel(AlgorithmType algorithmType);
}