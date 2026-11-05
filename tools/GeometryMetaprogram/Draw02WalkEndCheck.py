"""Independent source membership, slab-cap and far stepped-contact regression."""
from pathlib import Path
import json,struct,os
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/gap61';B=R/'artifacts/material-remap/Draw02/endcap60'
actual=os.environ.get('DRAW02_WALK_ACTUAL')=='1'
strip=json.loads((O/'far-source-strip.json').read_text())
def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3).copy();off=12+nv*12;m=np.frombuffer(b,'?',nf,off).copy();off+=nf;fr={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,off);off+=8;fr[face]=np.frombuffer(b,'<i4',n,off).tolist();off+=4*n
 return p,m,fr
def sweep(tris,tolerance):
 xs=sorted(set(tris[:,:,0].reshape(-1)));checks=0
 for a,b in zip(xs,xs[1:]):
  if b-a<=1e-12:continue
  x=(a+b)/2;intervals=[]
  for tri in tris:
   points=[]
   for p,q in zip(tri,np.roll(tri,-1,axis=0)):
    if min(p[0],q[0])<=x<=max(p[0],q[0]) and p[0]!=q[0]:points.append(p[1]+(q[1]-p[1])*(x-p[0])/(q[0]-p[0]))
   if len(points)>=2 and max(points)>min(points):intervals.append((min(points),max(points)))
  assert intervals
  right=9.800048828125
  for low,high in sorted(intervals):
   assert abs(low-right)<=tolerance,('band-gap-or-overlap',x,low,right,tolerance)
   right=high
  assert abs(right-10)<=tolerance
  checks+=1
 return checks
cases=0;caps=0;areas=0;minArea=float('inf');contactMax=0.;sweepChecks=0;negative=False
for lod in range(3):
 source=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=source['mesh0_positions'];f=source['mesh0_indices'].reshape(-1,3)
 walks=json.loads((R/f'artifacts/material-remap/Draw02/underside52/lod{lod}-walk-members.json').read_text())
 for delta in ('-0.1','0','2','4','5.75','8'):
  for scenario in range(6):
   old,mask,fr=read(B/f'after60-lod{lod}-delta{delta}-case{scenario}.bin');om=mask.copy();of={k:list(x) for k,x in fr.items()};p=old.copy();changed=set();newFaces=set();spans={}
   for side in (-1,1):
    width=([3.5,0,3,0,2,3.5] if side<0 else [3.5,0,0,3,4,3.5])[scenario]
    edge=np.float32(9.5+float(delta));inner=np.float32(side*np.float32(edge-width));outer=np.float32(side*edge);spans[side]=(width,inner,outer)
   if lod<2:
    for walk in walks:
     side=walk['side'];width,inner,outer=spans[side];bound=abs(inner) if width else np.float32(13.25 if delta=='5.75' else 6+float(delta));shift=np.float32(side*bound-np.float32(side*6.0999755859375))
     for face in walk['endCapFaces']:
      if not om[face]:continue
      newFaces.add(face);mask[face]=False;fr.pop(face,None)
      for i in f[face]:
       changed.add(int(i))
       if width:
        start=np.float32(-9.5 if side<0 else 6);spanStart=outer if side<0 else inner;coefficient=np.float32((v[i,0]-start)/np.float32(3.5));p[i,0]=np.float32(np.float32(v[i,0]+np.float32(spanStart-start))+np.float32(coefficient*np.float32(width-3.5)))
       if abs(v[i,0])==np.float32(6.553955078125):p[i,0]=np.float32(v[i,0]+shift)
    assert len(newFaces)==(4 if lod==0 else 2)
   else:
    local={};extra=[]
    for kind in (0,1):
     for i,node in enumerate(strip['nodes']):
      width,inner,_=spans[node['side']]
      if not width or node['kind']!=kind:continue
      a,b,c=node['a'],node['b'],node['c'];wb=np.float32(node['wB']);wc=np.float32(node['wC'])
      point=np.float32(v[a]+np.float32(np.float32(v[b]-v[a])*wb))
      if kind:point=np.float32(point+np.float32(np.float32(v[c]-v[a])*wc))
      outer=old[strip['references'][0 if node['side']<0 else 1],0];point[0]=np.float32(inner+np.float32(np.float32(node['fraction'])*np.float32(outer-inner)))
      local[i]=len(old)+len(extra);extra.append(point)
    if extra:p=np.concatenate([p,np.asarray(extra,dtype=np.float32)])
    for rec in strip['records']:
     if not spans[rec['side']][0]:continue
     newFaces.add(rec['face']);fr[rec['face']].extend(local[i] for i in rec['indices'])
   if actual:p,mask,fr=read(O/f'after61-lod{lod}-delta{delta}-case{scenario}.bin')
   unaffected=np.array(sorted(set(range(len(v)))-changed));assert np.array_equal(p[unaffected],old[unaffected])
   untouched=np.array(sorted(set(range(len(f)))-newFaces));assert np.array_equal(mask[untouched],om[untouched]);assert {k:x for k,x in fr.items() if k not in newFaces}=={k:x for k,x in of.items() if k not in newFaces}
   assert np.array_equal(p[:len(v),1:],v[:,1:])
   if lod<2:
    for walk in walks:
     side=walk['side'];width,inner,outer=spans[side]
     bound=abs(inner) if width else np.float32(13.25 if delta=='5.75' else 6+float(delta));shift=np.float32(side*bound-np.float32(side*6.0999755859375))
     for face in walk['endCapFaces']:
      assert not mask[face] and face not in fr
      q=p[f[face]].astype(float);src=v[f[face]].astype(float);normal=np.cross(q[1]-q[0],q[2]-q[0]);sn=np.cross(src[1]-src[0],src[2]-src[0]);assert normal@sn>0
      assert q[:,1].min()==9.85009765625 and q[:,1].max()==10
      for i in f[face]:
       expected=np.float32(v[i,0]+shift) if abs(v[i,0])==np.float32(6.553955078125) else outer
       assert p[i,0]==expected,(lod,delta,scenario,face,int(i),p[i,0],expected)
      caps+=1
   else:
    assert np.array_equal(p[:len(old)],old)
    # Every added band rests on the original cap's horizontal contact; its
    # projected X/Y area is exactly the required side span times native height.
    # Independent side/leaf/end groups avoid accepting a missing mirrored band.
    for side in (-1,1):
     width,inner,_=spans[side]
     for leaf in (-1,1):
      for end in ('tip','hinge'):
       triangles=[]
       for rec in strip['records']:
        src=v[f[rec['face']]]
        if rec['side']!=side or np.sign(src[:,2].mean())!=leaf or (abs(src[:,2].mean())<1)!=(end=='tip'):continue
        ids=fr[rec['face']][len(of[rec['face']]):];triangles.extend(p[ids].reshape(-1,3,3))
       if not width:assert not triangles;continue
       tris=np.asarray(triangles,dtype=float);norm=np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0]);assert np.all(np.linalg.norm(norm,axis=1)>0)
       normalSign=-leaf if end=='tip' else leaf;assert np.all(norm[:,2]*normalSign>0)
       outer=float(old[strip['references'][0 if side<0 else 1],0]);expected=abs(outer-float(inner))*(10-9.800048828125);measured=np.abs(norm[:,2]).sum()/2
       # Bound from individual float32 coordinate intervals and projected edges.
       step=np.max(np.abs(np.spacing(tris.astype(np.float32))),axis=(0,1));extent=np.ptp(tris.reshape(-1,3),axis=0);budget=len(tris)*4*(extent[0]*step[1]+extent[1]*step[0]+step[0]*step[1])
       assert abs(measured-expected)<=budget,(delta,scenario,side,leaf,end,measured,expected,budget)
       assert tris[:,:,1].min()>=9.800048828125-step[1] and abs(tris[:,:,1].max()-10)<=step[1]
       if side>0:assert tris[:,:,0].min()>=float(inner)-step[0]
       else:assert tris[:,:,0].max()<=float(inner)+step[0]
       keys=[tuple(sorted(map(tuple,t))) for t in tris];assert len(keys)==len(set(keys))
       sweepChecks+=sweep(tris,4*step[1])
       if not negative and len(tris)>1:
        try:sweep(tris[1:],4*step[1])
        except AssertionError:negative=True
        else:raise AssertionError('Missing far band triangle was accepted')
       areas+=1;minArea=min(minArea,measured);contactMax=max(contactMax,abs(tris[:,:,1].min()-9.800048828125))
   cases+=1
assert negative
result=dict(actualFrozenDll=actual,cases=cases,completeThinCapFaces=caps,farSideLeafEndAreaChecks=areas,farGapOverlapSweepChecks=sweepChecks,removedBandTriangleNegativeControl=True,minimumFarBandProjectedArea=minArea,maximumLowerContactYRoundoff=contactMax,unrelatedOriginalPositionsMasksFragmentsUnchanged=True)
(O/('actual-geometry-result.json' if actual else 'offline-geometry-result.json')).write_text(json.dumps(result,indent=2));print(result)
