using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.HITL;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;
using Synapse.Problems.Function;
using Synapse.Test.Utils;
using GeneticAlgorithm = Synapse.Algorithms.GeneticAlgorithm.GeneticAlgorithm;

namespace Synapse.Test;

public class TestGeneticAlgorithmRealValues : IMetaheuristicTester
{
    private static void HandleGenerationFinished(ProgressEventArgs? e)
    {
        Console.WriteLine(e?.Message);
    }
    
    public static async Task Run()
    {
        var algorithmCtrl = new AlgorithmController();
        algorithmCtrl.OnProgress += HandleGenerationFinished;
        algorithmCtrl.Paused += () => Console.WriteLine("Paused");
        algorithmCtrl.Resumed += () => Console.WriteLine("Resumed");
        
        var hitlCtrl = new HitlController
        {
            AskPreferenceInterval = 1000
        };
        
        var cfg = new GeneticAlgorithmConfig {
            PopulationSize = 700,
            MaxIterations = 500,
            CrossoverProbability = 0.95,
            MutationProbability = 0.1,
            AlgorithmController = algorithmCtrl,
            HitlController = hitlCtrl,
            Selection = new EliteSelection(1),
            Crossover = new UniformCrossover(0.5f),
            Mutation = new FlipBitMutation(),
            ProgressInterval = 10,
        };
        
        MapperBootstrap.RegisterAllMappings();
        var logger = LoggerUtils.GetConsoleLogger<GeneticAlgorithm>();
        IMetaheuristic ga = new GeneticAlgorithm(cfg, logger);


        IFunctionProblemInstance functionProblemInstance = new F1FunctionProblemInstance();
        IProblem problem = new FunctionProblem(functionProblemInstance);
        var bestParameters = (await ga.SolveAsync(problem, CancellationToken.None)) as FunctionSolution;
        Console.WriteLine(Math.Abs(bestParameters?.Fitness ?? 0.0) + "\t" + bestParameters);
        
        // https://diegogiacomelli.com.br/function-optimization-with-geneticsharp/
    }
}
