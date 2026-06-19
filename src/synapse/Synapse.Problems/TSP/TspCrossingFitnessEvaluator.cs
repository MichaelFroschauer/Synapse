using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.TSP;

public class TspCrossingFitnessEvaluator : IFitnessEvaluator
{
    private readonly TspProblem _problem;

    public TspCrossingFitnessEvaluator(TspProblem problem)
    {
        _problem = problem;
    }

    public double Evaluate(ISolution solution)
    {
        var s = solution as PermutationSolution 
            ?? throw new ArgumentException($"Expecting {nameof(PermutationSolution)}");

        var parameters = s.GetParametersWithType();
        var coords = _problem.Coordinates;

        int n = parameters.Length;
        int crossings = 0;

        for (int i = 0; i < n; i++)
        {
            int a1 = parameters[i];
            int a2 = parameters[(i + 1) % n];

            for (int j = i + 1; j < n; j++)
            {
                int b1 = parameters[j];
                int b2 = parameters[(j + 1) % n];

                // Skip adjacent edges and the same edge
                if (i == j) continue;
                if ((i + 1) % n == j) continue;
                if (i == (j + 1) % n) continue;

                if (SegmentsIntersect(
                    coords[a1].X, coords[a1].Y,
                    coords[a2].X, coords[a2].Y,
                    coords[b1].X, coords[b1].Y,
                    coords[b2].X, coords[b2].Y))
                {
                    crossings++;
                }
            }
        }

        return crossings;
    }

    public bool Minimize => false;

    private static bool SegmentsIntersect(
        double x1, double y1, double x2, double y2,
        double x3, double y3, double x4, double y4)
    {
        double d1 = Direction(x3, y3, x4, y4, x1, y1);
        double d2 = Direction(x3, y3, x4, y4, x2, y2);
        double d3 = Direction(x1, y1, x2, y2, x3, y3);
        double d4 = Direction(x1, y1, x2, y2, x4, y4);

        return (d1 * d2 < 0) && (d3 * d4 < 0);
    }

    private static double Direction(
        double xi, double yi,
        double xj, double yj,
        double xk, double yk)
    {
        return (xk - xi) * (yj - yi) - (xj - xi) * (yk - yi);
    }
}
