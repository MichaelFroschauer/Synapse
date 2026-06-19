using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.HITL.Problems.TSP;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;
using Synapse.Problems.TSP;

namespace Synapse.Test.Experiments;

public class Experiment1(IJobManager jobManager) : BaseExperiment(jobManager)
{
    private Guid _gaJobBaseline;
    private Guid _gaJobIteration0;
    private Guid _gaJobIteration100;

    public override void Setup()
    {
        base.Setup();

        var problemInstancesBasePath = ResolveProblemInstancesBasePath(null);
        string problemInstancePath = $"{problemInstancesBasePath}/TSP/berlin52.tsp";
        IProblemParser parser = new TspProblemParser();
        IProblem problem = parser.Parse(problemInstancePath);
        IProblemInstance problemInstance = new ProblemInstance { Name = "berlin52", ProblemType = ProblemType.Tsp };

        IAlgorithmConfig config = new GeneticAlgorithmConfig()
        {
            Name = "GA TSP Constraint Baseline",
            MaxIterations = 500,
            PopulationSize = 500,
            Selection = new TournamentSelection(3),
            Crossover = new OrderedCrossover(),
            Mutation = new TworsMutation(),
            CrossoverProbability = 0.95,
            MutationProbability = 0.05,
            Seed = 12345678
        };

        /* Baseline */
        var c1 = (GeneticAlgorithmConfig)config.Clone(); c1.Name = "GA TSP Baseline";
        _gaJobBaseline = JobManager.QueueJob(c1, problem, problemInstance);
        
        /* Intervention at iteration 0 */
        var c2 = (GeneticAlgorithmConfig)config.Clone(); c2.Name = "GA TSP Constraint Iteration 0";
        _gaJobIteration0 = JobManager.QueueJob(c2, problem, problemInstance);
        InterventionRunner.For(_gaJobIteration0)
            .AddIntervention(0, new TspClusterConstraint([33, 34, 35, 38, 39]) { RepairSolution = true })
            .AddIntervention(0, new TspFixedEdgeConstraint([(34, 35), (23, 47)]) { RepairSolution = true })
            .AddIntervention(0, new TspForbiddenEdgeConstraint(1, 51) { RepairSolution = true });
        
        /* Intervention at iteration 100 */
        var c3 = (GeneticAlgorithmConfig)config.Clone(); c3.Name = "GA TSP Constraint Iteration 100";
        _gaJobIteration100 = JobManager.QueueJob(c3, problem, problemInstance);
        InterventionRunner.For(_gaJobIteration100)
            .AddIntervention(100, new TspClusterConstraint([33, 34, 35, 38, 39]) { RepairSolution = true })
            .AddIntervention(100, new TspFixedEdgeConstraint([(34, 35), (23, 47)]) { RepairSolution = true })
            .AddIntervention(100, new TspForbiddenEdgeConstraint(1, 51) { RepairSolution = true });
    }
    
    public override async Task RunAsync()
    {
        try
        {
            await Task.WhenAll([
                JobManager.StartQueuedJobAsync(_gaJobBaseline),
                JobManager.StartQueuedJobAsync(_gaJobIteration0),
                JobManager.StartQueuedJobAsync(_gaJobIteration100)
            ]);
        }
        finally
        {
            InterventionRunner?.Dispose();
        }
    }
}