using Synapse.HITL.Problems.QAP;
using Synapse.Problems.QAP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class QapPairwiseAssignmentConstraintTests
{
    [Test]
    public void IsSatisfied_ReturnsTrue_WhenFacilitiesAreWithinMaxDistance()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1);
        var solution = new QapSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenFacilitiesExceedMaxDistance()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1);
        var solution = new QapSolution([0, 3, 1, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_Throws_ForNonQapSolution()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1);

        Assert.That(() => constraint.IsSatisfied(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1)
        {
            RepairSolution = false
        };
        var original = new QapSolution([0, 3, 1, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1)
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_FixesDistanceViolation_AndPreservesPermutation()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1)
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 3, 1, 2]);

        var repaired = constraint.Repair(original) as QapSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(repaired, Is.Not.SameAs(original));
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_WhenEnabled_ReturnsNull_WhenNoSingleSwapCanSatisfy()
    {
        var problem = CreateLinearDistanceProblem(2);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 0)
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 1]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Repair_WhenEnabled_Throws_ForNonQapSolution()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 0, facilityB: 1, maxDistance: 1)
        {
            RepairSolution = true
        };

        Assert.That(() => constraint.Repair(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Description_AndTextVisualization_ContainConfiguredValues()
    {
        var problem = CreateLinearDistanceProblem(4);
        var constraint = new QapPairwiseAssignmentConstraint(problem, facilityA: 5, facilityB: 9, maxDistance: 4.5);

        Assert.That(constraint.Description, Does.Contain("facilities 5 & 9"));
        Assert.That(constraint.Description, Does.Contain("4.5"));
        Assert.That(constraint.TextVisualization, Does.Contain("Pairwise Assignment Constraint"));
        Assert.That(constraint.TextVisualization, Does.Contain("Facility A: 5"));
        Assert.That(constraint.TextVisualization, Does.Contain("Facility B: 9"));
    }

    private static QapProblem CreateLinearDistanceProblem(int n)
    {
        var distance = new int[n, n];
        var flow = new int[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                distance[i, j] = Math.Abs(i - j);
                flow[i, j] = 0;
            }
        }

        return new QapProblem(n, distance, flow);
    }
}