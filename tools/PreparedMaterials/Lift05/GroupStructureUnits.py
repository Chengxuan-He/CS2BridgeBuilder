"""Finite complete Lift05 structural grouping and source-authored semantic unit batch."""
import collections,copy,hashlib,io,json,struct,sys
from pathlib import Path
import numpy as np
from BuildUvCharts import inputs,domains,ROOT
sys.path.insert(0,str(ROOT/'artifacts/agent-material-env/Lift05/python-deps'))
from shapely.geometry import Polygon,LineString,Point
OUT=ROOT/'artifacts/prepared-materials/Lift05/structure-groups'
OUT.mkdir(parents=True,exist_ok=True)
def save(name,v):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n');return {'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def ref(p):return {'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
source=json.loads((ROOT/'artifacts/prepared-materials/Lift05/rebuild-native-v1/inputs/native-inputs.json').read_text())
dig={x['cid']:x['sha256'] for x in source['sources']};bindings={(x['render'],x['materialSlot']):dig[x['surfaceCid']] for x in source['bindings']}
meta=json.loads((ROOT/'artifacts/prepared-materials/Lift05/domain-charts/charts.json').read_text());streams={(x['name'],x['mesh']):x['domains'] for x in meta['streams']};shapes=[]
for chart in meta['charts']:
 s=Polygon()
 for loop in chart['loops']:s=s.symmetric_difference(Polygon(loop))
 shapes.append(s)
receiver=ROOT/'artifacts/prepared-materials/Lift05/region-coverage/receivers.json.receivers.bin';r=io.BytesIO(receiver.read_bytes());counts=collections.Counter();bad=collections.defaultdict(set)
def read(f):return struct.unpack('<'+f,r.read(struct.calcsize('<'+f)))
def text():
 n=s=0
 while True:
  b=read('B')[0];n|=(b&127)<<s
  if b<128:break
  s+=7
 return r.read(n).decode()
while r.tell()<len(r.getbuffer()):
 name=text();mesh,delta,count=read('ifi')
 for _ in range(count):
  d,repeat,direct,n=read('i??i');uv=np.asarray(read('f'*(n*2))).reshape(-1,2);identity=(name,mesh,d);counts[identity]+=1
  if direct:continue
  refs=streams[(name,mesh)][d]
  if repeat:bad[identity].add('conditioned-phase');continue
  if not refs:bad[identity].add('no-reliable-chart');continue
  for i in range(0,n,3):
   q=uv[i:i+3];t=Polygon(q)
   if t.area==0:
    unique=np.unique(q,axis=0);t=Point(unique[0]) if len(unique)==1 else LineString(unique)
   if not any(shapes[x].covers(t) for x in refs):bad[identity].add('outside-authored-chart');break
allinputs=list(inputs());unitSpecs=[('Lift05.UpperCrossbeam.PaintedStrip','LiftBridge05 Mesh',0,2133),('Lift05.DeckSoffit.PaintedPanel','LiftBridge05_LOD1 Mesh',0,12386)]
base=json.loads((ROOT/'artifacts/prepared-materials/Lift05/repeat-unit-trial-v2/input/manifest.json').read_text())['units'][0];units=[];evidences=[]
for uid,name,mesh,d in unitSpecs:
 _,_,p,uv,f=next(v for v in allinputs if v[:2]==(name,mesh));fs=domains(name,mesh)[d];points=uv[f[fs]].reshape(-1,2).astype(float);lo,hi=points.min(0),points.max(0);world=p[f[fs]].reshape(-1,3).astype(float)
 # A complete original rectangle, never a receiver or arbitrary crop.
 assert len(fs)==2 and len(np.unique(points,axis=0))==4
 corners={tuple(q):w for q,w in zip(points,world)};origin=corners[tuple(lo)];pu=np.linalg.norm(corners[(hi[0],lo[1])]-origin);pv=np.linalg.norm(corners[(lo[0],hi[1])]-origin)
 u=copy.deepcopy(base);u.update(id=uid,min=lo.tolist(),max=hi.tolist(),physicalSize=[pu,pv],measuredTexelsPerMetre=((hi-lo)*4096/[pu,pv]).tolist(),runtimeTexelsPerMetre=((hi-lo)*4096/[pu,pv]).tolist(),originalFaces=fs,cutout=False,regions=[],aliases=[],status='SOURCE_AUTHORED_RECTANGLE_NATIVE_VALIDATION_PENDING')
 evidence={'unit':uid,'render':name,'mesh':mesh,'domain':d,'faces':fs,'uv':points.tolist(),'positions':world.tolist(),'bounds':lo.tolist()+hi.tolist(),'physicalPeriod':[pu,pv],'sourceDigest':bindings[(name,mesh)],'semanticPattern':'Original pale painted structural surface, reused across repeated upper crossbeam strips' if 'Crossbeam' in uid else 'Original pale painted horizontal deck soffit panel, reused across repeated four-metre deck modules','scope':'Complete original two-triangle rectangular surface. Pattern edges may differ from atlas neighbours; preserve density and report boundary differences for human observation. No receiver-specific crop or triangle fold.','fixture':ref(ROOT/'artifacts/uv-density/layout-plans.bin'),'fullMipEvidence':ref(ROOT/'artifacts/prepared-materials/Lift05/repeat-unit-trial-v2/extraction.json')}
 u['evidence']=save(uid+'.evidence.json',evidence);units.append(u);evidences.append(evidence)
inventory=[];groups=collections.defaultdict(list);membership=[]
for name,mesh,p,uv,f in allinputs:
 for d,fs in enumerate(domains(name,mesh)):
  identity=(name,mesh,d);label=f'{name}|{mesh}|{d}';digest=bindings[(name,mesh)];inventory.append(dict(id=label,sourceDigest=digest,receivers=counts[identity]));q=uv[f[fs]].reshape(-1,2);lo,hi=q.min(0),q.max(0);unit=None
  if bad[identity]:
   for candidate in units:
    if digest==candidate['sourceDigest'] and np.all(lo>=candidate['min']) and np.all(hi<=candidate['max']):unit=candidate;break
  if unit:category=unit['id']
  elif not bad[identity]:category='original'
  elif mesh==1:category='interior-glass'
  elif 'LOD2' in name:category='far-packed-atlas'
  elif 'no-reliable-chart' in bad[identity]:category='ambiguous-part-topology'
  elif 'conditioned-phase' in bad[identity]:category='conditioned-mixed-island'
  else:category='nonperiodic-authored-detail'
  groups[(name,mesh,digest,category)].append(label)
  if bad[identity]:membership.append(dict(id=label,category=category,receivers=counts[identity],constraints=sorted(bad[identity]),bounds=lo.tolist()+hi.tolist()))
evidence=save('group-membership.json',{'scope':'All original domains; changed receivers sampled at six half-width deltas -1.25,-0.25,0,2,8,24. Not proof of every future width. Zero-count domains retain original mapping; no receiver mutation observed.','receiverEvidence':ref(receiver),'chartEvidence':ref(ROOT/'artifacts/prepared-materials/Lift05/domain-charts/charts.json'),'nonOriginal':membership})
inv=save('original-domains.json',dict(schema='BridgeBuilder/StructureDomains/1',style='Lift05',domains=inventory))
reasons={'original':'Native original Surface CID and original UV; sampled changed receivers are direct or contained in their own authored chart. Zero-count original domains retain their existing unchanged mapping, not a proof for future widths.','interior-glass':'Template2 interior glass has interior mapping and transparency; cannot substitute Default0 painted base. Preserve original shader and provide native periodic interior mapping if width-changed chart exits original region.','far-packed-atlas':'LOD2 atlas combines baked detail and shadows in separately packed part islands. Need corresponding far material semantic unit or an authored detail/base decomposition; near-LOD rectangle must not replace packed far UV.','ambiguous-part-topology':'Original part has no reliable closed UV chart; no safe material-membership polygon established. Need offline topology/part evidence before widening region coverage.','conditioned-mixed-island':'Original conditioned domain spans material/detail boundaries outside identified genuine units. Need semantic base/detail separation preserving old phase, not triangle-fold baking.','nonperiodic-authored-detail':'Density-corrected receiver exits its own original mixed/detail island and neither measured genuine unit contains the original domain. Need measured detail/base decomposition or bounded native variable composition preserving authored detail.'}
groupList=[]
for (name,mesh,digest,category),ids in groups.items():
 g=dict(id=f'{name}|{mesh}|{category}',sourceDigest=digest,domains=ids,evidence=evidence)
 if category.startswith('Lift05.'):
  g.update(kind='REPEAT_BASE',unit=category)
 else:g.update(kind='ORIGINAL' if category=='original' else 'NONREPEATING',reason=reasons[category])
 groupList.append(g)
used={g.get('unit') for g in groupList};assert all(u['id'] in used for u in units)
save('manifest.json',dict(schema='BridgeBuilder/StructureUnits/1',style='Lift05',inventory=inv,units=[dict(id=u['id'],sourceDigest=u['sourceDigest'],semanticPattern=e['semanticPattern'],bounds=u['min']+u['max'],physicalPeriod=u['physicalSize'],evidence=u['evidence'],mipEvidence=e['fullMipEvidence']) for u,e in zip(units,evidences)],groups=groupList))
save('input/manifest.json',dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift05',units=units))
print(json.dumps(dict(domains=len(inventory),groups=len(groupList),units=len(units),receivers=sum(counts.values()),repeatDomains=sum(len(g['domains']) for g in groupList if g['kind']=='REPEAT_BASE'))))
