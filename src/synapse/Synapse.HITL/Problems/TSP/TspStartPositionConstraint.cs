using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

[DisplayInfo("TSP Starting City Constraint",
    "Forces the tour to begin at a specific city. During repair, the chosen city is moved to the first position while keeping the rest of the tour order intact.")]
[ApplicableSolution(typeof(TspSolution))]
public class TspStartPositionConstraint(
    [DisplayInfo("Start City")] int start
    ) : IConstraint
{
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not TspSolution s) return true;
        var tour = s.GetParametersWithType();
        int idx = Array.IndexOf(tour, start);
        return idx == 0;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (solution is not TspSolution s) return solution;
        var tour = s.GetParametersWithType();
        if (tour is null) return null;

        int n = tour.Length;
        if (n == 0) return new TspSolution(tour);

        int idx = Array.IndexOf(tour, start);
        if (idx < 0) return null;          // City not in tour -> cannot be repaired
        if (idx == 0) return new TspSolution(tour);

        // Build new array: City at position 0, the previous elements 0..idx-1 are
        // shifted 1 to the right, elements by idx remain in their position.
        int[] newTour = new int[n];
        newTour[0] = tour[idx];

        for (int i = 0; i < idx; i++)
            newTour[i + 1] = tour[i];

        for (int i = idx + 1; i < n; i++)
            newTour[i] = tour[i];

        return new TspSolution(newTour);
    }

    public string Description => $"TSP start city fixed to {start}";

    public string TextVisualization
    {
        get
        {
            return
                $@"Start Position Constraint
Start city: {start}

Description: The tour must always begin at the specified city.
Repair behavior: If enabled, the city is moved to position 0 while preserving the order of the remaining cities.";
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}
