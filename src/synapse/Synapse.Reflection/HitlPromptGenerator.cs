using System.Reflection;
using System.Text;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.Reflection.Models;

namespace Synapse.Reflection;

/// <summary>
/// Generates textual API descriptions of IConstraint and IManualEditApplier classes
/// for use in AI chat prompts. Classes are discovered via reflection and filtered
/// by the <see cref="ApplicableSolutionAttribute"/> so that only classes relevant
/// to the current solution type are included.
/// </summary>
public static class HitlPromptGenerator
{
    /// <summary>
    /// Generates prompt lines describing available constraints and manual-edit appliers
    /// applicable to the given <paramref name="solutionType"/>.
    /// </summary>
    public static IEnumerable<string> GenerateProblemSpecifics(Type solutionType, ITypeRegistry? registry = null)
    {
        registry ??= new ReflectionRegistry();

        var constraintLines = GenerateClassLines<IConstraint>(registry, solutionType);
        var editApplierLines = GenerateClassLines<IManualEditApplier>(registry, solutionType);

        var lines = new List<string>();

        if (constraintLines.Count > 0)
        {
            lines.Add("You can add constraints with the IHitlController. You have these constraints available:");
            lines.AddRange(constraintLines);
        }

        if (editApplierLines.Count > 0)
        {
            lines.Add("");
            lines.Add("You can add human edit suggestions with the IHitlController by adding solution segments as suggestions.");
            lines.AddRange(editApplierLines);
        }

        return lines;
    }

    /// <summary>
    /// Generates the IHitlController API description lines for use in the prompt header.
    /// </summary>
    public static IEnumerable<string> GenerateHitlControllerDescription()
    {
        return
        [
            "  and IHitlController exposes only:",
            "    -  void AddManualEdit(ISolution s, IManualEditApplier a, double probability = 1.0, string? name = null);",
            "    -  void ClearManualEdit(string name);",
            "    -  void AddConstraint(IConstraint c);",
            "    -  void RemoveConstraint(IConstraint c);",
            "    -  void AddSolutionPreference(Func<ISolution, bool> matcher, double weight = 1.0, string? name = null);",
            "    -  void AddSolutionPreference(ISolutionSimilarity similarityEvaluator, ISolution referenceSolution, double weight = 1.0, string? name = null);",
            "    -  void ClearPreference(string name);",
            "    -  void ClearPreferences();",
            "    -  void SetParameter(string key, object value);",
        ];
    }

    /// <summary>
    /// Discovers all implementations of <typeparamref name="TInterface"/> that are applicable
    /// to the given solution type and formats each as a single-line class signature with description.
    /// </summary>
    private static List<string> GenerateClassLines<TInterface>(ITypeRegistry registry, Type solutionType)
    {
        var classes = registry.GetUserSettableClassesFor<TInterface>()
            .Where(c => IsApplicableToSolution(c, solutionType))
            .ToList();

        var lines = new List<string>();
        foreach (var cls in classes)
        {
            var signature = FormatClassSignature(cls, typeof(TInterface));
            var description = cls.Description;
            var line = string.IsNullOrWhiteSpace(description)
                ? $"    - {signature}"
                : $"    - {signature}  // {description}";
            lines.Add(line);
        }

        return lines;
    }

    /// <summary>
    /// Checks if a <see cref="UserSettableClass"/> is applicable to the given solution type.
    /// If the class has no ApplicableSolutionAttribute it is considered applicable to all solution types.
    /// </summary>
    private static bool IsApplicableToSolution(UserSettableClass cls, Type solutionType)
    {
        if (cls.ApplicableOnSolution == null)
            return true; // no restriction => applicable to all
        return cls.ApplicableOnSolution.IsAssignableFrom(solutionType);
    }

    /// <summary>
    /// Formats a class as a C# constructor signature, e.g.:
    /// <c>class TspStartPositionConstraint(int start) : IConstraint</c>
    /// Only user-settable parameters (those with DisplayInfo) are included, since
    /// non-user parameters (like problem instances) are injected automatically.
    /// </summary>
    private static string FormatClassSignature(UserSettableClass cls, Type interfaceType)
    {
        var sb = new StringBuilder();
        sb.Append("class ");
        sb.Append(cls.Name);
        sb.Append('(');

        var userParams = cls.ConstructorParameters
            .Where(p => p.UserSettable)
            .OfType<UserSettableProperty>()
            .ToList();

        for (int i = 0; i < userParams.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatTypeName(userParams[i].Type));
            sb.Append(' ');
            sb.Append(userParams[i].Name);
        }

        sb.Append(')');
        sb.Append(" : ");
        sb.Append(interfaceType.Name);

        return sb.ToString();
    }

    /// <summary>
    /// Converts a CLR type to a human-readable C# type name.
    /// </summary>
    private static string FormatTypeName(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";

        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null) return FormatTypeName(underlying) + "?";

        // IEnumerable<T>
        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(IEnumerable<>))
            {
                var arg = type.GetGenericArguments()[0];
                return $"IEnumerable<{FormatTypeName(arg)}>";
            }
            if (genDef == typeof(IList<>) || genDef == typeof(List<>))
            {
                var arg = type.GetGenericArguments()[0];
                return $"IList<{FormatTypeName(arg)}>";
            }

            // Tuple types: ValueTuple
            if (type.FullName?.StartsWith("System.ValueTuple") == true)
            {
                var args = type.GetGenericArguments();
                return $"({string.Join(", ", args.Select(FormatTypeName))})";
            }

            // Generic fallback
            var name = type.Name;
            var idx = name.IndexOf('`');
            if (idx >= 0) name = name[..idx];
            var genericArgs = type.GetGenericArguments();
            return $"{name}<{string.Join(", ", genericArgs.Select(FormatTypeName))}>";
        }

        return type.Name;
    }
}
