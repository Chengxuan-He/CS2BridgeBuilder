"""Independent final end-cap topology, directed sections and shared-contact checks."""
from pathlib import Path
import json,struct,os
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/endcap60'
recipes=json.loads((O/'source-recipe.json').read_text())
tag=os.environ.get('DRAW02_PLAN_TAG','after60')
def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3);off=12+nv*12;m=np.frombuffer(b,'?',nf,off);off+=nf;fr={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,off);off+=8;fr[face]=np.frombuffer(b,'<i4',n,off).tolist();off+=n*4
 return p,m,fr
cases=0;faceVisits=0;contactChecks=0;minWidth=float('inf');maxWidthError=0.;negative=False
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 for delta in ('-0.1','0','2','4','5.75','8'):
  for scenario in range(6):
   p,m,fr=read(O/f'{tag}-lod{lod}-delta{delta}-case{scenario}.bin');old,om,of=read(O/f'baseline60-lod{lod}-delta{delta}-case{scenario}.bin')
   allowed=set();changedFaces=set();newStart=len(old)
   assert np.array_equal(p[:len(v),1:],v[:,1:])
   for rec in [r for r in recipes if r['lod']==lod]:
    allowed.update(s['vertex'] for s in rec['source']);allowed.update(rec['panelVertices']);changedFaces.update(map(int,rec['faceKinds']))
    for faceText,kind in rec['faceKinds'].items():
     face=int(faceText);ids=fr.get(face,f[face].tolist() if not m[face] else [])
     assert ids and (m[face] or face not in fr)
     triangles=p[ids].reshape(-1,3,3).astype(float);original=v[f[face]].astype(float)
     n=np.cross(original[1]-original[0],original[2]-original[0]);axis=int(np.argmax(np.abs(n)))
     normals=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0]);assert np.all(normals[:,axis]*n[axis]>0),(lod,delta,scenario,face,'reversed')
     # Merge coincident hard-frame copies, then require a single closed boundary
     # with paired opposite internal edges. No missing fan, hole or duplicate tri.
     edges={};triKeys=set()
     for tri in triangles:
      points=[tuple(x) for x in tri];key=tuple(sorted(points));assert key not in triKeys;triKeys.add(key)
      for a,b in zip(points,points[1:]+points[:1]):edges.setdefault(tuple(sorted((a,b))),[]).append((a,b))
     boundary={}
     for uses in edges.values():
      assert len(uses)<=2
      if len(uses)==2:assert uses[0]==uses[1][::-1]
      else:
       a,b=uses[0];assert a not in boundary;boundary[a]=b
     assert len(boundary)>=3
     first=next(iter(boundary));point=first;seen=set()
     while point not in seen:seen.add(point);point=boundary[point]
     assert point==first and len(seen)==len(boundary)
     if lod<2:
      assert all(tuple(p[i]) in seen for i in f[face])
     else:
      assert triangles[:,:,1].max()<=9.80005 and triangles[:,:,1].max()>9.7999
     faceVisits+=1
    if lod<2:
     side=rec['side'];edge=np.float32(9.5+float(delta));width=([3.5,0,3,0,2,3.5] if side<0 else [3.5,0,0,3,4,3.5])[scenario]
     bound=np.float32(edge-width) if width else np.float32(13.25 if delta=='5.75' else 6+float(delta))
     shift=np.float32(side*bound-np.float32(side*6.0999755859375))
     inner=np.flatnonzero((v[:,0]==np.float32(side*5.74609375))&(v[:,1]==np.float32(9.699951171875))&(np.abs(v[:,2])<79))
     outer=np.float32(np.float32(side*6.14599609375)+shift)
     expected=np.float32(v[inner,0]+shift);assert np.array_equal(p[inner,0],expected),(lod,delta,scenario,side,[(int(inner[i]),float(p[inner[i],0]),float(expected[i])) for i in np.flatnonzero(p[inner,0]!=expected)[:6]])
     section=side*(outer-p[inner,0]);assert np.all(section>0);minWidth=min(minWidth,float(section.min()))
     err=np.abs(section.astype(float)-.39990234375);maxWidthError=max(maxWidthError,float(err.max()))
     budget=np.abs(np.spacing(np.float32(outer)))+np.abs(np.spacing(p[inner,0]));assert np.all(err<=budget)
     panel=np.array(rec['panelVertices']);assert np.array_equal(p[panel,0],np.float32(v[panel,0]+shift))
     assert side*outer-float(bound)>0
     for s in rec['source']:
      if np.float32(s['coefficients'][0])==1:assert p[s['vertex'],0]==np.float32(v[s['vertex'],0]+shift)
     contactChecks+=len(inner)+len(panel)
     if lod==0 and delta=='4' and scenario==4 and side==1:
      assert np.any(side*(outer-old[inner,0])<0);negative=True
    newStart+=len(rec['edges'])
   assert len(p)==newStart
   untouched=np.array(sorted(set(range(len(v)))-allowed));assert np.array_equal(p[untouched],old[untouched])
   otherFaces=np.array(sorted(set(range(len(f)))-changedFaces));assert np.array_equal(m[otherFaces],om[otherFaces])
   assert {k:x for k,x in fr.items() if k not in changedFaces}=={k:x for k,x in of.items() if k not in changedFaces}
   cases+=1
assert negative
result=dict(cases=cases,sourceFaceTopologyVisits=faceVisits,originalSectionContactVertexChecks=contactChecks,minimumDirectedUpperBeamWidth=minWidth,maximumWidthFloat32Deviation=maxWidthError,originalWidth=.39990234375,allSourceYZUnchanged=True,unrelatedMasksPositionsFragmentsUnchanged=True,oneClosedFaceBoundaryNoDuplicateOrReversedTriangles=True,farNativeCapReachesOriginalRoadContact=True,wideSidewalkOldReversedSectionNegativeControl=True)
(O/('geometry-result.json' if tag=='after60' else f'{tag}-geometry-result.json')).write_text(json.dumps(result,indent=2));print(result)
