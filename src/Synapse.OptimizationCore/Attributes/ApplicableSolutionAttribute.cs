namespace Synapse.OptimizationCore.Attributes;

public class ApplicableSolutionAttribute(Type solutionType) : Attribute
{
    public Type SolutionType { get; set; } = solutionType;
}
