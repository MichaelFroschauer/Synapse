using Synapse.OptimizationCore.Interfaces;

namespace Synapse.JobManagement;

public record IterationFitness(int Iteration, double Fitness);
public record IterationDiversity(int Iteration, double Diversity);
public record IterationPopulationSize(int Iteration, double PopulationSize);

public class JobInfo
{
    public Guid Id { get; init; }
    public JobStatus Status { get; internal set; }
    public double? BestFitness { get; internal set; }
    public ISolution? BestSolution { get; internal set; }
    public ISolution? CurrentSolution { get; internal set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; internal set; }
    public DateTimeOffset? FinishedAt { get; internal set; }
    public string? LastMessage { get; internal set; }
    public int IterationCount { get; internal set; }
    public List<IterationFitness> FitnessPerIteration { get; } = new();
    public List<IterationFitness> RawFitnessPerIteration { get; } = new();
    public List<IterationFitness> PopulationFitnessPerIteration { get; } = new();
    public List<IterationFitness> PopulationRawFitnessPerIteration { get; } = new();
    public List<IterationDiversity> DiversityPerIteration { get; } = new();
    public List<IterationFitness> MeanFitnessPerIteration { get; } = new();
    public List<IterationFitness> MedianFitnessPerIteration { get; } = new();
    public List<IterationFitness> WorstFitnessPerIteration { get; } = new();
    public List<IterationPopulationSize> PopulationSizePerIteration { get; } = new();

    public required IAlgorithmConfig Config { get; init; }
    public required IProblem Problem { get; init; }
    public required IProblemInstance ProblemInstance { get; init; }
    public int LastSnapshotIteration { get; set; } = -1;
}
