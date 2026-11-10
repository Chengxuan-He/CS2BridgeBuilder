from pathlib import Path
import numpy as np,json,struct
R=Path('E:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/edge55';O.mkdir(exist_ok=True)
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f];lo=p.min(1);hi=p.max(1);bone=d['mesh0_bones'].reshape(-1);b=(R/f'artifacts/material-remap/Draw02/underside52/before-lod{lod}-delta2.bin').read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);mask=np.frombuffer(b,dtype='?',offset=12+nv*12,count=nf)
 ids=np.flatnonzero(((lo[:,0]>=9.4999)|(hi[:,0]<=-9.4999))&np.all(bone[f]!=0,axis=1));selected=ids[mask[ids]];old=set(json.loads((R/f'artifacts/material-remap/Draw02/closure53/lod{lod}-outer-members.json').read_text())['removedFaces']);new=sorted(map(int,set(selected)-old))
 print(lod,'outer removed',len(selected),'additional',len(new),'additional bounds',p[new].min((0,1)).tolist() if new else None,p[new].max((0,1)).tolist() if new else None)
 (O/f'lod{lod}-outer-members.json').write_text(json.dumps(dict(faces=ids.tolist(),vertices=np.unique(f[ids]).tolist(),removedFaces=selected.tolist(),additionalFaces=new),indent=2))
