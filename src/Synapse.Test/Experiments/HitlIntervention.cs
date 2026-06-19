using Synapse.JobManagement;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Test.Experiments;

public sealed class HitlInterventionRunner : IDisposable
{
    private readonly IJobManager _jobManager;
    private readonly object _sync = new();
    private readonly List<ScheduledIntervention> _scheduled = new();
    private readonly EventHandler<(Guid JobId, Synapse.OptimizationCore.Common.ProgressEventArgs Progress)> _onProgress;

    public HitlInterventionRunner(IJobManager jobManager)
    {
        _jobManager = jobManager;
        _onProgress = (_, args) =>
        {
            ApplyDueInterventions(args.JobId, args.Progress.Iteration);
        };

        _jobManager.JobProgress += _onProgress;
    }

    public JobInterventionPlan For(Guid jobId) => new(this, jobId);

    public void AddConstraintIntervention(Guid jobId, int iteration, IConstraint constraint)
    {
        if (iteration < 0) throw new ArgumentOutOfRangeException(nameof(iteration));
        ArgumentNullException.ThrowIfNull(constraint);

        lock (_sync)
        {
            _scheduled.Add(ScheduledIntervention.Constraint(jobId, iteration, constraint));
        }
    }

    public void AddManualEditIntervention(
        Guid jobId,
        int iteration,
        ISolution solution,
        IManualEditApplier manualEditApplier,
        double probability = 1.0,
        int executeForNrOfIterations = 0,
        string? name = null)
    {
        if (iteration < 0) throw new ArgumentOutOfRangeException(nameof(iteration));
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(manualEditApplier);

        lock (_sync)
        {
            _scheduled.Add(ScheduledIntervention.ManualEdit(
                jobId,
                iteration,
                solution,
                manualEditApplier,
                probability,
                executeForNrOfIterations,
                name));
        }
    }

    // Interventions are executed once when progress reaches or passes the target iteration.
    private void ApplyDueInterventions(Guid jobId, int currentIteration)
    {
        List<ScheduledIntervention> due;
        lock (_sync)
        {
            due = _scheduled
                .Where(x => !x.Executed && x.JobId == jobId && currentIteration >= x.Iteration)
                .ToList();
        }

        if (due.Count == 0) return;

        var hitlController = _jobManager.GetJobInfo(jobId)?.Config.HitlController;
        if (hitlController is null) return;

        foreach (var intervention in due)
        {
            intervention.Apply(hitlController);
            intervention.Executed = true;
        }
    }

    public void Dispose()
    {
        _jobManager.JobProgress -= _onProgress;
    }
}

public sealed class JobInterventionPlan
{
    private readonly HitlInterventionRunner _runner;
    private readonly Guid _jobId;

    internal JobInterventionPlan(HitlInterventionRunner runner, Guid jobId)
    {
        _runner = runner;
        _jobId = jobId;
    }

    public JobInterventionPlan AddIntervention(int iteration, IConstraint constraint)
    {
        _runner.AddConstraintIntervention(_jobId, iteration, constraint);
        return this;
    }

    public JobInterventionPlan AddIntervention(
        int iteration,
        ISolution solution,
        IManualEditApplier manualEditApplier,
        double probability = 1.0,
        int executeForNrOfIterations = 0,
        string? name = null)
    {
        _runner.AddManualEditIntervention(
            _jobId,
            iteration,
            solution,
            manualEditApplier,
            probability,
            executeForNrOfIterations,
            name);

        return this;
    }
}

internal sealed class ScheduledIntervention
{
    private readonly Action<IHitlController> _apply;

    private ScheduledIntervention(Guid jobId, int iteration, Action<IHitlController> apply)
    {
        JobId = jobId;
        Iteration = iteration;
        _apply = apply;
    }

    public Guid JobId { get; }
    public int Iteration { get; }
    public bool Executed { get; set; }

    public void Apply(IHitlController hitlController) => _apply(hitlController);

    public static ScheduledIntervention Constraint(Guid jobId, int iteration, IConstraint constraint)
        => new(jobId, iteration, hitl => hitl.AddConstraint(constraint));

    public static ScheduledIntervention ManualEdit(
        Guid jobId,
        int iteration,
        ISolution solution,
        IManualEditApplier manualEditApplier,
        double probability,
        int executeForNrOfIterations,
        string? name)
        => new(jobId, iteration, hitl =>
            hitl.AddManualEdit(solution, manualEditApplier, probability, executeForNrOfIterations, name));
}
