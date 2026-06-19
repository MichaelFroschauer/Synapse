using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Problems.QAP;

public class QapSolution : PermutationSolution
{
    public QapSolution(Parameter[] parameters) : base(parameters) { }
    public QapSolution(int length) : base(length) { }
    public QapSolution(int[] assignment) : base(assignment) { }
    public override ISolution Create(int[] parameters) => new QapSolution(parameters);
    public override ISolution CreateRandom()
    {
        int[] uniqueInts = RandomProvider.Value.GetUniqueInts(Length, 0, Length);
        var parameters = uniqueInts.Select(i => new Parameter(i)).ToArray();
        return new QapSolution(parameters);
    }
    
    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() => new QapSolutionSimilarity();

    public override string ToString()
    {
        var perm = GetParametersWithType();
        var indexed = string.Join(", ", perm.Select((v,i) => $"{i}->{v}"));
        return $"(Location -> Facility) [{indexed}]";
    }
}
