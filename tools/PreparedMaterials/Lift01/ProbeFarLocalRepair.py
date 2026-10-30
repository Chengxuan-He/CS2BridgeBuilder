"""Bounded same-atlas PBR donor experiment; no production/native payload writes."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift01/far-finite/local-repair-probe'
SOURCE=OUT.parent

def srgb(v):return np.where(v<=.04045,v/12.92,((v+.055)/1.055)**2.4)
def encode(v):return np.where(v<=.0031308,12.92*v,1.055*np.maximum(v,0)**(1/2.4)-.055)
def reduced(v,m):
 q=2**m;w=512//q
 return v.reshape(w,q,w,q,4).mean((1,3))
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
  donor=levels[0][yy,dx];original=levels[0][yy,xx]
  delta=np.zeros_like(levels[0]);delta[yy,xx]=donor-original
  ch=dict(name=name,sourceSHA256=hashlib.sha256(raw).hexdigest(),mips=[])
  if name=='_MaskMap':
   assert np.all(donor[:,:3]==0),'A proposed clean donor carries metallic/material control content'
  for m,source in enumerate(levels):
   correction=reduced(delta,m);footprint=reduced(np.repeat(mask[...,None],4,2).astype(float),m)[...,0]>0
   candidate=source+correction
   # Compare actual authored mip to a box reduction. A nonzero residual means
   # difference transfer is an experiment, not reconstruction of authored filtering.
   reconstructed=reduced(levels[0],m);residual=source-reconstructed
   outside=np.max(np.abs((candidate-source)[~footprint])) if (~footprint).any() else 0
   assert outside==0
   np.save(OUT/(name+f'-mip{m}.npy'),candidate)
   ch['mips'].append(dict(mip=m,affectedPixels=int(footprint.sum()),
     maxCorrection=float(np.max(abs(correction))),outsideChanged=0,
     originalFilterModelResidual=float(np.max(abs(residual[footprint]))),
     minimum=float(candidate.min()),maximum=float(candidate.max()),
     wouldRequireClamp=bool((candidate<0).any() or (candidate>1).any()),
     correctedMaskRedRange=[float(candidate[...,0][footprint].min()),float(candidate[...,0][footprint].max())] if name=='_MaskMap' else None))
  report['channels'].append(ch)
 report['interpretation']='Mip-zero footprint copies measured same-row clean PBR samples jointly. Coarse delta assumes box support; report residual and out-of-range without clamping. Original authored filtering must be established before native encoding or promotion.'
 (OUT/'report.json').write_text(json.dumps(report,indent=2))
 print(OUT/'report.json')
if __name__=='__main__':main()
