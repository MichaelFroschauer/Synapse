using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Problems.Function;


[ProblemInstance(Name = "Random Function Problem", ProblemType = ProblemType.Function)]
public class F1FunctionProblemInstance(int dimensions = -1) : IFunctionProblemInstance
{
    public int Dimensions => 2;
    public bool Minimize => true;
    public Func<double[], double> Function => v => Math.Pow(v[0] - 3.14, 2) + Math.Pow(v[1] - 2.72, 2) + Math.Sin(3 * v[0] + 1.41) + Math.Sin(4 * v[1] - 1.73);
}
