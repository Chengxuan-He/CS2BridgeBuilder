"""Build Draw01's bounded original-paint replacement authoring variant (offline only)."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
upstream=(ROOT/'tools/GpuBake/PrepareRepeatUnits.Editor.cs').read_bytes()
upstream_sha256=hashlib.sha256(upstream).hexdigest()
s=upstream.decode('utf-8-sig')
s=s.replace('public static class PrepareRepeatUnits','public static class Draw01PrepareBoundedNearUnits')
s=s.replace('public string repeatAxes;', 'public string repeatAxes;\n        public double[] paintU, donorU;')
s=s.replace('w.Write(px);w.Write(py);w.Write(channel.encoding??"");','w.Write(px);w.Write(py);w.Write(channel.encoding??"");if(unit.paintU!=null){foreach(double n in unit.paintU)w.Write(n);foreach(double n in unit.donorU)w.Write(n);}')
s=s.replace('unit.RepeatU,unit.RepeatV,unit.finiteSourceGrid,channel.name=="_NormalMap",unit.normalTransform);','unit.RepeatU,unit.RepeatV,unit.finiteSourceGrid,channel.name=="_NormalMap",unit.normalTransform,unit.paintU,unit.donorU);')
s=s.replace('double[] normalTransform=null)','double[] normalTransform=null,double[] paintU=null,double[] donorU=null)')
s=s.replace('program.SetInt("EncodeSrgb",srgb?1:0);','program.SetInt("EncodeSrgb",srgb?1:0);\n            program.SetInt("ReplacePaint",paintU==null?0:1);\n            program.SetVector("PaintDonorU",paintU==null?Vector4.zero:new Vector4((float)paintU[0],(float)paintU[1],(float)donorU[0],(float)donorU[1]));\n            program.SetInt("SourceWidth",source.width);')
s=s.replace('"Assets/RepeatUnit.compute"','"Assets/Draw01BoundedUnit.compute"')
assert 'unit.paintU,unit.donorU);' in s and 'double[] paintU=null,double[] donorU=null)' in s, 'Upstream extraction interface changed; review bounded-mask integration first.'
assert s.count('if(unit.paintU!=null)') == 1, 'The bounded composition must enter the texture content key.'
assert s.count('program.SetInt("ReplacePaint"') == 1, 'The source mask must be supplied to the GPU.'
p=ROOT/'tools/PreparedMaterials/Draw01/PrepareBoundedNearUnits.Editor.cs';p.write_text(s)
c=(ROOT/'tools/GpuBake/RepeatUnit.compute').read_text().replace('int EncodeSrgb;','int EncodeSrgb;\nint ReplacePaint;\nint SourceWidth;\nfloat4 PaintDonorU;')
c=c.replace('float4 c=Source.SampleLevel(sampler_Source,uv,max(0,lod));','// Replace only the measured stripe plus its current-mip filter footprint.\n    // Donor is an explicitly selected original asphalt strip at the same V.\n    if(ReplacePaint!=0){float footprint=.5*exp2(max(0,lod))/SourceWidth;if(uv.x>=PaintDonorU.x-footprint&&uv.x<=PaintDonorU.y+footprint)uv.x=clamp(uv.x+PaintDonorU.z-PaintDonorU.x,PaintDonorU.z,PaintDonorU.w);}\n    float4 c=Source.SampleLevel(sampler_Source,uv,max(0,lod));')
(ROOT/'tools/PreparedMaterials/Draw01/Draw01BoundedUnit.compute').write_text(c)
assert c.count('if(ReplacePaint!=0)') == 1, 'The shared kernel changed; do not silently emit an unmodified painted unit.'
m=json.loads((OUT/'manifest.json').read_text());m['units']=m['units'][:1]
u=m['units'][0];u['paintU']=[787/4096,810/4096];u['donorU']=[403/4096,426/4096]
# Keep the complete measured 4 m period. This new candidate isolates the bounded
# paint operation from BC7 loss and locks finite-axis source phase through mip 2.
# It does not resolve or waive fractional repeat-grid errors at higher mips.
u['finiteGridMip']=2
for channel in u['channels']:channel['encoding']=''
e=OUT/'bounded-paint-evidence.json';e.write_text(json.dumps(dict(operation='Defined source-asphalt composition; not recovery of unknown paint-free original pixels',upstreamBakerSha256=upstream_sha256,sourcePixelStripeX=[787,809],sourcePixelStripeY=[2778,3509],sourceDonorX=[403,425],sourceDonorV='unchanged',mipPolicy='Every channel samples the same original donor projection at its authored mip; only stripe plus half-mip-texel filter footprint is changed.',sourceInspectionImageSha256=hashlib.sha256((ROOT/'artifacts/prepared-materials/Draw01/actual-retile/source-color.png').read_bytes()).hexdigest(),sourceEvidence='source-evidence.json',pixelEvidence='Across all 732 source rows, red-blue > 35 identifies exactly x 787..809; adjacent x 781..786 and 810..815 have max red-blue <=4. Native GPU probe at u=0.96 of full domain confirms original yellow color.'),indent=2)+'\n')
u['evidence']=dict(path=str(e),sha256=hashlib.sha256(e.read_bytes()).hexdigest())
(OUT/'bounded-manifest.json').write_text(json.dumps(m,indent=2)+'\n')
print(p)
