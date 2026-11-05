"""Actual triangle-plane intersection check for complete Y-frame support contacts."""
import json,sys,struct
from pathlib import Path
import numpy as np
r,out=map(Path,sys.argv[1:]); report=[]
def sections(v,tri,z):
 a=v[tri].astype(float); seg=[]
 for q in a[(a[:,:,2].min(1)<z)&(a[:,:,2].max(1)>z)]:
  pts=[]
  for u,w in zip(q,np.roll(q,-1,axis=0)):
   if min(u[2],w[2])<z<max(u[2],w[2]):pts.append((u+(w-u)*(z-u[2])/(w[2]-u[2]))[:2])
  if len(pts)==2:seg.append(pts)
 return np.array(seg)
for lod in (0,1):
 d=np.load(r/f'lift01-main-{lod}.npz');v,f=d['positions'],d['indices'];cross=json.loads((r/f'lift01-crossing-{lod}.json').read_text());side=json.loads((r/f'lift01-side-{lod}.json').read_text())
 raw=Path(f'artifacts/material-remap/Lift01/coverage54/lod{lod}-raw.bin').read_bytes();nv,ni,st,na=struct.unpack_from('<4i',raw);offset=16+ni*st
 for _ in range(na):
  a,fmt,dim,n=struct.unpack_from('<4i',raw,offset);offset+=16
  if a==13:bones=np.frombuffer(raw,dtype='<u4',count=n//4,offset=offset)
  offset+=n
 ys=[(i,p) for i,p in enumerate(cross) if p['maximum'][0]==3.62957763671875]
 beams=[(i,p) for i,p in enumerate(side) if min(abs(p['minimum'][0]),abs(p['maximum'][0]))>3.3 and max(abs(p['minimum'][0]),abs(p['maximum'][0]))<4 and p['minimum'][1]<10 and p['maximum'][1]>6 and p['maximum'][2]-p['minimum'][2]>10]
 triangles={i:f[np.isin(f,p['vertices']).all(1)] for i,p in beams}
 for ordinal,p in ys:
  own=f[np.isin(f,p['vertices']).all(1)]
  for phase in (.25,.5,.75):
   z=p['minimum'][2]+(p['maximum'][2]-p['minimum'][2])*phase
   sl=sections(v,own,z);vertical=[q for q in sl if q[0][0]==q[1][0] and abs(q[0][0])>3.5]
   support=[(i,sections(v,triangles[i],z)) for i,b in beams if b['minimum'][2]<z<b['maximum'][2]]
   for orientation in (-1,1):
    for delta in (-1.25,0,2,8,10.5):
     hits=[];k=1+delta/3.56268310546875
     for q in vertical:
      if np.sign(q[0][0])!=orientation:continue
      x=q[0][0]*k-orientation*delta;ylo,yhi=sorted([q[0][1],q[1][1]])
      for i,segments in support:
       for a,b in segments:
        if min(a[0],b[0])<=x<=max(a[0],b[0]) and a[0]!=b[0]:
         y=a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0])
         if ylo<=y<=yhi:
          assert set(bones[p['vertices']])==set(bones[side[i]['vertices']]), 'Contact crosses independently moving owners'
          hits.append(dict(beam=i,y=float(y),bone=int(bones[p['vertices'][0]])))
     report.append(dict(lod=lod,part=ordinal,z=z,phase=phase,delta=delta,side=orientation,verticalSegments=len(vertical),intersections=hits))
out.write_text(json.dumps(report,indent=2));print('cases',len(report),'without physical intersection',sum(not x['intersections'] for x in report))
