using GeneticSharp;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.GeneticAlgorithm.Mapper;

public static class MappingExtensions
{
    /// <summary> Convert IChromosome to ISolution by looking up mapper via the chromosome's runtime type. </summary>
    public static ISolution ToSolution(this IChromosome chromosome)
    {
        if (chromosome == null) throw new ArgumentNullException(nameof(chromosome));
        var chromType = chromosome.GetType();
        if (!SolutionChromosomeRegistry.TryGetByChromosome(chromType, out var entry))
            throw new InvalidOperationException($"No mapper registered for chromosome type {chromType.FullName}. Did you call 'MapperBootstrap.RegisterAllMappings();'?");
        var solution = entry!.ChromToSolution(chromosome);
        solution.Fitness = chromosome.Fitness;
        return solution;
    }

    /// <summary> Convert ISolution to IChromosome by looking up mapper via the solution's runtime type. </summary>
    public static IChromosome ToChromosome(this ISolution solution)
    {
        if (solution == null) throw new ArgumentNullException(nameof(solution));
        var solType = solution.GetType();
        if (!SolutionChromosomeRegistry.TryGetBySolution(solType, out var entry))
            throw new InvalidOperationException($"No mapper registered for solution type {solType.FullName}. Did you call 'MapperBootstrap.RegisterAllMappings();'?");
        var chromosome = entry!.SolutionToChrom(solution);
        chromosome.Fitness = solution.Fitness;
        return chromosome;
    }

    // Enumerable helpers
    public static IEnumerable<ISolution> ToSolutions(this IEnumerable<IChromosome> chromosomes)
    {
        foreach (var c in chromosomes) yield return c.ToSolution();
    }

    public static IEnumerable<IChromosome> ToChromosomes(this IEnumerable<ISolution> solutions)
    {
        foreach (var s in solutions) yield return s.ToChromosome();
    }

    public static GeneticSharp.IMutation? ToGsMutation(this Synapse.Algorithms.GeneticAlgorithm.Mutator.IMutation mutation)
    {
        return mutation switch
        {
            Mutator.TworsMutation => new GeneticSharp.TworsMutation(),
            Mutator.ReverseSequenceMutation => new GeneticSharp.ReverseSequenceMutation(),
            Mutator.FlipBitMutation => new GeneticSharp.FlipBitMutation(),
            _ => null
        };
    }
    
    public static GeneticSharp.ISelection? ToGsSelection(this Synapse.Algorithms.GeneticAlgorithm.Selector.ISelection selection)
    {
        return selection switch
        {
            Selector.TournamentSelection s => new GeneticSharp.TournamentSelection(s.GroupSize),
            Selector.RouletteWheelSelection => new GeneticSharp.RouletteWheelSelection(),
            Selector.EliteSelection s => new GeneticSharp.EliteSelection(s.NrOfElites),
            Selector.RandomSelection => new GsExtension.RandomSelection(),
            _ => null
        };
    }
    
    public static GeneticSharp.ICrossover? ToGsCrossover(this Synapse.Algorithms.GeneticAlgorithm.Crossover.ICrossover crossover)
    {
        return crossover switch
        {
            Crossover.OrderedCrossover => new GeneticSharp.OrderedCrossover(),
            Crossover.OnePointCrossover => new GeneticSharp.OnePointCrossover(),
            Crossover.UniformCrossover c => new GeneticSharp.UniformCrossover(c.MixProbability),
            _ => null
        };
    }
}
