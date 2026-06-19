using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.Algorithms.RAPGA;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.RAPGA;

public class RapGaGenericSelectionConfigViewModel : RapGaConfigViewModel
{
    public override void ApplyTo(RAPGAConfig config, object? configObject)
    {
        ApplyTo(config, configObject, true);
    }

    public override void ApplyTo(RAPGAConfig config, object? configObject, bool primary)
    {
        if (configObject is null) return;
        if (configObject is not ISelection selection) return;
        if (primary) config.Selection1 = selection;
        else config.Selection2 = selection;
    }

    public override void LoadFrom(RAPGAConfig config, bool primary) { /* nothing */ }
}
