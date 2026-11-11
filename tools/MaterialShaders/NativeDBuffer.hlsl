// Native four-target DBuffer representation. Pass blend/write-mask state and
// receiver/depth tests must match the source native material separately.
struct BBDBufferOutput
{
    float4 color : SV_Target0;
    float4 normal : SV_Target1;
    float4 mask : SV_Target2;
    float2 maskWeight : SV_Target3;
};
BBDBufferOutput BBEncodeDBuffer(BBMaterialSample material, float3 normalWS,
    float colorOpacity, float normalOpacity, float maskOpacity, bool suppressColor)
{
    BBDBufferOutput output;
    output.color = float4(material.albedo, suppressColor ? 0.0 : colorOpacity);
    output.normal = float4(normalize(normalWS) * 0.5 + 0.5, normalOpacity);
    output.mask = float4(suppressColor ? 0.0 : material.metallic, 1.0,
        suppressColor ? 0.0 : material.smoothness, maskOpacity);
    output.maskWeight = maskOpacity.xx;
    return output;
}
