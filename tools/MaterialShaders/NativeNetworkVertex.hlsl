// Generated GBuffer/Forward vertex adapter for already evaluated road geometry.
// The native source pixel programs remain responsible for layered HDRP shading.
// Binding records must be migrated with this code; this file alone is not a
// complete Unity Shader resource or an independently loadable material.
#ifndef BB_GLOBAL_REGISTER
#define BB_GLOBAL_REGISTER b0
#define BB_VISIBLE_REGISTER b1
#define BB_BUILTIN_REGISTER b2
#define BB_MATERIAL_REGISTER b4
#endif
#ifndef BB_DRAW_REGISTER
#define BB_DRAW_REGISTER b1
#define BB_MATERIAL_VALUE_REGISTER b2
#endif
cbuffer BBGlobals : register(BB_GLOBAL_REGISTER) { float4 bbGlobals[183]; };
#if defined(BB_NON_DOTS)
cbuffer BBObjectDraw : register(BB_DRAW_REGISTER) { float4 bbDraw[39]; };
// Original pixel-stage fields retain their offsets. The serialized material
// block appends the bone range after the native 1040-byte layout.
cbuffer BBVertexMaterial : register(BB_MATERIAL_VALUE_REGISTER) { uint4 bbVertexMaterial[66]; };
ByteAddressBuffer bbBones : register(t0);
#else
cbuffer BBVisibility : register(BB_VISIBLE_REGISTER) { uint4 bbVisible[256]; };
cbuffer BBBuiltinMetadata : register(BB_BUILTIN_REGISTER) { uint4 bbBuiltin[2]; };
cbuffer BBMaterialMetadata : register(BB_MATERIAL_REGISTER) { uint4 bbMaterial[3]; };
ByteAddressBuffer bbInstances : register(t0);
ByteAddressBuffer bbBones : register(t1);
#endif
#if defined(BB_MOTION_PASS)
cbuffer BBPreviousFrame : register(b0) { uint4 bbPreviousFrame[13]; };
#endif
struct BBEvaluatedRoadVertex
{
    float3 position : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 uv : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
#if !defined(BB_NON_DOTS)
    uint instance : SV_InstanceID;
#endif
    uint bone : BLENDINDICES;
};
struct BBNetworkVaryings
{
#if defined(BB_MOTION_PASS)
    float3 currentClip : TEXCOORD8;
    float3 previousClip : TEXCOORD9;
#endif
    float4 position : SV_POSITION;
    float4 tangent : INTERP0;
#if defined(BB_COMPACT_VARYINGS)
    float4 evaluatedUv : INTERP1;
    float3 worldPosition : INTERP2;
    float3 normal : INTERP3;
#else
    float4 uv1 : INTERP1;
    float4 uv2 : INTERP2;
    float4 evaluatedUv : INTERP3;
    float3 worldPosition : INTERP4;
    float3 normal : INTERP5;
#endif
#if !defined(BB_NON_DOTS)
    nointerpolation uint instance : CUSTOM_INSTANCE_ID;
#endif
};
#if !defined(BB_NON_DOTS)
float3 BBInstanceVector(uint metadata, uint instance, float3 direction)
{
    uint address = BBInstanceAddress(metadata, instance, 48u);
    float4 a = asfloat(bbInstances.Load4(address));
    float4 b = asfloat(bbInstances.Load4(address + 16u));
    float4 c = asfloat(bbInstances.Load4(address + 32u));
    return a.xyz * direction.x + float3(a.w, b.xy) * direction.y + float3(b.zw, c.x) * direction.z;
}
float3 BBInstanceNormal(uint metadata, uint instance, float3 normal)
{
    uint address = BBInstanceAddress(metadata, instance, 48u);
    float4 a = asfloat(bbInstances.Load4(address));
    float4 b = asfloat(bbInstances.Load4(address + 16u));
    float4 c = asfloat(bbInstances.Load4(address + 32u));
    return float3(dot(a.xyz, normal), dot(float3(a.w,b.xy), normal), dot(float3(b.zw,c.x), normal));
}
#endif
float3 BBDecodeOctahedral(float2 encoded)
{
    float3 result = float3(encoded, 1.0 - abs(encoded.x) - abs(encoded.y));
    float correction = max(-result.z, 0.0);
    result.xy += float2(encoded.x >= 0.0 ? -correction : correction, encoded.y >= 0.0 ? -correction : correction);
    return normalize(result);
}
BBNetworkVaryings BBNetworkVertex(BBEvaluatedRoadVertex input)
{
#if defined(BB_PACKED_FRAME)
    input.normal = BBDecodeOctahedral(input.normal.xy);
    uint bits = asuint(input.tangent.x);
    int x = (int)(bits << 17) >> 17;
    int y = (int)(bits << 1) >> 17;
    input.tangent = float4(BBDecodeOctahedral(max(float2(x,y) / 16383.0, -1.0)), (bits & 0x80000000u) != 0u ? -1.0 : 1.0);
#endif
#if defined(BB_NON_DOTS)
    uint2 range = bbVertexMaterial[65].xy;
    float3 objectPosition = BBBonePosition(bbBones, range, input.bone, input.position);
    float3 worldPosition = (bbDraw[0] * objectPosition.x + bbDraw[1] * objectPosition.y +
        bbDraw[2] * objectPosition.z + bbDraw[3]).xyz - bbGlobals[44].xyz;
#else
    uint visible = bbVisible[input.instance].x;
    uint2 range = BBInstanceBoneRange(bbInstances, bbMaterial[2].w, visible);
    float3 objectPosition = BBBonePosition(bbBones, range, input.bone, input.position);
    float3 worldPosition = BBInstancePoint(bbInstances, bbBuiltin[0].x, visible, objectPosition) - bbGlobals[44].xyz;
#endif
    float3 normal = BBBoneDirection(bbBones, range, input.bone, input.normal);
    float3 tangent = BBBoneDirection(bbBones, range, input.bone, input.tangent.xyz);
    BBNetworkVaryings output;
    output.position = bbGlobals[20] * worldPosition.x + bbGlobals[21] * worldPosition.y +
        bbGlobals[22] * worldPosition.z + bbGlobals[23];
    output.worldPosition = worldPosition;
#if defined(BB_NON_DOTS)
    output.normal = normalize(float3(dot(bbDraw[4].xyz, normal), dot(bbDraw[5].xyz, normal), dot(bbDraw[6].xyz, normal)));
    output.tangent = float4(normalize(bbDraw[0].xyz * tangent.x + bbDraw[1].xyz * tangent.y + bbDraw[2].xyz * tangent.z), input.tangent.w);
#else
    output.normal = normalize(BBInstanceNormal(bbBuiltin[0].y, visible, normal));
    output.tangent = float4(normalize(BBInstanceVector(bbBuiltin[0].x, visible, tangent)), input.tangent.w);
#endif
#if !defined(BB_COMPACT_VARYINGS)
    output.uv1 = input.uv1;
    output.uv2 = input.uv2;
#endif
    // Input is post-network-evaluation UV, never the encoded pre-deformation road UV.
    output.evaluatedUv = float4(input.uv.xy, 0, input.uv.w);
#if !defined(BB_NON_DOTS)
    output.instance = input.instance;
#endif
#if defined(BB_MOTION_PASS)
    output.currentClip = (bbGlobals[32] * worldPosition.x + bbGlobals[33] * worldPosition.y +
        bbGlobals[34] * worldPosition.z + bbGlobals[35]).xyw;
#if defined(BB_NON_DOTS)
    bool disableMotion = bbDraw[38].y == 0.0;
#else
    bool disableMotion = (bbVisible[0].w & 2u) != 0u;
#endif
    if (disableMotion)
        output.previousClip = float3(0, 0, 1);
    else
    {
        // Previous matrices are used only for the native motion-vector pass.
        // Visible geometry and decal projection always use the current sample.
        uint address = min(range.x + (input.bone & 255u), range.y) * 64u;
#if defined(BB_NON_DOTS)
        address += bbPreviousFrame[0].x;
#else
        address += bbPreviousFrame[0].y;
#endif
        float3 previousObject = asfloat(bbBones.Load3(address)) * input.position.x +
            asfloat(bbBones.Load3(address + 16u)) * input.position.y +
            asfloat(bbBones.Load3(address + 32u)) * input.position.z + asfloat(bbBones.Load3(address + 48u));
#if defined(BB_NON_DOTS)
        float3 previousWorld = (bbDraw[30] * previousObject.x + bbDraw[31] * previousObject.y +
            bbDraw[32] * previousObject.z + bbDraw[33]).xyz - bbGlobals[44].xyz;
#else
        float3 previousWorld = BBInstancePoint(bbInstances, bbBuiltin[1].z, visible, previousObject) - bbGlobals[44].xyz;
#endif
        output.previousClip = (bbGlobals[36] * previousWorld.x + bbGlobals[37] * previousWorld.y +
            bbGlobals[38] * previousWorld.z + bbGlobals[39]).xyw;
    }
#endif
    return output;
}

