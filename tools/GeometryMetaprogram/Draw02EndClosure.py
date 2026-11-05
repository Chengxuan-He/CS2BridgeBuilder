"""Measured upper-beam cross-section and original leaf end closures; no invented faces."""
from pathlib import Path
import json,struct,hashlib
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/endcap60';O.mkdir(exist_ok=True)
LOW=7.935546875;HIGH=9.699951171875;ROAD=9.800048828125;INNER=5.74609375
records=[]
for lod in range(3):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);q=v[f];normal=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0])
 sides=(-1,1) if lod<2 else (0,)
 for side in sides:
  if lod<2:
   inner=np.flatnonzero((q[:,:,0]==np.float32(side*INNER)).all(1)&(q[:,:,1].max(1)==np.float32(HIGH))&(np.abs(q[:,:,2]).max(1)<79))
   cap=np.flatnonzero((q[:,:,0]*side).min(1)>=0)
   # Both authored leaf ends. Near has a three-face folded lip (front/back/
   # downward bottom); mid collapses its bottom to the front/back intersection.
   # These are structural folds, not the upward road receiver being replaced.
   cap=[int(i) for i in cap if np.abs(q[i,:,0]).max()<=6.1461
        and (np.abs(q[i,:,2]).max()<.25 or (np.abs(q[i,:,2]).min()>=78.49 and np.abs(q[i,:,2]).max()<78.71))
        and q[i,:,1].max()<=ROAD
        and ((np.abs(q[i,:,0]).max()<=INNER and q[i,:,1].min()>=9.330810546875)
             or (np.abs(q[i,:,0]).min()>=INNER and q[i,:,1].min()>=LOW))]
   assert len(inner)==80 and len(cap)==(56 if lod==0 else 40),(lod,side,len(inner),len(cap))
   assert all(normal[i,1]<=.0001 for i in cap)
   faces={int(i):'inner' for i in inner}
   for i in cap:faces[i]='cap' if np.abs(q[i,:,0]).min()>=INNER else 'lip'
   panels=np.flatnonzero((v[:,0]==np.float32(side*5.800048828125))&(v[:,1]==np.float32(HIGH))&(np.abs(v[:,2])<79)).tolist()
   reference=int(f[inner[0],0])
  else:
   end=(np.abs(q[:,:,2]).max(1)<.04)|((np.abs(q[:,:,2]).min(1)==np.float32(77.970703125))&(np.abs(q[:,:,2]).max(1)==np.float32(79.6953125)))
   cap=np.flatnonzero(end&(q[:,:,1].max(1)==np.float32(10.5283203125))&(np.abs(q[:,:,0]).max(1)<=np.float32(9.64990234375)))
   assert len(cap)==16
   faces={int(i):'far' for i in cap};panels=[];reference=0
  edges=[];source={};edgeLookup={};fragments=[];whole=[]
  def coefficients(point,mode,domain):
   if mode=='far':return (0.,0.,0.)
   y=point[1];w=0 if domain=='bottom' else 1 if domain=='top' else (y-LOW)/(HIGH-LOW)
   w=float(np.clip(w,0,1));x=side*point[0]/INNER if mode=='lip' else 1.
   return (w*x,side*w/INNER if mode=='lip' else 0.,x/(HIGH-LOW) if domain=='middle' else 0.)
  def node(bary,tri,mode,domain):
   point=bary@v[tri].astype(float);coeff=coefficients(point,mode,domain)
   nz=np.flatnonzero(np.abs(bary)>1e-12)
   if len(nz)==1:
    vertex=int(tri[nz[0]])
    if vertex not in source:source[vertex]=coeff;return vertex
    if source[vertex]==coeff:return vertex
    a=b=vertex;t=.5
   else:
    assert len(nz)==2
    a,b=map(int,tri[nz]);t=float(bary[nz[1]])
    if a>b:a,b,t=b,a,1-t
    t=float(np.float32(t))
   key=(a,b,t,coeff)
   if key not in edgeLookup:
    edgeLookup[key]=len(edges);edges.append(dict(a=a,b=b,fraction=t,coefficients=coeff))
   return -edgeLookup[key]-1
  def split(poly,points,height,above):
   out=[]
   for a,b in zip(poly,poly[1:]+poly[:1]):
    da=float(a@points[:,1])-height;db=float(b@points[:,1])-height
    if (da>=0 if above else da<=0):out.append(a)
    if da*db<0:out.append(a+(b-a)*(-da)/(db-da))
   return out
  for face,mode in faces.items():
   tri=f[face];points=v[tri].astype(float);poly=[np.eye(3)[i] for i in range(3)];polys=[]
   if mode=='far':polys=[('bottom',split(poly,points,ROAD,False))]
   else:
    bottom=split(poly,points,LOW,False);middle=split(split(poly,points,LOW,True),points,HIGH,False);top=split(poly,points,HIGH,True)
    polys=[('bottom',bottom),('middle',middle),('top',top)]
   output=[]
   for domain,p in polys:
    if len(p)<3:continue
    a=np.array(p)@points
    if sum(np.linalg.norm(np.cross(a[i]-a[0],a[i+1]-a[0])) for i in range(1,len(a)-1))<1e-10:continue
    ids=[node(b,tri,mode,domain) for b in p]
    for i in range(1,len(ids)-1):output.extend([ids[0],ids[i],ids[i+1]])
   if output==tri.tolist():whole.append(face)
   else:fragments.append(dict(face=face,indices=output))
  records.append(dict(lod=lod,side=side,sourceVertices=len(v),sourceFaces=len(f),reference=reference,source=[dict(vertex=i,coefficients=c) for i,c in sorted(source.items())],edges=edges,fragments=fragments,wholeFaces=whole,panelVertices=panels,faceKinds=faces))
ints=[0]*65;floats=[]
for n,rec in enumerate(records):
 row=[rec['sourceVertices'],rec['sourceFaces'],rec['reference'],len(ints),len(rec['source'])]
 for s in rec['source']:ints.extend([s['vertex'],len(floats)]);floats.extend(s['coefficients'])
 row.extend([len(ints),len(rec['edges'])])
 for e in rec['edges']:ints.extend([e['a'],e['b'],len(floats)]);floats.extend([e['fraction'],*e['coefficients']])
 row.extend([len(ints),len(rec['fragments'])])
 for f in rec['fragments']:ints.extend([f['face'],len(f['indices']),*f['indices']])
 row.extend([len(ints),len(rec['wholeFaces'])]);ints.extend(rec['wholeFaces'])
 row.extend([len(ints),len(rec['panelVertices'])]);ints.extend(rec['panelVertices'])
 ints[n*13:n*13+13]=row
blob=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+np.array(ints,dtype='<i4').tobytes()+np.array(floats,dtype='<f4').tobytes();blob+=hashlib.sha256(blob).digest()
(R/'assets/HardcodedNumbers/Draw02/Types_Draw02_EndClosureMeasurements.bin').write_bytes(blob)
(O/'source-recipe.json').write_text(json.dumps(records,indent=2));print(len(blob),[(x['lod'],x['side'],len(x['source']),len(x['edges']),len(x['fragments']),len(x['wholeFaces'])) for x in records])
