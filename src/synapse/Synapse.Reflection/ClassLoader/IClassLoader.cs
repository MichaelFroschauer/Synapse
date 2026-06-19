using System.Reflection;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public interface IClassLoader
{
    IReadOnlyList<IClassDescriptor> Load();
}
