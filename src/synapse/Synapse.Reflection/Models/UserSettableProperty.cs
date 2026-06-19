namespace Synapse.Reflection.Models;

public class UserSettableProperty : SettableProperty
{
    public required string DisplayName { get; init; }
    public override bool UserSettable => true;
    public override string ToString() => $"[{DisplayName}] {base.ToString()}";
}
