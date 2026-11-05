"""Sample every actual transverse profile cell on both sides of motion seams."""
from pathlib import Path
import json,struct
import numpy as np
root=Path(__file__).resolve().parents[2];out=root/'artifacts/material-remap/Lift02/underlay70'
boards=json.loads((out/'boards.json').read_text());results=[]
for label,delta in [('road',8.5),('track',0),('narrow',-3.75),('leftwalk',6.75),('rightwalk',6.75)]:
 for lod in range(3):
  raw=(out/f'{label}-lod{lod}-output.bin').read_bytes();nv,ni=struct.unpack_from('<ii',raw)
  p=np.frombuffer(raw,'<f4',count=nv*3,offset=8).reshape(-1,3);f=np.frombuffer(raw,'<i4',count=ni,offset=8+nv*12).reshape(-1,3)
  faces=np.fromfile(out/f'{label}-lod{lod}-faces.bin','<i4');q=p[f].astype(float)
  road=np.frombuffer((out/f'{label}-lod{lod}-road.bin').read_bytes(),'<f4',offset=4).reshape(-1,3,3).astype(float)
  normal=np.cross(road[:,1]-road[:,0],road[:,2]-road[:,0]);road=road[normal[:,1]>0]
  failures=[];samples=0;caps=0;unrepresentable=0
  for board in [b for b in boards if b['lod']==min(lod,1)]:
   ends=[]
   if board['start'] in [-106,-82,82]:ends.append((3,board['start']))
   if board['end'] in [-82,82,106]:ends.append((4,board['end']))
   for kind,z in ends:
    caps+=1;segments=[]
    for tri in road:
     points=[]
     for i,a in enumerate(tri):
      b=tri[(i+1)%3]
      if a[2]==z:points.append(a[:2])
      if (a[2]<z<b[2]) or (b[2]<z<a[2]):points.append((a+(b-a)*((z-a[2])/(b[2]-a[2])))[:2])
     if len(points)>1:
      points.sort(key=lambda p:p[0]);a,b=points[0],points[-1]
      if a[0]!=b[0]:segments.append((a,b))
    left=min(a[0] for a,b in segments);right=max(b[0] for a,b in segments)
    half=4.217529296875 if board['bones']==['bone_1'] else 4.9730224609375
    il,ir=float(np.float32(-half-delta)),float(np.float32(half+delta))
    xs=sorted(set([v[0] for s in segments for v in s]+[x for x in [il,ir] if left<x<right]))
    ts=q[np.isin(faces,board['sourceFaces'][kind])][:,:,[0,1]]
    a=ts[:,1]-ts[:,0];b=ts[:,2]-ts[:,0];d=a[:,0]*b[:,1]-a[:,1]*b[:,0]
    valid=d!=0;a,b,d,ts=a[valid],b[valid],d[valid],ts[valid]
    for xa,xb in zip(xs,xs[1:]):
     if float(np.nextafter(np.float32(xa),np.float32(np.inf)))>=xb:unrepresentable+=1;continue
     for fraction in [.283,.719]:
      x=xa+(xb-xa)*fraction
      top=max(sa[1]+(sb[1]-sa[1])*((x-sa[0])/(sb[0]-sa[0])) for sa,sb in segments if sa[0]<=x<=sb[0])
      bottom=9.2666015625 if lod==2 and il<=x<=ir else 9.27197265625
      for yf in [.237,.683]:
       point=np.array([x,bottom+(top-bottom)*yf]);v=point-ts[:,0]
       s1=(a[:,0]*v[:,1]-a[:,1]*v[:,0])/d;s2=(v[:,0]*b[:,1]-v[:,1]*b[:,0])/d
       count=int(np.sum((s1>=0)&(s2>=0)&(s1+s2<=1)));samples+=1
       if count!=1:failures.append([board['component'],kind,x,float(point[1]),count])
  result=dict(input=label,lod=lod,caps=caps,samples=samples,failures=failures,unrepresentableProfileCells=unrepresentable)
  results.append(result);print(label,lod,'caps',caps,'samples',samples,'failures',len(failures),'subfloatcells',unrepresentable)
(out/'cap-result.json').write_text(json.dumps(results,indent=2));assert all(not r['failures'] for r in results)
