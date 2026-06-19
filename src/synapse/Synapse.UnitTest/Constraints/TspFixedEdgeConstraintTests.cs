using Synapse.HITL.Problems.TSP;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.TSP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class TspFixedEdgeConstraintTests
{
    [SetUp]
    public void Setup()
    {
        // Keep pseudo-random behavior reproducible for repair-related tests.
        RandomProvider.SetSeed(12345);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenDirectedEdgeExists()
    {
        var constraint = new TspFixedEdgeConstraint(1, 2, directed: true);
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_WhenDirectedEdgeOnlyExistsInReverse()
    {
        var constraint = new TspFixedEdgeConstraint(1, 2, directed: true);
        var solution = new TspSolution([0, 2, 1, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenUndirectedEdgeExistsInReverse()
    {
        var constraint = new TspFixedEdgeConstraint(1, 2, directed: false);
        var solution = new TspSolution([0, 2, 1, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_ForWrapAroundEdge()
    {
        var constraint = new TspFixedEdgeConstraint(3, 0, directed: true);
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_ForNonTspSolution()
    {
        var constraint = new TspFixedEdgeConstraint(1, 2);

        Assert.That(constraint.IsSatisfied(new DummySolution()), Is.True);
    }

    [Test]
    public void Repair_ReturnsSameInstance_ForNonTspSolution()
    {
        var constraint = new TspFixedEdgeConstraint(1, 2);
        var nonTsp = new DummySolution();

        var repaired = constraint.Repair(nonTsp);

        Assert.That(repaired, Is.SameAs(nonTsp));
    }

    [Test]
    public void Repair_WhenDisabled_ReturnsOriginalInstance_ForTspSolution()
    {
        var constraint = new TspFixedEdgeConstraint(1, 3, directed: true)
        {
            RepairSolution = false
        };
        var original = new TspSolution([0, 1, 2, 3, 4]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.SameAs(original));
    }

    [Test]
    public void Repair_ReturnsNull_ForSelfEdge()
    {
        var constraint = new TspFixedEdgeConstraint(2, 2, directed: true)
        {
            RepairSolution = true
        };
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.Repair(solution), Is.Null);
    }

    [Test]
    public void Repair_ReturnsNull_WhenRequiredNodeDoesNotExist()
    {
        var constraint = new TspFixedEdgeConstraint(1, 9, directed: true)
        {
            RepairSolution = true
        };
        var solution = new TspSolution([0, 1, 2, 3]);

        Assert.That(constraint.Repair(solution), Is.Null);
    }

    [Test]
    public void Repair_InsertsMissingDirectedEdge_AndPreservesPermutation()
    {
        var constraint = new TspFixedEdgeConstraint(1, 3, directed: true)
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
    public void Repair_InsertsAllRequiredUndirectedEdges()
    {
        var constraint = new TspFixedEdgeConstraint(new[] { (1, 4), (2, 5) }, directed: false)
        {
            RepairSolution = true
        };
        var original = new TspSolution([0, 1, 2, 3, 4, 5]);

        var repaired = constraint.Repair(original) as TspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
    }

    [Test]
    public void Repair_ReturnsNull_WhenDirectedRequirementsConflict()
    {
        var constraint = new TspFixedEdgeConstraint(new[] { (0, 1), (0, 2) }, directed: true)
        {
            RepairSolution = true
        };
        var original = new TspSolution([0, 1, 2, 3]);

        var repaired = constraint.Repair(original);

        Assert.That(repaired, Is.Null);
    }

    [Test]
    public void Description_FormatsDirectedEdges()
    {
        var constraint = new TspFixedEdgeConstraint(new[] { (1, 2), (3, 4) }, directed: true);

        Assert.That(constraint.Description, Does.Contain("1->2"));
        Assert.That(constraint.Description, Does.Contain("3->4"));
    }

    [Test]
    public void TextVisualization_ContainsUndirectedModeDetails()
    {
        var constraint = new TspFixedEdgeConstraint(new[] { (1, 2) }, directed: false);

        var text = constraint.TextVisualization;

        Assert.That(text, Does.Contain("Fixed-Edge Constraint"));
        Assert.That(text, Does.Contain("Must contain undirected adjacencies"));
        Assert.That(text, Does.Contain("Wrap-around edge (last->first) also counts."));
    }
}