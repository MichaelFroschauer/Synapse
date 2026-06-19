using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Interfaces;

// A problem provides initial solutions, evaluates fitness and can do problem-specific helpers
public interface IProblem
{
    ISolution CreateRandomSolution();

    /// <summary>
    /// Returns a fitness evaluator tied to this problem (allows caching, batch eval, constraints, etc.)
    /// </summary>
    IFitnessEvaluator GetFitnessEvaluator();

    /// <summary>
    /// Optional: a short textual description (for logging/monitoring)
    /// </summary>
    string Describe();
    
    ProblemType ProblemType { get; }
}
