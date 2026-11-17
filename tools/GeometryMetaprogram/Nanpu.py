"""Measure Nanpu's one pylon and cable fans; emit immutable all-LOD affine recipes."""
from pathlib import Path
import argparse,hashlib,json,struct,tempfile,zipfile
import numpy as np
from UvDensityAudit import NativeGeometry
ENTRIES=['nanpudaqiao_71350b6a65bedb74a6135045732ffcb5.Geometry','nanpudaqiaoLOD1_8e217469ffa4c2b5a5884b501005957e.Geometry','nanpudaqiaoLOD2_07c3376af0d71d8692665a2d881c7a92.Geometry']
def write_bbn(path,floats=(),ints=()):
 f=np.asarray(floats,dtype='<f4').ravel();i=np.asarray(ints,dtype='<i4').ravel();b=struct.pack('<6i',len(i),0,0,0,len(f),0)+i.tobytes()+f.tobytes();path.write_bytes(b+hashlib.sha256(b).digest())
def measure(v,faces):
 # Source graph has a rigid central H pylon, two footings and raised lettering.
 # All other islands are the two longitudinal cable fans and their anchor shoes.
 q=v[faces];tower=np.all(np.abs(q[:,:,2])<7,axis=1)
 assert not np.any(tower & (np.ptp(q[:,:,2],axis=1)>14))
 origin=v.copy();origin[:,1]-=np.float32(48)
 coeff=np.sign(v[:,0]).astype('<f4')
 # The two crossbeams connect the columns' measured inner interfaces. Their
 # side closure/column vertices translate whole; centre sections span between
 # these interfaces. Data emitted below freezes this offline identification.
 for y,left,right in [(39.48828125,-14.6474609375,14.607421875),(46,-15.605712890625,15.61279296875)]:
  mask=(v[:,1]==np.float32(y))&(v[:,0]>=left)&(v[:,0]<=right)&(np.abs(v[:,2])<7)
  coeff[mask]=v[mask,0]/np.where(v[mask,0]<0,-left,right)
 mask=(v[:,1]>=np.float32(104.36328125))&(v[:,1]<=np.float32(112.001953125))&(v[:,0]>=np.float32(-9.78173828125))&(v[:,0]<=np.float32(9.7509765625))&(np.abs(v[:,2])<np.float32(3.364))
 # Raised name lettering is an independent authored side member, not the beam.
 mask &= (v[:,2]!=np.float32(-3.32794189453125))&(v[:,2]!=np.float32(3.3936767578125))
 coeff[mask]=v[mask,0]/np.where(v[mask,0]<0,np.float32(9.78173828125),np.float32(9.7509765625))
 return origin,coeff,tower

# Source cap centres; user-authorized translation puts both fans on the central anchor plane.
ANCHOR=3.316650390625
ROLES=[(-48.,-8.51171875,-1),(-48.,-8.51171875,1),(-8.51171875,-2.,0),(-2.,55.966796875,-1),(-2.,55.966796875,1),(55.966796875,64.40234375,0),(64.40234375,107.,-1),(64.40234375,107.,1)]
def clip(poly,q,axis,plane,lower):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  aa=a@q[:,axis];bb=b@q[:,axis];ia=aa<=plane if lower else aa>=plane;ib=bb<=plane if lower else bb>=plane
  if ia:result.append(a)
  if ia!=ib:result.append(a+(b-a)*((plane-aa)/(bb-aa)))
 return result

def recipe(v,f,origin,coeff,membership):
 ids=[];weights=[];points=[];displacement=[];gradients=[];triangles=[];source_faces=[]
 for fi,face in enumerate(f):
  if not membership[fi]:continue
  gradient=np.linalg.lstsq(origin[face[1:]].astype('f8')-origin[face[0]],coeff[face[1:]].astype('f8')-coeff[face[0]],rcond=None)[0]
  first=len(points)
  for k in range(3):
   ids.append(face);weights.append(np.eye(3)[k]);points.append(origin[face[k]]);displacement.append(coeff[face[k]]);gradients.append(gradient)
  triangles.append([first,first+1,first+2]);source_faces.append(fi)
 return tuple(np.asarray(a) for a in [ids,weights,points,displacement,gradients,triangles,source_faces])

def tower_parts(arrays,frames):
 src,weights,p,co,grad,tri,sf=arrays;result=[];coverage=np.zeros(len(tri));record=[]
 for role,(lo,hi,side) in enumerate(ROLES):
  pp=[];cc=[];gg=[];ss=[];ww=[];tt=[];ff=[]
  for fi,face in enumerate(tri):
   q=p[face].astype('f8')
   if side and ((lo!=-48 and np.all(q[:,1]==lo)) or (hi!=107 and np.all(q[:,1]==hi))):continue
   poly=list(np.eye(3));poly=clip(poly,q,1,lo,False);poly=clip(poly,q,1,hi,True)
   if side:poly=clip(poly,q,0,0,side<0)
   if len(poly)<3:continue
   for a in range(1,len(poly)-1):
    bary=np.array([poly[0],poly[a],poly[a+1]]);fraction=np.linalg.det(bary)
    if fraction<1e-12:continue
    coverage[fi]+=fraction;first=len(pp)
    for b in bary:
     pp.append(b@q);cc.append(b@co[face]);gg.append(grad[face[0]]);ss.append(src[face[0]]);ww.append(b@weights[face])
    tt.append([first,first+1,first+2]);ff.append(sf[fi])
  pp=np.array(pp);cc=np.array(cc);gg=np.array(gg)
  if len(frames)<=role:
   R=np.eye(3)
   if side:
    axis=np.linalg.eigh(np.cov(np.unique(pp[:,:2],axis=0).T))[1][:,-1];axis*=np.sign(axis[1]);R[:2,:2]=[[axis[1],axis[0]],[-axis[0],axis[1]]]
   frames.append(R)
  R=frames[role];local=pp@R
  if side:
   assert np.max(abs(cc-side))<1e-7
   cc=np.zeros(len(cc));gg=np.zeros_like(gg)
  result.append(tuple(np.asarray(a) for a in [ss,ww,local,cc,gg,tt,ff]))
 assert np.max(abs(coverage-1))<1e-8
 return result

def emit(out,lod,part,arrays):
 ids,weights,positions,displacement,gradients,triangles,source_faces=arrays;pre=f'Types_Nanpu_{lod}_{part}_'
 for name,value in [('Origins',positions),('Displacement',displacement),('Frames',gradients[:,0]),('ShearY',gradients[:,1]),('ShearZ',gradients[:,2]),('Weights',weights)]:write_bbn(out/(pre+name+'.bin'),value)
 for name,value in [('SourceTriangles',ids),('Triangles',triangles),('SourceFaces',source_faces)]:write_bbn(out/(pre+name+'.bin'),ints=value)
 return dict(part=part,vertices=len(positions),triangles=len(triangles),bounds=[positions.min(0).tolist(),positions.max(0).tolist()])

def main():
 a=argparse.ArgumentParser();a.add_argument('archive');a.add_argument('game');a.add_argument('output');a=a.parse_args();out=Path(a.output);out.mkdir(parents=True,exist_ok=True);frames=[];records=[]
 reader=NativeGeometry(Path(a.game)/'Plugins/x86_64/PipelinePlugin.dll')
 with zipfile.ZipFile(a.archive) as z,tempfile.TemporaryDirectory() as tmp:
  for lod,entry in enumerate(ENTRIES):
   raw=z.read(entry);p=Path(tmp)/entry;p.write_bytes(raw);m=reader.read(p)[0];v=m['positions'];f=m['faces'];origin,coeff,tower=measure(v,f)
   cable=recipe(v,f,origin,coeff,~tower);cable[2][:,2]-=np.sign(cable[2][:,2])*ANCHOR
   parts=[cable]+tower_parts(recipe(v,f,origin,coeff,tower),frames)
   records.append(dict(lod=lod,geometry=entry,sha256=hashlib.sha256(raw).hexdigest(),parts=[emit(out,lod,part,arrays) for part,arrays in enumerate(parts)]))
 poses=[]
 for R,(_,_,side) in zip(frames,ROLES):
  angle=np.arctan2(R[1,0],R[0,0]);poses.extend([np.sin(angle/2),np.cos(angle/2),side])
 write_bbn(out/'Types_Nanpu_TowerPoses.bin',poses)
 write_bbn(out/'Types_Nanpu_Parameters.bin',[23.35009765625,40.6552734375,407.4140625-2*ANCHOR,48,100.44140625,ANCHOR])
 (out/'Nanpu-measurements.json').write_text(json.dumps(records,indent=2));print('Measured all three LODs: one fan piece and eight native tower objects.')
if __name__=='__main__':main()
