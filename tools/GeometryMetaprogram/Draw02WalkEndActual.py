"""Identify the asymmetric missing original slab end plates in native 60r4 output."""
from pathlib import Path
import json,struct,numpy as np
R=Path(__file__).resolve().parents[2];A=R/'artifacts/material-remap/shared/draw02-gap61';O=R/'artifacts/material-remap/Draw02/gap61'
rows=[]
for lod in (0,1):
 source=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=source['mesh0_positions'];f=source['mesh0_indices']
 b=(R/f'artifacts/material-remap/Draw02/endcap60/after60-lod{lod}-delta4-case5.bin').read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3);mask=np.frombuffer(b,'?',nf,12+nv*12)
 actual=set()
 for item in json.loads((A/'attributes/index.json').read_text()):
  names=[x['prefab'] for x in item['bindings'] if x['prefab'].startswith('Mechanism') and (('LOD' not in x['prefab']) if lod==0 else f'LOD{lod}' in x['prefab'])]
  if not names:continue
  for mesh in json.loads((A/f'attributes/{item["cid"]}.attributes.json').read_text())['meshes']:
   q=np.asarray(mesh['positions'])[np.asarray(mesh['faces'])]
   actual.update(tuple(sorted(map(tuple,t))) for t in q)
 for walk in json.loads((R/f'artifacts/material-remap/Draw02/underside52/lod{lod}-walk-members.json').read_text()):
  for face in walk['endCapFaces']:
   q=v[f[face]];normal=np.cross(q[1]-q[0],q[2]-q[0]);found=tuple(sorted(map(tuple,p[f[face]]))) in actual
   assert found==bool(not mask[face])
   rows.append(dict(lod=lod,side=walk['side'],sourceFace=face,nativeOutputPresent=found,sourceNormal=normal.tolist(),sourceBones=source['mesh0_bones'][f[face]].reshape(-1).tolist()))
missing=[x for x in rows if not x['nativeOutputPresent']]
assert [x['sourceFace'] for x in missing if x['lod']==0]==[8764,8765,19722,19723]
assert [x['sourceFace'] for x in missing if x['lod']==1]==[5469,12044]
assert all(x['side']==-1 and x['sourceNormal'][1]>0 for x in missing)
result=dict(owner='bc5d09bce-1ecb-4aa7-bdc9-c366a3917d1a',source='Large Road Divided',sourceCaps=rows,
            cause='Measured vertical slab end plates were excluded by a near-zero normal.Y sign, then removed as road.',
            farActualCapY=9.800048828125,farActualSidewalkY=10.000020980834961,originalSidewalkContactY=10,
            originalVsActualSidewalkQuantizationDifference=10.000020980834961-10)
(O/'actual60r4-source-evidence.json').write_text(json.dumps(result,indent=2));print('actual source caps',len(rows),'missing',len(missing))
