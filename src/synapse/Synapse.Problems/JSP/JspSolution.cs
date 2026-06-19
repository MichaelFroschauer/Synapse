using System.Text;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Problems.JSP;

public class JspSolution : PermutationSolution
{
    public int NumJobs { get; private set; }
    public int NumMachines { get; private set; }
    
    public JspSolution(int numJobs, int numMachines) : base(numJobs * numMachines)
    {
        if (numJobs <= 0) throw new ArgumentException(nameof(numJobs));
        if (numMachines <= 0) throw new ArgumentException(nameof(numMachines));
        
        NumJobs = numJobs;
        NumMachines = numMachines;
    }

    public JspSolution(Parameter[] parameters) : base(parameters)
    {
        InferDimensionsFromLength(parameters?.Length ?? 0);
    }

    public JspSolution(int[] parameters) : base(parameters)
    {
        InferDimensionsFromLength(parameters?.Length ?? 0);
    }

    public override ISolution Create(int[] parameters) => new JspSolution(parameters);
    public override ISolution CreateRandom()
    {
        int[] unique = RandomProvider.Value.GetUniqueInts(Length, 0, Length);
        return new JspSolution(unique);
    }
    
    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() => new JspSolutionSimilarity();

    public void SetProblemProperties(JspProblem problem)
    {
        NumJobs = problem.NumJobs;
        NumMachines = problem.NumMachines;
    }
    
    public override string ToString()
    {
        // permutation (operation ids)
        int[] perm = GetParametersWithType();

        // basic header
        var sb = new StringBuilder();
        sb.AppendLine($"JspSolution: Length={Length}, NumJobs={(NumJobs>0?NumJobs.ToString():"unknown")}, NumMachines={(NumMachines>0?NumMachines.ToString():"unknown")}");

        // show permutation preview
        sb.AppendLine("Permutation (opId order) preview:");
        sb.AppendLine("  " + TruncateList(perm, 80));

        // build position map: pos[opId] = index in permutation
        int total = perm.Length;
        var pos = new int[total];
        for (int i = 0; i < perm.Length; ++i)
        {
            int opId = perm[i];
            if ((uint)opId >= (uint)total) pos[opId % total] = i; // shouldn't happen
            else pos[opId] = i;
        }

        // If we know NumJobs/NumMachines -> print per-job op positions
        if (NumJobs > 0 && NumMachines > 0 && NumJobs * NumMachines == total)
        {
            sb.AppendLine("Per-job operation positions (format: opIdx:opId@pos):");
            for (int j = 0; j < NumJobs; ++j)
            {
                sb.Append($"  Job {j}: ");
                var parts = new string[NumMachines];
                for (int k = 0; k < NumMachines; ++k)
                {
                    int opId = j * NumMachines + k;
                    int p = pos[opId];
                    parts[k] = $"{k}:{opId}@{p}";
                }
                sb.AppendLine(string.Join(", ", parts));
            }

            // additionally, print for each job the order of opIdx by permutation position (helps spot job-order violations)
            sb.AppendLine(" Per-job ops sorted by their permutation position (opIdx@pos):");
            for (int j = 0; j < NumJobs; ++j)
            {
                var opEntries = Enumerable.Range(0, NumMachines)
                                          .Select(k => (opIdx: k, opId: j * NumMachines + k, position: pos[j * NumMachines + k]))
                                          .OrderBy(x => x.position)
                                          .Select(x => $"{x.opIdx}@{x.position}");
                sb.AppendLine($"  Job {j}: {string.Join(", ", opEntries)}");
            }
        }
        else
        {
            // throw new NotSupportedException(
            //     $"Could not infer {nameof(NumJobs)} and {nameof(NumMachines)} to create string of  {nameof(JspSolution)}." +
            //     $"Set {nameof(SetProblemProperties)}({nameof(JspProblem)}) before calling {nameof(ToString)}.");
            
            // fallback: we can still list job/opIdx pairs if we assume some numMachines (best-effort)
            sb.AppendLine("Per-opId decode (job,opIdx @ pos):");
            int guessedNumMachines = -1;
            // try sqrt as guess
            int r = (int)Math.Round(Math.Sqrt(total));
            if (r * r == total) guessedNumMachines = r;
            
            if (guessedNumMachines > 0)
            {
                for (int i = 0; i < Math.Min(total, 200); ++i) // limit output
                {
                    int opId = perm[i];
                    int job = opId / guessedNumMachines;
                    int opIdx = opId % guessedNumMachines;
                    sb.AppendLine($"  perm[{i}] = opId {opId} -> job {job}, opIdx {opIdx}, pos {pos[opId]}");
                }
                if (total > 200) sb.AppendLine($"  ... ({total - 200} more operations)");
            }
            else
            {
                // minimal fallback: show first 200 opId@pos pairs
                for (int i = 0; i < Math.Min(total, 200); ++i)
                {
                    int opId = perm[i];
                    sb.AppendLine($"  perm[{i}] = opId {opId} @ pos {pos[opId]}");
                }
                if (total > 200) sb.AppendLine($"  ... ({total - 200} more operations)");
            }
        }

        return sb.ToString();
    }
    
    private static string TruncateList(int[] arr, int maxItems = 40)
    {
        if (arr == null) return "null";
        if (arr.Length <= maxItems) return string.Join(",", arr);
        return string.Join(",", arr.Take(maxItems)) + $",...(+{arr.Length - maxItems} more)";
    }
    
    private void InferDimensionsFromLength(int length)
    {
        if (length <= 0)
        {
            NumJobs = -1;
            NumMachines = -1;
            return;
        }
        
        int r = (int)Math.Round(Math.Sqrt(length));
        if (r * r == length)
        {
            NumJobs = r;
            NumMachines = r;
        }
        else
        {
            NumJobs = -1;
            NumMachines = -1;
        }
    }
}
