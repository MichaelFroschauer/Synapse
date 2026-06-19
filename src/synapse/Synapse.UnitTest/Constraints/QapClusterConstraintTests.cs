using Synapse.HITL.Problems.QAP;
using Synapse.Problems.QAP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class QapClusterConstraintTests
{
    [Test]
    public void IsSatisfied_ReturnsTrue_WhenAllClusterFacilitiesUseAllowedLocations()
    {
        var constraint = new QapClusterConstraint(facilities: [1, 3], allowedLocations: [0, 2]);
        var solution = new QapSolution([1, 0, 3, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenAnyClusterFacilityUsesForbiddenLocation()
    {
        var constraint = new QapClusterConstraint(facilities: [1, 3], allowedLocations: [0, 2]);
        var solution = new QapSolution([1, 3, 0, 2]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_Throws_ForNonPermutationSolution()
    {
        var constraint = new QapClusterConstraint(facilities: [0], allowedLocations: [0]);

        Assert.That(() => constraint.IsSatisfied(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance()
    {
        var constraint = new QapClusterConstraint(facilities: [0, 1], allowedLocations: [2, 3])
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
        var constraint = new QapClusterConstraint(facilities: [0, 1], allowedLocations: [2, 3])
        {
            RepairSolution = true
        };
        var original = new QapSolution([2, 3, 0, 1]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_RepairsSolution_AndPreservesPermutation()
    {
        var constraint = new QapClusterConstraint(facilities: [0, 1], allowedLocations: [2, 3])
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original) as QapSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_ReturnsNull_WhenAllowedLocationsAreInsufficient()
    {
        var constraint = new QapClusterConstraint(facilities: [0, 1, 2], allowedLocations: [0, 1])
        {
            RepairSolution = true
        };
        var original = new QapSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Repair_Throws_ForNonPermutationSolution_WhenEnabled()
    {
        var constraint = new QapClusterConstraint(facilities: [0], allowedLocations: [0])
        {
            RepairSolution = true
        };

        Assert.That(() => constraint.Repair(new DummySolution()), Throws.ArgumentException);
    }

    [Test]
    public void TextVisualization_ContainsReadableConfiguration()
    {
        var constraint = new QapClusterConstraint(facilities: [5, 1], allowedLocations: [9, 3]);

        var text = constraint.TextVisualization;

        Assert.That(text, Does.Contain("QAP Cluster Constraint"));
        Assert.That(text, Does.Contain("Facilities: 1, 5"));
        Assert.That(text, Does.Contain("Allowed locations: 3, 9"));
    }
}