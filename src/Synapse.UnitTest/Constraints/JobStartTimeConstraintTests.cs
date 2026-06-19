using Synapse.HITL.Problems.JSP;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.JSP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class JobStartTimeConstraintTests
{
    [SetUp]
    public void Setup()
    {
        RandomProvider.SetSeed(12345);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenOperationStartsAtDesiredTime()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 2);
        var solution = new JspSolution([1, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenWithinTolerance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 1, tolerance: 1);
        var solution = new JspSolution([1, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenOutsideTolerance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 0, tolerance: 1);
        var solution = new JspSolution([1, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_ForNullNonPermutationAndDecodeFailure()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 0);
        var invalid = new JspSolution([0, 0, 1]);

        Assert.That(constraint.IsSatisfied(null!), Is.False);
        Assert.That(constraint.IsSatisfied(new DummySolution()), Is.False);
        Assert.That(constraint.IsSatisfied(invalid), Is.False);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 0)
        {
            RepairSolution = false
        };
        var original = new JspSolution([1, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 2)
        {
            RepairSolution = true
        };
        var original = new JspSolution([1, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_ReturnsNull_ForNullNonPermutationAndDecodeFailure()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 0, desiredStart: 1)
        {
            RepairSolution = true
        };
        var invalid = new JspSolution([0, 0, 1]);

        Assert.That(constraint.Repair(null!), Is.Null);
        Assert.That(constraint.Repair(new DummySolution()), Is.Null);
        Assert.That(constraint.Repair(invalid), Is.Null);
    }

    [Test]
    public void Repair_WhenEnabled_CanMoveJobLaterToDesiredStart()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 2, desiredStart: 7)
        {
            RepairSolution = true
        };
        var original = new JspSolution([2, 1, 0]);

        var repaired = constraint.Repair(original) as JspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
    }

    [Test]
    public void Repair_WhenDesiredStartIsUnreachable_ReturnsNull_InsteadOfUnsatisfiedSolution()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 1, desiredStart: -1)
        {
            RepairSolution = true
        };
        var original = new JspSolution([1, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Description_AndTextVisualization_ContainConfiguredValues()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobStartTimeConstraint(problem, jobId: 2, desiredStart: 11, opIdxToCheck: 0, tolerance: 3);

        Assert.That(constraint.Description, Does.Contain("Job #2"));
        Assert.That(constraint.Description, Does.Contain("11"));
        Assert.That(constraint.TextVisualization, Does.Contain("Job Start-Time Constraint"));
        Assert.That(constraint.TextVisualization, Does.Contain("Desired start time: 11"));
        Assert.That(constraint.TextVisualization, Does.Contain("Tolerance: 3"));
    }

    private static JspProblem CreateSingleMachineProblem()
    {
        return new JspProblem(
        [
            new Job([new Operation(0, 5)]),
            new Job([new Operation(0, 2)]),
            new Job([new Operation(0, 3)])
        ],
        machinesCount: 1,
        name: "job-start-time-test");
    }
}