"""Independent final-Plan and original contact-edge verification for Draw02."""
from pathlib import Path
import json,struct,os
import numpy as np
R=Path(__file__).resolve().parents[2];Evidence=R/'artifacts/material-remap/Draw02/gap59'
O=Path(os.environ.get('DRAW02_CLOSURE_CHECK_OUTPUT',str(R/'artifacts/material-remap/Draw02/gap59r2')))
records=json.loads((Evidence/'source-members.json').read_text())
splitRecipes=json.loads((Evidence/'road-contact-split.json').read_text())
rails=json.loads((R/'artifacts/material-remap/Draw02/lane-rails/original-high-separators.json').read_text())
def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3);off=12+nv*12;m=np.frombuffer(b,'?',nf,off);off+=nf;frag={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,off);off+=8;frag[face]=np.frombuffer(b,'<i4',n,off).tolist();off+=n*4
 return p,m,frag
cases=0;wallVisits=0;singleOwnerVisits=0;contacts=0;minClearance=float('inf');areaError=0.
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 for delta in ('-0.1','0','2','4','5.75','8'):
  outer=np.float32(13.25 if delta=='5.75' else 6+float(delta))
  for scenario in range(6):
   p,m,fr=read(O/f'after59-lod{lod}-delta{delta}-case{scenario}.bin')
   old,om,of=read(Evidence/f'baseline59-lod{lod}-delta{delta}-case{scenario}.bin')
   if lod==2:
    assert np.array_equal(p,old) and np.array_equal(m,om) and fr==of;cases+=1;continue
   changed=set();faces=set();nextConstructed=len(old)
   for rec in [x for x in records if x['lod']==lod]:
    side=rec['side'];edge=np.float32(9.5+float(delta));width=([3.5,0,3,0,2,3.5] if side<0 else [3.5,0,0,3,4,3.5])[scenario]
    bound=np.float32(edge-width) if width else outer
    shift=np.float32(side*bound-np.float32(side*6.0999755859375))
    ids=np.array(sorted(set(rec['wallTop']+rec['slabInner'])))
    assert np.array_equal(p[ids,0],np.float32(v[ids,0]+shift))
    assert np.array_equal(p[ids,1:],v[ids,1:])
    changed.update(ids);faces.update(rec['wallFaces']);faces.update(rec['railOwnedFaces'])
    recipe=next(x for x in splitRecipes if x['lod']==lod and x['side']==side)
    coincident={}
    for i,e in enumerate(recipe['edges']):
     key=(e['a'],e['b'],e['fraction'])
     if key in coincident:assert np.array_equal(p[nextConstructed+i],coincident[key])
     else:coincident[key]=p[nextConstructed+i]
    assert len(coincident)*2==len(recipe['edges'])
    nextConstructed+=len(recipe['edges'])
    assert m[rec['wallFaces']].all() and m[rec['railOwnedFaces']].all()
    assert not set(fr).intersection(rec['railOwnedFaces'])
    for face in rec['wallFaces']:
     assert len(fr[face])==9
     q=p[fr[face]].reshape(-1,3,3).astype(float);src=v[f[face]].astype(float)
     normal=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]);original=np.cross(src[1]-src[0],src[2]-src[0])
     assert np.all(normal[:,0]*original[0]>0)
     # Projected YZ coverage preserves original source face area. Bound Float32
     # lerp rounding before computing the shoelace area, rather than fitting tolerance.
     err=abs(np.abs(normal[:,0]).sum()-abs(original[0]));areaError=max(areaError,err)
     eps=4*np.finfo(np.float32).eps*np.maximum(1,np.abs(src).max(0))
     boundArea=12*(np.ptp(src[:,1])*eps[2]+np.ptp(src[:,2])*eps[1]+eps[1]*eps[2])
     assert err<=boundArea
     upper=q[q[:,:,1].max(1)>9.8]
     assert upper.shape[0]>0
     clearance=side*upper[:,:,0]-float(bound)
     assert clearance.min()>0
     minClearance=min(minClearance,float(clearance.min()))
     low=q[q[:,:,1].max(1)<=9.8]
     assert low[:,:,1].max()<9.7999
    # Verify against actual original rail members, not just duplicated formula constants.
    for leaf in (-1,1):
     rail=next(x for x in rails if x['lod']==lod and x['side']==side and x['leaf']==leaf)
     rid=np.unique(f[rail['faces']]);rv=v[rid].copy();rv[:,0]=np.float32(rv[:,0]+shift)
     points={tuple(x) for x in rv}
     railPositions=v.copy();railPositions[rid,0]=np.float32(railPositions[rid,0]+shift)
     edges=[(railPositions[a],railPositions[b]) for face in rail['faces'] for a,b in zip(f[face],np.roll(f[face],-1))]
     for i in rec['wallTop']+rec['slabInner']:
      if np.sign(v[i,2])==leaf:
       onEdge=any(np.array_equal(a[:2],p[i,:2]) and np.array_equal(b[:2],p[i,:2]) and min(a[2],b[2])<=p[i,2]<=max(a[2],b[2]) for a,b in edges)
       assert tuple(p[i]) in points or onEdge,(lod,side,leaf,i,p[i]);contacts+=1
    wallVisits+=len(rec['wallFaces']);singleOwnerVisits+=len(rec['railOwnedFaces'])
   assert nextConstructed==len(p)
   untouched=np.array(sorted(set(range(len(v)))-changed))
   assert np.array_equal(p[untouched],old[untouched])
   untouchedFaces=np.array(sorted(set(range(len(f)))-faces))
   assert np.array_equal(m[untouchedFaces],om[untouchedFaces])
   assert {k:val for k,val in fr.items() if k not in faces}=={k:val for k,val in of.items() if k not in faces}
   # Original failed clipping and rail drift must not satisfy these exact contacts.
   assert any(not np.array_equal(p[i],old[i]) for i in changed)
   assert any(len(fr[i])!=len(of.get(i,[])) or fr[i]!=of.get(i) for rec in records if rec['lod']==lod for i in rec['wallFaces'])
   cases+=1
prior,_,_=read(Evidence/'after59-lod0-delta5.75-case1.bin')
d=np.load(R/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod0-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
rec=next(x for x in records if x['lod']==0 and x['side']==1);q=prior[f[rec['wallFaces'][0]]]
bottom=q[np.argmin(q[:,1])];top=q[np.argmax(q[:,1])]
atRoad=bottom+(top-bottom)*(9.9-bottom[1])/(top[1]-bottom[1]);assert atRoad[0]<13.25
report=dict(cases=cases,restoredWallFaceVisits=wallVisits,singleRailOwnedFaceVisits=singleOwnerVisits,exactOriginalRailContactVertexVisits=contacts,minimumAboveRoadCarClearance=minClearance,maxProjectedDoubleAreaError=areaError,unrelatedPositionsMasksFragmentsUnchanged=True,lowerWallsBaseSupportsCentralGirderUniformRibsUnchanged=True,farNativeMergedBottomUnchanged=True,oldGapNegativeControl=True,prior59ShoulderIntrusionRejected=True,splitContactDuplicatesCoincident=True)
(O/'geometry-result.json').write_text(json.dumps(report,indent=2));print(report)
