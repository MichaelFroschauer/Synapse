using Synapse.HITL.Problems.QAP;
using Synapse.OptimizationCore.Common;
using Synapse.Problems.QAP;
using Synapse.Problems.TSP;

namespace Synapse.UnitTest.ManualEdits;

[TestFixture]
public class QapManualEditApplierTests
{
    [Test]
    public void Apply_UsesCompleteValidManualMapping()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]) { Fitness = 10 };
        var manual = new QapSolution([2, 3, 1, 0]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 2, 3, 1, 0 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_RepairsDuplicateLocations_ForCompleteManualMapping_Deterministically()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]);
        var manual = new QapSolution([2, 2, 2, 2]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 2, 0, 1, 3 }));
    }

    [Test]
    public void Apply_UsesPartialManualAssignments_AndMaintainsPermutation()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]) { Fitness = 7 };
        var manual = new QapSolution([
            new Parameter(2),
            new Parameter("invalid"),
            new Parameter(1),
            new Parameter(-1)
        ]);

        var result = applier.Apply(candidate, manual);

        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 2, 0, 1, 3 }));
        Assert.That(result.GetParameters().Select(p => (int)p.Value), Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        Assert.That(result.Fitness, Is.Null);
    }

    [Test]
    public void Apply_DoesNotMutateOriginalCandidate()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]) { Fitness = 5 };
        var manual = new QapSolution([2, 3, 1, 0]);

        _ = applier.Apply(candidate, manual);

        Assert.That(candidate.GetParameters().Select(p => (int)p.Value), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(candidate.Fitness, Is.EqualTo(5));
    }

    [Test]
    public void Apply_Throws_ForNullArguments()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]);
        var manual = new QapSolution([2, 3, 1, 0]);

        Assert.That(() => applier.Apply(null!, manual), Throws.ArgumentNullException);
        Assert.That(() => applier.Apply(candidate, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Apply_Throws_WhenCandidateIsNotQapSolution()
    {
        var applier = new QapManualEditApplier();
        var candidate = new TspSolution([0, 1, 2, 3]);
        var manual = new QapSolution([2, 3, 1, 0]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }

    [Test]
    public void Apply_Throws_WhenManualLengthDiffersFromCandidateLength()
    {
        var applier = new QapManualEditApplier();
        var candidate = new QapSolution([0, 1, 2, 3]);
        var manual = new QapSolution([0, 1, 2]);

        Assert.That(() => applier.Apply(candidate, manual), Throws.ArgumentException);
    }
}