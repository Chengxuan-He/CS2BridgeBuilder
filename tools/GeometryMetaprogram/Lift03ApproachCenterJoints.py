"""Emit exact authored center-joint ownership for the fixed Lift03 NetPiece."""
from pathlib import Path
import sys,json,numpy as np
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import write_bbn
O=ROOT/'artifacts/material-remap/Lift03/appearance76/fixed-source';data=[1,3,0,0,0];report=[]
for lod in range(3):
 m=np.load(O/f'lod{lod}.npz');v=m['positions'];f=m['faces'];weights=np.sign(v[:,0]).astype(int);ranges=[]
 for i,weight in enumerate(weights):
  if ranges and ranges[-1][2]==int(weight):ranges[-1][1]=i
  else:ranges.append([i,i,int(weight)])
 data[2+lod]=len(data);data.extend([len(v),int(f.size),len(ranges)]);data.extend(np.array(ranges).reshape(-1).tolist())
 shared={}
 for i,p in enumerate(v):
  key=tuple(p);assert key not in shared or shared[key]==weights[i];shared[key]=weights[i]
 faces=v[f];same=(np.ptp(weights[f],axis=1)==0);checks=[]
 for delta in (0.,2.,8.,-.25,-8.,-16.):
  q=v.astype(float).copy();q[:,0]+=weights*delta;assert np.isfinite(q).all() and np.array_equal(q[:,1:],v[:,1:]);assert np.allclose(q[f][same]-faces[same],np.array([1,0,0])*weights[f[same,0],None,None]*delta,atol=1e-10,rtol=0)
  n=np.cross(faces[:,1]-faces[:,0],faces[:,2]-faces[:,0]);t=q[f];nn=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);bad=int(((np.linalg.norm(n,axis=1)>1e-7)&((n*nn).sum(1)<=0)).sum());checks.append(dict(delta=delta,nonpositiveOrientation=bad))
  if delta>=-.25:assert bad==0
 report.append(dict(lod=lod,sourceVertices=len(v),sourceFaces=len(f),sourceIndices=int(f.size),ranges=len(ranges),rigidFaces=int(same.sum()),centerConnectionFaces=int((~same).sum()),contactMismatches=0,checks=checks))
(ROOT/'assets/HardcodedNumbers/Lift03/Types_Lift03_ApproachCenterJoints.bin').write_bytes(write_bbn(data,[]))
(O/'center-joints-verification.json').write_text(json.dumps(report,indent=2)+'\n');print(report)
