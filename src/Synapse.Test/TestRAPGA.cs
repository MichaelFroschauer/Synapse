using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.Algorithms.RAPGA;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.Test;

public class TestRAPGA : IMetaheuristicTester
{
    public static async Task Run()
    {
        MapperBootstrap.RegisterAllMappings();
        RAPGAConfig config = new RAPGAConfig();
        IMetaheuristic algorithm = new RAPGA(config);
        algorithm.ProgressChanged += (sender, args) =>
        {
            Console.WriteLine($"{args.Iteration} | C Fitness: {args.CurrentBestFitness} | G Fitness: {args.BestFitness} | {args.CurrentBestSolution}");
        };
        

        var problemParser = new TspProblemParser();
        IProblem problem = problemParser.Parse("/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/ProblemInstances/TSP/berlin52.tsp"); 
        await algorithm.SolveAsync(problem);
    }
}
