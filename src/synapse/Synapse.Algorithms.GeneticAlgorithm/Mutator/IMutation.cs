namespace Synapse.Algorithms.GeneticAlgorithm.Mutator;

public interface IMutation : ICloneable
{
    Guid Id { get; }
}
