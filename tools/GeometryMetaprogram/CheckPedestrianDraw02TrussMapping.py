"""Verify measured PedestrianDraw02 mappings; accepts ROOT RESEARCH OUTPUT."""
import sys,json,struct,hashlib
from pathlib import Path
import numpy as np
root,r,out=map(Path,sys.argv[1:]);out.mkdir(parents=True,exist_ok=True)
b=(root/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs.bin').read_bytes(); assert hashlib.sha256(b[:-32]).digest()==b[-32:]
c=struct.unpack_from('<6i',b,8);a=np.frombuffer(b,dtype='<i4',count=c[0],offset=32);f=np.frombuffer(b,dtype='<f4',count=c[4],offset=32+4*c[0]);ip=fp=0;reports=[]
for lod in range(2):
 d=np.load(r/f'pedestrian02-main-{lod}.npz');v=d['positions'];faces=d['indices'];parts=json.loads((r/f'pedestrian02-main-{lod}-parts.json').read_text());n,na,nr=map(int,a[ip:ip+3]);ids=a[ip+3:ip+3+na];rids=a[ip+3+na:ip+3+na+nr];h=f[fp];weights=f[fp+1:fp+1+nr];assert len(v)==n
 selected=set(map(int,ids));rset=set(map(int,rids));assert not selected&rset
 # Membership contains complete original source faces, never partial triangles.
 for group in (selected,rset):
  hit=np.isin(faces,list(group)).sum(axis=1);assert not np.any((hit>0)&(hit<3))
 groups=[p for p in parts if set(p['vertices'])<=rset];assert len(groups)==14
 baseline_failures=0;case=[]; covered=selected|rset; unrelated=[i for i in range(n) if i not in covered]
 for delta in (-6.,-float(h),-1.,0.,2.,8.,24.):
  delta=np.float32(delta);scale=np.float32(np.float32(h+delta)/h);target=v.copy();target[ids,0]=v[ids,0]*scale;target[rids,0]=v[rids,0]+weights*delta
  assert np.array_equal(target[:,1:],v[:,1:]);assert np.array_equal(target[unrelated],v[unrelated])
  widths=[];centres=[]
  for p in groups:
   ix=p['vertices'];widths.append(float(np.ptp(target[ix,0])));assert abs(np.ptp(target[ix,0])-np.ptp(v[ix,0]))<4e-6
   if p['minimum'][0]<0<p['maximum'][0]:assert np.array_equal(target[ix],v[ix])
   centres.append((np.sign((p['minimum'][2]+p['maximum'][2])/2),float((target[ix,0].min()+target[ix,0].max())/2)))
  for side in (-1,1):
   cc=sorted(x for z,x in centres if z==side);gaps=np.diff(cc); assert np.ptp(gaps)<3e-5
  # Old centre-crossing member own-span output is wrong for the thin connector.
  connector=next(p for p in parts if p['minimum'][0]<0<p['maximum'][0] and p['maximum'][0]-p['minimum'][0]<1.3 and set(p['vertices'])<=selected)
  ix=connector['vertices'];lo,hi=connector['minimum'][0],connector['maximum'][0];old=v[ix,0]+(2*v[ix,0]-lo-hi)/(hi-lo)*delta
  if delta!=0:assert np.max(abs(old-target[ix,0]))>.01;baseline_failures+=1
  case.append(dict(delta=float(delta),scale=float(scale),assemblyTargetSpan=float(2*abs(h+delta)),maxRibWidth=max(widths)))
 reports.append(dict(lod=lod,assemblyVertices=na,ribVertices=nr,assemblySourceSpan=float(2*h),allMemberFacesWhole=True,baselineRejectedCases=baseline_failures,cases=case));ip+=3+na+nr;fp+=1+nr
(out/'geometry-check.json').write_text(json.dumps(reports,indent=2));print('PASS: 14 measured geometry cases, complete source-face membership, rigid ribs/equal spacing, unchanged Y/Z and unrelated vertices; 12 baseline counterexamples. Native frame/upload checks separate.')
