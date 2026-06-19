namespace Synapse.OptimizationCore.Random;

using System;

public class ThreadLocalRandom : RandomBase
{
    public ThreadLocalRandom() { }

    public ThreadLocalRandom(int seed)
    {
        _baseSeed = seed;
    }
    
    private Random? _localRandom;

    private int? _baseSeed;

    private Random LocalRandom
    {
        get
        {
            if (_localRandom == null)
            {
                if (_baseSeed.HasValue) _localRandom = new Random(_baseSeed.Value);
                else _localRandom = new Random();
            }
            return _localRandom;
        }
    }

    public void SetSeed(int seed)
    {
        _baseSeed = seed;
        _localRandom = null;
    }

    public override int GetInt() => LocalRandom.Next();

    public override int GetInt(int max) => LocalRandom.Next(max);

    public override int GetInt(int min, int max) => LocalRandom.Next(min, max);

    public override float GetFloat() => (float)LocalRandom.NextDouble();

    public override double GetDouble() => LocalRandom.NextDouble();
}
