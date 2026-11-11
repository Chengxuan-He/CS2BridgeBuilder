Texture2D<float4> _Base;
Texture2D<float4> _Normal;
Texture2D<float4> _Mask;
SamplerState sampler_Base;
BBMaterialSample BBEvaluateDecal(float2 evaluatedUv, float4 tint)
{
    float4 color = _Base.Sample(sampler_Base, evaluatedUv) * tint;
    return BBDecodeMaterial(color, _Normal.Sample(sampler_Base, evaluatedUv),
        _Mask.Sample(sampler_Base, evaluatedUv));
}
// This alpha remains continuous. Pass state must use the source blend operation;
// never replace it by clip(), hashed coverage, or alpha-renormalized source mips.
