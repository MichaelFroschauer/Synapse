using Synapse.Algorithms.GeneticAlgorithm;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.GA;

public abstract class GaConfigViewModel : ViewModelBase
{
    public abstract void ApplyTo(GeneticAlgorithmConfig config, object? configObject);
    public abstract void ApplyTo(GeneticAlgorithmConfig config, object? configObject, bool primary);
    public abstract void LoadFrom(GeneticAlgorithmConfig config, bool primary);
}
