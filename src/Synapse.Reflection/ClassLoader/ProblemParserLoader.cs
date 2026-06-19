using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.Problems;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public class ProblemParserLoader : BaseClassLoader
{
    private readonly ITypeRegistry _registry;

    public ProblemParserLoader(ITypeRegistry registry)
    {
        _registry = registry;
    }
    
    public override IReadOnlyList<ProblemParserDescriptor> Load()
    {
        var types = _registry.GetAssignableTypes<IProblemParser>();
        var list = new List<ProblemParserDescriptor>(capacity: types.Count);
        foreach (var t in types)
            if (TryBuild(t, out var d))
                list.Add(d!);
        return list;
    }

    private bool TryBuild(Type t, out ProblemParserDescriptor? descriptor) =>
        TryBuild<ProblemParserDescriptor, IProblemParser, ProblemParserAttribute>(
            t, (typ, attr) => new ProblemParserDescriptor(typ, attr), out descriptor
        );
}
