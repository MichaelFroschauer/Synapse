namespace Synapse.OptimizationCore.Interfaces;

public interface ISolutionSimilarity
{
    double GetSimilarity(ISolution s1, ISolution s2);
}
