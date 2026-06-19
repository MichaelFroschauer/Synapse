using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems;

public interface IProblemParser
{
    IProblem Parse(string input);
}