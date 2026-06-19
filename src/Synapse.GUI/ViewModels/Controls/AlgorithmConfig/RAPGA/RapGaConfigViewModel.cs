using Synapse.Algorithms.RAPGA;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.RAPGA;

public abstract class RapGaConfigViewModel : ViewModelBase
{
    public abstract void ApplyTo(RAPGAConfig config, object? configObject);
    public abstract void ApplyTo(RAPGAConfig config, object? configObject, bool primary);
    public abstract void LoadFrom(RAPGAConfig config, bool primary);
}
