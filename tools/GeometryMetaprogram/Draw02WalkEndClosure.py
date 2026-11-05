"""Original far end-cap strip at the measured prototype sidewalk contact."""
from pathlib import Path
import json,struct,hashlib
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/gap61';O.mkdir(exist_ok=True)
d=np.load(R/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod2-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
base=json.loads((R/'artifacts/material-remap/Draw02/endcap60/source-recipe.json').read_text())[-1]
nodes=[];records=[]
def clip(poly,p,axis,value,sign):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da=sign*(float(a@p[:,axis])-value);db=sign*(float(b@p[:,axis])-value)
  if da>=0:result.append(a)
  if da*db<0:result.append(a+(b-a)*(-da)/(db-da))
 return result
for face in map(int,base['faceKinds']):
 tri=f[face];p=v[tri].astype(float);side=1 if p[:,0].max()>1 else -1
 poly=clip([np.eye(3)[i] for i in range(3)],p,0,side*6,side)
 poly=clip(poly,p,1,9.800048828125,1);poly=clip(poly,p,1,10,-1)
 if len(poly)<3:continue
 ids=[]
 for b in poly:
  point=b@p;nonzero=np.flatnonzero(b>1e-12);assert len(nonzero)>=2
  if len(nonzero)==2:
   a,c=nonzero;node=dict(kind=0,a=int(tri[a]),b=int(tri[c]),c=0,wB=float(b[c]),wC=0.)
  else:node=dict(kind=1,a=int(tri[0]),b=int(tri[1]),c=int(tri[2]),wB=float(b[1]),wC=float(b[2]))
  node.update(side=side,fraction=float((abs(point[0])-6)/(9.64990234375-6)),source=point.tolist())
  ids.append(len(nodes));nodes.append(node)
 indices=[]
 for i in range(1,len(ids)-1):indices.extend([ids[0],ids[i],ids[i+1]])
 records.append(dict(face=face,side=side,indices=indices))
assert len(records)==12
references=[int(np.flatnonzero(v[:,0]==np.float32(side*9.64990234375))[0]) for side in (-1,1)]
ints=[len(v),len(f),len(nodes),len(records),*references,0];floats=[]
for node in nodes:
 ints.extend([node['kind'],node['a'],node['b'],node['c'],node['side'],len(floats)])
 floats.extend([node['wB'],node['wC'],node['fraction']])
ints[6]=len(ints)
for rec in records:ints.extend([rec['face'],rec['side'],len(rec['indices']),*rec['indices']])
blob=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+np.asarray(ints,dtype='<i4').tobytes()+np.asarray(floats,dtype='<f4').tobytes();blob+=hashlib.sha256(blob).digest()
(R/'assets/HardcodedNumbers/Draw02/Types_Draw02_WalkEndMeasurements.bin').write_bytes(blob)
(O/'far-source-strip.json').write_text(json.dumps(dict(nodes=nodes,records=records,references=references),indent=2))
print(len(blob),'records',len(records),'nodes',len(nodes),'edges',sum(n['kind']==0 for n in nodes),'interior',sum(n['kind']==1 for n in nodes))
