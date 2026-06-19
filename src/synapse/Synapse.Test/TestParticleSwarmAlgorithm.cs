using Microsoft.Extensions.Logging;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.HITL;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;
using Synapse.Problems.Function;
using Synapse.Problems.TSP;

namespace Synapse.Test;

public class TestParticleSwarmAlgorithm : IMetaheuristicTester
{
    private static ILogger<ParticleSwarmAlgorithm>? _logger;

    private static double[,] ParseCsvData()
    {
        string path = "distance_matrix_20.csv";
        var lines = File.ReadAllLines(path);

        int size = lines.Length - 1;
        double[,] matrix = new double[size, size];
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            for (int j = 1; j < parts.Length; j++)
            {
                matrix[i - 1, j - 1] = double.Parse(parts[j]);
            }
        }

        return matrix;
    }
    
    private static IProblem ParseTspProblem()
    {
        string path =
            //"/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/Synapse.Problems/TSP/Data/berlin52.tsp";
            "/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/Synapse.Problems/TSP/Data/bier127.tsp";
        IProblemParser parser = new TspProblemParser();
        return parser.Parse(path);
    }
    
    private static void HandleGenerationFinished(ProgressEventArgs? e)
    {
        if (e == null || e.Config is null) return;
        Console.WriteLine(e.Message);

        // if (e.Config is ParticleSwarmConfig ps)
        // {
        //     ps.Inertia -= 0.001;
        //     ps.Cognitive -= 0.002;
        //     ps.Social -= 0.001;
        // }
        
        if (e.AlgorithmController is null || e.HitlController is null) return;
        // if (e.Iteration == 10 && _stopped == false)
        // {
        //     e.AlgorithmController.RequestPauseAsync();
        //     _stopped = true;
        // }

        if (e.Iteration == 100) {
            var hitlCtrl = e.HitlController;
            //hitlCtrl.AddConstraint(new TspStartPositionConstraint(1));
            //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 1, mustComeAfter: 3));
            //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 3, mustComeAfter: 7));
            //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 17, mustComeAfter: 10));
            //hitlCtrl.AddConstraint(new TspClusterConstraint([1,0,31,48,34,35,33,38,36,39,37,47,23,37,39,4,14,5]));
            //hitlCtrl.AddConstraint(new TspClusterConstraint([1,2,3,4,5]));
            //hitlCtrl.AddConstraint(new TspFixedEdgeConstraint(mustHaveEdges: [(1,2)]));
        }
    }

    private static async Task Test1_F1()
    {
        var algorithmCtrl = new AlgorithmController();
        algorithmCtrl.OnProgress += HandleGenerationFinished;
        
        var config = new ParticleSwarmConfig
        {
            Name = "TSP-PSO",
            SwarmSize = 20,
            MaxIterations = 100,
            Inertia = 0.8,
            Cognitive = 0.1,
            Social = 0.1,
            VelocityClampFactor = 10.0,
            ProgressInterval = 1,
            AlgorithmController = algorithmCtrl,
        };
        
        var pso = new ParticleSwarmAlgorithm(config, _logger);
        var problemInstance = new F1FunctionProblemInstance();
        var problem = new FunctionProblem(problemInstance);
        var best = await pso.SolveAsync(problem, CancellationToken.None);
    }
    
    private static async Task Test2_Rastrigin()
    {
        var algorithmCtrl = new AlgorithmController();
        algorithmCtrl.OnProgress += HandleGenerationFinished;
        
        var config = new ParticleSwarmConfig
        {
            Name = "TSP-PSO",
            SwarmSize = 30,               // 20-50 für 5D; 30 ist ein guter Start
            MaxIterations = 5000,        // 1000-5000 je nach Budget
            // Option A: constriction-style (stable)
            Inertia = 0.729,             // chi / equivalent inertia
            Cognitive = 1.49445,         // c1
            Social = 1.49445,            // c2
            // Option B (alternative): use linearly decreasing inertia instead of fixed Inertia
            // InertiaStart = 0.9, InertiaEnd = 0.4 (would require updating Inertia each iter)

            // velocity clamp factor: fraction of variable range (0.1..0.2 recommended)
            VelocityClampFactor = 0.2,   // => for [-5.12,5.12] gives vmax ≈ 2.048
            ProgressInterval = 10,       // weniger Loglevels (z.B. alle 10 Iterationen)
            AlgorithmController = algorithmCtrl,
        };
        
        var pso = new ParticleSwarmAlgorithm(config, _logger);
        var problemInstance = new RastriginFunctionProblemInstance(5);
        //var functionProblemInstance = new SphereFunctionProblemInstance();
        var problem = new FunctionProblem(problemInstance);
        var best = await pso.SolveAsync(problem, CancellationToken.None);
    }
    
    private static async Task Test3_TSP()
    {
        double[,] distances = ParseCsvData();
        
        var algorithmCtrl = new AlgorithmController();
        algorithmCtrl.OnProgress += HandleGenerationFinished;
        var hitlCtrl = new HitlController();
        
        //hitlCtrl.AddSolutionPreference(sol => sol.ToString()?.Contains("-4-6-") ?? false, weight: 2.0);
        
        //var edited = new TspSolution(new int[]{ 0,1,2,3 });
        //var edited1 = new TspSolution(new int[]{ 30,20,16,6,1,41,29,22,19,49,28,15,45,24 });
        //hitlCtrl.AddManualEdit(edited, probability: 0.9, applier: new TspPermutationSequenceManualEditApplier(), name: "Set middle section of TSP");
        
        //hitlCtrl.AddConstraint(new TspStartPositionConstraint(1));
        //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 1, mustComeAfter: 3));
        //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 3, mustComeAfter: 7));
        //hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 17, mustComeAfter: 10));
        //hitlCtrl.AddConstraint(new TspClusterConstraint([1,0,31,48,34,35,33,38,36,39,37,47,23,37,39,4,14,5]));
        //hitlCtrl.AddConstraint(new TspClusterConstraint([1,2,3,4,5]));
        //hitlCtrl.AddConstraint(new TspFixedEdgeConstraint(mustHaveEdges: [(1,2)]));

        // hitlCtrl.AskPreferenceInterval = 10;
        // hitlCtrl.AskPreference = (IEnumerable<ISolution> solutions1, IEnumerable<ISolution> solutions2) =>
        // {
        //     var s1 = solutions1.First();
        //     var s2 = solutions2.First();
        //     Console.WriteLine($"Fitness S1: {s1.Fitness}, Solution: {s1}");
        //     Console.WriteLine($"Fitness S2: {s2.Fitness}, Solution: {s2}");
        //     Console.WriteLine("What solution would you like to choose? (1, 2)");
        //     var userSelect = Console.ReadLine();
        //     return userSelect == "2" ? 2 : 1;
        // };

        var config = new ParticleSwarmConfig
        {
            Name = "TSP-PSO",
            SwarmSize = 40,
            MaxIterations = 1000,
            //Inertia = 0.9,
            //Cognitive = 1.2,
            //Social = 1.2,
            Inertia = 0.729,             // chi / equivalent inertia
            Cognitive = 1.49445,         // c1
            Social = 1.49445,            // c2
            VelocityClampFactor = 10.0,
            AlgorithmController = algorithmCtrl,
            HitlController = hitlCtrl,
            ProgressInterval = 1
        };
        
        var pso = new ParticleSwarmAlgorithm(config, _logger);
        var problem = new TspProblem(distances);
        var ct = new CancellationTokenSource();
        //ct.CancelAfter(2000);
        var best = await pso.SolveAsync(problem, ct.Token);
        ct.Dispose();
    }
    
    
    public static async Task Run()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
        });
        _logger = loggerFactory.CreateLogger<ParticleSwarmAlgorithm>();
        
        //await Test1_F1();
        //await Test2_Rastrigin();
        await Test3_TSP();
    }
}