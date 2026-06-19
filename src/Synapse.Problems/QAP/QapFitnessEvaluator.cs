using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.QAP;

public class QapFitnessEvaluator : IFitnessEvaluator
{
    private readonly int[,] _D;
    private readonly int[,] _F;

    public QapFitnessEvaluator(int[,] d, int[,] f)
    {
        _D = d ?? throw new ArgumentNullException(nameof(d));
        _F = f ?? throw new ArgumentNullException(nameof(f));
    }

    public double Evaluate(ISolution solution)
    {
        if (solution is not PermutationSolution qsol) throw new ArgumentException($"Given Solution is not of type {nameof(PermutationSolution)}");
        var parms = qsol.GetParametersWithType() ?? throw new ArgumentException("Solution.Parameters are null"); 

        // calculate QAP cost: sum_{i,j} FlowMatrix[i,j] * DistanceMatrix[p[i],p[j]]
        int n = parms.Length;
        double cost = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                cost += _F[i, j] * _D[parms[i], parms[j]];
            }
        }
        return cost;
    }

    public bool Minimize => true;
}
