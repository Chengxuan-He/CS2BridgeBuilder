"""Offline source coverage, single-course anchors and native structural shape checks."""
from pathlib import Path
import argparse,hashlib,json,struct,tempfile,zipfile
import numpy as np
from UvDensityAudit import NativeGeometry
from Nanpu import ENTRIES,measure

def data(path):
 b=path.read_bytes();assert hashlib.sha256(b[:-32]).digest()==b[-32:];n=struct.unpack_from('<6i',b)
 return np.frombuffer(b,dtype='<i4',offset=24,count=n[0]),np.frombuffer(b,dtype='<f4',offset=24+4*n[0],count=n[4])
def rotation(z,w):
 z=np.float32(z);w=np.float32(w);c=np.float32(1)-np.float32(2)*z*z;s=np.float32(2)*z*w
 return np.array([[c,-s,0],[s,c,0],[0,0,1]],dtype='f4')
def inside(points,triangles):
 # Ray along +Z, independent of object bounding boxes; use actual source tower triangles.
 a=triangles[:,0];e1=triangles[:,1]-a;e2=triangles[:,2]-a;direction=np.array([0.,0.,1.]);h=np.cross(direction,e2);det=np.einsum('ij,ij->i',e1,h);valid=abs(det)>1e-12;inv=np.divide(1.,det,out=np.zeros_like(det),where=valid)
 result=[]
 for point in points:
  s=point-a;u=np.einsum('ij,ij->i',s,h)*inv;q=np.cross(s,e1);v=q[:,2]*inv;t=np.einsum('ij,ij->i',e2,q)*inv
  hits=valid&(u>=-1e-9)&(v>=-1e-9)&(u+v<=1+1e-9)&(t>1e-8)
  result.append(len(np.unique(np.round(t[hits],7)))%2==1)
 return np.array(result)
def main():
 p=argparse.ArgumentParser();p.add_argument('archive');p.add_argument('game');p.add_argument('recipes');p.add_argument('report');a=p.parse_args();root=Path(a.recipes);native=NativeGeometry(Path(a.game)/'Plugins/x86_64/PipelinePlugin.dll');params=data(root/'Types_Nanpu_Parameters.bin')[1];poses=data(root/'Types_Nanpu_TowerPoses.bin')[1].reshape(-1,3);records=[]
 with zipfile.ZipFile(a.archive) as z,tempfile.TemporaryDirectory() as tmp:
  for lod,e in enumerate(ENTRIES):
   path=Path(tmp)/e;path.write_bytes(z.read(e));m=native.read(path)[0];v=m['positions'].astype('f8');faces=m['faces'];original,coeff,tower=measure(v.copy(),faces);coverage=np.zeros(len(faces));maxerr=0;parts=[]
   for part in range(9):
    def read(k):return data(root/f'Types_Nanpu_{lod}_{part}_{k}.bin')
    ids=read('SourceTriangles')[0].reshape(-1,3);weights=read('Weights')[1].reshape(-1,3).astype('f8');tri=read('Triangles')[0].reshape(-1,3);sf=read('SourceFaces')[0];origin=read('Origins')[1].reshape(-1,3);c=read('Displacement')[1]
    source=np.einsum('ni,nij->nj',weights,original[ids]);sourcec=np.einsum('ni,ni->n',weights,coeff[ids]);assert np.max(abs(weights.sum(1)-1))<1e-7
    for i,face in enumerate(tri):
     assert np.all(ids[face]==faces[sf[i]]);coverage[sf[i]]+=np.linalg.det(weights[face])
    R=rotation(*poses[part-1,:2]) if part else np.eye(3,dtype='f4');side=poses[part-1,2] if part else 0
    for half in [-6,0,8,20]:
     local=origin.copy();local[:,0]+=c*half;actual=local@R.T+np.array([side*half,0,0]);expected=source.copy();expected[:,0]+=sourcec*half
     if not part:expected[:,2]-=np.sign(expected[:,2])*params[5]
     err=float(np.max(abs(actual-expected)));maxerr=max(maxerr,err);assert err<3e-5,(lod,part,half,err)
    parts.append(dict(part=part,triangles=len(tri),vertices=len(origin)))
    if part==0:
     anchors=np.unique(origin[abs(source[:,2])<7],axis=0).astype('f8');contact=[]
     for scale in [.6,1,1.4]:
      for slope in [-.2,0,.2]:
       q=anchors.copy();q[:,2]*=scale;q[:,1]+=q[:,2]*slope
       hit=inside(q,original[faces[tower]])
       assert np.all(hit),(lod,scale,slope,np.count_nonzero(~hit))
       contact.append(dict(lengthFactor=scale,slope=slope,anchorVertices=len(q),allInsideActualTower=True))
   assert np.max(abs(coverage-1))<3e-7
   collision=[]
   for half in [-6,0,8,20]:
    def hit(point):
     hits=[]
     for part in range(1,9):
      origin=data(root/f'Types_Nanpu_{lod}_{part}_Origins.bin')[1].reshape(-1,3).copy();c=data(root/f'Types_Nanpu_{lod}_{part}_Displacement.bin')[1];origin[:,0]+=c*half
      R=rotation(*poses[part-1,:2]);q=(np.array(point)-np.array([poses[part-1,2]*half,0,0]))@R;lo=origin.min(0);hi=origin.max(0)
      if np.all(q>=lo-1e-5) and np.all(q<=hi+1e-5):hits.append(part)
     return hits
    cases=[([0,0,0],False),([0,20,0],False),([0,-30,0],False),([0,-5,0],True),([0,60,0],True),([12+half,-47,0],True),([-12-half,-47,0],True)]
    for point,expected in cases:assert bool(hit(point))==expected,(lod,half,point,hit(point))
    for x in np.linspace(-float(params[0])/2-half,float(params[0])/2+half,19):
     for y in [0,2]:assert not hit([x,y,0]),(lod,half,x,y)
    collision.append(dict(halfWidthDelta=half,actualMemberFixtures=4,emptyOpeningFixtures=3,roadCrossSectionSamples=38,passed=True))
   records.append(dict(lod=lod,parts=parts,sourceCoverageError=float(np.max(abs(coverage-1))),maximumSourcePoseError=maxerr,anchorContact=contact,nativeOrientedBoxFixtures=collision))
 report=dict(referenceLength=float(params[2]),anchorTranslation=float(params[5]),results=records,nativeGameAcceptance='Not run',slopeScope='Straight graded course: vertical tower rotation follows native GetNodeRotation; upper cap small-Z slope offsets sampled independently. Native composed curve execution remains a separate integration check.')
 Path(a.report).write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if __name__=='__main__':main()
