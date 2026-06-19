namespace Synapse.GUI.Models;

public class ExpanderItem
{
    public string Label { get; set; }
    public string Value { get; set; }
    public string? Subtitle { get; set; }

    public ExpanderItem(string label, string value, string? subtitle = null)
    {
        Label = label;
        Value = value;
        Subtitle = subtitle;
    }
}
