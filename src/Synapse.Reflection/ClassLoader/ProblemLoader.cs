using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public class ProblemLoader : BaseClassLoader
{
    private readonly ITypeRegistry _registry;
    private readonly List<ProblemInstanceDescriptor> _problemInstances = new();

    public ProblemLoader(ITypeRegistry registry, ProblemInstanceLoader instanceLoader)
    {
        _registry = registry;
        _problemInstances.AddRange(instanceLoader.Load()
            .Select(i => i as ProblemInstanceDescriptor)
            .Where(i => i is not null)!);
    }

    public override IReadOnlyList<ProblemDescriptor> Load()
    {
        var types = _registry.GetAssignableTypes<IProblem>();
        var list = new List<ProblemDescriptor>(capacity: types.Count);
        foreach (var t in types)
        {
            if (TryBuild(t, out var d))
            {
                var relevantInstances = _problemInstances
                    .Where(i => i.ProblemTypeEnum == d!.ProblemType);
                d!.ProblemInstances.AddRange(relevantInstances);
                list.Add(d!);
            }
        }

        return list;
    }

    private bool TryBuild(Type t, out ProblemDescriptor? descriptor) =>
        TryBuild<ProblemDescriptor, IProblem, ProblemAttribute>(
            t, (typ, attr) => new ProblemDescriptor(typ, attr), out descriptor
        );
}
