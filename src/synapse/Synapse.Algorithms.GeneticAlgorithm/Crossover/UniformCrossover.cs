using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Crossover;

[DisplayInfo("Uniform Crossover")]
public class UniformCrossover : ICrossover
{
    public Guid Id { get; } = Guid.NewGuid();
    public float MixProbability { get; set; }

    public UniformCrossover(float mixProbability)
    {
        MixProbability = mixProbability;
    }
    
    public object Clone()
    {
        return new UniformCrossover(MixProbability);
    }
}
