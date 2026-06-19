using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Selector;

[DisplayInfo("Random Selection")]
public class RandomSelection : ISelection
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public object Clone()
    {
        return new RandomSelection();
    }
}
