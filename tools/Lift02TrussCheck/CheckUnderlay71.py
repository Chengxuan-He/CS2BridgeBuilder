"""Compare remapped native geometry with the exact task-70 input Plan surfaces."""
from pathlib import Path
from collections import defaultdict,Counter
import struct,json
import numpy as np
root=Path(__file__).resolve().parents[2];out=root/'artifacts/material-remap/Lift02/underlay71'
dtype=np.dtype([('face','<i4'),('material','<i4'),('vertices','<f4',(3,5))])
def read(path):
 b=path.read_bytes();n=struct.unpack_from('<i',b)[0];a=np.frombuffer(b,dtype,offset=4);assert len(a)==n;return a
results=[]
for label in ['asphalt','ballast','mixed','paving','unknown','mixed_native','one_sidewalk']:
 for lod in range(3):
  before=read(out/f'{label}-lod{lod}-baseline.bin');after=read(out/f'{label}-lod{lod}-triangles.bin')
  old=before['vertices'][:,:,:3].astype(float);new=after['vertices'][:,:,:3].astype(float)
  groups=defaultdict(list)
  for i,(face,t) in enumerate(zip(after['face'],new)):
   normal=np.cross(t[1]-t[0],t[2]-t[0]);axis=int(np.argmax(abs(normal)))
   if normal[axis]!=0:groups[(int(face),axis,float(t[0,axis]))].append(i)
  expected=defaultdict(float);actual=defaultdict(float);failures=[];samples=0;old_zero=Counter();new_zero=Counter()
  for face,t in zip(after['face'],new):
   normal=np.cross(t[1]-t[0],t[2]-t[0]);axis=int(np.argmax(abs(normal)))
   if normal[axis]==0:new_zero[(int(face),tuple(t.ravel()))]+=1
   else:actual[(int(face),axis,float(t[0,axis]))]+=abs(normal[axis])/2
  for face,t in zip(before['face'],old):
   normal=np.cross(t[1]-t[0],t[2]-t[0]);axis=int(np.argmax(abs(normal)))
   if normal[axis]==0:old_zero[(int(face),tuple(t.ravel()))]+=1;continue
   key=(int(face),axis,float(t[0,axis]));expected[key]+=abs(normal[axis])/2
   ids=groups[key];axes=[a for a in range(3) if a!=axis];q=new[ids][:,:,axes]
   if len(q)==0:failures.append(['missing-plane',key]);continue
   a=q[:,1]-q[:,0];b=q[:,2]-q[:,0];det=a[:,0]*b[:,1]-a[:,1]*b[:,0]
   for wb,wc in [(0.173,0.529),(0.613,0.251),(0.293,0.137)]:
    point=(t[0]+(t[1]-t[0])*wb+(t[2]-t[0])*wc)[axes];v=point-q[:,0]
    s1=(a[:,0]*v[:,1]-a[:,1]*v[:,0])/det;s2=(v[:,0]*b[:,1]-v[:,1]*b[:,0])/det
    count=int(np.sum((s1>=0)&(s2>=0)&(s1+s2<=1)));samples+=1
    if count!=1:failures.append(['coverage',key,point.tolist(),count])
  lost_zero=sum(max(0,n-new_zero[k]) for k,n in old_zero.items())
  unexpected=set(actual)-set(expected)
  area_error=max([abs(actual.get(k,0)-v) for k,v in expected.items()]+[0])
  materials=(out/f'{label}-lod{lod}-materials.txt').read_text().splitlines()
  assert not any('ea923855' in m or '006ce894' in m for m in materials)
  r=dict(input=label,lod=lod,baselineTriangles=len(old),mappedTriangles=len(new),materialKeys=materials,
         samples=samples,failures=failures,unexpectedPlanes=list(unexpected),lostDegenerateTriangles=lost_zero,
         maximumSourceFaceAreaDifference=area_error,totalAreaDifference=sum(actual.values())-sum(expected.values()))
  results.append(r);print(label,lod,'faces',len(old),len(new),'failures',len(failures),'extra planes',len(unexpected),'lost zero',lost_zero,'max area',area_error)
(out/'geometry-result.json').write_text(json.dumps(results,indent=2))
assert all(not r['failures'] and not r['unexpectedPlanes'] and r['lostDegenerateTriangles']==0 for r in results)
