using Synapse.HITL.Problems.QAP;
using Synapse.Problems.QAP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class QapForbiddenPairConstraintTests
{
    [Test]
    public void IsSatisfied_ReturnsTrue_WhenFacilitiesAreNotAdjacent()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1);
        var solution = new QapSolution([0, 2, 3, 1]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenFacilitiesAreAdjacent()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1);
        var solution = new QapSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_Throws_ForNonQapSolution()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1);

        Assert.That(() => constraint.IsSatisfied(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1)
        {
            RepairSolution = false
        };
        var original = new QapSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1)
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 2, 3, 1]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_BreaksAdjacency_AndPreservesPermutation()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1)
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original) as QapSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(repaired, Is.Not.SameAs(original));
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_WhenEnabled_ReturnsNull_WhenNoSwapCanBreakAdjacency()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1)
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
        var constraint = new QapForbiddenPairConstraint(facilityA: 0, facilityB: 1)
        {
            RepairSolution = true
        };

        Assert.That(() => constraint.Repair(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Description_AndTextVisualization_ContainConfiguredValues()
    {
        var constraint = new QapForbiddenPairConstraint(facilityA: 5, facilityB: 9);

        Assert.That(constraint.Description, Does.Contain("facilities 5 and 9"));
        Assert.That(constraint.TextVisualization, Does.Contain("QAP Forbidden Pair Constraint"));
        Assert.That(constraint.TextVisualization, Does.Contain("Facility A: 5"));
        Assert.That(constraint.TextVisualization, Does.Contain("Facility B: 9"));
    }
}