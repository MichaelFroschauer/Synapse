namespace Synapse.GUI.Models.Problems.Function;

/// <summary>
/// Represents one dimension of a real-valued solution for display in a table/chart.
/// </summary>
public class FunctionDimensionEntry
{
    public int Index { get; init; }
    public string Label => $"x{Index}";
    public double Value { get; init; }
    public string FormattedValue => Value.ToString("G6");
}
