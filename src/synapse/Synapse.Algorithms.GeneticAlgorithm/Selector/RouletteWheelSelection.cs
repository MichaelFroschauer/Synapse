using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Selector;

[DisplayInfo("Roulette Wheel Selection")]
public class RouletteWheelSelection : ISelection
{
    public Guid Id { get; } = Guid.NewGuid();
    public object Clone()
    {
        return new RouletteWheelSelection();
    }
}
