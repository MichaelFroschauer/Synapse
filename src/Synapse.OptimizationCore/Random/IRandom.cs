namespace Synapse.OptimizationCore.Random;

public interface IRandom
{
    int GetInt();
    int GetInt(int max);
    int GetInt(int min, int max);
    int[] GetInts(int length, int min, int max);
    int[] GetUniqueInts(int length, int min, int max);
    float GetFloat();
    float GetFloat(float min, float max);
    double GetDouble();
    double GetDouble(double min, double max);
    bool GetBool();
    bool GetBool(double probabilityTrue);
}