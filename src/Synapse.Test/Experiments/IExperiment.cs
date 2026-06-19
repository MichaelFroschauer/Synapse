using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Test.Experiments;

public interface IExperiment
{
    void Setup();
    Task RunAsync();
}
