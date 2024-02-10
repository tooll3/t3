// Points are particles share the same structure and stride,
// But some attributes change their meaning:
//   W -> Radius
//   Stretch -> Velocity
//   Selected -> BirthTime

struct Point
{
    float3 Position;
    float W;
    float4 Rotation;
    float4 Color;
    float3 Stretch;
    float Selected;
};

struct Particle
{
    float3 Position;
    float Radius;
    float4 Rotation;
    float4 Color;
    float3 Velocity;
    float BirthTime;
};
