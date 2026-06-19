using System;

namespace Synapse.GUI.Utils;

public class RangeMapper
{
    private readonly double _inMin;
    private readonly double _inMax;
    private readonly double _outMin;
    private readonly double _outMax;

    public RangeMapper(double inMin, double inMax, double outMin, double outMax)
    {
        _inMin = inMin;
        _inMax = inMax;
        _outMin = outMin;
        _outMax = outMax;
    }

    public double ScaleFactor => (_outMax - _outMin) / (_inMax - _inMin);

    public float Map(float value) => (float)Map((double)value);
    public int Map(int value) => (int)Math.Round(Map((double)value));

    public double Map(double value)
        => (value - _inMin) / (_inMax - _inMin) * (_outMax - _outMin) + _outMin;
}
