"""Independent native-output checks for the Draw02 underside candidate."""
from pathlib import Path
import numpy as np,struct,json
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/underside52'
def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,dtype='<f4',offset=12,count=nv*3).reshape(-1,3);offset=12+nv*12;m=np.frombuffer(b,dtype='?',offset=offset,count=nf);offset+=nf;frag={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,offset);offset+=8;frag[face]=np.frombuffer(b,dtype='<i4',offset=offset,count=n).tolist();offset+=n*4
 return p,m,frag
cases=0;restored=0;stagedDifferences=0;maxStagedDifference=0.0
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);center=json.loads((O/f'lod{lod}-center-members.json').read_text());walks=json.loads((O/f'lod{lod}-walk-members.json').read_text())
 for delta in (0,2,8):
  old,oldmask,oldfragments=read(O/f'before-lod{lod}-delta{delta}.bin')
  for scenario in range(5):
   p,m,fragments=read(O/f'after52-lod{lod}-delta{delta}-case{scenario}.bin');assert np.array_equal(p[:len(v),1:],v[:,1:]);assert fragments==oldfragments
   assert np.array_equal(p[center['vertices']],v[center['vertices']])
   if lod==2:assert np.array_equal(p,old) and np.array_equal(m,oldmask)
   spans=[((-9.5-delta,-6-delta),(6+delta,9.5+delta)),((0,0),(0,0)),((-12,-7),(0,0)),((0,0),(8,12)),((-13,-8),(9,11))][scenario]
   for walk,span in zip(walks,spans):
    ids=walk['vertices'];faces=walk['faces'];present=span[1]>span[0];assert np.all(m[faces]==(not present))
    if not ids or not present:continue
    assert not any(face in fragments for face in faces)
    minimum=np.float32(-9.5 if walk['side']<0 else 6);fraction=(v[ids,0]-minimum)/np.float32(3.5)
    origin=v[ids,0]+(np.float32(span[0])-minimum)
    factor=np.float32(span[1]-span[0]-3.5)
    staged=origin+factor*fraction
    expect=(origin.astype(np.float64)+float(factor)*fraction.astype(np.float64)).astype(np.float32)
    stagedDifferences+=int(np.count_nonzero(staged!=expect))
    maxStagedDifference=max(maxStagedDifference,float(np.max(np.abs(staged-expect))))
    assert np.array_equal(p[ids,0],expect),(lod,delta,scenario,walk["side"],v.dtype,[(ids[i],float(p[ids[i],0]),float(expect[i])) for i in np.flatnonzero(p[ids,0]!=expect)[:4]])
    a=v[f[faces]].astype(float);b=p[f[faces]].astype(float);n0=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);n1=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);assert np.all(n0[:,1]<=0) and np.all(np.einsum('ij,ij->i',n0,n1)>0)
    restored+=len(faces)
   cases+=1
 # Old widened source and deliberately missing slab must be rejected by the independent invariants.
 if lod<2:
  old,mask,_=read(O/f'before-lod{lod}-delta2.bin');assert not np.array_equal(old[center['vertices']],v[center['vertices']]);assert np.all(mask[walks[0]['faces']])
report=dict(arithmetic="Mono AddScaled extended product/add intermediate then Float32 store; exact comparison, no epsilon",stagedFloat32DifferenceCount=stagedDifferences,maxStagedFloat32Difference=maxStagedDifference,cases=cases,restoredFaceVisits=restored,allOriginalYZ=True,centerOriginalXYZ=True,sidewalkNativeAffine=True,absentSidesRemoved=True,noRestoredUpwardRoad=True,sourceWindingPreserved=True,fragmentsUnchanged=True,farShellUnchanged=True,oldWrongWidthAndDeletedSlabNegativeControls=True)
(O/'geometry-result.json').write_text(json.dumps(report,indent=2));print(report)