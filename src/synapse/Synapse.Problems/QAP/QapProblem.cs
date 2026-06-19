using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Problems.QAP;

[Problem(Name = "QAP Problem", ProblemType = ProblemType.Qap)]
public class QapProblem : IProblem
{
    public int N { get; }
    public int[,] DistanceMatrix { get; } // Specifies the distances between locations.
    public int[,] FlowMatrix { get; } // Indicates how strongly two institutions interact with each other
    
    private readonly IRandom _random = RandomProvider.Value;
    
    public QapProblem(int n, int[,] distanceMatrix, int[,] flowMatrix)
    {
        if (distanceMatrix == null) throw new ArgumentNullException(nameof(distanceMatrix));
        if (flowMatrix == null) throw new ArgumentNullException(nameof(flowMatrix));
        if (distanceMatrix.GetLength(0) != n || distanceMatrix.GetLength(1) != n) throw new ArgumentException("DistanceMatrix must be n x n");
        if (flowMatrix.GetLength(0) != n || flowMatrix.GetLength(1) != n) throw new ArgumentException("FlowMatrix must be n x n");
        N = n;
        DistanceMatrix = distanceMatrix;
        FlowMatrix = flowMatrix;
    }
    
    public ISolution CreateRandomSolution()
    {
        var perm = Enumerable.Range(0, N).ToArray();
        for (int i = N - 1; i > 0; i--)
        {
            int j = _random.GetInt(i + 1);
            (perm[i], perm[j]) = (perm[j], perm[i]);
        }
        
        Parameter[] parameters = new Parameter[N];
        for (int i = 0; i < N; i++)
        {
            parameters[i] = new Parameter(perm[i]);
        }

        return new QapSolution(parameters);
    }

    public IFitnessEvaluator GetFitnessEvaluator() => new QapFitnessEvaluator(DistanceMatrix, FlowMatrix);
    public string Describe() => $"QAP instance: N={N}";
    public ProblemType ProblemType => ProblemType.Qap;
}
