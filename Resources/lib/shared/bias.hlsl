// A collection of various functions

inline float4 GetBias(float4 x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

inline float4 GetGain(float4 x, float gain)
{
    return x < 0.5 ? GetBias(x * 2.0, gain)/2.0
                    : GetBias(x * 2.0 - 1.0,1.0 - gain)/2.0 + 0.5;
}

inline float GetBias(float x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

inline float GetGain(float x, float gain)
{
    return x < 0.5 ? GetBias(x * 2.0, gain)/2.0
                    : GetBias(x * 2.0 - 1.0,1.0 - gain)/2.0 + 0.5;
}