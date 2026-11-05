"""Build complete native-plan fixture and verify upper truss coverage. ROOT RESEARCH OUTPUT."""
import sys,json,struct,hashlib
from pathlib import Path
import numpy as np
root,research,out=map(Path,sys.argv[1:]);out.mkdir(parents=True,exist_ok=True)
def read(name):
 b=(root/'assets/HardcodedNumbers/PedestrianDraw02'/('Types_PedestrianDraw02_'+name+'_cs.bin')).read_bytes();assert hashlib.sha256(b[:-32]).digest()==b[-32:]
 c=struct.unpack_from('<6i',b,8)
 return np.frombuffer(b,'<i4',c[0],32),np.frombuffer(b,'<f4',c[4],32+4*c[0])
a,f=read('PedestrianDraw02TrussMapping');u,uf=read('PedestrianDraw02UpperTruss');ip=fp=up=0;fixture=[];reports=[]
for lod in range(2):
 d=np.load(research/f'pedestrian02-main-{lod}.npz');v=d['positions'];faces=d['indices'];parts=json.loads((research/f'pedestrian02-main-{lod}-parts.json').read_text())
 n,na,nr=map(int,a[ip:ip+3]);aid=a[ip+3:ip+3+na];rid=a[ip+3+na:ip+3+na+nr];nu=int(u[up+1]);uid=u[up+2:up+2+nu];assert n==len(v)==u[up]
 assert not(set(aid)&set(uid) or set(rid)&set(uid));assert len(uid)==len(set(uid))
 selected=[p for p in parts if set(p['vertices'])<=set(uid)];assert len(selected)==120
 assert set(uid)=={i for p in selected for i in p['vertices']}
 def whole(ids):
  hits=np.isin(faces,ids).sum(1);assert not np.any((hits>0)&(hits<3));return np.flatnonzero(hits==3).tolist()
 members=[]
 for p in parts:
  ids=p['vertices']
  if set(ids)<=set(rid):members.append(dict(centre=round((p['minimum'][0]+p['maximum'][0])/2),vertices=ids,faces=whole(ids)))
 assert len(members)==14
 fixture.append(dict(lod=lod,positions=v.tolist(),indices=faces.ravel().tolist(),assembly=aid.tolist(),halfspan=float(f[fp]),assemblyFaces=whole(aid),upper=uid.tolist(),upperHalfspan=float(uf[lod]),upperFaces=whole(uid),members=members))
 plates=[p for p in selected if p['minimum'][0]<0<p['maximum'][0] and p['maximum'][0]-p['minimum'][0]<2.2];assert len(plates)==12
 cases=[]
 for delta in [-6.,-float(f[fp]),-float(uf[lod]),-1.,0.,2.,8.,24.]:
  delta=np.float32(delta);k=np.float32(np.float32(uf[lod]+delta)/uf[lod]);target=v.copy();target[uid,0]=v[uid,0]*k
  assert np.array_equal(target[:,1:].view('u4'),v[:,1:].view('u4'))
  others=np.ones(len(v),bool);others[uid]=False;assert np.array_equal(target[others],v[others])
  residual=[]
  for p in plates:
   ids=p['vertices'];lo,hi=p['minimum'][0],p['maximum'][0];old=v[ids,0]+(2*v[ids,0]-lo-hi)/(hi-lo)*delta
   residual.append(float(np.max(abs(old-target[ids,0]))))
  if delta:assert min(residual)>.01
  cases.append(dict(delta=float(delta),scale=float(k),minimumOldPlateError=min(residual)))
 reports.append(dict(lod=lod,completeUpperParts=len(selected),thinCentralPlates=len(plates),vertices=len(uid),faces=len(whole(uid)),halfspan=float(uf[lod]),cases=cases))
 ip+=3+na+nr;fp+=1+nr;up+=2+nu
(out/'fixture.json').write_text(json.dumps(fixture,separators=(',',':')));(out/'upper-check.json').write_text(json.dumps(reports,indent=2));print('PASS16 offline cases;120 complete upper members and12 thin plates perLOD; native fixture ready')
