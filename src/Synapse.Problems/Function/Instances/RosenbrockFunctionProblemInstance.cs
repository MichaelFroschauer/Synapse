using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Problems.Function;

[ProblemInstance(Name = "Rosenbrock Function Problem", ProblemType = ProblemType.Function)]
public class RosenbrockFunctionProblemInstance([DisplayInfo("Dimensions")] int dimensions = 5) : IFunctionProblemInstance
{
    public int Dimensions => dimensions;

    public bool Minimize => true;

    public Func<double[], double> Function => v =>
    {
        double sum = 0.0;

        for (int i = 0; i < dimensions - 1; i++)
        {
            double difference = v[i + 1] - v[i] * v[i];
            double offset = 1.0 - v[i];

            sum += 100.0 * difference * difference
                   + offset * offset;
        }

        return sum;
    };
}
