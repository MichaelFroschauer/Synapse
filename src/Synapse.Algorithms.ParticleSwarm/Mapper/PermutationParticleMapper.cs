using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.ParticleSwarm.Mapper;

/// <summary>
/// Mapper for permutation problems using random-key encoding.
/// Position is a double[] of keys, solution is a permutation formed by sorting keys (stable sort by index).
/// </summary>
public class PermutationParticleMapper : BaseParticleMapper
{
    private readonly PermutationSolution _prototype;
    public override int Dimensions { get; }
    private readonly int[] _indices;

    public PermutationParticleMapper(PermutationSolution prototype)
    {
        _prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
        Dimensions = _prototype.Length;
        _indices = Enumerable.Range(0, _prototype.Length).ToArray();
    }

    public override (double[] Min, double[] Max)? Bounds
    {
        get
        {
            var min = Enumerable.Repeat(0.0, Dimensions).ToArray();
            var max = Enumerable.Repeat(1.0, Dimensions).ToArray();
            return (min, max);
        }
    }

    public override ISolution PositionToSolution(double[] position)
    {
        if (position.Length != Dimensions) throw new ArgumentException("Position length mismatch.", nameof(position));

        // convert keys into permutation by ordering indices by key value
        var pairs = _indices.Select(i => (Index: i, Key: position[i])).ToArray();
        Array.Sort(pairs, (a, b) =>
        {
            var cmp = a.Key.CompareTo(b.Key);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        var perm = pairs.Select(p => p.Index).ToArray();
        
        return _prototype.Create(perm);
    }
}
