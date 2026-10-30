"""Measured road units and continuous phase components; no baking or runtime cuts."""
import copy,hashlib,json
from collections import defaultdict,deque
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift05/road-continuous-candidate'

def main():
 plan=json.loads((OUT.parent/'road-projection-plan.json').read_text());boundary=json.loads((OUT.parent/'road-projection-boundaries.json').read_text())
 source=ROOT/'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism'
 template=json.loads((OUT.parent/'offline-repeat-candidate/manifest.json').read_text())['units'][0]
 keys={};units=[];records=[]
 for level,edges in zip(plan['levels'],boundary['levels']):
  lod=level['lod'];patches=level['patches'];faces={f['id']:f for f in json.loads((source/f'road-lod{lod}.json').read_text())['faces']}
  memberships={f:i for i,p in enumerate(patches) for f in p['faces']};pk={};proofs=[]
  for i,p in enumerate(patches):
   if lod==2 or p['sourceMax'][2]-p['sourceMin'][2]!=4:continue
   lo=np.array(p['uvMin']);hi=np.array(p['uvMax']);area=0
   for f in p['faces']:
    uv=np.array(faces[f]['uv']);area+=abs(np.linalg.det(np.array([uv[1]-uv[0],uv[2]-uv[0]])))/2
   # Only a complete native rectangle, not a bounding crop of a partial triangle.
   bound=64*np.finfo(float).eps*max(1,float(np.prod(hi-lo)))
   if abs(area-np.prod(hi-lo))>bound:continue
   k=tuple(np.r_[lo,hi]);pk[i]=k
   if k not in keys:
    uid='Lift05.Road.NativeUnit'+str(len(keys));keys[k]=uid
    u=copy.deepcopy(template);u.update(id=uid,min=lo.tolist(),max=hi.tolist(),physicalSize=[p['sourceMax'][0]-p['sourceMin'][0],4],originalFaces=p['faces'],repeatAxes='V',regions=[],aliases=[],measuredTexelsPerMetre=[None,float((hi[1]-lo[1])*4096/4)],runtimeTexelsPerMetre=[None,float((hi[1]-lo[1])*4096/4)],evidence={'path':str(OUT/'mapping-evidence.json')},status='MEASURED_CANDIDATE_NOT_BAKED_NOT_QUALITY_ACCEPTED')
    units.append(u)
  # Include complete rectangles with same exact bounds even where triangulation has split them.
  for i,p in enumerate(patches):
   k=tuple(p['uvMin']+p['uvMax'])
   if lod<2 and k in keys and p['sourceMax'][2]-p['sourceMin'][2]==4:pk[i]=k
  adj=defaultdict(list);reset_evidence=[];retained=[]
  for edge in edges['necessaryProjectionBoundaries']:
   a,b=[memberships[f] for f in edge['faces']]
   if a not in pk or b not in pk or pk[a]!=pk[b]:continue
   k=pk[a];span=np.array(k[2:])-k[:2];ca=np.array(patches[a]['coefficients'])/span;cb=np.array(patches[b]['coefficients'])/span
   diff=ca-cb;shift=np.rint(diff[2]);noise=128*np.finfo(float).eps*max(1,float(np.max(abs(ca))),float(np.max(abs(cb))))
   if np.max(abs(diff[:2]))<=noise and np.max(abs(diff[2]-shift))<=noise:
    adj[a].append((b,shift));adj[b].append((a,-shift));reset_evidence.append(dict(patches=[a,b],sourceFaces=edge['faces'],physicalEdge=edge['edge'],integerPhaseDifference=shift.tolist(),arithmeticBound=noise))
   else:retained.append(dict(patches=[a,b],sourceFaces=edge['faces'],physicalEdge=edge['edge'],gradientDifference=diff[:2].tolist(),integerResidual=(diff[2]-shift).tolist(),arithmeticBound=noise,reason='Stored source gradients cannot be merged by arithmetic equivalence. No new texture tolerance assumed.'))
   # Evidence for native U resets is separate from whether their quantized gradients merge.
   if abs(shift[0])==1 and edge['kind']=='reset':
    next(u for u in units if u['id']==keys[k])['repeatAxes']='UV'
  phase={};components=[]
  for start in sorted(pk):
   if start in phase:continue
   phase[start]=np.zeros(2);q=deque([start]);members=[]
   while q:
    a=q.popleft();members.append(a)
    for b,s in adj[a]:
     wanted=phase[a]+s
     if b in phase:assert np.array_equal(phase[b],wanted)
     else:phase[b]=wanted;q.append(b)
   components.append(members)
  output=[]
  for i,p in enumerate(patches):
   item=dict(patch=i,faces=p['faces'],mode='original-finite',coefficients=p['coefficients'])
   if i in pk:
    k=pk[i];span=np.array(k[2:])-k[:2];c=np.array(p['coefficients'])/span;c[2]-=np.array(k[:2])/span;c[2]+=phase[i]
    item.update(mode='prepared-continuous',unit=keys[k],unitCoefficients=c.tolist(),integerPhase=phase[i].tolist())
   output.append(item)
  records.append(dict(lod=lod,patchCount=len(patches),fullUnitPatches=len(pk),finitePatches=len(patches)-len(pk),continuousComponents=components,removedPeriodBoundaries=reset_evidence,retainedGradientBoundaries=retained,patches=output))
 OUT.mkdir(parents=True,exist_ok=True)
 evidence=dict(levels=records,sourceSurface=template['sourceCid'],sourceDigest=template['sourceDigest'],selectedTramGateEvidence=str(OUT.parent/'tram-input-layers.json'),distinction='The six-channel Tram network audit establishes whole-Asphalt receiver eligibility. Replacement pixels come from the prototype Lift05 road atlas, not the selected Tram six-channel material.',finitePolicy='Finite terminal patches and far LOD retain original Surface and original measured affine sampling. No finite or full-span raster is created.',quality='No new bake; source-grid boundary filtering remains unresolved. No tolerance relaxed.')
 (OUT/'mapping-evidence.json').write_text(json.dumps(evidence,indent=2)+'\n');digest=hashlib.sha256((OUT/'mapping-evidence.json').read_bytes()).hexdigest()
 for u in units:u['evidence']['sha256']=digest
 (OUT/'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3',style='Lift05',units=units),indent=2)+'\n')
 print([(l['lod'],l['fullUnitPatches'],l['finitePatches'],len(l['continuousComponents']),len(l['removedPeriodBoundaries'])) for l in records]);print([(u['id'],u['min'],u['max'],u['repeatAxes']) for u in units])
if __name__=='__main__':main()
