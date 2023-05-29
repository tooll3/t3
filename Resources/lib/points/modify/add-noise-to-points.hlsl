#include "lib/shared/hash-functions.hlsl"
#include "lib/shared/noise-functions.hlsl"
#include "lib/shared/point.hlsl"

cbuffer Transforms : register(b0)
{
    float4x4 CameraToClipSpace;
    float4x4 ClipSpaceToCamera;
    float4x4 WorldToCamera;
    float4x4 CameraToWorld;
    float4x4 WorldToClipSpace;
    float4x4 ClipSpaceToWorld;
    float4x4 ObjectToWorld;
    float4x4 WorldToObject;
    float4x4 ObjectToCamera;
    float4x4 ObjectToClipSpace;
};
  
cbuffer Params : register(b1)
{
    float Amount;
    float Frequency;
    float Phase;
    float Variation;

    float3 AmountDistribution;
    float RotationLookupDistance;

    float3 NoiseOffset;

    float UseWAsWeight;

}

StructuredBuffer<Point> SourcePoints : t0;        
RWStructuredBuffer<Point> ResultPoints : u0;   

float3 GetNoise(float3 pos, float3 variation) 
{
    float3 noiseLookup = (pos * 0.91 + variation + Phase ) * Frequency;
    return snoiseVec3(noiseLookup) * Amount/100 * AmountDistribution;
}

static float3 variationOffset;

void GetTranslationAndRotation(float weight, float3 pointPos, float4 rotation, 
                               out float3 offset, out float4 newRotation) 
{    
    offset = GetNoise(pointPos + NoiseOffset, variationOffset) * weight;

    float3 xDir = rotate_vector(float3(RotationLookupDistance,0,0), rotation);
    float3 offsetAtPosXDir = GetNoise(pointPos + xDir, variationOffset) * weight;
    float3 rotatedXDir = (pointPos + xDir + offsetAtPosXDir) - (pointPos + offset);

    float3 yDir = rotate_vector(float3(0, RotationLookupDistance,0), rotation);
    float3 offsetAtPosYDir = GetNoise(pointPos + yDir, variationOffset) * weight;
    float3 rotatedYDir = (pointPos + yDir + offsetAtPosYDir) - (pointPos + offset);

    float3 rotatedXDirNormalized = normalize(rotatedXDir);
    float3 rotatedYDirNormalized = normalize(rotatedYDir);
    
    float3 crossXY = cross(rotatedXDirNormalized, rotatedYDirNormalized);
    float3x3 orientationDest= float3x3(
        rotatedXDirNormalized, 
        cross(crossXY, rotatedXDirNormalized), 
        crossXY );

    newRotation = normalize(quaternion_from_matrix_precise(transpose(orientationDest)));
}

[numthreads(64,1,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint numStructs, stride;
    SourcePoints.GetDimensions(numStructs, stride);
    if(i.x >= numStructs) {
        ResultPoints[i.x].w = 0 ;
        return;
    }


    float3 variationOffset = hash31((float)(i.x%1234)/0.123 ) * Variation;

    Point p = SourcePoints[i.x];

    float weight = UseWAsWeight < 0 ? lerp(1, 1- p.w, -UseWAsWeight) 
                                : lerp(1, p.w, UseWAsWeight);

    float3 offset;;
    float4 newRotation = p.rotation;

    float4 posInWorld = mul(float4(p.position ,1), ObjectToWorld);
    GetTranslationAndRotation(weight , posInWorld.xyz + variationOffset, p.rotation, offset, newRotation);

    float3 oldPosition = ResultPoints[i.x].position;
    float3 newPosition = p.position + offset; 
    ResultPoints[i.x].position = newPosition;

   newRotation = q_look_at(  normalize(newPosition- oldPosition), float3(0,1,0));
   ResultPoints[i.x].rotation = q_slerp(ResultPoints[i.x].rotation, newRotation, 0.8);;


    float3 velocity = newPosition- oldPosition;

    // // Normalize the velocity and up vectors
    // velocity = -normalize(velocity);
    // float3 up = float3(0,1,0);

    // // // Calculate the forward direction (project velocity onto the plane defined by up)
    // float3 forward = velocity - dot(velocity, up) * up;
    // forward = normalize(forward);

    // // Calculate the right direction (cross product of up and forward)
    // float3 right = cross(up, forward);
    // right = normalize(right);

    // // Calculate the final up direction (cross product of forward and right)
    // up = cross(forward, right);
    // up = normalize(up);

    // // Create a rotation matrix using the right, up, and forward vectors
    // float4x4 rotation;
    // rotation[0] = float4(right, 0.0f);
    // rotation[1] = float4(up, 0.0f);
    // rotation[2] = float4(forward, 0.0f);
    // rotation[3] = float4(0.0f, 0.0f, 0.0f, 1.0f);

    // // Convert the rotation matrix to a quaternion
    // float4x4 rotationTranspose = transpose(rotation);
    // float4 quaternion;
    // quaternion.x = sqrt(1.0 + rotationTranspose[0].x + rotationTranspose[1].y + rotationTranspose[2].z) / 2.0;
    // quaternion.y = (rotationTranspose[2].y - rotationTranspose[1].z) / (4.0 * quaternion.x);
    // quaternion.z = (rotationTranspose[0].z - rotationTranspose[2].x) / (4.0 * quaternion.x);
    // quaternion.w = (rotationTranspose[1].x - rotationTranspose[0].y) / (4.0 * quaternion.x);

    // // Normalize the quaternion
    // quaternion = normalize(quaternion);
    // ResultPoints[i.x].rotation = quaternion;

    // // Calculate the forward direction (project velocity onto the plane defined by up)
    // float3 forward = velocity - dot(velocity, up) * up;
    // forward = normalize(forward);

    // // Calculate the right direction (cross product of forward and up)
    // float3 right = cross(forward, up);
    // right = normalize(right);

    // // Calculate the final up direction (cross product of right and forward)
    // up = cross(right, forward);
    // up = normalize(up);

    // // Create a rotation matrix using the right, up, and forward vectors
    // float4x4 rotation;
    // rotation[0] = float4(right, 0.0f);
    // rotation[1] = float4(up, 0.0f);
    // rotation[2] = float4(forward, 0.0f);
    // rotation[3] = float4(0.0f, 0.0f, 0.0f, 1.0f);

    // // Convert the rotation matrix to a quaternion
    // float4x4 rotationTranspose = transpose(rotation);
    // float4 quaternion;
    // quaternion.x = sqrt(1.0 + rotationTranspose[0].x + rotationTranspose[1].y + rotationTranspose[2].z) / 2.0;
    // quaternion.y = (rotationTranspose[2].y - rotationTranspose[1].z) / (4.0 * quaternion.x);
    // quaternion.z = (rotationTranspose[0].z - rotationTranspose[2].x) / (4.0 * quaternion.x);
    // quaternion.w = (rotationTranspose[1].x - rotationTranspose[0].y) / (4.0 * quaternion.x);

    // // Normalize the quaternion
    // quaternion = normalize(quaternion);
    // ResultPoints[i.x].rotation = quaternion;


    ResultPoints[i.x].w =  SourcePoints[i.x].w ;
}

