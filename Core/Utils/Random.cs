using System;

namespace T3.Core.Utils;

public static class RandomExtensions
{
    public static float NextFloat(this Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }
    
    public static double NextDouble(this Random random, double min, double max)
    {
        return random.NextDouble() * (max - min) + min;
    }
    
    public static int NextInt(this Random random, int min, int max)
    {
        return random.Next(max - min) + min;
    }
    
    public static long NextLong(this Random random, long min, long max)
    {
        return random.NextInt64(max - min) + min;
    }
}