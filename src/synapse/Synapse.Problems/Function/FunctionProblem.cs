using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.Function;

[Problem(Name = "Function Problem", ProblemType = ProblemType.Function)]
public class FunctionProblem(IFunctionProblemInstance functionProblemInstance) : IProblem
{
    public ProblemType ProblemType => ProblemType.Function;
    public ISolution CreateRandomSolution() => new FunctionSolution(functionProblemInstance.Dimensions);
    public IFitnessEvaluator GetFitnessEvaluator() => new FunctionFitnessEvaluator(functionProblemInstance.Function, functionProblemInstance.Minimize);

    public string Describe() =>  "Function Problem for a variety of function problem instances.";
}
