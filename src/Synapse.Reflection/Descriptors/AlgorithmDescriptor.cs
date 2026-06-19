using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Reflection.Descriptors;

public class AlgorithmDescriptor : BaseDescriptor
{
    public string Category { get; init; }
    public int Order { get; init; }
    public bool Visible { get; init; }
    public string IconPath { get; init; }
    public AlgorithmType AlgorithmType { get; init; }

    public AlgorithmDescriptor(Type type, AlgorithmAttribute attr) : base(type, attr)
    {
        Category = attr?.Category.ToString() ?? "";
        Visible = attr?.Visible ?? true;
        IconPath = attr?.IconPath ?? "";
        AlgorithmType =  attr?.AlgorithmType ?? AlgorithmType.Unknown;
    }

    // public IMetaheuristic CreateProblem(IServiceProvider? sp = null)
    // {
    //     if (sp != null)
    //         return (IMetaheuristic)ActivatorUtilities.CreateProblem(sp, ImplementationType);
    //     
    //     return (IMetaheuristic?)Activator.CreateProblem(ImplementationType) as IMetaheuristic
    //            ?? throw new InvalidOperationException($"Cannot create instance of {ImplementationType.FullName}");
    // }
}
