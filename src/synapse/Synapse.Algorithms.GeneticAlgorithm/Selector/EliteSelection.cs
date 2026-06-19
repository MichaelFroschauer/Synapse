using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Selector;

[DisplayInfo("Elite Selection")]
public class EliteSelection : ISelection
{
    public Guid Id { get; } = Guid.NewGuid();
    public int NrOfElites { get; set; }

    public EliteSelection(int nrOfElites)
    {
        NrOfElites = nrOfElites;
    }
    
    public object Clone()
    {
        return new EliteSelection(NrOfElites);
    }
}