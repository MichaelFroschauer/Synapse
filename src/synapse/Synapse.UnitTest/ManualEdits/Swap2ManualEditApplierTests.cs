using Synapse.HITL.Problems.Permutation;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.Problems.TSP;

namespace Synapse.UnitTest.ManualEdits;

[TestFixture]
public class Swap2ManualEditApplierTests
{
    [Test]
    public void Apply_SwapsRequestedValues_AndResetsFitness()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]) { Fitness = 42 };
        var manual = new PermutationSolution([1, 3]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 3, 2, 1 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_DoesNotMutateOriginalCandidate()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]) { Fitness = 7 };
        var manual = new PermutationSolution([1, 3]);

        _ = applier.Apply(candidate, manual);

        Assert.That(candidate.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(candidate.Fitness, Is.EqualTo(7));
    }

    [Test]
    public void Apply_WhenBothManualValuesAreEqual_ReturnsUnchangedClone_AndResetsFitness()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]) { Fitness = 9 };
        var manual = new PermutationSolution([2, 2]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result, Is.Not.SameAs(candidate));
        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_Throws_WhenManualLengthIsNotTwo()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 2, 3]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_WhenManualValueIsMissingInCandidate()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 9]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_ForNullArguments()
    {
        var applier = new Swap2ManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new PermutationSolution([1, 3]);

        Assert.That(() => applier.Apply(null!, manual), Throws.ArgumentNullException);
        Assert.That(() => applier.Apply(candidate, null!), Throws.ArgumentNullException);
    }
}