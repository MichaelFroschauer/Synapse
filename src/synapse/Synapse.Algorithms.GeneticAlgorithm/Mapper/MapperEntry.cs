using GeneticSharp;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.GeneticAlgorithm.Mapper;

public record MapperEntry
{
    public required Type ChromosomeType { get; init; }
    public required Type SolutionType { get; init; }
    public required Func<IChromosome, ISolution> ChromToSolution { get; init; }
    public required Func<ISolution, IChromosome> SolutionToChrom { get; init; }
}