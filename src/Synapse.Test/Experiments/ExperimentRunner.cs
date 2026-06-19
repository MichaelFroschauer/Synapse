using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.Algorithms.NSGA_II;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.Algorithms.RAPGA;
using Synapse.HITL;
using Synapse.JobManagement;
using Synapse.JobManagement.Persistence;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Test.Experiments;

public class ExperimentRunner
{
        public static async Task ConfigureAndRunAsync()
        {
                var builder = Host.CreateDefaultBuilder();
        
                builder.ConfigureLogging(logging =>
                {
                        logging.ClearProviders();
                        logging.AddSimpleConsole(opt =>
                        {
                                opt.SingleLine = true;
                                opt.TimestampFormat = "HH:mm:ss ";
                        });
                        logging.SetMinimumLevel(LogLevel.Information);
                });
        
                builder.ConfigureServices((ctx, services) =>
                {
                        // Controller
                        services.AddTransient<IAlgorithmController, AlgorithmController>();
                        services.AddTransient<IHitlController, HitlController>();
            
                        // Algorithms
                        services.AddTransient<GeneticAlgorithm>();
                        services.AddTransient<ParticleSwarmAlgorithm>();
                        services.AddTransient<RAPGA>();
                        services.AddTransient<Nsga2Algorithm>();
            
                        // JobManagement
                        services.AddSingleton<IJobSnapshotStore>(_ => new FileJobSnapshotStore("/home/michael/Desktop/snapshot/"));
                        services.AddSingleton<IJobManager, JobManager>();
            
                        services.AddTransient<IExperiment, Experiment1>();
            
                        MapperBootstrap.RegisterAllMappings();
                });

                using var host = builder.Build();
                foreach (var experiment in host.Services.GetServices<IExperiment>())
                {
                        experiment.Setup();
                        await experiment.RunAsync();
                }
        }
}