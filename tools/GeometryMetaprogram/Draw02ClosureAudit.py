from pathlib import Path
import numpy as np,json,struct
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/closure53';O.mkdir(exist_ok=True)
for lod,cid in enumerate(['47331a7032e9c0eda9bdb8b522f40ca1','c401f6a114b090084d35c02325febc23','d8ef11a742ab332ffb7e583befeb21dd']):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f];lo=p.min(1);hi=p.max(1);n=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);raw=(R/f'artifacts/material-remap/Draw02/underside52/after52-lod{lod}-delta2-case0.bin').read_bytes();nv,nf,nfrag=struct.unpack_from('<3i',raw);mask=np.frombuffer(raw,dtype='?',offset=12+nv*12,count=nf)
 # original faces at outer sidewalk rim below upper surface, not road replacement
 ids=np.flatnonzero((lo[:,0]>9.4)&(hi[:,0]<10.1)&(lo[:,1]>8)&(hi[:,1]<11.2)&(lo[:,2]>0)&(hi[:,2]<79))
 rows=[dict(face=int(i),vertices=f[i].tolist(),min=lo[i].tolist(),max=hi[i].tolist(),normal=n[i].tolist(),removed=bool(mask[i])) for i in ids]
 (O/f'lod{lod}-outer-faces.json').write_text(json.dumps(rows,indent=2));print(lod,'outer',len(ids),'removed',int(mask[ids].sum()))
 ids=np.flatnonzero((lo[:,2]>-2)&(hi[:,2]<2)&(hi[:,1]>9)&(lo[:,0]>-10)&(hi[:,0]<10))
 rows=[dict(face=int(i),vertices=f[i].tolist(),min=lo[i].tolist(),max=hi[i].tolist(),normal=n[i].tolist(),removed=bool(mask[i])) for i in ids]
 (O/f'lod{lod}-seam-faces.json').write_text(json.dumps(rows,indent=2));print(lod,'seam',len(ids),'removed',int(mask[ids].sum()))
 a=json.loads((R/f'artifacts/material-remap/shared/draw02-structure52/attributes/{cid}.attributes.json').read_text())
 print('actual',[(len(m['positions']),len(m['faces'])) for m in a['meshes']])
