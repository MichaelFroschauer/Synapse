using Synapse.HITL.Problems.JSP;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.JSP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class JobFinishRankConstraintTests
{
    [SetUp]
    public void Setup()
    {
        RandomProvider.SetSeed(12345);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenJobFinishesAtDesiredPlace()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 0, desiredPlace: 2);
        var solution = new JspSolution([1, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenJobFinishesAtDifferentPlace()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 0, desiredPlace: 1);
        var solution = new JspSolution([1, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_UsesJobIdAsTieBreaker_WhenFinishTimesAreEqual()
    {
        var problem = CreateSingleMachineZeroDurationProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 0, desiredPlace: 1);
        var solution = new JspSolution([2, 1, 0]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_ForNullAndNonPermutationInputs()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 1, desiredPlace: 2);

        Assert.That(constraint.IsSatisfied(null!), Is.False);
        Assert.That(constraint.IsSatisfied(new DummySolution()), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenPermutationCannotBeDecoded()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 1, desiredPlace: 2);
        var invalid = new JspSolution([0, 0, 1]);

        Assert.That(constraint.IsSatisfied(invalid), Is.False);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 2, desiredPlace: 3)
        {
            RepairSolution = false
        };
        var original = new JspSolution([2, 0, 1]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 2, desiredPlace: 1)
        {
            RepairSolution = true
        };
        var original = new JspSolution([2, 0, 1]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_ReturnsNull_ForNullNonPermutationAndDecodeFailure()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 1, desiredPlace: 2)
        {
            RepairSolution = true
        };
        var invalid = new JspSolution([0, 0, 1]);

        Assert.That(constraint.Repair(null!), Is.Null);
        Assert.That(constraint.Repair(new DummySolution()), Is.Null);
        Assert.That(constraint.Repair(invalid), Is.Null);
    }

    [Test]
    public void Repair_WhenEnabled_CanMoveJobLaterToDesiredPlace()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 2, desiredPlace: 3)
        {
            RepairSolution = true
        };
        var original = new JspSolution([2, 0, 1]);

        var repaired = constraint.Repair(original) as JspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
    }

    [Test]
    public void Repair_WhenEnabled_CanMoveJobEarlierToDesiredPlace()
    {
        var problem = CreateSingleMachineProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 1, desiredPlace: 2)
        {
            RepairSolution = true
        };
        var original = new JspSolution([0, 2, 1]);

        var repaired = constraint.Repair(original) as JspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
    }

    [Test]
    public void Repair_WhenTargetRankCannotBeReached_ReturnsNull_InsteadOfUnsatisfiedSolution()
    {
        var problem = CreateSingleMachineZeroDurationProblem();
        var constraint = new JobFinishRankConstraint(problem, jobId: 2, desiredPlace: 1)
        {
            RepairSolution = true
        };
        var original = new JspSolution([0, 1, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
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
        name: "single-machine-test");
    }

    private static JspProblem CreateSingleMachineZeroDurationProblem()
    {
        return new JspProblem(
        [
            new Job([new Operation(0, 0)]),
            new Job([new Operation(0, 0)]),
            new Job([new Operation(0, 0)])
        ],
        machinesCount: 1,
        name: "single-machine-zero-duration-test");
    }
}