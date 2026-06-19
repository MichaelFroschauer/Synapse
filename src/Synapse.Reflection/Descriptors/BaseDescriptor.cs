using Synapse.OptimizationCore.Attributes;
using Synapse.Reflection.Models;

namespace Synapse.Reflection.Descriptors;

public class BaseDescriptor : IClassDescriptor
{
    public Type ImplementationType { get; init; }
    public UserSettableClass? SettableClass { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }

    protected BaseDescriptor(Type type, DisplayInfoAttribute attr)
    {
        ImplementationType = type;
        SettableClass =  ImplementationType?.BuildUserSettableClass();
        Name = attr?.Name ?? type.Name;
        Description = attr?.Description ?? "";
    }
}
