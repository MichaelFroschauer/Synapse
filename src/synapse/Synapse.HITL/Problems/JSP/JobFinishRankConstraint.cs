using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.JSP;

namespace Synapse.HITL.Problems.JSP;

/// <summary>
/// Hard constraint: ensure that a job finishes at a given place among all jobs
/// (1 = finished first, 2 = finished second, ...).
/// Repair() is heuristic: it shifts all operations of the job in time and re-encodes a permutation.
/// </summary>
[DisplayInfo("Job Finish Rank Constraint",
    "Ensures that a job finishes at a specific rank among all jobs (e.g., 1 = finishes first, 2 = finishes second). "
    + "During repair, the job's operations are shifted in time so the job reaches the desired finishing position in the schedule.")]
[ApplicableSolution(typeof(JspSolution))]
public class JobFinishRankConstraint : IConstraint
{
    private readonly JspProblem _problem;
    public int JobId { get; }

    /// <summary>1-based desired place (1 = first finished)</summary>
    public int DesiredPlace { get; }

    /// <summary>if >0 allows finishing within +/- tolerance time compared to strict ranking (not used for ranking but can be used for future extension)</summary>
    public long Tolerance { get; } = 0;

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
    public string Description => $"Job #{JobId} should finish at place {DesiredPlace} among all jobs";
    public string TextVisualization =>
        $@"Job Finish-Rank Constraint
Job: {JobId}
Desired finish rank: {DesiredPlace}
Tolerance: {Tolerance}

Description: The selected job should complete in the specified rank among all jobs.
Repair behavior: If enabled, operations of the selected job are shifted and the permutation is rebuilt.";

    public JobFinishRankConstraint(JspProblem problem,
        [DisplayInfo("Job ID")] int jobId,
        [DisplayInfo("Desired Finish")] int desiredPlace,
        [DisplayInfo("Time Tolerance")] long tolerance = 0)
    {
        _problem = problem ?? throw new ArgumentNullException(nameof(problem));
        if (jobId < 0 || jobId >= problem.NumJobs) throw new ArgumentOutOfRangeException(nameof(jobId));
        if (desiredPlace < 1 || desiredPlace > problem.NumJobs)
            throw new ArgumentOutOfRangeException(nameof(desiredPlace));
        JobId = jobId;
        DesiredPlace = desiredPlace;
        Tolerance = tolerance;
    }

    public bool IsSatisfied(ISolution solution)
    {
        if (solution == null) return false;
        if (solution is not PermutationSolution permSol) return false;
        List<OpSchedule> schedule;
        try
        {
            schedule = JspFitnessEvaluator.DecodeSchedule(_problem, permSol);
        }
        catch
        {
            return false;
        }

        // compute finish time per job (finish = max finish of ops of that job — normally last op)
        var finishes = JobFinishes(schedule, _problem.NumJobs);

        // sort by finish asc, tie-break by job id to get deterministic ranking
        var sorted = finishes.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).ToList();
        int actualIndex = sorted.FindIndex(kv => kv.Key == JobId);
        if (actualIndex < 0) return false;
        int actualPlace = actualIndex + 1; // 1-based
        return actualPlace == DesiredPlace;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution == null) return null;
        if (solution is not PermutationSolution permSol) return null;
        List<OpSchedule> schedule;
        try
        {
            schedule = JspFitnessEvaluator.DecodeSchedule(_problem, permSol);
        }
        catch
        {
            return null;
        }

        int numJobs = _problem.NumJobs;

        // finish times per job
        var finishes = JobFinishes(schedule, numJobs);

        // produce sorted list of job finishes (ascending)
        var sorted = finishes.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).ToList();
        int currentIndex = sorted.FindIndex(kv => kv.Key == JobId);
        if (currentIndex < 0) return null;
        int currentPlace = currentIndex + 1;
        if (currentPlace == DesiredPlace)
        {
            // already satisfied
            return solution;
        }

        // determine the "target" finish threshold we want to be before/after
        // if we need to move earlier (currentPlace > DesiredPlace), targetFinish = finish of job currently at DesiredPlace - 1
        // if we need to move later (currentPlace < DesiredPlace), targetFinish = finish of job currently at DesiredPlace + 1
        long targetFinish;
        int desiredIndex = DesiredPlace - 1;
        long currentFinish = finishes[JobId];
        if (currentPlace > DesiredPlace)
        {
            // move earlier -> get finish of the job currently at desiredIndex and aim to be strictly before it
            long refFinish = sorted[desiredIndex].Value;
            targetFinish = refFinish - 1;
            if (targetFinish < 0) targetFinish = 0;
        }
        else
        {
            // move later -> aim to be strictly after the job currently at desiredIndex
            long refFinish = sorted[desiredIndex].Value;
            targetFinish = refFinish + 1;
        }

        long delta = targetFinish - currentFinish;
        if (delta == 0)
        {
            // borderline: if equal, nudge by 1 depending on desired direction
            delta = (currentPlace > DesiredPlace) ? -1 : 1;
        }

        // shift all operations of the job by delta (start/finish)
        var modified = schedule.Select(s => new OpSchedule
        {
            OpId = s.OpId,
            Job = s.Job,
            OpIdx = s.OpIdx,
            Machine = s.Machine,
            ProcessingTime = s.ProcessingTime,
            Start = s.Start,
            Finish = s.Finish
        }).ToList();
        foreach (var s in modified.Where(x => x.Job == JobId))
        {
            s.Start += delta;
            if (s.Start < 0) s.Start = 0;
            s.Finish = s.Start + s.ProcessingTime;
        }

        // re-encode permutation: sort ops by Start asc, break ties by Machine, Finish, OpId
        int[] newPerm = EncodePermutationFromSchedules(modified);

        // try to build concrete solution: prefer JspSolution
        if (solution is JspSolution)
        {
            try
            {
                var repaired = new JspSolution(newPerm);
                return IsSatisfied(repaired) ? repaired : null;
            }
            catch
            {
                // fallback to reflection below
            }
        }

        // try int[] ctor on solution type
        var solType = solution.GetType();
        var ctor = solType.GetConstructor(new[] { typeof(int[]) });
        if (ctor != null)
        {
            try
            {
                var inst = ctor.Invoke(new object[] { newPerm }) as ISolution;
                if (inst != null) return IsSatisfied(inst) ? inst : null;
            }
            catch
            {
                /* ignore */
            }
        }

        // unable to craft repaired solution
        return null;
    }

    // Helper: compute finish (max finish of ops) per job
    private static Dictionary<int, long> JobFinishes(IList<OpSchedule> schedule, int numJobs)
    {
        var dict = new Dictionary<int, long>(numJobs);
        foreach (var s in schedule)
        {
            if (!dict.TryGetValue(s.Job, out var f) || s.Finish > f) dict[s.Job] = s.Finish;
        }

        // ensure all jobs exist (if some missing, set finish 0)
        for (int j = 0; j < numJobs; ++j)
            if (!dict.ContainsKey(j))
                dict[j] = 0;
        return dict;
    }

    // local encoder (same strategy used earlier)
    private static int[] EncodePermutationFromSchedules(IList<OpSchedule> schedules)
    {
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
