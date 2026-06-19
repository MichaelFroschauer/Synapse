using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;

namespace Synapse.Algorithms.NSGA_II;

public class Nsga2AlgorithmConfig : BaseAlgorithmConfig
{
    public override AlgorithmType AlgorithmType => AlgorithmType.NSGA_II;

    public int PopulationSize { get; set; } = 500;

    [DisplayInfo("Objective Importance Weights",
        "Relative objective importance. Higher means more important. " +
        "If fewer weights are provided than objectives, remaining objectives default to 1.0.")]
    public List<double> ObjectiveImportanceWeights { get; set; } = new();

    [DisplayInfo("Tournament Size")]
    public int TournamentSize { get; set; } = 3;

    [DisplayInfo("Crossover")]
    public ICrossover Crossover { get; set; } = new OrderedCrossover();

    [DisplayInfo("Mutation")]
    public IMutation Mutation { get; set; } = new TworsMutation();

    [DisplayInfo("Crossover Probability")]
    public double CrossoverProbability { get; set; } = 0.95;

    [DisplayInfo("Mutation Probability")]
    public double MutationProbability { get; set; } = 0.05;

    public Nsga2AlgorithmConfig() { }

    private Nsga2AlgorithmConfig(Nsga2AlgorithmConfig config) : base(config)
    {
        PopulationSize = config.PopulationSize;
        ObjectiveImportanceWeights = [..config.ObjectiveImportanceWeights];
        TournamentSize = config.TournamentSize;
        Crossover = (ICrossover)config.Crossover.Clone();
        Mutation = (IMutation)config.Mutation.Clone();
        CrossoverProbability = config.CrossoverProbability;
        MutationProbability = config.MutationProbability;
    }

    public override object Clone() => new Nsga2AlgorithmConfig(this);
}
