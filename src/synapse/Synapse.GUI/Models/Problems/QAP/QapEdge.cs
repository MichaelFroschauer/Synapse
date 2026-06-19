using Avalonia;
using Avalonia.Media;

namespace Synapse.GUI.Models.Problems.QAP;

public class QapEdge
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public double Thickness { get; set; }
    public IBrush Brush { get; set; }
    public double Opacity { get; set; }
    public double FlowValue { get; set; }

    // optional: store which locations/facilities this edge represents
    public int LocationA { get; set; }
    public int LocationB { get; set; }
    public int FacilityA { get; set; }
    public int FacilityB { get; set; }

    public QapEdge()
    {
        Brush = Brushes.Gray;
        Opacity = 1.0;
    }
}
