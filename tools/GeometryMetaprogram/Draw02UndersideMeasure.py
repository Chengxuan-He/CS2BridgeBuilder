from pathlib import Path
import numpy as np,json
R=Path('E:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/underside52'
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f];lo=p.min(1);hi=p.max(1)
 center=np.flatnonzero((lo[:,0]>=-.201)&(hi[:,0]<=.201)&(lo[:,1]>=4.2055)&(hi[:,1]<=9.7001)&(lo[:,2]>=-78.51)&(hi[:,2]<=78.51))
 ids=np.unique(f[center]);print('CENTER',lod,len(center),len(ids),v[ids].min(0).tolist() if len(ids) else [],v[ids].max(0).tolist() if len(ids) else [])
 # Print coarse source cross-section: standalone original far faces.
 if lod==2:
  for k in np.flatnonzero((lo[:,2]>0)&(hi[:,2]<79)&(lo[:,1]<9.8)):
   print(k, f[k].tolist(), lo[k].tolist(),hi[k].tolist())
 (O/f'lod{lod}-center-members.json').write_text(json.dumps(dict(faces=center.tolist(),vertices=ids.tolist()),indent=2))
 walk=np.flatnonzero((lo[:,1]>=9.5)&(hi[:,1]<=10.01)&(lo[:,2]>0)&(hi[:,2]<78.8)&(lo[:,0]>=6.5)&(hi[:,0]<=9.65))
 n=np.cross(p[walk,1]-p[walk,0],p[walk,2]-p[walk,0]);print('WALK',lod,len(walk),'negativeY',int((n[:,1]<0).sum()),'positiveY',int((n[:,1]>0).sum()))
for lod in range(2):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);m=json.loads((O/f'lod{lod}-center-members.json').read_text());ids=np.array(m['vertices']);incident=np.flatnonzero(np.isin(f,ids).any(1));print('center incident',lod,len(incident),len(set(incident)-set(m['faces'])))
 p=v[f];lo=p.min(1);hi=p.max(1);n=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);bottom=np.flatnonzero((lo[:,1]>=9.5)&(hi[:,1]<=10.01)&(lo[:,2]>0)&(hi[:,2]<78.8)&(lo[:,0]>=6.5)&(hi[:,0]<=9.65)&(n[:,1]<0))
 print('walk bottom faces',lod,bottom.tolist(),'Y',np.unique(p[bottom,:,1]).tolist(),'X',np.unique(p[bottom,:,0]).tolist());(O/f'lod{lod}-walk-bottom.json').write_text(json.dumps(bottom.tolist()))

import struct
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f];lo=p.min(1);hi=p.max(1);normal=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);data=(O/f'before-lod{lod}-delta2.bin').read_bytes();nv,nf,nfrag=struct.unpack_from('<3i',data);moved=np.frombuffer(data,dtype='<f4',offset=12,count=nv*3).reshape(-1,3);mask=np.frombuffer(data,dtype='?',offset=12+nv*12,count=nf)
 center=json.loads((O/f'lod{lod}-center-members.json').read_text());ids=center['vertices'];print('BEFORE central',lod,'width',float(np.ptp(moved[ids,0])) if ids else None)
 walks=[]
 for side in (-1,1):
  lateral=(lo[:,0]>=6.5539)&(hi[:,0]<=9.5001) if side>0 else (lo[:,0]>=-9.5001)&(hi[:,0]<=-6.5539)
  moving=((lo[:,2]>.029)&(hi[:,2]<78.7))|((lo[:,2]>-78.7)&(hi[:,2]<-.029))
  # The authored slab's thin terminal plate spans the complete measured slab
  # width and height at either leaf end. Its left source Z values differ by
  # native quantization steps: normal.Y is slightly positive although the face
  # is vertical, so a normal-sign road-top predicate must not delete it.
  terminal=(np.abs(p[:,:,2]).max(1)<.031)|(np.abs(p[:,:,2]).min(1)>78.69)
  end_caps=lateral&moving&terminal&(lo[:,1]==np.float32(9.85009765625))&(hi[:,1]==10)&((hi[:,0]-lo[:,0])==np.float32(9.5-6.553955078125))
  if lod<2:assert int(end_caps.sum())==(8 if lod==0 else 6),(lod,side,int(end_caps.sum()))
  selected=np.flatnonzero(lateral&moving&(lo[:,1]>=9.85)&(hi[:,1]<=10.0001)&((normal[:,1]<=0)|end_caps))
  ids=np.unique(f[selected]);print('walks',lod,side,len(selected),len(ids),'removed',int(mask[selected].sum()),'crossshares',int(np.isin(f,ids).any(1).sum()-len(selected)))
  walks.append(dict(side=side,faces=selected.tolist(),vertices=ids.tolist(),endCapFaces=np.flatnonzero(end_caps).tolist(),minimum=float(v[ids,0].min()) if len(ids) else None,maximum=float(v[ids,0].max()) if len(ids) else None))
 (O/f'lod{lod}-walk-members.json').write_text(json.dumps(walks,indent=2))
