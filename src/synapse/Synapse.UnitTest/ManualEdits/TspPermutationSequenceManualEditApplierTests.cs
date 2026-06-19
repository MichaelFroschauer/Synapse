using Synapse.HITL.Problems.TSP;
using Synapse.Problems.QAP;
using Synapse.Problems.TSP;

namespace Synapse.UnitTest.ManualEdits;

[TestFixture]
public class TspPermutationSequenceManualEditApplierTests
{
    [Test]
    public void Apply_PlacesManualCitiesFirst_ThenAppendsRemainingCities()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 50 };
        var manual = new TspSolution([3, 1]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 3, 1, 2, 4 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_WhenNoValidManualCities_ReturnsUnchangedCloneWithResetFitness()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 12 };
        var manual = new TspSolution([8, 9]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result, Is.Not.SameAs(candidate));
        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_DoesNotMutateOriginalCandidate()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]) { Fitness = 7 };
        var manual = new TspSolution([3, 1]);

        _ = applier.Apply(candidate, manual);

        Assert.That(candidate.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        Assert.That(candidate.Fitness, Is.EqualTo(7));
    }

    [Test]
    public void Apply_Throws_ForNullArguments()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3, 4]);
        var manual = new TspSolution([3, 1]);

        Assert.That(() => applier.Apply(null!, manual), Throws.ArgumentNullException);
        Assert.That(() => applier.Apply(candidate, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Apply_Throws_WhenCandidateIsNotTspSolution()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]);
        var manual = new TspSolution([1, 2]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_WhenManualIsNotTspSolution()
    {
        var applier = new TspPermutationSequenceManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new QapSolution([0, 1, 2, 3]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }
}