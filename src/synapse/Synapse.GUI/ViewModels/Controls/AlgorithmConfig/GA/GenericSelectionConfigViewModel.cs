using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Selector;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.GA;

public class GenericSelectionConfigViewModel : GaConfigViewModel
{
    public override void ApplyTo(GeneticAlgorithmConfig config, object? configObject)
    {
        ApplyTo(config, configObject, true);
    }

    public override void ApplyTo(GeneticAlgorithmConfig config, object? configObject, bool primary)
    {
        if (configObject is null) return;
        if (configObject is not ISelection selection) return;
        if (primary) config.Selection = selection;
        else config.Selection2 = selection;
    }

    public override void LoadFrom(GeneticAlgorithmConfig config, bool primary) { /* nothing */ }
}
