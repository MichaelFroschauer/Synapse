using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Selector;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.GA;

public class TournamentSelectionConfigViewModel : GaConfigViewModel
{
    private int _groupSize = 2;
    public int GroupSize
    {
        get => _groupSize;
        set => SetProperty(ref _groupSize, value);
    }

    public override void ApplyTo(GeneticAlgorithmConfig config, object? configObject)
    {
        ApplyTo(config, configObject, true);
    }

    public override void ApplyTo(GeneticAlgorithmConfig config, object? configObject, bool primary)
    {
        var sel = new TournamentSelection(GroupSize);
        if (primary) config.Selection = sel;
        else config.Selection2 = sel;
    }

    public override void LoadFrom(GeneticAlgorithmConfig config, bool primary)
    {
        var sel = ((primary) ? config.Selection : config.Selection2) as TournamentSelection;
        if (sel != null)
            GroupSize = sel.GroupSize;
    }
}
