using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;

namespace Synapse.Algorithms.RAPGA;

/// <summary>
/// Configuration for the Relevant Alleles Preserving Genetic Algorithm (RAPGA).
/// </summary>
public class RAPGAConfig : BaseAlgorithmConfig
{
    public override AlgorithmType AlgorithmType => AlgorithmType.RAPGA;
    
    [DisplayInfo("Selection Male")]
    public ISelection Selection1 { get; set; } = new TournamentSelection(2);
    
    [DisplayInfo("Selection Female")]
    public ISelection Selection2 { get; set; } = new RouletteWheelSelection();
    
    [DisplayInfo("Crossover")]
    public ICrossover Crossover { get; set; } = new OrderedCrossover();
    
    [DisplayInfo("Mutation")]
    public IMutation Mutation { get; set; } = new TworsMutation();
    
    [DisplayInfo("Mutation Probability")]
    public double MutationProbability { get; set; } = 0.05;
    
    [DisplayInfo("Comparison Factor")]
    public double ComparisonFactor { get; set; } = 0.0;
    public int Elites { get; set; } = 0;
    public int PopulationSize { get; set; } = 500;
    public int MinimumPopulationSize { get; set; } = 2;
    public int MaximumPopulationSize { get; set; } = 1000;
    public int Effort { get; set; } = 2000;

    [DisplayInfo("Similarity Diversity Threshold",
        "Solutions with a similarity score >= this threshold are considered identical genomes and excluded from the population. " +
        "Set to 1.0 to use exact comparison, or lower (e.g. 0.95) to also reject near-duplicate solutions. " +
        "Only active when greater than 0, otherwise exact similarity will be used.")]
    public double SimilarityDiversityThreshold { get; set; } = 0.0;
    
    public RAPGAConfig() { }
    
    private RAPGAConfig(RAPGAConfig config) : base(config)
    {
        Selection1 = (ISelection)config.Selection1.Clone();
        Selection2 = (ISelection)config.Selection2.Clone();
        Crossover = (ICrossover)config.Crossover.Clone();
        Mutation = (IMutation)config.Mutation.Clone();
        MutationProbability = config.MutationProbability;
        ComparisonFactor = config.ComparisonFactor;
        Elites = config.Elites;
        PopulationSize = config.PopulationSize;
        MinimumPopulationSize = config.MinimumPopulationSize;
        MaximumPopulationSize = config.MaximumPopulationSize;
        Effort = config.Effort;
        SimilarityDiversityThreshold = config.SimilarityDiversityThreshold;
    }
    
    public override object Clone() => new RAPGAConfig(this);
}
