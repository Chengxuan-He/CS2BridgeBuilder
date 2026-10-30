"""Read-only original far-atlas donor audit; never synthesizes a texture."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift01/far-finite'

def main():
 charts=json.loads((OUT/'floor-content-inspection.json').read_text())['charts']
 sources={};hashes={}
 for channel in ['_BaseColorMap','_NormalMap','_MaskMap']:
  raw=(OUT/(channel+'.Texture')).read_bytes();hashes[channel]=hashlib.sha256(raw).hexdigest();at=18;levels=[]
  for mip in range(10):
   width=max(1,512>>mip);size=((width+3)//4)**2*16
   levels.append(np.array(Image.frombytes('RGBA',(width,width),raw[at:at+size],'bcn',(7,'BC7'))));at+=size
  assert at==len(raw);sources[channel]=levels
 rows=[]
 for chart in charts:
  x0,y0,x1,y1=chart['inspectionPixelBounds'];horizontal=chart['name']=='fixed-positive'
  # Inspection coordinates are recorded candidates, not a periodicity claim.
  donor=[303,313] if horizontal else {'moving-negative':[57,67],'moving-positive':[361,371],'fixed-negative':[187,197]}[chart['name']]
  audit=[]
  for mip in range(10):
   scale=2**mip;w=max(1,512>>mip)
   start,end=(x0,x1) if horizontal else (y0,y1)
   lo=max(0,int(np.ceil(start/scale)));hi=min(w,int(np.floor(end/scale)))
   channels={}
   for ch,levels in sources.items():
    data=[]
    for d in donor:
     coord=min(w-1,d//scale);v=levels[mip][coord,lo:hi] if horizontal else levels[mip][lo:hi,coord]
     data.append(dict(nativeCoordinate=coord,samples=len(v),minimum=v.min(0).tolist() if len(v) else None,maximum=v.max(0).tolist() if len(v) else None,mean=v.mean(0).tolist() if len(v) else None))
    channels[ch]=data
   audit.append(dict(mip=mip,donorColumnsCoincide=donor[0]//scale==donor[1]//scale,channels=channels))
  rows.append(dict(chart=chart['name'],sourceFaces=chart['sourceFaces'],axis='Y' if horizontal else 'X',mipZeroDonorCoordinates=donor,levels=audit))
 report=dict(sourceSurface='e0101c8ca7e9dc14016d187d012704a4',sourceHashes=hashes,scope='Original atlas read-only candidate donor statistics, all original mips and PBR channels. Not an approved hidden-texture reconstruction.',rootCause='Rails/sleepers are present in the original far atlas before any generated overlay. Pure Ballast source-network appearance is distinct from its selected rail geometry.',rows=rows,repairProduced=False,productionQualified=False)
 (OUT/'far-donor-audit.json').write_text(json.dumps(report,indent=2))
 print(OUT/'far-donor-audit.json')
if __name__=='__main__':main()
