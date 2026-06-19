namespace Synapse.OptimizationCore.Random;

public class ThreadSafeRandom : RandomBase
{
    public override int GetInt()
        => System.Random.Shared.Next();

    public override int GetInt(int max)
        => System.Random.Shared.Next(max);

    public override int GetInt(int min, int max)
        => System.Random.Shared.Next(min, max);

    public override float GetFloat() 
        => System.Random.Shared.NextSingle();

    public override double GetDouble() 
        => System.Random.Shared.NextDouble();
}
