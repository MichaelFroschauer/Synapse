using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.HITL;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.Function;

namespace Synapse.Test;

public class TestJobManager : IMetaheuristicTester
{
    public static async Task Run()
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
            
            // JobManagement
            services.AddTransient<IJobManager, JobManager>();
        });

        using var host = builder.Build();
        //await host.StartAsync();

        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var jobManager = sp.GetRequiredService<IJobManager>();
            
        var tcs = new TaskCompletionSource<JobStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            
        void OnStatusChanged(object? sender, (Guid JobId, JobStatus Status) info)
        {
            // hier kannst du jobId filtern (wenn mehrere Jobs laufen)
            if (info.Status == JobStatus.Completed ||
                info.Status == JobStatus.Cancelled ||
                info.Status == JobStatus.Failed ||
                info.Status == JobStatus.Stopped)
            {
                tcs.TrySetResult(info.Status);
            }
        }
            
        jobManager.JobStatusChanged += OnStatusChanged;
        jobManager.JobProgress += (s, payload) =>
        {
            var (jobId, progress) = payload;
            Console.WriteLine($"[Job {jobId}] Iter {progress.Iteration} Best={progress.BestFitness:F4} Msg='{progress.Message}'");
        };
            
        var cfg = new ParticleSwarmConfig
        {
            Name = "PSO-Test",
            SwarmSize = 30,
            MaxIterations = 500,
            Inertia = 0.729,
            Cognitive = 1.49445,
            Social = 1.49445,
            VelocityClampFactor = 0.2,
            ProgressInterval = 500
        };
        var problemInstance = new RastriginFunctionProblemInstance();
        var problem = new FunctionProblem(problemInstance);
        Guid jobId = jobManager.QueueJob(cfg, problem, new ProblemInstance{ Name = "RastriginTest", ProblemType = ProblemType.Function });
        _ = jobManager.StartQueuedJobAsync(jobId);
        // var jobId2 = jobManager.StartJob("PSO-Rastrigin-Test-2", cfg, problem);
        // var jobId3 = jobManager.StartJob("PSO-Rastrigin-Test-2", cfg, problem);
            
            
        Console.WriteLine($"Job gestartet: {jobId}");
            
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(5)));
        if (completedTask == tcs.Task)
        {
            Console.WriteLine($"Job beendet mit Status: {tcs.Task.Result}");
        }
        else
        {
            Console.WriteLine("Timeout: Job nicht innerhalb von 5 Minuten fertig — stoppe ihn.");
            await jobManager.StopJobAsync(jobId);
        }

        var info = jobManager.GetJobInfo(jobId);
        Console.WriteLine($"JobInfo: Id={info?.Id}, Status={info?.Status}, BestFitness={info?.BestFitness}");
        jobManager.JobStatusChanged -= OnStatusChanged;

        // await host.StopAsync();
        // await host.WaitForShutdownAsync();
    }
}
