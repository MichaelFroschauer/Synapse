using Synapse.HITL.Problems.TSP;
using Synapse.OptimizationCore.Random;
using Synapse.Problems.TSP;
using Synapse.UnitTest.Utils;

namespace Synapse.UnitTest.Constraints;

[TestFixture]
public class TspClusterConstraintTests
{
    [SetUp]
    public void Setup()
    {
        // Keep pseudo-random behavior reproducible for repair-related tests.
        RandomProvider.SetSeed(12345);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_WhenClusterIsLinearContiguous()
    {
        var constraint = new TspClusterConstraint(clusterCities: [2, 3, 4], allowWrapAround: false);
        var solution = new TspSolution([0, 2, 3, 4, 1]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_ForWrapAroundCluster_WhenAllowed()
    {
        var constraint = new TspClusterConstraint(clusterCities: [0, 3, 4], allowWrapAround: true);
        var solution = new TspSolution([3, 4, 1, 2, 0]);

        Assert.That(constraint.IsSatisfied(solution), Is.True);
    }

    [Test]
    public void IsSatisfied_ReturnsFalse_ForWrapAroundCluster_WhenDisallowed()
    {
        var constraint = new TspClusterConstraint(clusterCities: [0, 3, 4], allowWrapAround: false);
        var solution = new TspSolution([3, 4, 1, 2, 0]);

        Assert.That(constraint.IsSatisfied(solution), Is.False);
    }

    [Test]
    public void IsSatisfied_ReturnsTrue_ForNonTspSolution()
    {
        var constraint = new TspClusterConstraint(clusterCities: [1, 2, 3]);
        var nonTsp = new DummySolution();

        Assert.That(constraint.IsSatisfied(nonTsp), Is.True);
    }

    [Test]
    public void Repair_ReturnsSameInstance_ForNonTspSolution()
    {
        var constraint = new TspClusterConstraint(clusterCities: [1, 2, 3]);
        var nonTsp = new DummySolution();

        var repaired = constraint.Repair(nonTsp);

        Assert.That(repaired, Is.SameAs(nonTsp));
    }

    [Test]
    public void Repair_CreatesSatisfiedSolution_AndPreservesTourMembership()
    {
        var constraint = new TspClusterConstraint(
            clusterCities: [1, 2, 3],
            shuffleBlock: true,
            randomInsertPosition: false,
            allowWrapAround: true);

        var original = new TspSolution([0, 3, 4, 1, 5, 2]);

        var repaired = constraint.Repair(original) as TspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
        Assert.That(repaired.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Repair_WithRandomInsertPosition_StillProducesSatisfiedPermutation()
    {
        var constraint = new TspClusterConstraint(
            clusterCities: [2, 4, 5],
            shuffleBlock: true,
            randomInsertPosition: true,
            allowWrapAround: true);

        var original = new TspSolution([0, 1, 2, 3, 4, 5, 6]);

        var repaired = constraint.Repair(original) as TspSolution;

        Assert.That(repaired, Is.Not.Null);
        Assert.That(constraint.IsSatisfied(repaired!), Is.True);
        Assert.That(repaired!.GetParametersWithType(), Is.EquivalentTo(original.GetParametersWithType()));
    }

    [Test]
    public void TextVisualization_ContainsConfiguredFlags()
    {
        var constraint = new TspClusterConstraint(
            clusterCities: [7, 1, 4],
            shuffleBlock: false,
            randomInsertPosition: true,
            allowWrapAround: false);

        var text = constraint.TextVisualization;

        Assert.That(text, Does.Contain("Cluster: [1, 4, 7]"));
        Assert.That(text, Does.Contain("Shuffle inside block: no"));
        Assert.That(text, Does.Contain("Random insert position: yes"));
        Assert.That(text, Does.Contain("Wrap-around allowed: no"));
    }
}