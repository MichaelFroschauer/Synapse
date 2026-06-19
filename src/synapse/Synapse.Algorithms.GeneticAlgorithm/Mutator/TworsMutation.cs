using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Mutator;

[DisplayInfo("Twors Mutation")]
public class TworsMutation : IMutation
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public object Clone()
    {
        return new TworsMutation();
    }
}