namespace Synapse.OptimizationCore.Common;

public readonly struct Parameter(object value) : IEquatable<Parameter>
{
    public object Value { get; } = value;

    public static bool operator ==(Parameter first, Parameter second) => first.Equals(second);

    public static bool operator !=(Parameter first, Parameter second) => !(first == second);

    public override string ToString() => Value?.ToString() ?? string.Empty;

    public bool Equals(Parameter other)
    {
        return Value is null ? other.Value == null : Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => obj is Parameter other && Equals(other);

    public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
}
