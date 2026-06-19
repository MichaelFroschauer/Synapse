using System.Collections.ObjectModel;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection;

public interface IDescriptorRegistry
{
    public IEnumerable<TDescriptor> Get<TDescriptor>() where TDescriptor : IClassDescriptor;
}
