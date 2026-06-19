using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.UnitTest.Utils;

public sealed class DummySolution : ISolution
{
    public double? Fitness { get; set; }
    public int Length => 0;
    public Parameter[] GetParameters() => [];
    public void SetParameters(Parameter[] parameters) => throw new NotSupportedException();
    public Parameter GetParameter(int index) => throw new NotSupportedException();
    public void SetParameter(int index, Parameter parameter) => throw new NotSupportedException();
    public ISolution CreateRandom() => this;
    public ISolution Clone() => this;
    public ISolutionSimilarity GetDefaultSolutionSimilarityClass() => throw new NotSupportedException();
}
