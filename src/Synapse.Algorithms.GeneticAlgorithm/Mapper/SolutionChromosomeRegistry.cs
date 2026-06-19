using System.Collections.Concurrent;
using GeneticSharp;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.GeneticAlgorithm.Mapper;

public static class SolutionChromosomeRegistry
{
    // Key by chromosome type for direct ToSolution lookup
    private static readonly ConcurrentDictionary<Type, MapperEntry> _byChromosome = new();

    // Key by solution type for direct ToChromosome lookup
    private static readonly ConcurrentDictionary<Type, MapperEntry> _bySolution = new();

    /// <summary> Register a mapper pair for specific chromosome & solution types. </summary>
    public static void Register<TChrom, TSol>(Func<TChrom, TSol> chromToSol, Func<TSol, TChrom> solToChrom)
        where TChrom : IChromosome where TSol : ISolution
    {
        if (chromToSol == null) throw new ArgumentNullException(nameof(chromToSol));
        if (solToChrom == null) throw new ArgumentNullException(nameof(solToChrom));
        var entry = new MapperEntry
        {
            ChromosomeType = typeof(TChrom),
            SolutionType = typeof(TSol),
            ChromToSolution = c => chromToSol((TChrom)c),
            SolutionToChrom = s => solToChrom((TSol)s)
        };
        _byChromosome.AddOrUpdate(entry.ChromosomeType, entry, (k, v) => entry);
        _bySolution.AddOrUpdate(entry.SolutionType, entry, (k, v) => entry);
    }

    /// <summary> Try to get mapper by actual chromosome type (exact match or assignable base). </summary>
    public static bool TryGetByChromosome(Type chromType, out MapperEntry? entry)
    {
        // Try exact
        if (_byChromosome.TryGetValue(chromType, out entry)) return true;

        // Fallback: find first registered mapper whose ChromosomeType is assignable from chromType
        // (covers polymorphism: e.g. registered base chromosome)
        entry = _byChromosome.Values.FirstOrDefault(e => e.ChromosomeType.IsAssignableFrom(chromType));
        return entry != null;
    }

    public static bool TryGetBySolution(Type solType, out MapperEntry? entry)
    {
        if (_bySolution.TryGetValue(solType, out entry)) return true;
        entry = _bySolution.Values.FirstOrDefault(e => e.SolutionType.IsAssignableFrom(solType));
        return entry != null;
    }
}