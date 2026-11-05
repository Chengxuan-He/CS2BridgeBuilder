import numpy as np,json,struct
from pathlib import Path
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/closure53'
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f];lo=p.min(1);hi=p.max(1);bones=d['mesh0_bones'].reshape(-1);print('bones',lod,np.unique(bones))
 lateral=((lo[:,0]>=9.4999)&(hi[:,0]<=10.0001))|((lo[:,0]>=-10.0001)&(hi[:,0]<=-9.4999));motion=np.all(bones[f]!=0,axis=1);ids=np.flatnonzero(lateral&motion)
 raw=(R/f'artifacts/material-remap/Draw02/underside52/after52-lod{lod}-delta2-case0.bin').read_bytes();nv,nf,nfrag=struct.unpack_from('<3i',raw);mask=np.frombuffer(raw,dtype='?',offset=12+nv*12,count=nf);pos=np.frombuffer(raw,dtype='<f4',offset=12,count=nv*3).reshape(-1,3)
 print('outerband',lod,len(ids),'removed',sum(mask[ids]),'bounds',p[ids].min((0,1)),p[ids].max((0,1)))
 (O/f'lod{lod}-outer-members.json').write_text(json.dumps(dict(faces=ids.tolist(),vertices=np.unique(f[ids]).tolist(),removedFaces=ids[mask[ids]].tolist()),indent=2))
