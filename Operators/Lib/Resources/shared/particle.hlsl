
struct Particle
{
    float3 position;
    float lifetime;
    // 4
    float3 velocity;
    float mass;
    // 2*4
    float4 color;

    // 3*4
    int emitterId;
    float3 normal;

    // 4*4
    float emitTime;
    float size;
    float2 __dummy;

    // 5*4 * 4 = 80bytes
};

struct ParticleIndex
{
    int index;
    float squaredDistToCamera;
};
