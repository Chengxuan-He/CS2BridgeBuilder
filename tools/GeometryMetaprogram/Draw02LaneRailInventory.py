"""Inventory original connected longitudinal rail parts; offline identification only."""
from pathlib import Path
import json,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
OUT=ROOT/'artifacts/material-remap/Draw02/lane-rails';OUT.mkdir(parents=True,exist_ok=True)
result=[]
for lod in range(3):
 path=SRC/f'lod{lod}-native.npz';d=np.load(path);v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 parent=list(range(len(v)))
 def root(a):
  while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
  return a
 def join(a,b):parent[root(int(a))]=root(int(b))
 for a,b,c in f:join(a,b);join(b,c)
 seen={}
 for i,p in enumerate(v):
  k=tuple(p)
  if k in seen:join(i,seen[k])
  else:seen[k]=i
 groups={}
 for i in range(len(v)):groups.setdefault(root(i),[]).append(i)
 candidates=[]
 for ids in groups.values():
  p=v[ids];lo=p.min(0);hi=p.max(0)
  if hi[2]-lo[2]>70 and hi[1]>10 and lo[1]<11 and hi[0]-lo[0]<2:
   faces=np.flatnonzero(np.isin(f[:,0],ids)).tolist()
   candidates.append(dict(vertices=ids,faces=faces,minimum=lo.tolist(),maximum=hi.tolist(),nativeLowSeparator=bool(9.78<lo[1]<9.79 and 10.29<hi[1]<10.31)))
 result.append(dict(lod=lod,source=str(path),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),candidates=candidates))
(OUT/'original-rail-components.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
for r in result:print(r['lod'],[(len(c['vertices']),len(c['faces']),c['minimum'],c['maximum'],c['nativeLowSeparator']) for c in r['candidates']])
# The orange high separator is welded into the lateral shell. Its complete
# cross-section (including the 9.8000488 m foot) is bounded by the measured
# x=6.0999756/6.5999756 edges; the adjoining walk extends to x=9.5 and is excluded.
# These predicates are offline identification only; emit exact face membership.
high=[]
for lod in range(2):
 d=np.load(SRC/f'lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);p=v[f]
 for side in (-1,1):
  for leaf in (-1,1):
   x=p[:,:,0]*side;z=p[:,:,2]*leaf
   keep=(x.min(1)>=6.0999)&(x.max(1)<=6.6001)&(p[:,:,1].min(1)>=9.7999)&(p[:,:,1].max(1)<=11.0001)&(z.min(1)>=0)&(z.max(1)<79)
   faces=np.flatnonzero(keep);ids=np.unique(f[faces]);q=v[ids]
   assert len(ids)>0
   bones=np.unique(d['mesh0_bones'][ids],axis=0)
   high.append(dict(lod=lod,side=side,leaf=leaf,anchorX=float(np.float32(side)*np.float32(6.3499755859375)),vertices=ids.tolist(),faces=faces.tolist(),minimum=q.min(0).tolist(),maximum=q.max(0).tolist(),bones=bones.tolist(),uvMinimum=d['mesh0_uv'][ids].min(0).tolist(),uvMaximum=d['mesh0_uv'][ids].max(0).tolist()))
(OUT/'original-high-separators.json').write_text(json.dumps(high,indent=2),encoding='utf-8')
for r in high:print('high',r['lod'],r['side'],r['leaf'],len(r['vertices']),len(r['faces']),r['minimum'],r['maximum'],'bones',r['bones'])
