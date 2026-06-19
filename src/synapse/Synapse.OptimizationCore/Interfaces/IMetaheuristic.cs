using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Interfaces;

// Basic metaheuristic interface
public interface IMetaheuristic
{
    event EventHandler<ProgressEventArgs> ProgressChanged;
    void SetConfig(IAlgorithmConfig config);
    Task<ISolution> SolveAsync(IProblem problem, CancellationToken ct = default);
}
