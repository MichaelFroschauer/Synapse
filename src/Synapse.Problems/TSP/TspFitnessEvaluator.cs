using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.TSP;

public class TspFitnessEvaluator : IFitnessEvaluator
{
    private readonly TspProblem _problem;
    
    private TspCrossingFitnessEvaluator _fitness2;

    public TspFitnessEvaluator(TspProblem problem)
    {
        _problem = problem;
        _fitness2 = new TspCrossingFitnessEvaluator(_problem);
    }

    public double Evaluate(ISolution solution)
    {
        var s = solution as PermutationSolution ?? throw new ArgumentException($"Expecting {nameof(PermutationSolution)}");
        var dist = _problem.DistanceMatrix;
        double sum = 0.0;
        var parameters = s.GetParametersWithType();
        for (int i = 0; i < parameters.Length; i++)
        {
            int a = parameters[i];
            int b = parameters[(i + 1) % parameters.Length];
            sum += dist[a, b];
        }
        return sum;
    }

    public bool Minimize => true;
}
