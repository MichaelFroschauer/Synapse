using System;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;

namespace Synapse.GUI.Services;

public static class ServiceInitializer
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        // Initialize Genetic Algorithm
        MapperBootstrap.RegisterAllMappings();
    }
}