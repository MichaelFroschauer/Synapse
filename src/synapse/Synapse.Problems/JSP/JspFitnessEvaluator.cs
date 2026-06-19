using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.JSP
{
    public class JspFitnessEvaluator : IFitnessEvaluator
    {
        private readonly JspProblem _problem;
        private readonly ThreadLocal<ThreadBuffers> _buffers;

        public JspFitnessEvaluator(JspProblem problem)
        {
            _problem = problem ?? throw new ArgumentNullException(nameof(problem));
            _buffers = new ThreadLocal<ThreadBuffers>(() => new ThreadBuffers());
        }

        private class ThreadBuffers
        {
            public int[] Perm => _perm ??= Array.Empty<int>();
            private int[]? _perm;
            public int[] Indegree => _indegree ??= Array.Empty<int>();
            private int[]? _indegree;
            public long[] EarliestStart => _earliestStart ??= Array.Empty<long>();
            private long[]? _earliestStart;
            public int[] ProcessingTime => _processingTime ??= Array.Empty<int>();
            private int[]? _processingTime;

            // machine successor per node (-1 if none)
            public int[] SuccMachine => _succMachine ??= Array.Empty<int>();
            private int[]? _succMachine;

            // per-machine fixed-size arrays storing operation ids in order (size numJobs)
            public int[][] MachineQueues => _machineQueues ??= Array.Empty<int[]>();
            private int[][]? _machineQueues;

            // current fill pointers for each machine
            public int[] MachinePointers => _machinePointers ??= Array.Empty<int>();
            private int[]? _machinePointers;

            // Kahn queue
            public int[] KahnQueue => _kahnQueue ??= Array.Empty<int>();
            private int[]? _kahnQueue;

            // positions of operations in permutation
            public int[] Positions => _positions ??= Array.Empty<int>();
            private int[]? _positions;

            public void EnsureCapacity(int totalOps, int numMachines, int numJobs)
            {
                if (_perm == null || _perm.Length < totalOps) _perm = new int[totalOps];
                if (_indegree == null || _indegree.Length < totalOps) _indegree = new int[totalOps];
                if (_earliestStart == null || _earliestStart.Length < totalOps) _earliestStart = new long[totalOps];
                if (_processingTime == null || _processingTime.Length < totalOps) _processingTime = new int[totalOps];
                if (_succMachine == null || _succMachine.Length < totalOps) _succMachine = new int[totalOps];
                if (_kahnQueue == null || _kahnQueue.Length < totalOps) _kahnQueue = new int[totalOps];
                if (_positions == null || _positions.Length < totalOps) _positions = new int[totalOps];
                if (_machineQueues == null || _machineQueues.Length < numMachines)
                {
                    _machineQueues = new int[numMachines][];
                    _machinePointers = new int[numMachines];
                }

                for (int i = 0; i < numMachines; ++i)
                {
                    if (_machineQueues[i] == null || _machineQueues[i].Length < numJobs)
                        _machineQueues[i] = new int[numJobs];
                    _machinePointers[i] = 0;
                }
            }
        }

        public double Evaluate(ISolution solution)
        {
            var permSol = solution as PermutationSolution ??
                          throw new ArgumentException($"Expecting {nameof(PermutationSolution)}");
            int numJobs = _problem.NumJobs;
            int numMachines = _problem.NumMachines;
            int totalOps = _problem.TotalOperations;
            int[] perm = permSol.GetParametersWithType();
            if (perm.Length != totalOps) throw new ArgumentException("Permutation length mismatch.");
            var b = _buffers.Value;
            b.EnsureCapacity(totalOps, numMachines, numJobs);

            // Restore original multi-pass decoding: schedule operations only when opIdx == nextOp[job].
// This accepts permutations where later ops of a job can appear before earlier ones.
            for (int m = 0; m < numMachines; ++m) b.MachinePointers[m] = 0;

// nextOp[j] = index of next operation of job j to schedule
            var nextOp = new int[numJobs];
            int scheduled = 0;
            while (scheduled < totalOps)
            {
                int progressThisPass = 0;
                for (int i = 0; i < totalOps; ++i)
                {
                    int opId = perm[i];
                    if ((uint)opId >= (uint)totalOps) throw new ArgumentException("Invalid opId in permutation.");
                    int job = opId / numMachines;
                    int opIdx = opId % numMachines;
                    if (opIdx == nextOp[job])
                    {
                        int machine = _problem.OpMachine[opId];
                        int ptr = b.MachinePointers[machine];
                        b.MachineQueues[machine][ptr] = opId;
                        b.MachinePointers[machine] = ptr + 1;
                        nextOp[job] = opIdx + 1;
                        scheduled++;
                        progressThisPass++;
                    }
                }

                if (progressThisPass == 0)
                {
                    var preview = string.Join(",", perm.Take(Math.Min(40, perm.Length)));
                    throw new InvalidOperationException(
                        $"Permutation cannot be decoded respecting job order (no progress in a pass). Scheduled={scheduled}/{totalOps}. PreviewPerm=[{preview}]");
                }
            }

            // Build indegree & succMachine
            Array.Clear(b.Indegree, 0, totalOps);
            for (int i = 0; i < totalOps; ++i) b.SuccMachine[i] = -1;

            // job precedence: op k has predecessor k-1
            for (int j = 0; j < numJobs; ++j)
            for (int k = 1; k < numMachines; ++k)
                b.Indegree[j * numMachines + k]++;

            // machine precedence
            for (int m = 0; m < numMachines; ++m)
            {
                int cnt = b.MachinePointers[m];
                for (int pos = 0; pos < cnt; ++pos)
                {
                    int node = b.MachineQueues[m][pos];
                    if (pos > 0)
                    {
                        b.Indegree[node]++;
                        int prev = b.MachineQueues[m][pos - 1];
                        b.SuccMachine[prev] = node;
                    }
                }
            }

            Array.Copy(_problem.OpProcessingTime, 0, b.ProcessingTime, 0, totalOps);

            // Kahn
            int qHead = 0, qTail = 0;
            for (int i = 0; i < totalOps; ++i)
            {
                b.EarliestStart[i] = 0L;
                if (b.Indegree[i] == 0) b.KahnQueue[qTail++] = i;
            }

            int processed = 0;
            long makespan = 0;
            while (qHead < qTail)
            {
                int node = b.KahnQueue[qHead++];
                processed++;
                long finish = b.EarliestStart[node] + (long)b.ProcessingTime[node];
                if (finish > makespan) makespan = finish;

                // job successor
                int job = node / numMachines;
                int opIdx = node - job * numMachines;
                if (opIdx + 1 < numMachines)
                {
                    int succ = job * numMachines + (opIdx + 1);
                    if (b.EarliestStart[succ] < finish) b.EarliestStart[succ] = finish;
                    if (--b.Indegree[succ] == 0) b.KahnQueue[qTail++] = succ;
                }

                // machine successor
                int succM = b.SuccMachine[node];
                if (succM != -1)
                {
                    if (b.EarliestStart[succM] < finish) b.EarliestStart[succM] = finish;
                    if (--b.Indegree[succM] == 0) b.KahnQueue[qTail++] = succM;
                }
            }

            if (processed != totalOps)
                throw new InvalidOperationException(
                    "Cycle detected in precedence graph (invalid solution) after decoding.");
            return (double)makespan;
        }

        public bool Minimize => true;

        /// <summary>
        /// Decodes a PermutationSolution into a list of OpSchedule (wie vorher) – single-pass.
        /// </summary>
        public static List<OpSchedule> DecodeSchedule(JspProblem problem, PermutationSolution sol)
        {
            int numJobs = problem.NumJobs;
            int numMachines = problem.NumMachines;
            int total = problem.TotalOperations;
            var perm = sol.GetParametersWithType();
            if (perm.Length != total) throw new ArgumentException("Permutation length mismatch");

            // ---- Multi-pass decoding (accepts permutations in which later operations come before earlier ones) ----
            var machineQueues = new List<int>[numMachines];
            for (int m = 0; m < numMachines; ++m) machineQueues[m] = new List<int>(numJobs);
            var nextOp = new int[numJobs]; // nextOp[j] == index (0..numMachines-1) of next op of job j to schedule
            int scheduled = 0;
            while (scheduled < total)
            {
                int progress = 0;
                for (int i = 0; i < total; ++i)
                {
                    int opId = perm[i];
                    if ((uint)opId >= (uint)total) throw new ArgumentException("Invalid opId in permutation.");
                    int job = opId / numMachines;
                    int opIdx = opId % numMachines;
                    if (opIdx == nextOp[job])
                    {
                        int machine = problem.OpMachine[opId];
                        machineQueues[machine].Add(opId);
                        nextOp[job] = opIdx + 1;
                        scheduled++;
                        progress++;
                    }
                }

                if (progress == 0)
                    throw new InvalidOperationException("Cannot decode permutation respecting job order (stalled).");
            }
            // ---- Ende Multi-pass decoding ----

            // build earliest starts exactly as before (Kahn)
            int[] indeg = new int[total];
            int[] machineSucc = Enumerable.Repeat(-1, total).ToArray();
            int[] processing = new int[total];
            for (int j = 0; j < numJobs; ++j)
            for (int k = 0; k < numMachines; ++k)
            {
                int nid = j * numMachines + k;
                processing[nid] = problem.OpProcessingTime[nid];
                if (k > 0) indeg[nid]++;
            }

            for (int m = 0; m < numMachines; ++m)
            {
                var q = machineQueues[m];
                for (int p = 1; p < q.Count; ++p)
                {
                    int prev = q[p - 1];
                    int cur = q[p];
                    indeg[cur]++;
                    machineSucc[prev] = cur;
                }
            }

            var earliest = new long[total];
            var queue = new Queue<int>();
            for (int i = 0; i < total; ++i)
                if (indeg[i] == 0)
                    queue.Enqueue(i);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                long finish = earliest[node] + processing[node];
                int job = node / numMachines;
                int opIdx = node % numMachines;
                if (opIdx + 1 < numMachines)
                {
                    int succ = job * numMachines + (opIdx + 1);
                    if (earliest[succ] < finish) earliest[succ] = finish;
                    if (--indeg[succ] == 0) queue.Enqueue(succ);
                }

                int ms = machineSucc[node];
                if (ms != -1)
                {
                    if (earliest[ms] < finish) earliest[ms] = finish;
                    if (--indeg[ms] == 0) queue.Enqueue(ms);
                }
            }

            var list = new List<OpSchedule>(total);
            for (int op = 0; op < total; ++op)
            {
                list.Add(new OpSchedule
                {
                    OpId = op,
                    Job = op / numMachines,
                    OpIdx = op % numMachines,
                    Machine = problem.OpMachine[op],
                    ProcessingTime = problem.OpProcessingTime[op],
                    Start = earliest[op],
                    Finish = earliest[op] + problem.OpProcessingTime[op]
                });
            }

            return list;
        }

        /// <summary>
        /// Encodes a (possibly edited) list of OpSchedule back into a permutation that decodes
        /// respecting job order. Strategy: sort by Start ascending (then by Machine, Finish, OpId)
        /// which yields positions increasing along jobs (since a job's op k must start <= op k+1's start).
        /// This returns an int[] permutation you can put into a PermutationSolution.
        /// </summary>
        public static int[] EncodePermutationFromSchedules(IList<OpSchedule> schedules)
        {
            if (schedules == null) throw new ArgumentNullException(nameof(schedules));
            var arr = schedules.ToArray();
            Array.Sort(arr, (a, b) =>
            {
                int c = a.Start.CompareTo(b.Start);
                if (c != 0) return c;
                c = a.Machine.CompareTo(b.Machine);
                if (c != 0) return c;
                c = a.Finish.CompareTo(b.Finish);
                if (c != 0) return c;
                return a.OpId.CompareTo(b.OpId);
            });
            return arr.Select(x => x.OpId).ToArray();
        }
    }

    public class OpSchedule
    {
        public int OpId { get; set; }
        public int Job { get; set; }
        public int OpIdx { get; set; }
        public int Machine { get; set; }
        public int ProcessingTime { get; set; }
        public long Start { get; set; }
        public long Finish { get; set; }
    }
}