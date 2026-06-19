using Synapse.HITL.Problems.Permutation;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.Problems.TSP;

namespace Synapse.UnitTest.ManualEdits;

[TestFixture]
public class ReverseSegmentByValuesManualEditApplierTests
{
    [Test]
    public void Apply_ReversesSegment_WhenDelimitersAppearInForwardOrder()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 42 };
        var manual = new PermutationSolution([1, 3]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 3, 2, 1, 4 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_ReversesSegment_WhenDelimitersAppearInReverseOrder()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 11 };
        var manual = new PermutationSolution([3, 1]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 3, 2, 1, 4 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_DoesNotMutateOriginalCandidate()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 7 };
        var manual = new PermutationSolution([1, 3]);

        _ = applier.Apply(candidate, manual);

        Assert.That(candidate.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        Assert.That(candidate.Fitness, Is.EqualTo(7));
    }

    [Test]
    public void Apply_WhenBothManualValuesAreEqual_ReturnsUnchangedClone_AndResetsFitness()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 9 };
        var manual = new PermutationSolution([2, 2]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result, Is.Not.SameAs(candidate));
        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_Throws_WhenManualLengthIsNotTwo()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 2, 3]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_WhenManualValueIsMissingInCandidate()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 9]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_ForNullArguments()
    {
        var applier = new ReverseSegmentByValuesManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 3]);

        Assert.That(() => applier.Apply(null!, manual), Throws.ArgumentNullException);
        Assert.That(() => applier.Apply(candidate, null!), Throws.ArgumentNullException);
    }
}