using Synapse.Reflection.ClassLoader;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection;

public class DescriptorRegistry : IDescriptorRegistry
{
    private readonly ITypeRegistry _typeRegistry;
    private readonly Dictionary<Type, IReadOnlyList<IClassDescriptor>> _descriptors = new();

    public DescriptorRegistry(ITypeRegistry typeRegistry)
    {
        _typeRegistry = typeRegistry;

        var algorithmLoader = new AlgorithmLoader(typeRegistry);
        var algorithmDescriptors = algorithmLoader.Load();
        _descriptors.Add(typeof(AlgorithmDescriptor), algorithmDescriptors);
      
        var problemParserLoader = new ProblemParserLoader(typeRegistry);
        var problemInstanceLoader = new ProblemInstanceLoader(typeRegistry, problemParserLoader);
        var problemLoader = new ProblemLoader(typeRegistry, problemInstanceLoader);
        var problemDescriptors = problemLoader.Load();
        _descriptors.Add(typeof(ProblemDescriptor), problemDescriptors);
    }

    public IEnumerable<TDescriptor> Get<TDescriptor>() where TDescriptor : IClassDescriptor
    {
        if (_descriptors.TryGetValue(typeof(TDescriptor), out var descriptors))
        {
            return descriptors.OfType<TDescriptor>();
        }
        
        return Enumerable.Empty<TDescriptor>();
    }
}
