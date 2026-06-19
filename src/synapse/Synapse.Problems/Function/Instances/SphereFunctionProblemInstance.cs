using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Problems.Function;


[ProblemInstance(Name = "Sphere Function Problem", ProblemType = ProblemType.Function)]
public class SphereFunctionProblemInstance([DisplayInfo("Dimensions")] int dimensions = 5) : IFunctionProblemInstance
{
    public int Dimensions => dimensions;
    public bool Minimize => false;
    public Func<double[], double> Function => v => Enumerable.Range(0, dimensions-1).Sum(n => Math.Pow(v[n], 2));
}
