namespace Synapse.Algorithms.GeneticAlgorithm.Selector;

public interface ISelection : ICloneable
{
    Guid Id { get; }
}
