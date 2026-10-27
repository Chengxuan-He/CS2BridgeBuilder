"""One complete Draw02 original-domain grouping and bounded existing-unit reuse pass."""
from pathlib import Path
import collections, hashlib, json, re, struct, sys, zipfile
import numpy as np

r=Path(sys.argv[1]).resolve();sys.path.insert(0,str(r/'artifacts/agent-material-env/Draw02/python'))
from shapely.geometry import Polygon, box
from ExportNativeCharts import domains
b=r/'artifacts/prepared-materials/Draw02/shared-source-migration'
out=r/'artifacts/prepared-materials/Draw02/structure-units';out.mkdir(parents=True,exist_ok=True)
charts=json.loads((b/'native-charts.json').read_text());source_map=json.loads((b/'source-sampler-map.json').read_text())
sources={s['cid']:s for s in source_map['sources']}
renders={}
for f in (r/'src/BridgeBuilder/Bridges/Types/Draw02').glob('*.cs'):
 for name,cids in re.findall(r'Create(?:<[^>]+>)?\("([^"]+)", "[0-9a-f]+", new\[\] \{ ([^}]+) \}',f.read_text()):
  renders[name]=re.findall('"([0-9a-f]{32})"',cids)
missing={renders[m['render']][m['mesh']] for m in charts['meshes']}-sources.keys()
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
for family in ['Game','BridgesAndPorts']:
 for p in (game/'Content'/family).glob('Blob*.cok'):
  if not missing:break
  with zipfile.ZipFile(p) as z:
   for n in z.namelist():
    if not n.endswith('.Surface.cid'):continue
    cid=z.read(n).decode().strip()
    if cid in missing:
     raw=z.read(n[:-4]);sources[cid]=dict(cid=cid,sha256=hashlib.sha256(raw).hexdigest(),template=struct.unpack_from('<i',raw,2)[0],archive=str(p),entry=n[:-4]);missing.remove(cid)
assert not missing,missing
def write(path,data):path.write_text(json.dumps(data,indent=2)+'\n')
def ref(p):return dict(path=str(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
units=json.loads((r/'artifacts/prepared-materials/Draw02/repeat-units-v2/units.json').read_text())['units']
units=[u for u in units if not u['bands']]
known=[]
aliases=json.loads((r/'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/aliases.json').read_text())['aliases']
for u in units:
 bounds=u['region']['min']+u['region']['max'];alias=next(a for a in aliases if a['unit']==u['id'])
 known.append(dict(unit=u,bounds=bounds,shape=box(*bounds),bindingKey=alias['unitKey']))
shapes=[]
for loops in charts['charts']:
 shape=Polygon()
 for loop in loops:shape=shape.symmetric_difference(Polygon(loop))
 shapes.append(shape)
sampled=collections.defaultdict(list)
for row in json.loads((b/'chart-plans.json.seams.json').read_text()):sampled[(row['render'],row['mesh'],row['domain'])].append(row)
inventory=[];groups=collections.defaultdict(list);evidence=[];used=set();counts=collections.Counter()
for m in charts['meshes']:
 name,index=m['render'],m['mesh'];source=sources[renders[name][index]];digest=source['sha256']
 nativepath=r/'artifacts/uv-density/native'/(name+'.npz')
 recipe=r/'assets/HardcodedNumbers/Draw02'/('UvDomain_'+name.replace(' ','_')+'_'+str(index)+'.bin')
 with np.load(nativepath) as mesh:
  uv=mesh[f'mesh{index}_uv0'][:,:2];faces=mesh[f'mesh{index}_faces']
  for di,face_ids in enumerate(domains(recipe)):
   identity=f'{name}|{index}|{di}';rows=sampled[(name,index,di)]
   pts=np.asarray(uv[faces[face_ids]],dtype=float).reshape(-1,2)
   low=pts.min(axis=0);high=pts.max(axis=0)
   repeat=next((u for u in known if digest==u['unit']['sourceSha256'] and np.all(low>=u['bounds'][:2]) and np.all(high<=u['bounds'][2:])),None)
   covered=0
   for row in rows:
    q=np.asarray(row['q'],dtype=np.float32).astype(float)
    if row['triangle']:
     a,bb,c=[np.asarray(row[k],dtype=np.float32).astype(float) for k in ['a','b','c']];target=a+q[:,0,None]*(bb-a)+q[:,1,None]*(c-a)
    else:
     bd=np.asarray(row['bounds'],dtype=np.float32).astype(float);target=bd[:2]+q*(bd[2:]-bd[:2])
    p=Polygon(target.astype(np.float32).astype(float))
    covered+=any(shapes[k].covers(p) for k in m['domains'][di])
   if repeat:
    kind='REPEAT_BASE';label=repeat['unit']['id'];used.add(label)
   elif rows and covered==len(rows):kind='ORIGINAL';label='verified-contained'
   else:kind='NONREPEATING';label='unobserved-widths' if not rows else 'outside-authored-chart'
   inventory.append(dict(id=identity,sourceDigest=digest,receivers=len(rows)))
   groups[(digest,kind,label)].append(identity)
   evidence.append(dict(id=identity,sourceCid=source['cid'],template=source['template'],faces=len(face_ids),bounds=[*low,*high],sampledReceivers=len(rows),containedReceivers=covered,classification=kind,unit=label if repeat else None))
   counts['beforeCovered']+=covered;counts['afterPotentialCovered']+=len(rows) if repeat else covered
write(out/'membership.json',dict(style='Draw02',nativeCharts=ref(b/'native-charts.json'),receiverPlans=ref(b/'chart-plans.json.seams.json'),
 scope='All original BBN domains in the 45-mesh all-LOD chart inventory. Receiver plans contain only changed cases delta=8/24; no claim for arbitrary widths. Zero sampled receivers is not proof of runtime reuse.',sources=list(sources.values()),domains=evidence,coverage=dict(counts)))
write(out/'original-domains.json',dict(schema='BridgeBuilder/StructureDomains/1',style='Draw02',domains=inventory))
unit_records=[]
for item in known:
 u=item['unit']
 if u['id'] not in used:continue
 unit_records.append(dict(id=u['id'],sourceDigest=u['sourceSha256'],semanticPattern='Measured original '+u['id']+' road surface; reuse the already prepared native unit only inside its measured region.',bounds=item['bounds'],physicalPeriod=u['physicalSize'],
 evidence=ref(r/'artifacts/prepared-materials/Draw02/repeat-units-v2/units.json'),mipEvidence=ref(r/'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/verification.json'),bindingKey=item['bindingKey']))
result=[]
for (digest,kind,label),ids in groups.items():
 row=dict(id=f'{digest[:12]}-{kind}-{label.rsplit("/",1)[-1]}',sourceDigest=digest,kind=kind,domains=ids,evidence=ref(out/'membership.json'))
 if kind.startswith('REPEAT'):row['unit']=label
 else:row['reason']='Every measured changed receiver is inside its exact original UV chart; native source or verified descriptor conversion retains source channels/mips. This is limited to the recorded widths.' if kind=='ORIGINAL' else ('No changed receiver is recorded for this original domain. Native repeatability and transformed containment are not established; extend actual production receiver coverage before material conversion.' if label=='unobserved-widths' else 'At least one changed receiver exits the exact authored UV chart. No genuine periodic unit is evidenced for this material island. Required implementation must preserve this original nonperiodic pattern or supply measured base/detail evidence; do not synthesize a per-triangle repeat.')
 result.append(row)
write(out/'manifest.json',dict(schema='BridgeBuilder/StructureUnits/1',style='Draw02',inventory=ref(out/'original-domains.json'),units=unit_records,groups=result))
print(json.dumps(dict(domains=len(inventory),groups=len(result),units=[u['id'] for u in unit_records],coverage=dict(counts),classified=dict(collections.Counter(x['classification'] for x in evidence)))))

