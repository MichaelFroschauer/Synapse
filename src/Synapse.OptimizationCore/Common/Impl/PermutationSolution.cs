using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.OptimizationCore.Common.Impl;

public class PermutationSolution : BaseSolution<int>
{
    public PermutationSolution(int length) : base(length)
    {
        int[] uniqueInts = RandomProvider.Value.GetUniqueInts(Length, 0, Length);
        SetParameters(uniqueInts);
    }
    public PermutationSolution(Parameter[] parameters) : base(parameters) { }
    public PermutationSolution(int[] parameters) : base(parameters.Length) => SetParameters(parameters);
    public override ISolution CreateRandom()
    {
        int[] uniqueInts = RandomProvider.Value.GetUniqueInts(Length, 0, Length);
        var parameters = uniqueInts.Select(i => new Parameter(i)).ToArray();
        return new PermutationSolution(parameters);
    }
    public override ISolution Create(int[] parameters) => new PermutationSolution(parameters);
    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() => new PermutationSolutionSimilarity();
    
    public override string ToString() => string.Join("-", GetParameters());
}
