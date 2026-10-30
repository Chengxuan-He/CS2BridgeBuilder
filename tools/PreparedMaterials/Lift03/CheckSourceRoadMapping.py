"""Replay Lift03 emitted material-domain decisions on original source interiors."""
import hashlib,json,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Lift03'
rows=json.loads((OUT/'source-road-mapping.json').read_text())['records'];audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift03/outputs/native-repeat-mechanism/audit.json').read_text());results=[]
weights=np.array([[1/3,1/3,1/3],[.2,.3,.5],[.6,.2,.2],[.2,.6,.2]]);fixture=[]
for report in audit['reports'][:4]:
 lod,deck=report['lod'],report['deck'];a=np.load(ROOT/'artifacts/uv-density/native'/('LiftBridge03'+('_LOD1' if lod else '')+' Mesh.npz'));verts=a['mesh0_positions'].astype(float);faces=a['mesh0_faces'];uv=a['mesh0_uv0'].astype(float);width=8 if deck=='Main' else 5;groups={}
 for r in rows:
  if r['lod']==lod and r['deck']==deck:groups.setdefault((r['xSide'],r['zSide']),{}).setdefault(r['cell'],[]).append(r)
 counts=0;maxExact=0;maxFloat=0;fail=[];samples=[]
 for fid in report['faceIds']:
  tri=faces[fid]
  for w in weights:
   p=w@verts[tri];q=w@uv[tri]
   if abs(p[0])>width or abs(p[0])<1e-8 or abs(p[2])<1e-8:continue
   h=uv[tri].astype(np.float16)
   lower=(np.nextafter(h,np.float16(-np.inf)).astype(float)+uv[tri])/2
   upper=(np.nextafter(h,np.float16(np.inf)).astype(float)+uv[tri])/2
   samples.append(dict(position=p.tolist(),uv=q.tolist(),lower=(w@lower).tolist(),upper=(w@upper).tolist()))
   xSide=-1 if p[0]<0 else 1;zSide=-1 if p[2]<0 else 1;point=np.array([-abs(p[0]),abs(p[2]),1]);cells=groups[xSide,zSide]
   def band(cell):
    rr=cells[cell];return next((r for r in rr if r['physicalX'][0]<=point[0]<=r['physicalX'][1]),rr[0])
   chosen=band(0)
   for cell in sorted(cells)[1:]:
    candidate=band(cell)
    if point@np.array(candidate['startPlane'])>=0:chosen=candidate
    else:break
   coeff=np.array(chosen['coeff'])
   if chosen['foldEvidence'] and point@np.array(chosen['foldPlane'])>0:
    coeff=np.array(chosen['alternate'])
    if chosen['alternate2'] is not None and point@np.array(chosen['foldPlane2'])>0:coeff=np.array(chosen['alternate2'])
   calculated=point@coeff;error=float(np.max(abs(calculated-q))*4096);floatError=float(np.max(abs(point.astype(np.float32)@coeff.astype(np.float32)-q))*4096)
   counts+=1;maxExact=max(maxExact,error);maxFloat=max(maxFloat,floatError)
   if error>1e-7:fail.append(dict(face=int(fid),cell=chosen['cell'],position=p.tolist(),sourceUV=q.tolist(),mappedUV=calculated.tolist(),sourcePixelError=error))
 results.append(dict(lod=lod,deck=deck,samples=counts,maxDoubleSourcePixelError=maxExact,maxFloat32SourcePixelError=maxFloat,failures=fail));fixture.append(dict(lod=lod,deck=deck,samples=samples))
result=dict(cases=results,sourceResourceSHA256=hashlib.sha256((ROOT/'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03SourceRoadMapping_cs.bin').read_bytes()).hexdigest(),scope='Original source face interiors and exact double domain models; float32 evaluation error recorded separately. Not generated bridge/render/load acceptance.')
(OUT/'source-road-domain-check.json').write_text(json.dumps(result,indent=2));print([(r['lod'],r['deck'],r['samples'],len(r['failures']),r['maxDoubleSourcePixelError'],r['maxFloat32SourcePixelError']) for r in results])
assert not any(r['failures'] for r in results)
(OUT/'source-road-replay-fixture.json').write_text(json.dumps(dict(cases=fixture)))
