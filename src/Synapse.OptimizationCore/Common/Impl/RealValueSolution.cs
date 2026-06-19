using System.Text;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.OptimizationCore.Common.Impl;

public class RealValueSolution : BaseSolution<double>
{
    public RealValueSolution(int length) : base(length)
    {
        var values = Enumerable.Range(0, Length).Select(_ => RandomProvider.Value.GetDouble()).ToArray();
        SetParameters(values);
    }

    public RealValueSolution(Parameter[] parameters) : base(parameters) { }
    
    public RealValueSolution(double[] values) : base(values.Length) => SetParameters(values);

    public override ISolution Create(double[] parameters) => new RealValueSolution(parameters);
    
    public override ISolution CreateRandom()
    {
        var solution = new RealValueSolution(Length);
        for (var i = 0; i < Length; i++)
        {
            solution.SetParameter(i, new Parameter(RandomProvider.Value.GetDouble()));
        }
        return solution;
    }

    public override ISolutionSimilarity GetDefaultSolutionSimilarityClass() =>
        new RealValueSolutionEuclideanSimilarity();

    public override string ToString()
    {
        var parameters = GetParametersWithType();
        StringBuilder sb = new StringBuilder();
        sb.Append('[');

        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(parameters[i].ToString("F2"));
        }

        sb.Append(']');
        return sb.ToString();
    }
}
