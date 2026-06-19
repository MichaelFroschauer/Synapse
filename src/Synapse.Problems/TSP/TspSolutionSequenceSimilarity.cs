using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.TSP;

[DisplayInfo(
    "Edge-Based Tour Sequence Similarity",
    "Compares two tours based on the number of shared edges based on the shorter tour, independent of traversal direction."
)]
[ApplicableSolution(typeof(TspSolution))]
public class TspSolutionSequenceSimilarity : ISolutionSimilarity
{
    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not TspSolution ps1)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(TspSolution)}");
        if (s2 is not TspSolution ps2)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(TspSolution)}");
        
        if (ps1.Length == 0 && ps2.Length == 0) return 1.0;
        if (ps1.Length == 0 || ps2.Length == 0) return 0.0;
        
        return EdgeBasedSimilarity(ps1.GetParametersWithType(), ps2.GetParametersWithType(), true);
    }
    
    private static double EdgeBasedSimilarity(int[] tour1, int[] tour2, bool symmetric = true)
    {
        if (tour1 == null) throw new ArgumentNullException(nameof(tour1));
        if (tour2 == null) throw new ArgumentNullException(nameof(tour2));

        int n1 = tour1.Length;
        int n2 = tour2.Length;

        if (n1 == 0 && n2 == 0) return 1.0;
        if (n1 == 0 || n2 == 0) return 0.0;

        // edges from Tour1
        var edges1 = BuildEdgeSet(tour1, symmetric);
        var edges2 = BuildEdgeSet(tour2, symmetric);

        int common = 0;
        foreach (var e in edges2)
        {
            if (edges1.Contains(e))
                common++;
        }
        
        var edgeCount = edges1.Count == edges2.Count ? Math.Min(edges1.Count, edges2.Count) : Math.Min(edges1.Count, edges2.Count) - 1;
        return 1.0 * common / edgeCount;
    }

    private static HashSet<(int, int)> BuildEdgeSet(IList<int> tour, bool symmetric)
    {
        int n = tour.Count;
        var set = new HashSet<(int, int)>();

        if (n <= 1)
            return set;

        for (int i = 0; i < n; i++)
        {
            int from = tour[i];
            int to   = tour[(i + 1) % n]; // cycle with overflow

            if (symmetric)
            {
                int a = Math.Min(from, to);
                int b = Math.Max(from, to);
                set.Add((a, b));
            }
            else
            {
                set.Add((from, to));
            }
        }

        return set;
    }
}
