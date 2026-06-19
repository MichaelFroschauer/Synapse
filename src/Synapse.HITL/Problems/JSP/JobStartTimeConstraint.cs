using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.JSP;

namespace Synapse.HITL.Problems.JSP;

/// <summary>
/// Hard constraint: ensure that the first operation (or a specific opIdx) of a job
/// starts at DesiredStart (within Tolerance).
/// Repair() will attempt to shift the job's operations and re-encode a permutation by
/// sorting operations by Start (then Machine/Finish/OpId). Returns null if it cannot repair.
/// </summary>
[DisplayInfo("Job Operation Start Time Constraint",
    "Ensures that a specific operation of a job starts at a desired time (within an optional tolerance). "
    + "During repair, the job's operations are shifted in time and the schedule is re-encoded to satisfy the target start time.")]
[ApplicableSolution(typeof(JspSolution))]
public class JobStartTimeConstraint : IConstraint
{
    private readonly JspProblem _problem;
    public int JobId { get; }
    public int OpIdxToCheck { get; } = 0;
    public long DesiredStart { get; }
    public long Tolerance { get; } = 0;
    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
    public string Description => $"Job #{JobId} must start at {DesiredStart} (opIdx {OpIdxToCheck})";

    public string TextVisualization =>
        $@"Job Start-Time Constraint
Job: {JobId}
Operation index: {OpIdxToCheck}
Desired start time: {DesiredStart}
Tolerance: {Tolerance}

Description: The chosen operation of the job should start at the desired time (within tolerance).
Repair behavior: If enabled, operations of the selected job are shifted and the schedule is re-encoded.";

    public JobStartTimeConstraint(
        JspProblem problem,
        [DisplayInfo("Job ID")] int jobId,
        [DisplayInfo("Desired Start Time")] long desiredStart,
        [DisplayInfo("Operation Index")] int opIdxToCheck = 0,
        [DisplayInfo("Time Tolerance")] long tolerance = 0)
    {
        _problem = problem ?? throw new ArgumentNullException(nameof(problem));
        if (jobId < 0 || jobId >= problem.NumJobs) throw new ArgumentOutOfRangeException(nameof(jobId));
        if (opIdxToCheck < 0 || opIdxToCheck >= problem.NumMachines)
            throw new ArgumentOutOfRangeException(nameof(opIdxToCheck));
        JobId = jobId;
        DesiredStart = desiredStart;
        OpIdxToCheck = opIdxToCheck;
        Tolerance = tolerance;
    }

    public bool IsSatisfied(ISolution solution)
    {
        if (solution == null) return false;
        if (solution is not PermutationSolution permSol) return false;
        List<OpSchedule> schedule;
        try
        {
            schedule = JspFitnessEvaluator.DecodeSchedule(_problem, (PermutationSolution)permSol);
        }
        catch
        {
            // cannot decode -> constraint not satisfied
            return false;
        }

        var op = schedule.FirstOrDefault(x => x.Job == JobId && x.OpIdx == OpIdxToCheck);
        if (op == null) return false;
        long diff = Math.Abs(op.Start - DesiredStart);
        return diff <= Tolerance;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution == null) return null;
        if (solution is not PermutationSolution permSol) return null;
        List<OpSchedule> schedule;
        try
        {
            schedule = JspFitnessEvaluator.DecodeSchedule(_problem, (PermutationSolution)permSol);
        }
        catch
        {
            return null;
        }

        var target = schedule.FirstOrDefault(x => x.Job == JobId && x.OpIdx == OpIdxToCheck);
        if (target == null) return null;
        long currentStart = target.Start;
        long delta = DesiredStart - currentStart;
        if (Math.Abs(delta) <= Tolerance)
        {
            // already satisfied
            return solution;
        }

        // Create a copy of the schedule and shift all operations of the job by delta
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

        // Encode new permutation by sorting by Start, then Machine, then Finish, then OpId
        int[] newPerm = EncodePermutationFromSchedules(modified);

        // Try to create a new solution. If original is JspSolution, use its concrete ctor.
        if (solution is JspSolution)
        {
            try
            {
                var js = new JspSolution(newPerm);
                // preserve inferred dimensions if original had them set (optional)
                if (((JspSolution)solution).NumJobs > 0 && ((JspSolution)solution).NumMachines > 0)
                {
                    // The JspSolution ctor(newPerm) already sets params; nothing else required.
                }

                return IsSatisfied(js) ? js : null;
            }
            catch
            {
                // fallback to reflection approach below
            }
        }

        // Fallback: try to construct via an int[] constructor on the concrete solution type
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
                // ignore and fallthrough to final null
            }
        }

        // Could not produce a repaired solution programmatically
        return null;
    }

    // local encoder (Start ascending, Machine, Finish, OpId)
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