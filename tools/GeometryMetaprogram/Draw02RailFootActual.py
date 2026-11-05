"""Save the actual native58 contact cross-section; no game execution."""
from pathlib import Path
import json,hashlib
import numpy as np
R=Path(__file__).resolve().parents[2];D=R/'artifacts/material-remap/shared/draw02-gap59';O=R/'artifacts/material-remap/Draw02/gap59'
rows=[]
for item in json.loads((D/'attributes/index.json').read_text()):
 names=[b['prefab'] for b in item['bindings'] if b['prefab'].startswith('Mechanism') and 'LOD' not in b['prefab']]
 if not names:continue
 path=D/'attributes'/f"{item['cid']}.attributes.json";data=json.loads(path.read_text())
 for slot,m in enumerate(data['meshes']):
  v=np.array(m['positions']);q=v[np.array(m['faces'])]
  selected=np.flatnonzero((q[:,:,2].min(1)<46)&(q[:,:,2].max(1)>46)&(q[:,:,0].max(1)>9.8)&(q[:,:,0].min(1)<11)&(q[:,:,1].max(1)>9.69)&(q[:,:,1].min(1)<10.01))
  for face in selected:
   points=q[face];hits=[]
   for a,b in zip(points,np.roll(points,-1,axis=0)):
    if (a[2]-46)*(b[2]-46)<0:hits.append((a+(b-a)*(46-a[2])/(b[2]-a[2]))[:2].tolist())
   if hits:rows.append(dict(prefab=names[0],geometry=item['cid'],slot=slot,face=int(face),sectionXY=hits,positions=points.tolist()))
report=dict(owner='bc7fe7295-2333-4954-9f2c-20d4281d75c9',build='58',source='Large Road Divided',z=46,mainWallTop=[10.14599609375,9.699951171875],railInnerFoot=[10.0460205078125,10],railOuterInnerFoot=[10.4539794921875,9.85009765625],mainSlabInner=[10.553955078125,9.85009765625],horizontalContactDrift=0.0999755859375,missingOriginalWallHeight=0.300048828125,rows=rows)
(O/'actual58-section.json').write_text(json.dumps(report,indent=2));print(len(rows))
