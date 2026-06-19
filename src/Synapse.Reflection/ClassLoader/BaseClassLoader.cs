using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public abstract class BaseClassLoader : IClassLoader
{
    public abstract IReadOnlyList<IClassDescriptor> Load();

    protected static bool TryBuild<TDescriptor, TClass, TAttribute>(
        Type t,
        Func<Type, TAttribute, TDescriptor> createDescriptor,
        out TDescriptor? descriptor)
        where TClass : class
        where TAttribute : DisplayInfoAttribute
    {
        descriptor = default;

        if (t.IsAbstract) return false;
        if (!typeof(TClass).IsAssignableFrom(t)) return false;
        
        var attr = t.GetCustomAttribute<TAttribute>(inherit: true);
        if (attr is null || !attr.Visible) return false;

        descriptor = createDescriptor(t, attr);
        return true;
    }
}
