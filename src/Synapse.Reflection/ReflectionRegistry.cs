
using Synapse.Reflection.Models;

namespace Synapse.Reflection;

using System.Collections.Concurrent;
using System.Reflection;

public sealed class ReflectionRegistry : ITypeRegistry
{
    private readonly Lazy<IReadOnlyList<Assembly>> _assemblies;
    private readonly ConcurrentDictionary<Type, Lazy<IReadOnlyList<Type>>> _assignableCache = new();

    public ReflectionRegistry(Func<IEnumerable<Assembly>>? assemblyProvider = null)
    {
        _assemblies = new Lazy<IReadOnlyList<Assembly>>(() =>
            (assemblyProvider?.Invoke() ?? DefaultAssemblies())
            .Distinct()
            .ToArray());
    }

    public IReadOnlyList<Type> GetAssignableTypes(Type baseType) =>
        _assignableCache.GetOrAdd(baseType, t => new Lazy<IReadOnlyList<Type>>(() =>
            LoadableTypes()
                .Where(x => !x.IsAbstract && !x.IsGenericTypeDefinition && t.IsAssignableFrom(x))
                .ToArray()
        )).Value;
    
    public IReadOnlyList<Type> GetAssignableTypes<TBase>() => GetAssignableTypes(typeof(TBase));
    
    public IReadOnlyList<UserSettableClass> GetUserSettableClassesFor(Type baseType) =>
        GetAssignableTypes(baseType).BuildUserSettableClasses();

    public IReadOnlyList<UserSettableClass> GetUserSettableClassesFor<TBase>() =>
        GetUserSettableClassesFor(typeof(TBase));

    private IReadOnlyList<Type> LoadableTypes() =>
        _assemblies.Value.SelectMany(GetLoadableTypes).ToArray();

    private static IEnumerable<Type> GetLoadableTypes(Assembly a)
    {
        try { return a.GetTypes().Where(t => t is not null)!; }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
        catch { return Array.Empty<Type>(); }
    }

    private static IEnumerable<Assembly> DefaultAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();

        var extra = new List<Assembly>();
        var entry = Assembly.GetEntryAssembly();
        if (entry != null) extra.Add(entry);
        var exec = Assembly.GetExecutingAssembly();
        if (exec != null) extra.Add(exec);

        return loaded.Concat(extra).Distinct();
    }
}
