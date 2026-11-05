from pathlib import Path
import numpy as np,json
R=Path('D:/CS2BridgeBuilder'); O=R/'artifacts/material-remap/Draw02/underside52';O.mkdir(exist_ok=True)
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 with (O/f'lod{lod}-fixture.bin').open('wb') as h:
  h.write(np.array([len(v),f.size],dtype='<i4').tobytes());h.write(v.astype('<f4').tobytes());h.write(f.astype('<i4').tobytes())
 # Unwelded authored topology isolates UV/material faces without combining coincident members.
 parent=np.arange(len(v))
 def root(i):
  while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
  return i
 def union(a,b):parent[root(a)]=root(b)
 for a,b,c in f:union(a,b);union(a,c)
 groups={}
 for i in np.unique(f):groups.setdefault(int(root(i)),[]).append(int(i))
 rows=[]
 for ids in groups.values():
  p=v[ids];lo=p.min(0);hi=p.max(0)
  if lo[0]<0<hi[0] and lo[1]<9.7:
   rows.append(dict(vertices=ids,minimum=lo.tolist(),maximum=hi.tolist()))
 (O/f'lod{lod}-central-groups.json').write_text(json.dumps(rows,indent=2))
 print(lod,[(len(r['vertices']),r['minimum'],r['maximum']) for r in rows if r['maximum'][0]-r['minimum'][0]<2])