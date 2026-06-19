namespace Synapse.Problems;

public interface IFunctionProblemInstance
{
    int Dimensions { get;  }
    Func<double[], double> Function { get; }
    bool Minimize { get; }
}
