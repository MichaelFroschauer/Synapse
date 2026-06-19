using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Synapse.GUI.Models.Problems.TSP;

public class City : INotifyPropertyChanged
{
    public int Id { get; }
    public double X { get; }
    public double Y { get; }
    
    private double _displayX;
    private double _displayY;

    public double DisplayX
    {
        get => _displayX;
        set { if (_displayX != value) { _displayX = value; OnPropertyChanged(); } }
    }

    public double DisplayY
    {
        get => _displayY;
        set { if (_displayY != value) { _displayY = value; OnPropertyChanged(); } }
    }

    public City(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
