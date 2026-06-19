using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public class AlgorithmLoader : BaseClassLoader
{
    private readonly ITypeRegistry _registry;

    public AlgorithmLoader(ITypeRegistry registry)
    {
        _registry = registry;
    }

    public override IReadOnlyList<AlgorithmDescriptor> Load()
    {
        var types = _registry.GetAssignableTypes<IMetaheuristic>();
        var list = new List<AlgorithmDescriptor>(capacity: types.Count);
        foreach (var t in types)
            if (TryBuild(t, out var d))
                list.Add(d!);
        return list;
    }

    private bool TryBuild(Type t, out AlgorithmDescriptor? descriptor) =>
        TryBuild<AlgorithmDescriptor, IMetaheuristic, AlgorithmAttribute>(
            t, (typ, attr) => new AlgorithmDescriptor(typ, attr), out descriptor
        );
}
