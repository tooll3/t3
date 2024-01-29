// A collection of various distribution functions

    
inline float4 GetBias(float4 x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

inline float4 GetSchlickBias(float4 x, float gain)
{
    return x < 0.5 ? GetBias(x * 2.0, gain)/2.0
                    : GetBias(x * 2.0 - 1.0,1.0 - gain)/2.0 + 0.5;
}

inline float GetBias(float x, float bias)
{
    return x / ((1 / bias - 2) * (1 - x) + 1);
}

float GetSchlickBias(float x, float gain)
{
    return x < 0.5 ? GetBias(x * 2.0, gain)/2.0
                    : GetBias(x * 2.0 - 1.0,1.0 - gain)/2.0 + 0.5;
}

// based on: https://arxiv.org/pdf/2010.09714.pdf
// but s remapped from [-64 .. 64] -> [0 .. 1] 
float ApplyGainBias(float x, float t, float s) 
{
    float eps = 0.0001;
    float r = 200;
    s *= 2;
    s = s < 1 ? (pow(r, 1-s)) : 1 / pow(r, s-1);
    return x < t 
    ? ((t*x)/(x+s*(t-x)+eps)) 
    : (((1-t)*(x-1))/(1-x-s*(t-x)+eps)+1);
}

float4 ApplyGainBias(float4 x, float t, float s) 
{
    float eps = 0.0001;
    float r = 200;
    s *= 2;
    s = s < 1 ? (pow(r, 1-s)) : 1 / pow(r, s-1);
    return x < t 
    ? ((t*x)/(x+s*(t-x)+eps)) 
    : (((1-t)*(x-1))/(1-x-s*(t-x)+eps)+1);
}