using Synapse.HITL.Problems.TSP;
using Synapse.Problems.TSP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class TspForbiddenEdgeConstraintTests
{
    [Test]
    public void IsSatisfied_ReturnsFalse_WhenForbiddenEdgeExistsInForwardDirection()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2);
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenForbiddenEdgeExistsInReverseDirection()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2);
        var solution = new TspSolution([0, 2, 1, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_ForWrapAroundForbiddenEdge()
    {
        var constraint = new TspForbiddenEdgeConstraint(3, 0);
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_ForNonTspSolution()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2);

        Assert.That(constraint.IsSatisfied(new DummySolution()), Is.True);
    }

    [Test]
    public void Repair_ReturnsSameInstance_ForNonTspSolution()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2)
        {
            RepairSolution = true
        };
        var nonTsp = new DummySolution();

        var repaired = constraint.Repair(nonTsp);

        Assert.That(repaired, Is.SameAs(nonTsp));
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance_ForTspSolution()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2)
        {
            RepairSolution = false
        };
        var original = new TspSolution([0, 1, 2, 3, 4]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabledAndAlreadySatisfied_ReturnsOriginalInstance()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2)
        {
            RepairSolution = true
        };
        var original = new TspSolution([0, 1, 3, 2, 4]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_WhenEnabled_RemovesForbiddenEdge_AndPreservesPermutation()
    {
        var constraint = new TspForbiddenEdgeConstraint(1, 2)
        {
            RepairSolution = true
        };
        var original = new TspSolution([0, 1, 2, 3, 4]);

        var repaired = constraint.Repair(original) as TspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_ReturnsNull_WhenForbiddenSetIsUnsatisfiable()
    {
        var constraint = new TspForbiddenEdgeConstraint(new[] { (0, 1), (1, 2), (2, 0) })
        {
            RepairSolution = true
        };
        var original = new TspSolution([0, 1, 2]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Description_ContainsForbiddenEdges()
    {
        var constraint = new TspForbiddenEdgeConstraint(new[] { (1, 2), (3, 4) });

        Assert.That(constraint.Description, Does.Contain("1-2"));
        Assert.That(constraint.Description, Does.Contain("3-4"));
    }

    [Test]
    public void TextVisualization_ContainsReadableDetails()
    {
        var constraint = new TspForbiddenEdgeConstraint(new[] { (1, 2) });

        var text = constraint.TextVisualization;

        Assert.That(text, Does.Contain("Forbidden Edge Constraint"));
        Assert.That(text, Does.Contain("Forbidden undirected edges"));
        Assert.That(text, Does.Contain("Repair behavior"));
    }
}