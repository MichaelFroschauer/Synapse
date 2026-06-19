using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Problems.Function;


[ProblemInstance(Name = "Rastrigin Function Problem", ProblemType = ProblemType.Function)]
public class RastriginFunctionProblemInstance([DisplayInfo("Dimensions")] int dimensions = 5) : IFunctionProblemInstance
{
    public int Dimensions => dimensions;
    public bool Minimize => true;
    public Func<double[], double> Function => v =>
        {
            double a = 10.0;
            double sum = 0.0;
            for (int i = 0; i < dimensions; i++)
                sum += v[i] * v[i] - a * Math.Cos(2 * Math.PI * v[i]);
            return a * dimensions + sum;
        };
}
