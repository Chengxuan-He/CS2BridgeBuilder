"""Emit Lift02 complete transverse truss maps from audited original components."""
from pathlib import Path
import json,struct,hashlib,sys
import numpy as np
research,repo=map(Path,sys.argv[1:]);source=repo/'artifacts/material-remap/Lift02/truss68'
out=repo/'artifacts/material-remap/Lift02/truss69';out.mkdir(parents=True,exist_ok=True)
rows=json.loads((source/'source-members.json').read_text());splits=json.loads((source/'joined-shell-source-split.json').read_text())
complete=json.loads(Path(__file__).with_name('Lift02CentralTrussMembers.json').read_text())
# Complete assembly datums: moving basal transverse frame, tower transverse frame,
# stationary lower transverse frame, original roadbed support frame, and complete
# terminal lifting frame. The terminal frame meets the inner edge at its own
# measured +/-5.1163330078125 boundaries; the roof's span is not that frame's span.
spans=[5.2056884765625,5.01171875,5.101806640625,3.96502685546875,5.1163330078125,5.1700439453125,4.9730224609375]
# Far complete assemblies use their native left support datums. Both far moving
# and terminal frames share the same measured interface; no member-local span.
names={'Lift02RoofBraces','Lift02MovingFrames','Lift02TransverseWebs','Lift02VerticalBraces','Lift02EndFrames','Lift02JointPlates','Lift02CrossMembers','Lift02HalfDiagonals','Lift02TowerBeams','Lift02TowerDiagonals','Lift02CentralHardware','Lift02DeckCrossMembers'}
groups=[];evidence=[]
for lod in range(3):
 d=np.load(research/f'lift02-main-{lod}-0.npz');v,f=d['positions'],d['indices'];sets=[set() for _ in spans];members=[[] for _ in spans]
 name='LiftBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh'
 digest=hashlib.sha256((research/'movable-skin'/(name+'.Geometry')).read_bytes()).hexdigest()
 assert all(p['geometrySha256']==digest for p in rows if p['lod']==lod)
 for p in rows:
  if p['lod']!=lod or p['recipe'] not in names:continue
  if p['recipe']=='Lift02DeckCrossMembers':g=3
  elif p['recipe']=='Lift02EndFrames':g=4
  elif p['bones']==['bone_1']:g=0
  elif p['bones']==['bone_0']:g=2 if p['maximum'][1]<10 else 1
  else:raise ValueError('Unresolved truss bone ownership')
  g=({0:5,1:6,4:5}.get(g,g) if lod==2 else g)
  sets[g].update(p['vertices']);members[g].append([p['recipe'],p['component']])
 # Original crossing-only recipes omitted independently authored half braces and
 # terminal plates. Their exact reviewed identities belong to the whole assembly,
 # including members which never reach X=0. No per-member span is introduced.
 extra=complete[lod];assert extra['lod']==lod and extra['geometrySha256']==digest
 native=json.loads((source/f'complete-native-members-lod{lod}.json').read_text())
 parts={p['component']:p for p in native['parts']}
 for g,components in enumerate(extra['componentsByAssembly']):
  for component in components:
   p=parts[component];assert p['recipe']=='RigidOrOther' and not p['trackRemoved']
   sets[g].update(p['vertices']);members[g].append(['CompleteAssemblyMember',component])
 for p in splits:
  if p['lod']!=lod:continue
  assert not p['sharedVertices'];g=0 if p['recipe']=='Lift02MovingBase' else 1
  if lod==2:g={0:5,1:6}[g]
  sets[g].update(map(int,f[p['centralFaces']].ravel()))
 seen=set()
 for g,ids in enumerate(sets):
  assert not seen.intersection(ids);seen.update(ids);ids=sorted(ids);mask=np.isin(f,ids);assert np.all(mask.any(1)==mask.all(1))
  groups.append(ids);evidence.append(dict(lod=lod,group=g,halfSpan=spans[g],vertices=len(ids),faces=int(mask.all(1).sum()),members=members[g],geometrySha256=digest))
ints=[len(x) for x in groups]+[i for x in groups for i in x]
raw=struct.pack('<8i',0x314e4242,1,len(ints),0,0,0,len(spans),0)+np.array(ints,'<i4').tobytes()+np.array(spans,'<f4').tobytes();raw+=hashlib.sha256(raw).digest()
(out/'Types_Lift02_CentralTrusses.bin').write_bytes(raw)
(repo/'assets/HardcodedNumbers/Lift02/Types_Lift02_CentralTrusses.bin').write_bytes(raw)
(out/'central-map-candidate.json').write_text(json.dumps(evidence,indent=2));print([(x['lod'],x['group'],x['vertices'],x['faces']) for x in evidence])
