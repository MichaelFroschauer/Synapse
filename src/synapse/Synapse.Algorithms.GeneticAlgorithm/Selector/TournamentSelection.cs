using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Selector;

[DisplayInfo("Tournament Selection")]
public class TournamentSelection : ISelection
{
    public Guid Id { get; } = Guid.NewGuid();
    public int GroupSize { get; set; }
    
    public TournamentSelection(int groupSize = 2)
    {
        GroupSize = groupSize;
    }
    
    public object Clone()
    {
        return new TournamentSelection(GroupSize);
    }
}