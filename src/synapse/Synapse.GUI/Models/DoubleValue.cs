using CommunityToolkit.Mvvm.ComponentModel;

namespace Synapse.GUI.Models;

public class DoubleValue : ObservableObject 
{
    private double _value;
    public double Value
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
