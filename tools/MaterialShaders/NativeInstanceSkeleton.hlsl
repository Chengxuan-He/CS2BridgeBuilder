#ifndef BRIDGE_BUILDER_NATIVE_INSTANCE_SKELETON
#define BRIDGE_BUILDER_NATIVE_INSTANCE_SKELETON
// Matches the inspected native procedural vertex addressing. Metadata is supplied
// by Unity DOTS instancing, and visibleInstance is unity_DOTSVisibleInstances[SV_InstanceID].
// The owning shader container must retain these native property bindings; arbitrary
// Material.SetBuffer aliases are not sufficient for a production native batch.
uint BBInstanceAddress(uint metadata, uint visibleInstance, uint stride)
{
    return (metadata & 0x7fffffffu) + ((metadata & 0x80000000u) != 0u ? visibleInstance * stride : 0u);
}
uint2 BBInstanceBoneRange(ByteAddressBuffer instanceData, uint metadata, uint visibleInstance)
{
    return instanceData.Load2(BBInstanceAddress(metadata, visibleInstance, 8u));
}
float3 BBInstancePoint(ByteAddressBuffer instanceData, uint metadata, uint visibleInstance, float3 position)
{
    uint address = BBInstanceAddress(metadata, visibleInstance, 48u);
    float4 a = asfloat(instanceData.Load4(address));
    float4 b = asfloat(instanceData.Load4(address + 16u));
    float4 c = asfloat(instanceData.Load4(address + 32u));
    return a.xyz * position.x + float3(a.w, b.xy) * position.y +
        float3(b.zw, c.x) * position.z + c.yzw;
}
float3 BBNativeSkinnedPoint(ByteAddressBuffer instanceData, ByteAddressBuffer currentBones,
    uint boneMetadata, uint objectMetadata, uint visibleInstance, uint packedBone, float3 position)
{
    return BBInstancePoint(instanceData, objectMetadata, visibleInstance,
        BBBonePosition(currentBones, BBInstanceBoneRange(instanceData, boneMetadata, visibleInstance), packedBone, position));
}
#endif

