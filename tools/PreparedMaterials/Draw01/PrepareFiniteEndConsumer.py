"""Bind the bounded endpoint composition with original finite atlas projection."""
import json,shutil,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
u=json.loads((OUT/'finite-ends-report.json').read_text())['units'][0]
m=json.loads((OUT/'finite-ends-manifest.json').read_text())['units'][0]
c=u['channels'][0]
assert all(x['contentMin']==c['contentMin'] and x['contentMax']==c['contentMax'] for x in u['channels'])
assert m['repeatAxes']=='None'
def f(n):
    s=format(n,'.17g');return s+('f' if '.' in s else '.0f')
vectors={name:'new Vector2('+','.join(f(n) for n in v)+')' for name,v in [('SourceMin',m['min']),('SourceMax',m['max']),('ContentMin',c['contentMin']),('ContentMax',c['contentMax'])]}
code='''using UnityEngine;
using BridgeBuilder.Runtime;
namespace BridgeBuilder.Bridges;
// Independent finite endpoint composition. The source projection is unchanged;
// only the measured embedded-paint region uses the defined source donor.
internal static class Draw01FiniteAsphalt
{
    internal static MovableExistingRoadSurface.Patch Create(Vector4 u,Vector4 v,Matrix4x4 frame,Vector2 sourceMin,Vector2 sourceMax,Vector4? tangent=null)
    {
        var nativeMin=SOURCE_MIN;var nativeMax=SOURCE_MAX;
        var contentMin=CONTENT_MIN;var contentMax=CONTENT_MAX;
        var scale=new Vector2((contentMax.x-contentMin.x)/(nativeMax.x-nativeMin.x),(contentMax.y-contentMin.y)/(nativeMax.y-nativeMin.y));
        u*=scale.x;u.w+=contentMin.x-nativeMin.x*scale.x;
        v*=scale.y;v.w+=contentMin.y-nativeMin.y*scale.y;
        var min=contentMin+Vector2.Scale(sourceMin-nativeMin,scale);
        var max=contentMin+Vector2.Scale(sourceMax-nativeMin,scale);
        return new MovableExistingRoadSurface.Patch(min,max,frame.transpose*u,frame.transpose*v,
            BridgePreparedMaterials.RepeatUnit("Draw01","KEY"),preparedUnit:true,tangent:tangent,projectTangent:!tangent.HasValue)
            .WithFiniteSampling(Vector2.zero,Vector2.one);
    }
}
'''
for token,name in [('SOURCE_MIN','SourceMin'),('SOURCE_MAX','SourceMax'),('CONTENT_MIN','ContentMin'),('CONTENT_MAX','ContentMax')]:code=code.replace(token,vectors[name])
code=code.replace('KEY',u['key'])
(ROOT/'src/BridgeBuilder/Bridges/Types/Draw01/Draw01FiniteAsphalt.cs').write_text(code)
paths=[Path('RepeatUnits')/u['key']/'Repeat.Surface']+[Path('Shared')/(x['cid']+'.Texture') for x in u['channels']]
files=[]
for rel in paths+[Path(str(p)+'.cid') for p in paths]:
    src=OUT/'finite-ends-products/Draw01'/rel;dst=OUT/'continuous-candidate/Materials/Draw01'/rel
    dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dst)
    files.append({'path':str(dst.relative_to(ROOT)),'bytes':src.stat().st_size,'sha256':hashlib.sha256(src.read_bytes()).hexdigest()})
(OUT/'finite-ends-closure.json').write_text(json.dumps({'candidateOnly':True,'qualityAccepted':False,'key':u['key'],'finiteSamplingMin':[0,0],'finiteSamplingMax':[1,1],'files':files,'bytes':sum(x['bytes'] for x in files)},indent=2))
