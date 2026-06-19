namespace Synapse.OptimizationCore.Random;

public abstract class RandomBase : IRandom
{
    public abstract int GetInt();
    public abstract int GetInt(int max);
    public abstract int GetInt(int min, int max);
    public abstract float GetFloat();
    public abstract double GetDouble();
    
    public virtual int[] GetInts(int length, int min, int max)
    {
        int[] ints = new int[length];
        for (int index = 0; index < length; ++index)
            ints[index] = this.GetInt(min, max);
        return ints;
    }

    public virtual int[] GetUniqueInts(int length, int min, int max)
    {
        int count = max - min;
        List<int> intList = count >= length 
            ? Enumerable.Range(min, count).ToList<int>() 
            : throw new ArgumentOutOfRangeException(nameof(length), $"The length is {length}, but the possible unique values between {min} (inclusive) and {max} (exclusive) are {count}.");
        int[] uniqueInts = new int[length];
        for (int index1 = 0; index1 < length; ++index1)
        {
            int index2 = this.GetInt(0, intList.Count);
            uniqueInts[index1] = intList[index2];
            intList.RemoveAt(index2);
        }
        return uniqueInts;
    }

    public virtual float GetFloat(float min, float max) => min + (max - min) * this.GetFloat();
    public virtual double GetDouble(double min, double max) => min + (max - min) * this.GetDouble();
    public virtual bool GetBool() => 0.5 < this.GetDouble();
    public virtual bool GetBool(double probabilityTrue) => probabilityTrue < this.GetDouble();
}
