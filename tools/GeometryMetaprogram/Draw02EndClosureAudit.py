"""Offline pre-freeze closure audit. Predictions are not native-consumer receipts."""
from pathlib import Path
from collections import defaultdict
import json, struct, os
import numpy as np

R=Path(__file__).resolve().parents[2]
O=R/'artifacts/material-remap/Draw02/endcap60'
recipes=json.loads((O/'source-recipe.json').read_text())
actualMode=os.environ.get('DRAW02_CLOSURE_ACTUAL')=='1'

def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b)
 p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3).copy();off=12+nv*12
 m=np.frombuffer(b,'?',nf,off).copy();off+=nf;fr={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,off);off+=8;fr[face]=np.frombuffer(b,'<i4',n,off).tolist();off+=n*4
 return p,m,fr

def write(path,p,m,fr):
 b=struct.pack('<3i',len(p),len(m),len(fr))+np.asarray(p,dtype='<f4').tobytes()+m.tobytes()
 for face,ids in fr.items():b+=struct.pack('<2i',face,len(ids))+np.asarray(ids,dtype='<i4').tobytes()
 path.write_bytes(b)

def lerp(a,b,t):return np.float32(a+np.float32(np.float32(b-a)*t))

reports=[];visits=0
for lod in range(3):
 data=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
 v=data['mesh0_positions'];f=data['mesh0_indices'].reshape(-1,3);q=v[f]
 records=[r for r in recipes if r['lod']==lod]
 sourceFaces={int(i) for rec in records for i in rec['faceKinds']}
 changed={s['vertex'] for rec in records for s in rec['source']}|{i for rec in records for i in rec['panelVertices']}
 incident=set(np.flatnonzero(np.isin(f,list(changed)).any(1)))
 # Independently enumerate complete original folded profiles at BOTH ends.
 if lod<2:
  endpoints=(np.abs(q[:,:,2]).max(1)<.25)|((np.abs(q[:,:,2]).min(1)>=78.49)&(np.abs(q[:,:,2]).max(1)<78.71))
  folds=endpoints&(np.abs(q[:,:,0]).max(1)<=5.74609375)&(q[:,:,1].min(1)>=9.330810546875)&(q[:,:,1].max(1)<=9.800048828125)
  caps=endpoints&(np.abs(q[:,:,0]).min(1)>=5.74609375)&(np.abs(q[:,:,0]).max(1)<=6.14599609375)&(q[:,:,1].min(1)>=7.935546875)&(q[:,:,1].max(1)<=9.800048828125)
  assert set(np.flatnonzero(folds|caps))<=sourceFaces
  profileCounts={}
  for leaf in (-1,1):
   for side in (-1,1):
    for end in ('tip','hinge'):
     sel=(folds|caps)&((q[:,:,0]*side).min(1)>=0)&((q[:,:,2]*leaf).min(1)>0)
     sel&=(np.abs(q[:,:,2]).max(1)<.25) if end=='tip' else (np.abs(q[:,:,2]).min(1)>78)
     assert sel.sum()==(14 if lod==0 else 10)
     profileCounts[f'{leaf}/{side}/{end}']=int(sel.sum())
 else:profileCounts={'nativeMergedCaps':len(sourceFaces)}
 sourceEdges=np.unique(np.sort(np.concatenate([f[:,[0,1]],f[:,[1,2]],f[:,[2,0]]]),axis=1),axis=0)
 aa=v[sourceEdges[:,0]].astype(float);ab=v[sourceEdges[:,1]].astype(float)-aa;den=(ab*ab).sum(1)
 incidences=[]
 for i in changed:
  t=np.divide(((v[i]-aa)*ab).sum(1),den,out=np.zeros(len(den)),where=den>0)
  hits=np.flatnonzero((np.linalg.norm(v[i]-aa-t[:,None]*ab,axis=1)<1e-12)&(t>1e-6)&(t<1-1e-6))
  incidences.extend((i,int(sourceEdges[j,0]),int(sourceEdges[j,1]),float(t[j])) for j in hits)
 (O/f'lod{lod}-source-edge-incidences.json').write_text(json.dumps(incidences))
 duplicateChecks=0;outside=set();maxSeparation=0.;tChecks=0;foldChains=0;panelChecks=0
 for delta in ('-0.1','0','2','4','5.75','8'):
  for scenario in range(6):
   old,mask,fr=read(O/f'baseline60-lod{lod}-delta{delta}-case{scenario}.bin')
   p=old.copy();extra=[]
   for rec in records:
    side=rec['side'];body=np.float32(old[rec['reference'],0]-v[rec['reference'],0])
    width=([3.5,0,3,0,2,3.5] if side<0 else [3.5,0,0,3,4,3.5])[scenario]
    edge=np.float32(9.5+float(delta));bound=np.float32(edge-width) if width else np.float32(13.25 if delta=='5.75' else 6+float(delta))
    shift=np.float32(side*bound-np.float32(side*6.0999755859375)) if lod<2 else body
    diff=np.float32(shift-body)
    for s in rec['source']:
     i=s['vertex'];w=np.float32(s['coefficients'][0]);p[i,0]=np.float32(v[i,0]+shift) if w==1 else np.float32(old[i,0]+np.float32(w*diff))
    for i in rec['panelVertices']:p[i,0]=np.float32(v[i,0]+shift)
    local=[]
    for e in rec['edges']:
     a,b=e['a'],e['b'];t=np.float32(e['fraction']);src=lerp(v[a],v[b],t);out=src.copy();w=np.float32(e['coefficients'][0])
     out[0]=np.float32(src[0]+shift) if w==1 else np.float32(lerp(old[a,0],old[b,0],t)+np.float32(w*diff))
     local.append(len(old)+len(extra));extra.append(out)
    for fragment in rec['fragments']:
     face=fragment['face'];mask[face]=True;fr[face]=[local[-i-1] if i<0 else i for i in fragment['indices']]
    for face in rec['wholeFaces']:mask[face]=False;fr.pop(face,None)
   p=np.concatenate([p,np.asarray(extra,dtype=np.float32)])
   if actualMode:p,mask,fr=read(O/f'after60-lod{lod}-delta{delta}-case{scenario}.bin')
   else:write(O/f'predicted60-lod{lod}-delta{delta}-case{scenario}.bin',p,mask,fr)
   used=set(f[~mask].flat)
   for ids in fr.values():used.update(i for i in ids if i<len(v))
   wholeUsed=set(f[~mask].flat)
   for i,a,b,t in incidences:
    if not {i,a,b}<=wholeUsed or np.array_equal(v[a,1:],v[b,1:]):continue
    start=p[a].astype(float);direction=p[b].astype(float)-start;relative=p[i].astype(float)-start
    station=relative@direction/(direction@direction)
    distance=np.linalg.norm(relative-np.clip(station,0,1)*direction)
    # Independent float32 coordinate/linear-projection rounding budget, not
    # a geometry-dependent tolerance tuned to the measured error.
    assert distance<=4*np.max(np.abs(np.spacing(p[[i,a,b]])))
    tChecks+=1
   if lod<2:
    for rec in records:
     side=rec['side'];lips=[int(i) for i,kind in rec['faceKinds'].items() if kind=='lip']
     for leaf in (-1,1):
      for end,z in [('tip',.23000335693359375),('hinge',78.5)]:
       intervals=[]
       for face in lips:
        for a,b in zip(f[face],np.roll(f[face],-1)):
         if v[a,1]==v[b,1]==9.699951171875 and v[a,2]==v[b,2]==leaf*z:
          intervals.append(sorted([side*float(p[a,0]),side*float(p[b,0])]))
       intervals.sort();assert intervals and intervals[0][0]==0
       right=0.
       for a,b in intervals:assert a<=right;right=max(right,b)
       anchor=[i for i in changed if v[i,0]==side*5.74609375 and v[i,1]==9.699951171875 and v[i,2]==leaf*z]
       assert anchor and right==side*float(p[anchor[0],0]) and right>0
       # The continuous original fold crosses the horizontal panel/rib profile.
       # Its interior parameter may slide past a rib slot as the section adapts;
       # requiring it to stay on one particular old panel edge is not closure.
       foldChains+=1
   groups=defaultdict(list)
   for i in used:groups[tuple(v[i])].append(i)
   for coord,ids in groups.items():
    if not changed.intersection(ids):continue
    # Distinct source indices with a shared native point may have independent
    # hard normals/UVs, but cannot acquire a new positional separation.
    before=float(np.ptp(old[ids,0]));after=float(np.ptp(p[ids,0]))
    ulps=float(np.abs(np.spacing(p[ids,0])).max())*2
    assert after<=max(before,ulps),(lod,delta,scenario,coord,ids,before,after)
    duplicateChecks+=len(ids);maxSeparation=max(maxSeparation,after)
   # Every directly incident surviving face must be accounted for. Horizontal
   # panel endpoint moves preserve its original flat plane/frame; other outside
   # faces are removed/replaced road or independent rail-supplement ownership.
   for face in incident-sourceFaces:
    if mask[face] and face not in fr:continue
    n=np.cross(q[face,1]-q[face,0],q[face,2]-q[face,0])
    assert n[0]==0,(lod,face,'unaccounted nonhorizontal incident face')
    ids=fr.get(int(face),f[face].tolist())
    triangles=p[ids].reshape(-1,3,3).astype(float)
    normals=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0])
    assert np.all(normals@n>0),(lod,delta,scenario,int(face),'incident-panel-foldover')
    panelChecks+=1
    outside.add(int(face))
   visits+=1
 reports.append(dict(lod=lod,sourceFaces=len(sourceFaces),completeEndProfiles=profileCounts,
                     allIncidentSourceFaces=len(incident),survivingPanelIncidentFaces=sorted(outside),
                     duplicateCoordinateVertexChecks=duplicateChecks,maximumSharedPointSeparation=maxSeparation,
                     nonHorizontalWholeSourceEdgeContacts=tChecks,continuousFoldContactChains=foldChains,
                     incidentPanelDirectedFaceChecks=panelChecks))
result=dict(kind='actual frozen-DLL final Plan' if actualMode else 'offline prediction, not native execution',cases=visits,lods=reports)
(O/('actual-closure-audit.json' if actualMode else 'offline-closure-audit.json')).write_text(json.dumps(result,indent=2))
print(result['kind'],visits,[(x['lod'],x['sourceFaces'],x['allIncidentSourceFaces'],x['duplicateCoordinateVertexChecks']) for x in reports])
