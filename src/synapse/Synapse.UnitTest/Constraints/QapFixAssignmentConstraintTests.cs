using Synapse.HITL.Problems.QAP;
using Synapse.Problems.QAP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class QapFixAssignmentConstraintTests
{
    [Test]
    public void IsSatisfied_ReturnsTrue_WhenFacilityIsAtRequiredLocation()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 0);
        var solution = new QapSolution([1, 3, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenFacilityIsAtDifferentLocation()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 1);
        var solution = new QapSolution([1, 3, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_Throws_ForNonQapSolution()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 0, location: 0);

        Assert.That(() => constraint.IsSatisfied(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 1)
        {
            RepairSolution = false
        };
        var original = new QapSolution([1, 3, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 0)
        {
            RepairSolution = true
        };
        var original = new QapSolution([1, 3, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_FixesAssignment_AndPreservesPermutation()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 1)
        {
            RepairSolution = true
        };
        var original = new QapSolution([1, 3, 0, 2]);

        var repaired = constraint.Repair(original) as QapSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(repaired, Is.Not.SameAs(original));
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_WhenEnabled_ReturnsNull_ForOutOfRangeLocation()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 2, location: 7)
        {
            RepairSolution = true
        };
        var original = new QapSolution([1, 3, 0, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Repair_WhenEnabled_Throws_ForNonQapSolution()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 0, location: 0)
        {
            RepairSolution = true
        };

        Assert.That(() => constraint.Repair(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Description_AndTextVisualization_ContainConfiguredValues()
    {
        var constraint = new QapFixAssignmentConstraint(facility: 5, location: 9);

        Assert.That(constraint.Description, Does.Contain("facility 5"));
        Assert.That(constraint.Description, Does.Contain("location 9"));
        Assert.That(constraint.TextVisualization, Does.Contain("QAP Fixed Assignment Constraint"));
        Assert.That(constraint.TextVisualization, Does.Contain("Facility: 5"));
        Assert.That(constraint.TextVisualization, Does.Contain("Required location: 9"));
    }
}