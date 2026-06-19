using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Mutator;

[DisplayInfo("Flip Bit Mutation")]
public class FlipBitMutation : IMutation
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public object Clone()
    {
        return new FlipBitMutation();
    }
}
