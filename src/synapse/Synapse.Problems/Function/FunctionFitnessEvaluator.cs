using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.Function;

public class FunctionFitnessEvaluator(Func<double[], double> func, bool minimize = true) : IFitnessEvaluator
{
    public double Evaluate(ISolution solution)
    {
        var p = solution.GetParameters().Select(p => (double)p.Value).ToArray();
        return func(p); 
    }

    public bool Minimize => minimize;
}
