from pathlib import Path
import numpy as np
import sys
r=Path(sys.argv[1])
import json
for lod,suffix in enumerate(['','_LOD1']):
 a=np.load(r/f'draw01-lod{lod}-uv-evidence.npz');v=np.array([list(map(float,l.split()[1:])) for l in (r/f'movable-skin/DrawBridge01{suffix} Mesh.mesh-0.obj').read_text().splitlines() if l.startswith('v ')])
 f=a['faces'];q=v[f];n=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]);m=(np.max(abs(q[:,:,1]-9.5),axis=1)<.001)&(n[:,1]<0)&(np.max(abs(q[:,:,0]),axis=1)<3.367)
 groups=[]
 for fi in np.flatnonzero(m):
  ids=set(f[fi]); hit=[g for g in groups if g[0]&ids]
  for g in hit:ids|=g[0];groups.remove(g)
  groups.append((ids,[fi]+sum([g[1] for g in hit],[])))
 pos=[g for g in groups if v[list(g[0]),2].mean()>0];neg=[g for g in groups if v[list(g[0]),2].mean()<0];out={}
 print(lod,'islands',len(neg),len(pos))
 for ids,faces in neg:
  pts=-v[list(ids)][:,[0,2]];lo=pts.min(0);hi=pts.max(0)
  target=min([g for g in pos if abs(sum(abs(n[j,1])*.5 for j in g[1])-np.prod(np.ptp(v[list(g[0])][:,[0,2]],axis=0)))<1e-5],key=lambda g:np.linalg.norm(v[list(g[0])][:,[0,2]].mean(0)-pts.mean(0)))
  ts=v[list(target[0])][:,[0,2]];tl=ts.min(0);th=ts.max(0)
  for i in ids:
   p=tl+(-v[i][[0,2]]-lo)/(hi-lo)*(th-tl)
   for j in target[1]:
    t=q[j][:,[0,2]];M=np.stack([t[1]-t[0],t[2]-t[0]],axis=1)
    if abs(np.linalg.det(M))<1e-9:continue
    bc=np.linalg.solve(M,p-t[0]);w=np.array([1-bc.sum(),*bc])
    if min(w)<-1e-6:continue
    out[int(i)]=(w@a['uv'][f[j]]).tolist();break
   else:raise Exception((lod,i))
 json.dump(out,open(Path(sys.argv[2])/f'islanduv{lod}.json','w'))


