#ifndef BRIDGE_BUILDER_MATERIAL_FUNCTIONS
#define BRIDGE_BUILDER_MATERIAL_FUNCTIONS
// Material evaluation shared by the generated decal and multilayer programs.
// Native pass serialization/registration is a separate integration requirement.
struct BBMaterialSample
{
    float3 albedo;
    float3 normalTS;
    float metallic;
    float smoothness;
    float occlusion;
    float alpha;
};
float3 BBDecodeNormal(float4 encoded)
{
    float2 xy = float2(encoded.r * encoded.a, encoded.g) * 2.0 - 1.0;
    return float3(xy, sqrt(1.0 - min(dot(xy, xy), 1.0)));
}
BBMaterialSample BBDecodeMaterial(float4 color, float4 normal, float4 mask)
{
    BBMaterialSample result;
    result.albedo = color.rgb;
    result.alpha = color.a;
    result.normalTS = BBDecodeNormal(normal);
    result.metallic = mask.r;
    result.occlusion = mask.g;
    result.smoothness = mask.a;
    return result;
}
BBMaterialSample BBMixMaterial(BBMaterialSample baseLayer, BBMaterialSample worldLayer, float weight)
{
    BBMaterialSample result;
    result.albedo = baseLayer.albedo + weight * (worldLayer.albedo - baseLayer.albedo);
    result.normalTS = normalize(baseLayer.normalTS + weight * (worldLayer.normalTS - baseLayer.normalTS));
    result.metallic = baseLayer.metallic + weight * (worldLayer.metallic - baseLayer.metallic);
    result.smoothness = baseLayer.smoothness + weight * (worldLayer.smoothness - baseLayer.smoothness);
    result.occlusion = baseLayer.occlusion + weight * (worldLayer.occlusion - baseLayer.occlusion);
    result.alpha = baseLayer.alpha + weight * (worldLayer.alpha - baseLayer.alpha);
    return result;
}
// The native procedural program uses 64-byte, column-major bone matrices.
// boneRange and instance mapping are read from the SAME native instance buffers;
// no sampled object transform or history buffer is accepted by this function.
float3 BBBonePosition(ByteAddressBuffer bones, uint2 boneRange, uint packedBone, float3 position)
{
    uint address = min(boneRange.x + (packedBone & 255u), boneRange.y) * 64u;
    float3 x = asfloat(bones.Load3(address));
    float3 y = asfloat(bones.Load3(address + 16u));
    float3 z = asfloat(bones.Load3(address + 32u));
    float3 t = asfloat(bones.Load3(address + 48u));
    return x * position.x + y * position.y + z * position.z + t;
}
float3 BBBoneDirection(ByteAddressBuffer bones, uint2 boneRange, uint packedBone, float3 direction)
{
    uint address = min(boneRange.x + (packedBone & 255u), boneRange.y) * 64u;
    return normalize(asfloat(bones.Load3(address)) * direction.x +
        asfloat(bones.Load3(address + 16u)) * direction.y +
        asfloat(bones.Load3(address + 32u)) * direction.z);
}
// Inverse projection uses this SAME current bone, not the object's history matrix.
// The instance WorldToObject operation precedes this function; the closed decal
// projection frame follows it. This also supports nonsingular scaled bones.
float3 BBBoneInversePosition(ByteAddressBuffer bones, uint2 boneRange, uint packedBone, float3 position)
{
    uint address = min(boneRange.x + (packedBone & 255u), boneRange.y) * 64u;
    float3 x = asfloat(bones.Load3(address));
    float3 y = asfloat(bones.Load3(address + 16u));
    float3 z = asfloat(bones.Load3(address + 32u));
    float3 delta = position - asfloat(bones.Load3(address + 48u));
    float3 rowX = cross(y, z);
    float determinant = dot(x, rowX);
    return float3(dot(rowX, delta), dot(cross(z, x), delta), dot(cross(x, y), delta)) / determinant;
}
#endif
