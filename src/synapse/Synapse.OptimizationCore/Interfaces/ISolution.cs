using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Interfaces;

// Basic solution representation
public interface ISolution
{
    double? Fitness { get; set; }
    int Length { get; }
    
    Parameter[] GetParameters();
    void SetParameters(Parameter[] parameters);
    
    Parameter GetParameter(int index);
    void SetParameter(int index, Parameter parameter);

    ISolution CreateRandom();
    ISolution Clone();
    ISolutionSimilarity GetDefaultSolutionSimilarityClass();
}
