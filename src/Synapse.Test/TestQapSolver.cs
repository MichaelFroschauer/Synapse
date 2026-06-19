using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.HITL;
using Synapse.HITL.Problems.QAP;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;
using Synapse.Problems.QAP;
using Synapse.Test.Utils;

namespace Synapse.Test;

public class TestQapSolver : IMetaheuristicTester
{
    private static void HandleGenerationFinished(ProgressEventArgs? e)
    {
        Console.WriteLine(e?.Message);
        // if (e?.Iteration % e?.Config?.HitlController?.AskPreferenceInterval == 0)
        // {
        //     Console.WriteLine("Give a new Mutation Probability:");
        //     try
        //     {
        //         var newMutation = Convert.ToDouble(Console.ReadLine());
        //         var gaConfig = (GeneticAlgorithmConfig?)e?.Config;
        //         if (gaConfig is not null)
        //         {
        //             gaConfig.MutationProbability = newMutation;
        //         }
        //     }
        //     catch
        //     {
        //         // ignored
        //     }
        // }

        if (e?.Iteration == 200)
        {
            // e?.Config?.HitlController?.AddConstraint(new QapFixAssignmentConstraint(10, 10));
            // e?.Config?.HitlController?.AddConstraint(new QapFixAssignmentConstraint(0, 0));
            Console.ReadKey();
        }
    }

    public static async Task Run()
    {
        var algorithmCtrl = new AlgorithmController();
        algorithmCtrl.OnProgress += HandleGenerationFinished;
        algorithmCtrl.Paused += () => Console.WriteLine("Paused");
        algorithmCtrl.Resumed += () => Console.WriteLine("Resumed");
        var hitlCtrl = new HitlController
        {
            AskPreferenceInterval = 100,
            // AskPreference = (solutions1, solutions2) =>
            // {
            //     var s1 = solutions1.First();
            //     var s2 = solutions2.First();
            //     Console.WriteLine($"Fitness S1: {s1.Fitness}, Solution: {s1}");
            //     Console.WriteLine($"Fitness S2: {s2.Fitness}, Solution: {s2}");
            //     Console.WriteLine("What solution would you like to choose? (1, 2)");
            //     var userSelect = Console.ReadLine();
            //     return userSelect == "2" ? 2 : 1;
            // }
        };
        
        //hitlCtrl.AddConstraint(new QapClusterConstraint([0,1,2], [0,1,2]));
        //hitlCtrl.AddConstraint(new QapClusterConstraint([3,4,5], [1,2,3]));
        //hitlCtrl.AddConstraint(new QapClusterConstraint([10,11], [10,11]));
        //hitlCtrl.AddConstraint(new QapFixAssignmentConstraint(10, 10));
        //hitlCtrl.AddConstraint(new QapFixAssignmentConstraint(0, 0));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(5, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(11, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(8, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(9, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(3, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenAssignmentConstraint(7, 1));
        //hitlCtrl.AddConstraint(new QapForbiddenPairConstraint(9, 2));
        //hitlCtrl.AddConstraint(new QapForbiddenPairConstraint(4, 2));

        var cfg = new GeneticAlgorithmConfig
        {
            PopulationSize = 100,
            MaxIterations = 1000,
            CrossoverProbability = 0.99,
            MutationProbability = 0.1,
            Selection = new TournamentSelection(4),
            Selection2 = new TournamentSelection(2),
            AlgorithmController = algorithmCtrl,
            HitlController = hitlCtrl,
            ProgressInterval = 10,
        };
        MapperBootstrap.RegisterAllMappings();
        var logger = LoggerUtils.GetConsoleLogger<GeneticAlgorithm>();
        IMetaheuristic ga = new GeneticAlgorithm(cfg, logger);
        string path =
            "/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/Synapse.Problems/QAP/Data/tai12a.dat";
        IProblemParser problemParser = new QapProblemParser();
        IProblem problem = (QapProblem)problemParser.Parse(path);
        
        hitlCtrl.AddConstraint(new QapPairwiseAssignmentConstraint((QapProblem)problem, 1, 2, 1));
        
        var bestParameters = (await ga.SolveAsync(problem, CancellationToken.None)) as QapSolution;
        Console.WriteLine(Math.Abs(bestParameters?.Fitness ?? 0.0) + "\t" + bestParameters);
    }
}
