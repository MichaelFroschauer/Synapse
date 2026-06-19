namespace Synapse.OptimizationCore.Random;

public static class RandomProvider
{
    public static IRandom Value { get; private set; }

    public static void SetSeed(int seed)
    {
        Value = new ThreadLocalRandom(seed);
    }

    static RandomProvider()
    {
        //Value = new ThreadSafeRandom();
        Value = new ThreadLocalRandom();
        //Value = new TestRandom(1);
    }
}
