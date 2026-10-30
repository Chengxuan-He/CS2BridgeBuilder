"""Prove actual physical end charts and emit compact finite source mapping."""
import ast,json,numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road';UNITS=OUT.parent/'near-units'
for file,name in [('AuditFarPrecision.py','feasible'),('BuildMiddleEdgeFields.py','decode')]:
 tree=ast.parse(Path(__file__).with_name(file).read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name);import itertools;exec(compile(ast.Module(body=[fn],type_ignores=[]),'<source helpers>','exec'))
r=json.loads((OUT/'recipe.json').read_text());d=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz');p=d['mesh0_positions'].astype(float);uv=d['mesh0_uv0'].astype(float);f=d['mesh0_faces'];bits=np.load(OUT/'middle-tangent-bits.npy');m=json.loads((UNITS/'finite-ends-manifest.json').read_text())['units'][0];product=json.loads((UNITS/'finite-ends-report.json').read_text())['units'][0];c=product['channels'][0];assert all(x['contentMin']==c['contentMin'] and x['contentMax']==c['contentMax'] for x in product['channels']);scale=(np.array(c['contentMax'])-c['contentMin'])/(np.array(m['max'])-m['min']);rows=[]
for q in r['lods'][1]['domains']:
 if q['domain']!='asphalt':continue
 ids=sorted({i for x in q['cells'] for i in x['faces']});z=abs(p[np.unique(f[ids]),2]);lo,hi=z.min(),z.max();width=abs(p[np.unique(f[ids]),0]).max()
 for end in [False,True]:
  boundary=hi if end else lo;choices=[i for i in ids if len(set(bits[f[i]].tolist()))==1 and np.count_nonzero(abs(p[f[i],2])==boundary)==2];assert len(choices)==1
  fid=choices[0];vi=f[fid];edgez=next(abs(x) for x in p[vi,2] if abs(x)!=boundary);axes=feasible(p[vi],uv[vi]);assert all(a['float32CoefficientsInsideRoundingCells'] and a['float32EvaluationInsideRoundingCells'] for a in axes)
  projected=np.column_stack([p[vi,0],p[vi,2],np.ones(3)]).astype(np.float32)@np.array([a['affineXz1'] for a in axes],np.float32).T
  mapped=np.array(c['contentMin'])+(projected-m['min'])*scale;assert np.all(mapped>=0) and np.all(mapped<=1)
  rows.append(dict(right=q['right'],positive=q['positive'],end=end,face=fid,wholeQuarterZRange=[lo,hi],physicalBoundaryZ=boundary,edgeVertexZ=edgez,sourceHalfWidth=width,positions=p[vi].tolist(),uv=uv[vi].tolist(),uvAxes=axes,tangentBits=int(bits[vi[0]]),tangent=decode(bits[vi[0]]).tolist(),hand=-1 if int(bits[vi[0]])&(1<<31) else 1,preparedFloat32VertexRange=[mapped.min(0).tolist(),mapped.max(0).tolist()],guardContainsEntireAffineTriangle=True,finiteEvidence='The chart has a complete physical end edge at the minimum/maximum Z of the entire source asphalt quarter; its third point lies inside that quarter. Other boundaries are the original end-chart diagonal, not a period reset plane.'))
(OUT/'middle-asphalt-ends.json').write_text(json.dumps(dict(rows=rows,existingUnitKey=product['key'],newMaterialFiles=0,samplingQualityAccepted=False),indent=2))
def ff(v):
 s=format(float(v),'.17g');return s+('f' if '.' in s or 'e' in s else '.0f')
def vec(v):return 'new Vector3('+','.join(map(ff,v))+')'
arms=[]
for r in rows:
 key=(4 if r['positive'] else 0)+(2 if r['right'] else 0)+(1 if r['end'] else 0)
 arms.append('            '+str(key)+' => new Chart('+','.join([vec(x['affineXz1']) for x in r['uvAxes']]+[vec(r['tangent']),ff(r['hand']),ff(r['physicalBoundaryZ']),ff(r['edgeVertexZ']),ff(r['sourceHalfWidth'])])+',new Vector2('+','.join(map(ff,np.min(r['uv'],axis=0)))+'),new Vector2('+','.join(map(ff,np.max(r['uv'],axis=0)))+')),')
code='''using System;
using System.Collections.Generic;
using UnityEngine;
using Partition=BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type=BridgeBuilder.Bridges.MovableRoadAppearance.Type;
namespace BridgeBuilder.Bridges;
internal static class Draw01MiddleAsphaltEnds
{
    private readonly struct Chart
    {
        internal readonly Vector3 U,V,T;internal readonly float Hand,End,Edge,Width;internal readonly Vector2 Min,Max;
        internal Chart(Vector3 u,Vector3 v,Vector3 t,float hand,float end,float edge,float width,Vector2 min,Vector2 max){U=u;V=v;T=t;Hand=hand;End=end;Edge=edge;Width=width;Min=min;Max=max;}
    }
    internal static Partition Create(bool right,bool positive,float minX,float maxX,Matrix4x4 frame,Partition outside,
        List<MovableExistingRoadSurface.Patch> patches,List<Type> types,Func<Vector4,Partition,Partition,Partition> cut)
    {
        float side=right?1:-1,sign=positive?1:-1;
        Partition End(bool last,Partition fallback)
        {
            var c=((positive?4:0)+(right?2:0)+(last?1:0)) switch {
ARMS
                _=>throw new InvalidOperationException("Invalid Draw01 finite asphalt end.")};
            float scale=c.Width/(maxX-minX),offset=(right?0:-c.Width)-scale*minX;
            Vector4 UV(Vector3 a)=>new Vector4(a.x*scale,0,a.y,a.x*offset+a.z);
            var t=frame.inverse.MultiplyVector(c.T);var tangent=new Vector4(t.x,t.y,t.z,c.Hand);
            int index=patches.Count;
            patches.Add(Draw01FiniteAsphalt.Create(UV(c.U),UV(c.V),frame,c.Min,c.Max,tangent));types.Add(Type.Asphalt);
            float slope=(c.Edge-c.End)/c.Width;
            var diagonal=new Vector4(-slope*side*scale,0,sign,-c.End-slope*side*offset);
            var terminal=new Vector4(0,0,sign,-c.End);
            return last?cut(terminal,cut(diagonal,fallback,new Partition(index)),fallback)
                :cut(terminal,fallback,cut(diagonal,new Partition(index),fallback));
        }
        return End(false,End(true,outside));
    }
}
'''.replace('ARMS','\n'.join(arms))
(ROOT/'src/BridgeBuilder/Bridges/Types/Draw01/Draw01MiddleAsphaltEnds.cs').write_text(code)
print('eight finite ends validated; existing guarded product reused')
