using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Problems.TSP;

// A concrete solution for TSP: sequence of city indices
public class TspSolution : PermutationSolution
{
    public TspSolution(Parameter[] parameters) : base(parameters) { }
    public TspSolution(int length) : base(length) { }
    public TspSolution(int[] tour) : base(tour) { }
    
    public override ISolution CreateRandom()
    {
        int[] uniqueInts = RandomProvider.Value.GetUniqueInts(Length, 0, Length);
        var parameters = uniqueInts.Select(i => new Parameter(i)).ToArray();
        return new TspSolution(parameters);
    }
    
    public override ISolution Create(int[] permutation) => new TspSolution(permutation);
    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() => new TspSolutionSimilarity();
    public override string ToString() => string.Join("-", GetParameters());
}
