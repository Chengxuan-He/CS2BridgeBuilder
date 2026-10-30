"""Stage a private /3 producer extension without baking or publishing candidates."""
from pathlib import Path
import hashlib
import json

ROOT=Path(__file__).resolve().parents[3]
private=ROOT/'artifacts/material-remap/environments/Lift01/unity/Assets'
source=ROOT/'tools/GpuBake/PrepareRepeatUnits.Editor.cs'
code=source.read_text()
assert 'BridgeBuilder/RepeatUnits/3' in code


def replace(old,new):
    global code
    assert code.count(old)==1,(old,code.count(old))
    code=code.replace(old,new)


replace('public bool finiteSourceGrid;', 'public bool finiteSourceGrid;\n        public double[] finiteWarp;')
replace('        Matrix(u.normalTransform);', '''        Matrix(u.normalTransform);
        if(u.finiteWarp!=null&&(u.finiteWarp.Length!=19||u.RepeatU||u.RepeatV||u.finiteSourceGrid||u.bands?.Length>0||u.finiteWarp.Any(x=>!double.IsFinite(x))))
            throw new InvalidDataException("Invalid Lift01 finite-only offline warp.");''')
replace('w.Write(unit.finiteSourceGrid);','w.Write(unit.finiteSourceGrid);w.Write(unit.finiteWarp?.Length??0);foreach(var value in unit.finiteWarp??Array.Empty<double>())w.Write(value);')
replace('unit.RepeatU,unit.RepeatV,unit.finiteSourceGrid,channel.name=="_NormalMap",unit.normalTransform);','unit.RepeatU,unit.RepeatV,unit.finiteSourceGrid,channel.name=="_NormalMap",unit.normalTransform,unit.finiteWarp);')
replace('bool finiteSourceGrid=false,bool normalChannel=false,double[] normalTransform=null)','bool finiteSourceGrid=false,bool normalChannel=false,double[] normalTransform=null,double[] finiteWarp=null)')
replace('int kernel=program.FindKernel("Extract");program.SetTexture(kernel,"Source",source);','''int kernel=program.FindKernel("Extract");program.SetTexture(kernel,"Source",source);
            program.SetInt("FiniteWarpEnabled",finiteWarp==null?0:1);
            if(finiteWarp!=null) {
                Vector4 Part(int offset,int count)=>new Vector4((float)finiteWarp[offset],(float)finiteWarp[offset+1],(float)finiteWarp[offset+2],count==4?(float)finiteWarp[offset+3]:0);
                program.SetVector("FiniteWarpShape",Part(0,4));program.SetVector("FiniteWarpSeam",Part(4,3));
                program.SetVector("FiniteWarpU0",Part(7,3));program.SetVector("FiniteWarpV0",Part(10,3));
                program.SetVector("FiniteWarpU1",Part(13,3));program.SetVector("FiniteWarpV1",Part(16,3));
            }''')
shader=(ROOT/'tools/GpuBake/RepeatUnit.compute').read_text()
needle='[numthreads(8,8,1)]'
assert shader.count(needle)==1
shader=shader.replace(needle,'#include "Lift01FiniteTrapezoid.hlsl"\n'+needle)
needle='    float4 c=Source.SampleLevel'
assert shader.count(needle)==1
shader=shader.replace(needle,'    if(FiniteWarpEnabled!=0)Lift01FiniteWarp(q,uv,lod);\n'+needle)
(private/'Editor/PrepareRepeatUnits.cs').write_text(code)
(private/'RepeatUnit.compute').write_text(shader)
(private/'Lift01FiniteTrapezoid.hlsl').write_text((ROOT/'tools/PreparedMaterials/Lift01/FiniteTrapezoid.hlsl').read_text())
proposal_path=ROOT/'artifacts/material-remap/Lift01/far-finite/recipe-proposal.json'
proposal=json.loads(proposal_path.read_text())
unit=proposal['units'][0]
shape=unit['offlineFiniteTrapezoid'];panels=shape['panels']
import numpy as np
a,b=np.array(panels[0]['xz'])[:2]
seam=np.array([b[1]-a[1],a[0]-b[0],b[0]*a[1]-a[0]*b[1]])
if np.dot(seam,np.r_[panels[0]['xz'][2],1])>0:seam=-seam
unit['finiteWarp']=[shape[k] for k in ('halfWidthAtV0','halfWidthAtV1','zAtV0','length')]+seam.tolist()+[x for p in panels for row in p['sourceUvAffine'] for x in row]
unit.pop('offlineFiniteTrapezoid')
manifest=dict(recipe=proposal['recipe'],style='Lift01',units=[unit])
target=proposal_path.with_name('private-manifest.json');target.write_text(json.dumps(manifest,indent=2))
proof=dict(sharedProducerSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    privateProducerSha256=hashlib.sha256(code.encode()).hexdigest(),
    shaderSHA256=hashlib.sha256(shader.encode()).hexdigest(),
    productionReady=False,scope='Private staging only; source panels are offline sampling evidence; no runtime geometry clipping or full-span pixel growth.')
target.with_name('private-producer.json').write_text(json.dumps(proof,indent=2))
print(target)
