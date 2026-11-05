"""Measure complete original Lift04 internal rib components; emit immutable BBN map."""
from pathlib import Path
import numpy as np,json,struct,hashlib
R=Path(__file__).resolve().parents[2]; O=R/'artifacts/material-remap/Lift04/longitudinal72';O.mkdir(parents=True,exist_ok=True)
ints=[0]*6; floats=[]; reports=[]
hashes=['4f1fcb2cce6aba77adf1379d7cb51321a537e0982aba3735ceb4c0ad827ba81f','dfa77a584e751dc035e5f61c4cec8eb92a6d61c27132fd251d69e6903b0d7da2','10246dfc835353704c622d5217ddd3e3f0ad391c2ba99a6a2335977f63571f08']
for lod in range(3):
 path=R/('artifacts/uv-density/native/LiftBridge04'+['','_LOD1','_LOD2'][lod]+' Mesh.npz');d=np.load(path);v=d['mesh0_positions'];f=d['mesh0_faces']; p=np.arange(len(v))
 assert hashlib.sha256(path.read_bytes()).hexdigest()==hashes[lod]
 def root(a):
  while p[a]!=a:p[a]=p[p[a]];a=p[a]
  return a
 def join(a,b):p[root(int(a))]=root(int(b))
 for a,b,c in f:join(a,b);join(a,c)
 seen={}
 for i,q in enumerate(v):
  key=tuple(q)
  if key in seen:join(i,seen[key])
  else:seen[key]=i
 groups={}
 for i in range(len(v)):groups.setdefault(int(root(i)),[]).append(i)
 selected=[]
 for ids in groups.values():
  q=v[ids];lo=q.min(0);hi=q.max(0)
  if lo[1]>=7.90 and hi[1]<=8.82 and abs(lo[0])<3 and abs(hi[0])<3:selected.extend(ids)
 assert len(selected)==[1840,1629,0][lod]
 mask=np.zeros(len(v),bool);mask[selected]=True
 assert np.all(mask[f].any(1)==mask[f].all(1))
 # Four full section envelopes, including quantized end-cap coordinates.
 bands=[(-2.46466064453125,-2.15374755859375),(-.8646697998046875,-.55377197265625),(.5537872314453125,.86468505859375),(2.15380859375,2.46466064453125)]
 support=3.78802490234375; widths=[b-a for a,b in bands]; gap=(2*support-sum(widths))/5
 ints[lod*2]=len(ints);ints[lod*2+1]=len(selected)
 entries=[]
 for i in sorted(selected):
  j=next(j for j,(a,b) in enumerate(bands) if a<=v[i,0]<=b)
  a,b=bands[j]; center=(a+b)/2
  target=-support+(j+1)*gap+sum(widths[:j])+widths[j]/2
  weight=-1+2*(j+1)/5
  ints.append(i);floats.extend([target-center,weight]);entries.append([i,j,target-center,weight])
 tests=[]
 for delta in (-4.,-1.,0.,2.,10.25,24.):
  q=v.copy()
  for i,j,offset,weight in entries:q[i,0]=v[i,0]+offset+weight*delta
  assert np.array_equal(q[:,1:],v[:,1:]);assert np.array_equal(q[~mask],v[~mask])
  for j in range(4):
   ids=[e[0] for e in entries if e[1]==j]
   if ids:assert abs(np.ptp(q[ids,0])-np.ptp(v[ids,0]))<1e-5
  tests.append(dict(delta=delta,clearGap=gap+2*delta/5))
 reports.append(dict(lod=lod,sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),vertices=len(selected),faces=int(mask[f].all(1).sum()),bands=bands,supportInnerEdges=[-support,support],entries=entries,tests=tests))
raw=b'BBN1'+struct.pack('<7i',1,len(ints),0,0,0,len(floats),0)+struct.pack('<'+str(len(ints))+'i',*ints)+struct.pack('<'+str(len(floats))+'f',*floats)
(R/'assets/HardcodedNumbers/Lift04/Types_Lift04_LongitudinalRibMeasurements.bin').write_bytes(raw+hashlib.sha256(raw).digest())
(O/'measurement.json').write_text(json.dumps(reports,indent=2));print([(r['lod'],r['vertices'],r['faces']) for r in reports])
