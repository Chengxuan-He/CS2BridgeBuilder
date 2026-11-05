"""Record original moving wall/slab edges that meet the orange rail inner shell."""
from pathlib import Path
import hashlib,json,struct
import numpy as np
R=Path(__file__).resolve().parents[2]
O=R/'artifacts/material-remap/Draw02/gap59';O.mkdir(exist_ok=True)
ints=[0]*40;records=[]
rails=json.loads((R/'artifacts/material-remap/Draw02/lane-rails/original-high-separators.json').read_text())
for lod in range(2):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
 v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f]
 walks=json.loads((R/f'artifacts/material-remap/Draw02/underside52/lod{lod}-walk-members.json').read_text())
 for side in (-1,1):
  wall=np.flatnonzero(np.all(p[:,:,0]==np.float32(side*6.14599609375),axis=1)&(p[:,:,1].max(1)==10)&(p[:,:,1].min(1)==np.float32(7.935546875))&(np.abs(p[:,:,2]).max(1)<79))
  assert len(wall)==80
  top=np.unique(f[wall]);top=top[v[top,1]==10]
  walk=next(w for w in walks if w['side']==side)
  inner=np.array([i for i in walk['vertices'] if v[i,0]==np.float32(side*6.553955078125)],dtype=np.int32)
  assert len(top)>0 and len(inner)>0
  row=[len(v),len(f)]
  duplicate=sorted(set(walk['faces'])&{i for rail in rails if rail['lod']==lod and rail['side']==side for i in rail['faces']})
  for ids in (wall,top,inner,duplicate):row.extend([len(ints),len(ids)]);ints.extend(map(int,ids))
  h=(lod*2+(side>0))*10;ints[h:h+10]=row
  records.append(dict(lod=lod,side=side,wallFaces=wall.tolist(),wallTop=top.tolist(),slabInner=inner.tolist(),railOwnedFaces=duplicate,originalWallTopX=float(side*6.14599609375),originalSlabInnerX=float(side*6.553955078125),originalLowerY=7.935546875))
blob=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,0,0)+np.array(ints,dtype='<i4').tobytes();blob+=hashlib.sha256(blob).digest()
(R/'assets/HardcodedNumbers/Draw02/Types_Draw02_RailFootClosureMeasurements.bin').write_bytes(blob)
(O/'source-members.json').write_text(json.dumps(records,indent=2))
print(len(blob),hashlib.sha256(blob).hexdigest(),[(r['lod'],r['side'],len(r['wallFaces']),len(r['wallTop']),len(r['slabInner'])) for r in records])
