using System.Text;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

[DisplayInfo("TSP Fixed Edge Constraint",
    "Requires that one or more specific edges (connections between two cities) must appear in the tour. "
    + "Edges can be directed or undirected. During repair, missing edges are inserted by rearranging the tour.")]
[ApplicableSolution(typeof(TspSolution))]
public class TspFixedEdgeConstraint : IConstraint
{
    private readonly List<(int,int)> _mustHave;
    private readonly bool _directed;

    public TspFixedEdgeConstraint(
        [DisplayInfo("Necessary Edges")] IEnumerable<(int, int)> mustHaveEdges,
        [DisplayInfo("Directed Edges")] bool directed = false)
    {
        _directed = directed;
        _mustHave = mustHaveEdges.ToList();
    }

    public TspFixedEdgeConstraint(
        [DisplayInfo("Node 1")] int node1,
        [DisplayInfo("Node 2")] int node2,
        [DisplayInfo("Directed Edges")] bool directed = false
        )
    {
        _directed = directed;
        _mustHave = new List<(int, int)>();
        _mustHave.Add((node1, node2));
    }

    private IRandom _rnd => RandomProvider.Value;

    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not TspSolution s) return true;
        var tour = s.GetParametersWithType();
        return _mustHave.All(e => IsAdjacent(tour, e.Item1, e.Item2));
    }

    public ISolution? Repair(ISolution solution)
    {
        if (solution is not TspSolution s) return solution;
        if (!RepairSolution) return solution;
        var tour = s.GetParametersWithType().ToArray();

        foreach (var (uTmp,vTmp) in _mustHave)
        {
            // skip trivial self-edge
            if (uTmp == vTmp) return null;

            // check if already satisfied
            if (IsAdjacent(tour, uTmp, vTmp)) continue;

            var u = uTmp;
            var v = vTmp;
            if (!_directed && _rnd.GetBool()) (u, v) = (v, u);
            
            // Try simple repairs: first try make v follow u (u->v), else try make u follow v (v->u)
            int idxU = Array.FindIndex(tour, val => val == u);
            int idxV = Array.FindIndex(tour, val => val == v);
            if (idxU < 0 || idxV < 0) return null;

            bool repaired = false;

            // Only allow the second option if constraint is undirected
            // Option 1: move v so that it comes immediately after u
            {
                var tmp = tour.ToList();
                tmp.RemoveAt(idxV);
                idxU = tmp.IndexOf(u); // idxU may shift if idxV < idxU
                tmp.Insert(idxU + 1, v);

                // quick check: did we create the required adjacency (respecting directed flag)?
                for (int i = 0; i < tmp.Count; i++)
                {
                    int a = tmp[i];
                    int b = tmp[(i+1) % tmp.Count];
                    if (_directed)
                    {
                        if (a == u && b == v) { tour = tmp.ToArray(); repaired = true; break; }
                    }
                    else
                    {
                        if ((a == u && b == v) || (a == v && b == u)) { tour = tmp.ToArray(); repaired = true; break; }
                    }
                }
            }

            if (!repaired && !_directed)
            {
                // Option 2: move u so that it comes immediately after v
                idxU = Array.FindIndex(tour, val => val == u);
                idxV = Array.FindIndex(tour, val => val == v);
                var tmp = tour.ToList();
                tmp.RemoveAt(idxU);
                idxV = tmp.IndexOf(v); // idxV may shift
                tmp.Insert(idxV + 1, u);

                for (int i = 0; i < tmp.Count; i++)
                {
                    int a = tmp[i];
                    int b = tmp[(i+1) % tmp.Count];
                    if ((a == u && b == v) || (a == v && b == u))
                    {
                        tour = tmp.ToArray();
                        repaired = true;
                        break;
                    }
                }
            }

            if (!repaired)
            {
                // couldn't repair this edge with our simple moves
                return null;
            }
        }

        // Ensure no previously repaired edge was invalidated by a later move.
        if (!_mustHave.All(e => IsAdjacent(tour, e.Item1, e.Item2)))
        {
            return null;
        }

        return new TspSolution(tour.ToArray());
    }

    public string Description
    {
        get
        {
            var edges = _directed
                ? string.Join(", ", _mustHave.Select(e => $"{e.Item1}->{e.Item2}"))
                : string.Join(", ", _mustHave.Select(e => $"{e.Item1}-{e.Item2}"));
            return $"TSP fixed edge(s): {edges}";
        }
    }

    public string TextVisualization
    {
        get
        {
            var edgesText = _directed
                ? $"Must contain directed edges: {string.Join(", ", _mustHave.Select(e => $"{e.Item1}->{e.Item2}"))}"
                : $"Must contain undirected adjacencies: {string.Join(", ", _mustHave.Select(e => $"{e.Item1}-{e.Item2}"))}";

            var sb = new StringBuilder();
            sb.AppendLine("Fixed-Edge Constraint");
            sb.AppendLine($"Required edges: {edgesText}");
            sb.AppendLine(_directed
                ? "Description: Each required tuple (u, v) mandates that node v immediately follows node u in the tour."
                : "Description: Each required tuple (u, v) enforces adjacency in either direction.");
            sb.AppendLine("Notes:");
            sb.AppendLine(_directed
                ? "  - Directed mode: u->v means v must be successor of u."
                : "  - Undirected mode: u-v accepts u->v or v->u.");
            sb.AppendLine("  - Wrap-around edge (last->first) also counts.");
            sb.AppendLine("  - Repair inserts missing edges by local tour rearrangement when possible.");
            return sb.ToString().TrimEnd();
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;

    private bool IsAdjacent(int[] tour, int u, int v)
    {
        var n = tour.Length;
        for (int i = 0; i < n; i++)
        {
            int a = tour[i];
            int b = tour[(i + 1) % n];
            if (_directed)
            {
                if (a == u && b == v) return true; // only u->v
            }
            else
            {
                if ((a == u && b == v) || (a == v && b == u)) return true; // any order
            }
        }
        return false;
    }
}
