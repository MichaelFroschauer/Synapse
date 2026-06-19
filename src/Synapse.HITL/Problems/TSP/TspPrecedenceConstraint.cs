using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

// Example constraint for TSP: ensure that city A occurs after city B (i.e. index(A) > index(B)).
[DisplayInfo("TSP Precedence Constraint",
    "Ensures that a specific city is visited after another city in the tour. "
    + "The two cities do not need to be directly next to each other — other cities may appear in between. "
    + "During repair, the city that must come later is moved to a position after the reference city.")]
[ApplicableSolution(typeof(TspSolution))]
public class TspPrecedenceConstraint(
    [DisplayInfo("City Before")] int reference,
    [DisplayInfo("City After")] int mustComeAfter
    ) : IConstraint
{
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not TspSolution s) return true;
        var tour = s.GetParametersWithType();
        int idxA = Array.IndexOf(tour, mustComeAfter); // city index that must come after reference
        int idxB = Array.IndexOf(tour, reference);     // city index that must be before
        if (idxA < 0 || idxB < 0) return false;
        return idxA > idxB;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (solution is not TspSolution s) return solution;
        int[] tour = s.GetParametersWithType();
        int idxA = Array.IndexOf(tour, mustComeAfter);
        int idxB = Array.IndexOf(tour, reference);
        if (idxA < 0 || idxB < 0) return null;
        if (idxA > idxB) return new TspSolution(tour);

        int n = tour.Length;
        int val = tour[idxA];

        // Create an array without the element idxA (length n-1)
        int[] temp = new int[n - 1];
        int t = 0;
        for (int i = 0; i < n; i++)
        {
            if (i == idxA) continue;
            temp[t++] = tour[i];
        }

        // Calculate insertion position (after B, where B is shifted by 1 in the ‘temp’ array if idxA < idxB)
        int insertPos = (idxA < idxB) ? idxB : idxB + 1;
        // If idxA < idxB, then B is in temp at index (idxB - 1), and after B, insertPos = idxB.
        // otherwise (idxA > idxB) this would already be treated as a no-op above

        // Build new tour array of original length and insert val at insertPos
        int[] newTour = new int[n];
        int k = 0;
        for (int i = 0; i < insertPos; i++) newTour[k++] = temp[i];
        newTour[k++] = val;
        for (int i = insertPos; i < temp.Length; i++) newTour[k++] = temp[i];

        return new TspSolution(newTour);
    }

    public string Description => $"TSP: city {mustComeAfter} must be traveled after {reference} (there can be cities in between)";

    public string TextVisualization
    {
        get
        {
            return
                $@"Precedence Constraint
Reference city (must come first): {reference}
Dependent city (must come later): {mustComeAfter}

Description: In the tour, city {mustComeAfter} must be visited at some point after city {reference}.
Other cities may appear in between. The constraint enforces relative order, not immediate adjacency.
Repair behavior: If enabled, the dependent city is repositioned after the reference city.";
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}