using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Synapse.GUI.Models.Problems.QAP;

public class QapSolutionEntry : INotifyPropertyChanged
{
    public string Text => $"Loc {Location} / Fac {Facility}";
    public int Location { get; set; }
    public int Facility { get; set; }
    
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
