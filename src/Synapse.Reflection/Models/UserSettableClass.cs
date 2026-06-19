using System.Reflection;
using System.Text;

namespace Synapse.Reflection.Models;


public class UserSettableClass<T>
{
    public UserSettableClass()
    {}
}

public class UserSettableClass
{
    public required string DisplayName { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required ConstructorInfo Constructor { get; init; }
    public Type BaseType { get; init; }
    public Type? ApplicableOnSolution { get; init; }
    public List<SettableProperty> ConstructorParameters { get; } = new();
    public List<UserSettableProperty> UserProperties { get; } = new();

    public IReadOnlyList<SettableProperty> SettableProperties
        => ConstructorParameters;
    public IReadOnlyList<UserSettableProperty> UserSettableProperties
        => ConstructorParameters.Where(p => p.UserSettable).Concat(UserProperties)
            .OfType<UserSettableProperty>().ToList();
    public IReadOnlyList<SettableProperty> NonUserSettableProperties
        => ConstructorParameters.Where(p => !p.UserSettable).OfType<SettableProperty>().ToList();

    public object GetInstance()
    {
        var instance = Constructor.Invoke(GetConstructorParameters());
        foreach (var property in UserProperties)
        {
            BaseType.GetProperty(property.Name)?.SetValue(instance, property.Value);
        }
        return instance;
    }
    
    public bool TryGetInstance(out object? instance, out string? error)
    {
        try { instance = GetInstance(); error = null; return true; }
        catch (Exception ex) { instance = null; error = ex.Message; return false; }
    }
    
    public bool TryGetInstance<T>(out T? instance, out string? error)
        where T : class
    {
        try
        {
            instance = GetInstance() as T;
            error = null;
            return true;
        }
        catch (Exception ex) { instance = null; error = ex.Message; return false; }
    }

    public bool ConstructorContainsDerivedType(Type baseType)
        => Constructor.GetParameters().Any(p => baseType.IsAssignableFrom(p.ParameterType));

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DisplayName}] {Name}");
        sb.AppendLine("{");
        foreach (var p in ConstructorParameters) sb.AppendLine("    " + p);
        sb.Append("}");
        return sb.ToString();
    }

    private object[] GetConstructorParameters()
    {
        var ctorParams = Constructor.GetParameters();

        return ctorParams.Select(param =>
        {
            var prop = ConstructorParameters.FirstOrDefault(p =>
                string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));

            if (prop?.Value is null)
            {
                if (param.HasDefaultValue) return param.DefaultValue!;
                throw new InvalidOperationException($"{Name}: Parameter '{param.Name}' is not set.");
            }

            return prop.Value;
        }).ToArray();
    }
}
