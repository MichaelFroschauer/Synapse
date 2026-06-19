namespace Synapse.GUI.Models;

public class Arrow
{
    public double X { get; set; }
    public double Y { get; set; }
    public double AngleDegrees { get; set; }

    public Arrow(double x, double y, double angleDegrees)
    {
        X = x;
        Y = y;
        AngleDegrees = angleDegrees;
    }
}