Texture2D<float4> _Base;
Texture2D<float4> _Normal;
Texture2D<float4> _Mask;
Texture2D<float4> _WorldBase;
Texture2D<float4> _WorldNormal;
Texture2D<float4> _WorldMask;
SamplerState sampler_Base;
SamplerState sampler_WorldBase;
BBMaterialSample BBEvaluateLayered(float2 baseUv, float2 worldUv, float4 tint)
{
    float4 baseMask = _Mask.Sample(sampler_Base, baseUv);
    BBMaterialSample baseLayer = BBDecodeMaterial(_Base.Sample(sampler_Base, baseUv) * tint,
        _Normal.Sample(sampler_Base, baseUv), baseMask);
    BBMaterialSample worldLayer = BBDecodeMaterial(_WorldBase.Sample(sampler_WorldBase, worldUv) * tint,
        _WorldNormal.Sample(sampler_WorldBase, worldUv), _WorldMask.Sample(sampler_WorldBase, worldUv));
    return BBMixMaterial(baseLayer, worldLayer, baseMask.b);
}
// baseUv and worldUv are independent evaluated coordinate fields. The world
// projection is chosen by source semantics, not by multiplying baseUv density.
