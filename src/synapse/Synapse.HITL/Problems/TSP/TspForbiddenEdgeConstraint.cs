using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

[DisplayInfo("TSP Forbidden Edge Constraint",
    "Prevents specific edges (connections between two cities) from appearing in the tour. "
    + "If any of the forbidden edges are present, the constraint is violated.")]
[ApplicableSolution(typeof(TspSolution))]
public class TspForbiddenEdgeConstraint : IConstraint
{
    private readonly List<(int, int)> _mustNotHaveEdges = new();

    public TspForbiddenEdgeConstraint(IEnumerable<(int, int)> mustNotHaveEdges)
    {
        _mustNotHaveEdges = mustNotHaveEdges.ToList();
    }

    public TspForbiddenEdgeConstraint(
        [DisplayInfo("Node 1")] int node1,
        [DisplayInfo("Node 2")] int node2)
    {
        _mustNotHaveEdges.Add((node1, node2));
    }
    
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is TspSolution tspSolution)
        {
            var parameters = tspSolution.GetParametersWithType();
            var parameterLength = parameters.Length;
            for (int i = 0; i < parameterLength; i++)
            {
                var n1 = parameters[i];
                var n2 = parameters[(i + 1) % parameterLength];
                if (_mustNotHaveEdges.Contains((n1, n2))) return false;
                if (_mustNotHaveEdges.Contains((n2, n1))) return false;
            }
        }
        return true;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not TspSolution tspSolution) return solution;

        var tour = tspSolution.GetParametersWithType().ToArray();
        if (tour.Length < 2) return solution;

        int currentViolations = CountViolations(tour);
        if (currentViolations == 0) return solution;

        // Deterministic local search: keep swapping nodes while strictly reducing violations.
        int maxIterations = tour.Length * tour.Length * 20;
        for (int iter = 0; iter < maxIterations && currentViolations > 0; iter++)
        {
            int bestI = -1;
            int bestJ = -1;
            int bestViolations = currentViolations;

            for (int i = 0; i < tour.Length - 1; i++)
            {
                for (int j = i + 1; j < tour.Length; j++)
                {
                    Swap(tour, i, j);
                    int candidateViolations = CountViolations(tour);
                    Swap(tour, i, j);

                    if (candidateViolations < bestViolations)
                    {
                        bestViolations = candidateViolations;
                        bestI = i;
                        bestJ = j;
                        if (bestViolations == 0) break;
                    }
                }

                if (bestViolations == 0) break;
            }

            if (bestI < 0)
            {
                // No improving move exists from this state.
                return null;
            }

            Swap(tour, bestI, bestJ);
            currentViolations = bestViolations;
        }

        if (CountViolations(tour) > 0) return null;
        return new TspSolution(tour);
    }
    
    public string Description =>
        $"TSP forbidden edge(s): {string.Join(", ", _mustNotHaveEdges.Select(e => $"{e.Item1}-{e.Item2}"))}";

    public string TextVisualization
    {
        get
        {
            var edges = string.Join(", ", _mustNotHaveEdges.Select(e => $"{e.Item1}-{e.Item2}"));
            return $@"Forbidden Edge Constraint
Forbidden undirected edges: {edges}

Description: None of these edges may appear in the tour in either direction.
Repair behavior: not implemented for this constraint.";
        }
    }
    /*[DisplayInfo("Repair Solution")]*/ public bool RepairSolution { get; set; } = false;

    private int CountViolations(int[] tour)
    {
        int violations = 0;
        for (int i = 0; i < tour.Length; i++)
        {
            int n1 = tour[i];
            int n2 = tour[(i + 1) % tour.Length];
            if (_mustNotHaveEdges.Contains((n1, n2)) || _mustNotHaveEdges.Contains((n2, n1)))
            {
                violations++;
            }
        }

        return violations;
    }

    private static void Swap(int[] arr, int i, int j)
    {
        (arr[i], arr[j]) = (arr[j], arr[i]);
    }
}
