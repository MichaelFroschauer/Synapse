namespace Synapse.OptimizationCore.Interfaces;

public interface IFitnessEvaluator
{
    double Evaluate(ISolution solution);
    double[] Evaluate(IEnumerable<ISolution> solutions)
        => solutions.Select(Evaluate).ToArray();
    
    bool Minimize { get; }
}
