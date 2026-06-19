using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.Reflection.Models;

namespace Synapse.Reflection;

public static class ReflectionExtensions
{
    public static IEnumerable<UserSettableClass>
        ApplicableToSolution(this IEnumerable<UserSettableClass> typeList, Type solutionType) =>
        typeList.Where(t => t.ApplicableOnSolution != null && t.ApplicableOnSolution.IsAssignableFrom(solutionType));

    public static IReadOnlyList<UserSettableClass> BuildUserSettableClasses(this IEnumerable<Type> types) =>
        types.Select(BuildUserSettableClass).ToArray();

    public static UserSettableClass BuildUserSettableClass(this Type t)
    {
        var displayInfo = t.GetCustomAttribute<DisplayInfoAttribute>(inherit: true);
        var modifiesAttr = t.GetCustomAttribute<ApplicableSolutionAttribute>(inherit: true);
        var ctor = SelectConstructor(t);
        var usc = new UserSettableClass
        {
            DisplayName = displayInfo?.Name ?? t.Name,
            Name = t.Name,
            BaseType = t,
            Description = displayInfo?.Description ?? null,
            Constructor = ctor,
            ApplicableOnSolution = modifiesAttr?.SolutionType
        };
        foreach (var p in ctor.GetParameters())
        {
            var pDisplayInfo = p.GetCustomAttribute<DisplayInfoAttribute>(inherit: false);
            if (pDisplayInfo != null)
            {
                usc.ConstructorParameters.Add(new UserSettableProperty
                {
                    DisplayName = pDisplayInfo.Name, Name = p.Name ?? string.Empty, Type = p.ParameterType
                });
            }
            else
            {
                usc.ConstructorParameters.Add(new SettableProperty
                {
                    Name = p.Name ?? string.Empty, Type = p.ParameterType
                });
            }
        }

        foreach (var p in t.GetProperties())
        {
            var pDisplayInfo = p.GetCustomAttribute<DisplayInfoAttribute>(inherit: false);
            if (pDisplayInfo != null)
            {
                usc.UserProperties.Add(new UserSettableProperty
                {
                    DisplayName = pDisplayInfo.Name, Name = p.Name ?? string.Empty, Type = p.PropertyType
                });
            }
        }

        return usc;
    }

    public static T CreateInstanceWithDefaults<T>(this Type type) where T : Type 
    {
        if (CreateInstanceWithDefaults(type) is T instance) return instance;
        throw new Exception($"Unable to create instance of type {type}");
    }

    public static object? CreateInstanceWithDefaults(this Type type)
    {
        // use parameterless constructor
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null) return Activator.CreateInstance(type);

        // search constructor with the least amount of parameters
        var ctors = type.GetConstructors();
        if (!ctors.Any()) throw new InvalidOperationException($"No public constructor for {type.FullName} found.");
        var ctor = ctors.OrderBy(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];

            // If the parameter got a default value, use it
            if (p.HasDefaultValue)
            {
                args[i] = p.DefaultValue;
                continue;
            }

            // Otherwise use a reasonable default value for this type
            args[i] = GetDefaultValueForType(p.ParameterType);
        }

        return ctor.Invoke(args);
    }

    private static object? GetDefaultValueForType(Type t)
    {
        if (!t.IsValueType) return null; // Nullable<T> -> null
        if (Nullable.GetUnderlyingType(t) != null) return null;

        // special treatment for Guid
        if (t == typeof(Guid)) return Guid.Empty;

        // for Enums -> 0 cast
        if (t.IsEnum) return Activator.CreateInstance(t);

        // for values types (int, double, struct...) -> Activator.CreateProblem
        return Activator.CreateInstance(t);
    }

    private static ConstructorInfo SelectConstructor(Type t)
    {
        var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        return ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault() ??
               throw new InvalidOperationException($"No public constructor found for {t.FullName}.");
    }
}