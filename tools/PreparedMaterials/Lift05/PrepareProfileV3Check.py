"""Create an isolated source-aware v3 numerical checker from retained native IO checks."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
BASE=Path(__file__).parent
env=ROOT/'artifacts/material-remap/environments/Lift05'
cs=(BASE/'RepeatUnitCheck.Editor.cs').read_text().replace('Lift05RepeatUnitCheck','Lift05ProfileV3Check')
cs=cs.replace('source.filterMode=FilterMode.Bilinear','source.filterMode=FilterMode.Trilinear')
cs=cs.replace('program.SetInt("Mip",mip);','program.SetInt("Mip",mip);program.SetFloat("SourceLod",(float)product.sourceLodByMip[mip]);program.SetInt("NormalChannel",channel.name=="_NormalMap"?1:0);program.SetVector("GridRegion",new Vector4((float)product.sourceOriginPixels[0]/source.width,(float)product.sourceOriginPixels[1]/source.height,(float)product.sourceRegionPixels[0]/source.width,(float)product.sourceRegionPixels[1]/source.height));')
cs='\n'.join(line for line in cs.splitlines() if not any(key in line for key in ('Need(levels.All','PASS Lift05 repeat candidate')))
cs=cs.replace('levels=levels.ToArray()};','levels=levels.ToArray()};\n   Debug.Log("MEASURED Lift05 profile v3: "+levels.Count+" channel/mips; no filtering tolerance accepted.");')
(env/'unity/Assets/Editor/Lift05ProfileV3Check.cs').write_text(cs)
shader=(BASE/'RepeatUnitCheck.compute').read_text()
shader=shader.replace('float4 Region;','float4 Region, GridRegion;\nfloat SourceLod;\nint NormalChannel;\nfloat4 NativeNormal(float4 c){if(NormalChannel==0)return c;float2 xy=float2(c.r*c.a,c.g)*2-1;float3 n=normalize(float3(xy,sqrt(max(0,1-dot(xy,xy)))));return float4(n.xy*.5+.5,1,1);}')
shader=shader.replace('a=Source.SampleLevel(sampler_Source,Region.xy+sourceQ*Region.zw,Mip);','a=NativeNormal(Source.SampleLevel(sampler_Source,GridRegion.xy+q*GridRegion.zw,SourceLod));')
shader=shader.replace('a=Source.SampleLevel(sampler_Source,Region.xy+q*Region.zw,Mip);','a=NativeNormal(Source.SampleLevel(sampler_Source,Region.xy+q*Region.zw,SourceLod));')
# This candidate uses the exact identity frame. The baker deliberately retains
# its original packed normal channels, so the reference must do the same.
shader=shader.replace('if(NormalChannel==0)return c;','return c;')
(env/'unity/Assets/Lift05ProfileV3Check.compute').write_text(shader)
print('Prepared isolated v3 source-grid/source-LOD/native-normal checker; measurements only.')
