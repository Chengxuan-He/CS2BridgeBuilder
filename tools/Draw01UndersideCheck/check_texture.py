from pathlib import Path
import sys
import numpy as np
from PIL import Image
r=Path(sys.argv[1])
im=np.array(Image.open(r/'draw01-main-albedo-raw.png'))
c=np.loadtxt(sys.argv[2],delimiter=',')
for lod,suffix in enumerate(['','_LOD1']):
 a=np.load(r/f'draw01-lod{lod}-uv-evidence.npz');v=np.array([list(map(float,l.split()[1:])) for l in (r/f'movable-skin/DrawBridge01{suffix} Mesh.mesh-0.obj').read_text().splitlines() if l.startswith('v ')])
 f=a['faces'];q=v[f];n=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]);mask=(np.max(abs(q[:,:,1]-9.5),axis=1)<.001)&(n[:,1]<0)&(np.max(abs(q[:,:,0]),axis=1)<3.367)&(q[:,:,2].mean(1)<0)
 old=a['uv'].copy();new=old.copy();row=c[c[:,0]==lod];new[row[:,1].astype(int),:2]=row[:,2:]
 # Every connected source panel must retain one affine UV chart, including seam vertices.
 groups=[]
 for tri in f[mask]:
  ids=set(tri);hit=[g for g in groups if ids & g]
  for g in hit:ids|=g;groups.remove(g)
  groups.append(ids)
 worst=0
 for group in groups:
  ids=sorted(group);points=np.c_[v[ids,0],v[ids,2],np.ones(len(ids))]
  fit=np.linalg.lstsq(points,new[ids,:2],rcond=None)[0]
  worst=max(worst,float(np.max(abs(points@fit-new[ids,:2]))))
 assert worst<1e-6,(lod,'UV island is discontinuous',worst)
 print('LOD',lod,'UV islands',len(groups),'maximum affine residual',worst)
 counts=[]
 for uv in [old,new]:
  count=0
  for tri in f[mask]:
   w=np.array([[1-i/100-j/100,i/100,j/100] for i in range(101) for j in range(101-i)])
   xy=np.clip((w@uv[tri,:2]*4096).astype(int),0,4095);rgb=im[xy[:,1],xy[:,0],:3].astype(float)
   count+=np.count_nonzero((rgb[:,0]>rgb[:,2]*1.6)&(rgb[:,1]>rgb[:,2]*1.35)&(rgb[:,0]>100))
  counts.append(count)
 print('LOD',lod,'yellow samples before/after',counts)
 assert counts[0]>0 and counts[1]==0
