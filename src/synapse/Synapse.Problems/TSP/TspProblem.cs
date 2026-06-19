using System.Numerics;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Problems.TSP;

[Problem(
    Name = "TSP Problem",
    ProblemType = ProblemType.Tsp)]
public class TspProblem : IProblem
{
    public ProblemType ProblemType => ProblemType.Tsp;
    
    public double[,] DistanceMatrix { get; }
    private readonly int _n;

    public string InstanceName { get; init; } = string.Empty;
    public double XMaxCoordinate { get; init; }
    public double YMaxCoordinate { get; init; }
    public IList<Vector2> Coordinates { get; init;  } = new List<Vector2>();
    public double BestKnownFitness { get; init; }

    public TspProblem(double[,] distanceMatrix)
    {
        DistanceMatrix = distanceMatrix;
        _n = distanceMatrix.GetLength(0);
    }
    
    public TspProblem(double[,] distanceMatrix, IList<(double X, double Y)> coordinates)
    {
        DistanceMatrix = distanceMatrix;
        _n = distanceMatrix.GetLength(0);
        Coordinates = coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)).ToList();
    }

    public ISolution CreateRandomSolution() => new TspSolution(_n);
    
    public IFitnessEvaluator GetFitnessEvaluator() => new TspFitnessEvaluator(this);
    //public IFitnessEvaluator GetFitnessEvaluator() => new TspCrossingFitnessEvaluator(this);

    public string Describe() => $"TSP with {_n} cities";
}
