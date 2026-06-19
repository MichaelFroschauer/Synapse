namespace Synapse.OptimizationCore.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class DisplayInfoAttribute : Attribute
{
    public DisplayInfoAttribute() { }
    
    public DisplayInfoAttribute(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
    
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Visible { get; init; } = true;
}
