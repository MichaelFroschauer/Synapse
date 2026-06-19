using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Mutator;

[DisplayInfo("Reverse Sequence Mutation")]
public class ReverseSequenceMutation : IMutation
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public object Clone()
    {
        return new ReverseSequenceMutation();
    }
}
