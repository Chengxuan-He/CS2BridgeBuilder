"""Bounded same-atlas PBR donor experiment; no production/native payload writes."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift01/far-finite/local-convex-probe'
SOURCE=OUT.parent

def srgb(v):return np.where(v<=.04045,v/12.92,((v+.055)/1.055)**2.4)
def encode(v):return np.where(v<=.0031308,12.92*v,1.055*np.maximum(v,0)**(1/2.4)-.055)
def reduced(v,m):
 q=2**m;w=512//q
 return v.reshape(w,q,w,q,4).mean((1,3))
def normal_decode(c):
 xy=np.stack((c[...,0]*c[...,3],c[...,1]),-1)*2-1
 z=np.sqrt(np.maximum(0,1-(xy*xy).sum(-1)))
 n=np.concatenate((xy,z[...,None]),-1)
 return n/np.maximum(np.linalg.norm(n,axis=-1,keepdims=True),1e-30)
def normal_encode(n):
 # Same native R*A / G interpretation used by RepeatUnit.compute.
 return np.concatenate((n[...,:2]*.5+.5,np.ones(n.shape[:-1]+(2,))),-1)
def main():
 OUT.mkdir(parents=True,exist_ok=True)
 # Observed central rail/sleeper footprint in moving-negative chart. End bands
 # are deliberately absent: no pure same-row donor has been established there.
 mask=np.zeros((512,512),bool);mask[250:420,59:66]=True
 yy,xx=np.nonzero(mask);dx=np.where(xx<=62,57,67)
 report=dict(scope='One original moving-negative chart, central track footprint only; terminal bands unresolved.',
  sourceSurface='e0101c8ca7e9dc14016d187d012704a4',pixelBounds=[59,250,66,420],
  donorColumns=[57,67],sameRow=True,changedMipZeroPixels=int(mask.sum()),
  usesNearTexture=False,createsRepeatUnit=False,sourceResolution=[512,512],
  completeBridgeQualified=False,productionAssetsWritten=False,channels=[])
 for name in ['_BaseColorMap','_NormalMap','_MaskMap']:
  raw=(SOURCE/(name+'.Texture')).read_bytes();at=18;levels=[]
  for m in range(10):
   w=max(1,512>>m);size=((w+3)//4)**2*16
   a=np.asarray(Image.frombytes('RGBA',(w,w),raw[at:at+size],'bcn',(7,'BC7')),dtype=np.float64)/255;at+=size
   if name=='_BaseColorMap':a[...,:3]=srgb(a[...,:3])
   levels.append(a)
  donor=levels[0][yy,dx]
  donor_field=np.zeros_like(levels[0]);donor_field[yy,xx]=donor
  ch=dict(name=name,sourceSHA256=hashlib.sha256(raw).hexdigest(),mips=[])
  if name=='_MaskMap':assert np.all(donor[:,:3]==0),'Donor is not clean'
  normal_field=np.zeros((512,512,4))
  if name=='_NormalMap':normal_field[yy,xx,:3]=normal_decode(donor)
  for m,source in enumerate(levels):
   coverage=reduced(np.repeat(mask[...,None],4,2).astype(float),m)[...,0]
   footprint=coverage>0;weight=coverage[...,None]
   replacement=reduced(donor_field,m)/np.maximum(weight,1e-30)
   candidate=source.copy()
   if name=='_NormalMap':
    n=reduced(normal_field,m)[...,:3]/np.maximum(weight,1e-30)
    n=n/np.maximum(np.linalg.norm(n,axis=-1,keepdims=True),1e-30)
    blend=(1-weight)*normal_decode(source)+weight*n
    blend=blend/np.maximum(np.linalg.norm(blend,axis=-1,keepdims=True),1e-30)
    candidate[footprint]=normal_encode(blend)[footprint]
    angular=np.max(abs(np.linalg.norm(normal_decode(candidate[footprint]),axis=-1)-1))
   else:
    candidate[footprint]=((1-weight)*source+weight*replacement)[footprint]
    angular=None
   assert np.array_equal(candidate[~footprint],source[~footprint])
   assert candidate.min()>=0 and candidate.max()<=1,'Convex bounds violated'
   np.save(OUT/(name+f'-mip{m}.npy'),candidate)
   if name=='_BaseColorMap' and m==0:
    rgba=candidate.copy();rgba[...,:3]=encode(rgba[...,:3]);Image.fromarray(np.rint(rgba*255).astype('uint8'),'RGBA').save(OUT/'mip0-color.png')
   ch['mips'].append(dict(mip=m,affectedPixels=int(footprint.sum()),coverageMin=float(coverage[footprint].min()),coverageMax=float(coverage.max()),outsideChanged=0,minimum=float(candidate.min()),maximum=float(candidate.max()),normalUnitError=None if angular is None else float(angular),correctedMaskRedRange=[float(candidate[...,0][footprint].min()),float(candidate[...,0][footprint].max())] if name=='_MaskMap' else None))
  report['channels'].append(ch)
 report['interpretation']='New local road composite, not recovery of unknown hidden source content. Exact mip-zero mask is box-filtered to coverage. Coverage blends authored mip with conditional mean of clean donor samples; normals decode R*A/G, average vectors, normalize and reencode. Zero coverage retains original channel values. Deep mip retains some original contamination where footprint coverage is fractional; rendered quality and endpoint donors remain unresolved. No native compression performed.'
 (OUT/'report.json').write_text(json.dumps(report,indent=2))
 print(OUT/'report.json')
if __name__=='__main__':main()
