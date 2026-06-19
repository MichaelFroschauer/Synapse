using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;

namespace Synapse.Algorithms.GeneticAlgorithm;

/// <summary>
/// Configuration for the Genetic Algorithm
/// </summary>
public class GeneticAlgorithmConfig : BaseAlgorithmConfig
{
    public override AlgorithmType AlgorithmType => AlgorithmType.Genetic;

    public int PopulationSize { get; set; } = 500;
    public ISelection Selection { get; set; } = new TournamentSelection(2);
    public ISelection? Selection2 { get; set; }
    public ICrossover Crossover { get; set; } = new OrderedCrossover();
    public IMutation Mutation { get; set; } = new TworsMutation();
    public double CrossoverProbability { get; set; } = 0.95;
    public double MutationProbability { get; set; } = 0.05;
    
    public GeneticAlgorithmConfig() { }
    
    private GeneticAlgorithmConfig(GeneticAlgorithmConfig config) : base(config)
    {
        PopulationSize = config.PopulationSize;
        Selection = (ISelection)config.Selection.Clone();
        Selection2 = config.Selection2 is not null ? (ISelection)config.Selection2.Clone() : null;
        Crossover = (ICrossover)config.Crossover.Clone();
        Mutation = (IMutation)config.Mutation.Clone();
        CrossoverProbability = config.CrossoverProbability;
        MutationProbability = config.MutationProbability;
    }
    
    public override object Clone() => new GeneticAlgorithmConfig(this);
}
