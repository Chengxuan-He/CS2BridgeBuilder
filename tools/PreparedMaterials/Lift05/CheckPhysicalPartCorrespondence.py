import json
from pathlib import Path
r=Path(__file__).resolve().parents[3];o=r/'artifacts/material-remap/Lift05';src=r/'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism'
e=json.loads((o/'road-continuous-candidate/mapping-evidence.json').read_text());p=json.loads((o/'road-projection-plan.json').read_text());c=json.loads((o/'road-continuous-candidate/consumer-plan.json').read_text());out=[]
for ev,old,cur in zip(e['levels'],p['levels'],c['levels']):
 lod=ev['lod'];fs=json.loads((src/f'road-lod{lod}.json').read_text())['faces'];groups=ev['continuousComponents']+[[x['patch']] for x in ev['patches'] if x['mode']=='original-finite'];expected={f:g for g,ids in enumerate(groups) for i in ids for f in old['patches'][i]['faces']};errors=[];checks=0
 for f in fs:
  for w in ((1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)):
   x=sum(a*b[0] for a,b in zip(w,f['positions']));z=sum(a*b[2] for a,b in zip(w,f['positions']));band=next(b for b in cur['bands'] if b['minX']<x<b['maxX']);node=band['root']
   while node>=0:
    n=cur['nodes'][node];a,b,k=n['lineXZ1'];node=n['positive' if a*x+b*z+k>=0 else 'negative']
   g=-node-2;checks+=1
   if g!=expected[f['id']]:errors.append([f['id'],g,expected[f['id']]])
 out.append(dict(lod=lod,faces=len(fs),interiorCorrespondenceChecks=checks,wrongSourceDomain=errors,patches=len(cur['patches']),sourceKeys=list(fs[0])))
assert not any(x['wrongSourceDomain'] for x in out),out
(o/'physical-part-correspondence-check.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
