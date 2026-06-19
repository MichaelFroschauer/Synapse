using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.HITL;

public interface IManualEditApplier
{
    ISolution Apply(ISolution candidate, ISolution manual);
}