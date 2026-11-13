"""Reproduce CoveredPedestrian measured construction maps from the native archive.
Usage: python CoveredPedestrian.py <Cities2_Data> <output-directory>
The output directory receives only numerical resources and an evidence JSON.
"""
from pathlib import Path
import sys,zipfile,tempfile,hashlib,struct,json
import numpy as np
from UvDensityAudit import NativeGeometry
G=Path(sys.argv[1]);O=Path(sys.argv[2]);O.mkdir(parents=True,exist_ok=True)
entry='PedestrianBridgeCovered_821820996088a0941bca40147a7b74a1.Geometry'
with zipfile.ZipFile(G/'Content/Game/Blob.cok') as archive: payload=archive.read(entry)
sha=hashlib.sha256(payload).hexdigest()
assert sha=='675f8f57d39bbe1b161c81004bdb1f28465031c96e4b0e3ca0b388c365cde69f'
with tempfile.TemporaryDirectory() as tmp:
 path=Path(tmp)/entry;path.write_bytes(payload)
 meshes=NativeGeometry(G/'Plugins/x86_64/PipelinePlugin.dll').read(path)
assert len(meshes)==1
p=meshes[0]['positions'];f=meshes[0]['faces'];assert p.shape==(4008,3) and f.shape==(3560,3)
_,inverse=np.unique(np.round(p,4),axis=0,return_inverse=True)
parents=list(range(inverse.max()+1))
def root(i):
 while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
 return i
for a,b,c in inverse[f]:parents[root(b)]=root(a);parents[root(c)]=root(a)
labels=np.array([root(i) for i in inverse]);c=np.zeros(len(p),dtype='<f4');der=np.zeros_like(c);parts=[]
for label in np.unique(labels):
 ids=np.flatnonzero(labels==label);v=p[ids];x=v[:,0];lo,hi=x.min(),x.max();count=len(ids)
 if count in (1072,92):
  # Central flat crown joins independently curved eaves at this measured station.
  span=np.float32(2.48333740234375);inside=abs(x)<span
  c[ids]=np.where(inside,x/span,np.sign(x));der[ids]=np.where(inside,1/span,0)
 elif count==172:
  # Nine inclined side-post frames. All cap vertices are authored endpoints,
  # so recorded endpoint displacement equals cap stretching, without widening posts.
  c[ids]=np.sign(x)
 elif lo<=0<=hi:
  span=max(abs(lo),abs(hi));c[ids]=x/span;der[ids]=1/span
 else:c[ids]=np.sign(x)
 parts.append(dict(vertices=count,min=v.min(0).tolist(),max=v.max(0).tolist()))
assert len(parts)==36 and sum(a['vertices']==172 for a in parts)==9
coincident={}
for i,v in enumerate(p):coincident.setdefault(tuple(v),[]).append(i)
assert all(np.ptp(c[ids])==0 for ids in coincident.values())
for name,v in [('Membership',c),('Frames',der)]:
 blob=struct.pack('<8i',0x314e4242,1,0,0,0,0,len(v),0)+v.tobytes()
 (O/f'Types_CoveredPedestrian_{name}.bin').write_bytes(blob+hashlib.sha256(blob).digest())
checks=[]
for extra in (0,2,10,-2):
 q=p.copy();q[:,0]+=c*np.float32(extra/2)
 assert np.array_equal(q[:,1:].view('u4'),p[:,1:].view('u4'))
 if extra==0:assert np.array_equal(q.view('u4'),p.view('u4'))
 for part in np.unique(labels):
  ids=np.flatnonzero(labels==part)
  if len(ids)==172:
   for side in (-1,1):
    ii=ids[np.sign(p[ids,0])==side]
    assert np.max(abs((q[ii,0]-p[ii,0])-np.float32(side*extra/2)))<1e-6
 checks.append(dict(extra=extra,min=q.min(0).tolist(),max=q.max(0).tolist()))
(O/'CoveredPedestrian-evidence.json').write_text(json.dumps(dict(source=entry,sha256=sha,parts=parts,checks=checks,coincidentSeamMismatches=0),indent=2)+'\n')
print('Verified source identity, 4008 vertices, 3560 faces, 36 parts, all coincident seams, four widths.')
