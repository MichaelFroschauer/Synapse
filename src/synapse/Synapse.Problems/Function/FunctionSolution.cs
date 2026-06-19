using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.Function;

public class FunctionSolution : RealValueSolution
{
    public FunctionSolution(int length) : base(length) { }
    public FunctionSolution(Parameter[] parameters) : base(parameters) { }
    public FunctionSolution(double[] values) : base(values) { }
    public override ISolution Create(double[] parameters) => new FunctionSolution(parameters);
    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() => new RealValueSolutionEuclideanSimilarity();
    public override string ToString()
    {
        return $"[ { string.Join(", ", GetParameters()) } ]";
    }
}
