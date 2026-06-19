using Synapse.Reflection.Models;

namespace Synapse.Reflection;

public interface ITypeRegistry
{
    IReadOnlyList<Type> GetAssignableTypes(Type baseType);
    IReadOnlyList<Type> GetAssignableTypes<TBase>();
    IReadOnlyList<UserSettableClass> GetUserSettableClassesFor(Type baseType);
    IReadOnlyList<UserSettableClass> GetUserSettableClassesFor<TBase>();
}
