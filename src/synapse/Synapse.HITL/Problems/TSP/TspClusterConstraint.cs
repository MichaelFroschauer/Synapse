using System.Text;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

[DisplayInfo("TSP Cluster Cities Constraint",
    "Enforces that the specified cities appear consecutively as a single block in the TSP tour. "
    + "During repair, the cities are removed, optionally shuffled/rotated, and reinserted as one block "
    + "at a fixed or random position. If wrap-around is allowed, the block may span the tour end/start.")]

[ApplicableSolution(typeof(TspSolution))]
public class TspClusterConstraint : IConstraint
{
    private readonly HashSet<int> _cluster;
    private readonly bool _shuffleBlock;
    private readonly bool _randomInsertPosition;
    private readonly bool _allowWrapAround;
    private readonly IRandom _rng = RandomProvider.Value;

    public TspClusterConstraint(
        [DisplayInfo("Clustered Cities")] IEnumerable<int> clusterCities,
        [DisplayInfo("Shuffle clustered cities")] bool shuffleBlock = true,
        [DisplayInfo("Random Insert")] bool randomInsertPosition = false,
        [DisplayInfo("Allow Wrap Around")] bool allowWrapAround = true)
    {
        _cluster = new HashSet<int>(clusterCities);
        _shuffleBlock = shuffleBlock;
        _randomInsertPosition = randomInsertPosition;
        _allowWrapAround = allowWrapAround;
    }

    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not TspSolution s) return true;
        var tour = s.GetParametersWithType();
        int n = tour.Length;
        if (n == 0) return true;

        // alle Indices der Cluster-Städte in der Tour-Reihenfolge
        var indicesInTourOrder = tour
            .Select((val, idx) => (val, idx))
            .Where(x => _cluster.Contains(x.val))
            .Select(x => x.idx)
            .ToArray();

        if (indicesInTourOrder.Length == 0) return true;

        // Standard-Check (keine Wrap-Around): sind die Indices linear aufeinanderfolgend?
        bool contiguousLinear = true;
        Array.Sort(indicesInTourOrder);
        for (int i = 1; i < indicesInTourOrder.Length; i++)
        {
            if (indicesInTourOrder[i] != indicesInTourOrder[i - 1] + 1)
            {
                contiguousLinear = false;
                break;
            }
        }
        if (contiguousLinear) return true;

        // NOTE: if you want to allow wrap-around (block across end/start),
        // implement additional check here.
        // -> zusätzliche Prüfung: gibt es eine Position p in der Tour, so dass
        //    tour[p], tour[p+1], ..., tour[p+blockCount-1] (mod n) genau die Cluster-Städte sind?
        if (_allowWrapAround)
        {
            int blockCount = _cluster.Count;
            // scan starting positions 0..n-1; for jede startposition prüfen ob die nächsten blockCount Elemente nur Cluster-Städte sind
            for (int start = 0; start < n; start++)
            {
                bool ok = true;
                for (int k = 0; k < blockCount; k++)
                {
                    int idx = (start + k) % n;
                    if (!_cluster.Contains(tour[idx]))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) return true;
            }
        }

        return false;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (solution is not TspSolution s) return solution;

        var tour = s.GetParametersWithType().ToList();
        var block = tour.Where(c => _cluster.Contains(c)).ToList();

        if (block.Count == 0) return new TspSolution(tour.ToArray());

        // entferne alle Cluster-Elemente
        tour.RemoveAll(c => _cluster.Contains(c));

        // Exploration: block ggf. mischen oder rotieren
        if (_shuffleBlock)
        {
            ShuffleInPlace(block, _rng);
        }
        else if (block.Count > 1)
        {
            int rot = _rng.GetInt(0, block.Count);
            if (rot > 0)
                block = block.Skip(rot).Concat(block.Take(rot)).ToList();
        }

        // Insert-Position wählen (bei allowWrapAround: insertIndex == tour.Count entspricht Insert an End
        // was in zyklischer Sicht genauso einen zusammenhängenden Block erzeugt — kein Special-Splitting nötig)
        int insertIndex = 1;
        if (_randomInsertPosition)
        {
            insertIndex = _rng.GetInt(0, tour.Count + 1);
        }

        // clamp
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > tour.Count) insertIndex = tour.Count;

        tour.InsertRange(insertIndex, block);
        return new TspSolution(tour.ToArray());
    }

    public string Description =>
        $"TSP cluster cities: [{string.Join(", ", _cluster.OrderBy(i => i))}]";

    private static void ShuffleInPlace<T>(IList<T> list, IRandom rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.GetInt(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public string TextVisualization
    {
        get
        {
            var clusterList = _cluster.OrderBy(i => i).Select(i => i.ToString()).ToArray();
            var clusterText = "[" + string.Join(", ", clusterList) + "]";
            var sShuffle = _shuffleBlock ? "yes" : "no";
            var sRandom = _randomInsertPosition ? "yes" : "no";
            var sWrap = _allowWrapAround ? "yes" : "no";

            var sb = new StringBuilder();
            sb.AppendLine("Cluster Constraint");
            sb.AppendLine($"Cluster: {clusterText}");
            sb.AppendLine("Description: Listed cities must appear as one contiguous block in the route.");
            sb.AppendLine("Options:");
            sb.AppendLine($"  - Shuffle inside block: {sShuffle}");
            sb.AppendLine($"  - Random insert position: {sRandom}");
            sb.AppendLine($"  - Wrap-around allowed: {sWrap}");
            return sb.ToString().TrimEnd();
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}
