using CommunityToolkit.Mvvm.ComponentModel;

namespace Synapse.GUI.Models;

public class IntValue : ObservableObject
{
    private int _value;
    public int Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
    
    private int? _index;
    public int? Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }
}
