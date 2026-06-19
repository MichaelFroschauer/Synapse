using Microsoft.Extensions.DependencyInjection;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.NSGA_II;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.Algorithms.RAPGA;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.JobManagement.Factory;

public static class MetaheuristicAlgorithmFactory
{
    public static IMetaheuristic Create(IAlgorithmConfig config, IServiceProvider serviceProvider)
    {
        // using var scope = serviceProvider.CreateScope();
        // var sp = scope.ServiceProvider;

        return config switch
        {
            // if not constructor parameters are needed
            //GeneticAlgorithmConfig g => serviceProvider.GetRequiredService<GeneticAlgorithm>(),
            GeneticAlgorithmConfig p => (GeneticAlgorithm)ActivatorUtilities.CreateInstance(serviceProvider, typeof(GeneticAlgorithm), p),
            ParticleSwarmConfig p => (ParticleSwarmAlgorithm)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ParticleSwarmAlgorithm), p),
            RAPGAConfig p => (RAPGA)ActivatorUtilities.CreateInstance(serviceProvider, typeof(RAPGA), p),
            Nsga2AlgorithmConfig p => (Nsga2Algorithm)ActivatorUtilities.CreateInstance(serviceProvider, typeof(Nsga2Algorithm), p),
            _
                => throw new ArgumentException($"Config for unknown algorithm type: {config.GetType().Name}")
        };
    }

    public static IMetaheuristic Create(IAlgorithmConfig config)
    {
        return config switch
        {
            GeneticAlgorithmConfig _ => new GeneticAlgorithm(config),
            ParticleSwarmConfig _ => new ParticleSwarmAlgorithm(config),
            RAPGAConfig _ => new RAPGA(config),
            Nsga2AlgorithmConfig _ => new Nsga2Algorithm(config),
            _
                => throw new ArgumentException($"Config for unknown algorithm type: {config.GetType().Name}")
        };
    }
}