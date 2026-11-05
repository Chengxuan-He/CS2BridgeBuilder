"""Compare actual production plans with original source and prior production plan."""
from pathlib import Path
import json,numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Lift04/longitudinal72'
m=json.loads((O/'measurement.json').read_text());old=json.loads((O/'baseline-plan.json').read_text());new=json.loads((O/'candidate-plan.json').read_text()); reports=[]
for row,prior in zip(new,old):
 assert (row['name'],row['delta'])==(prior['name'],prior['delta'])
 lod=row['lod'];delta=row['delta'];d=np.load(R/('artifacts/uv-density/native/'+row['name']+'.npz'));v=d['mesh0_positions'];f=d['mesh0_faces'];q=np.array(row['positions'],dtype=np.float32).astype(float);p=np.array(prior['positions'],dtype=np.float32).astype(float);ids=np.array([e[0] for e in m[lod]['entries']],dtype=int);mask=np.zeros(len(q),bool);mask[ids]=True
 assert q.shape==p.shape and np.array_equal(q[~mask],p[~mask])
 assert np.array_equal(q[ids,1:],p[ids,1:])
 for i,j,o,w in m[lod]['entries']:assert abs(q[i,0]-(v[i,0]+o+w*delta))<5e-6
 for j in range(4):
  member=np.array([e[0] for e in m[lod]['entries'] if e[1]==j],dtype=int)
  if len(member):assert np.ptp(q[member,0]-v[member,0])<5e-6
 touched=np.isin(f,ids).all(1);a=v[f[touched]].astype(float);b=q[f[touched]]
 if len(a):
  assert np.max(abs((a[:,1:]-a[:,:1])-(b[:,1:]-b[:,:1])))<5e-6
 reports.append(dict(lod=lod,delta=delta,selectedVertices=len(ids),selectedFaces=int(touched.sum()),unselectedUnchanged=True,sectionRigid=True,yzUnchanged=True))
(O/'actual-plan-check.json').write_text(json.dumps(dict(status='PASS',cases=reports),indent=2));print('PASS actual production plan',len(reports),'cases')
