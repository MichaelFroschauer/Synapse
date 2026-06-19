using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.Algorithms.RAPGA;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig.RAPGA;

public class RapGaTournamentSelectionConfigViewModel : RapGaConfigViewModel
{
    private int _groupSize = 2;
    public int GroupSize
    {
        get => _groupSize;
        set => SetProperty(ref _groupSize, value);
    }

    public override void ApplyTo(RAPGAConfig config, object? configObject)
    {
        ApplyTo(config, configObject, true);
    }

    public override void ApplyTo(RAPGAConfig config, object? configObject, bool primary)
    {
        var sel = new TournamentSelection(GroupSize);
        if (primary) config.Selection1 = sel;
        else config.Selection2 = sel;
    }

    public override void LoadFrom(RAPGAConfig config, bool primary)
    {
        var sel = ((primary) ? config.Selection1 : config.Selection2) as TournamentSelection;
        if (sel != null)
            GroupSize = sel.GroupSize;
    }
}
