namespace Synapse.OptimizationCore.Random;

using System;

public class TestRandom : RandomBase
{
    private Random _random;
    private readonly Lock _lock = new Lock();

    public TestRandom(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void SetSeed(int seed)
    {
        lock (_lock)
        {
            _random = new Random(seed);
        }
    }

    public override int GetInt()
    {
        lock (_lock)
            return _random.Next();
    }

    public override int GetInt(int max)
    {
        lock (_lock)
            return _random.Next(max);
    }

    public override int GetInt(int min, int max)
    {
        lock (_lock)
            return _random.Next(min, max);
    }

    public override float GetFloat()
    {
        lock (_lock)
            return (float)_random.NextDouble();
    }

    public override double GetDouble()
    {
        lock (_lock)
            return _random.NextDouble();
    }
}
