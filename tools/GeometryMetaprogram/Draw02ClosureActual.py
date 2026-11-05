import json,numpy as np
from pathlib import Path
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/closure53';rows=json.loads((R/'artifacts/material-remap/shared/draw02-structure52/mechanism-bindings.json').read_text());report=[]
for lod in range(3):
 found=set();target=[]
 for r in rows:
  name=r['bindings'][0]['prefab'];level=2 if 'LOD2' in name else 1 if 'LOD1' in name else 0
  if lod!=level:continue
  a=json.loads(Path(r['file']).read_text());target.append(name)
  for m in a['meshes']:
   p=np.array(m['positions'],dtype=np.float32);f=np.array(m['faces']);found.update(tuple(sorted(tuple(x) for x in p[t])) for t in f)
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'].copy();v[:,0]+=np.sign(v[:,0])*np.float32(4);f=d['mesh0_indices'].reshape(-1,3);m=json.loads((O/f'lod{lod}-outer-members.json').read_text());present=[i for i in m['faces'] if tuple(sorted(tuple(x) for x in v[f[i]])) in found];missing=sorted(set(m['faces'])-set(present));removedPresent=set(present)&set(m['removedFaces']);print(lod,len(m['faces']),len(present),len(missing),'deleted but inactual',len(removedPresent));report.append(dict(lod=lod+1,groups=target,sourceOuterFaces=len(m['faces']),exactOriginalTrianglesPresent=len(present),missing=missing,previouslyDeletedButPresent=sorted(removedPresent)))
(O/'actual52-outer-coverage.json').write_text(json.dumps(report,indent=2))
