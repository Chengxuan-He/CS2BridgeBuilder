"""Match original source-cap recipes against both actual installed-59r2 owners."""
from pathlib import Path
import json,struct
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/endcap60'
recipes=json.loads((O/'source-recipe.json').read_text());report=[]
def read(path):
 b=path.read_bytes();nv,nf,nr=struct.unpack_from('<3i',b);p=np.frombuffer(b,'<f4',nv*3,12).reshape(-1,3);off=12+nv*12;m=np.frombuffer(b,'?',nf,off);off+=nf;fr={}
 for _ in range(nr):
  face,n=struct.unpack_from('<2i',b,off);off+=8;fr[face]=np.frombuffer(b,'<i4',n,off).tolist();off+=n*4
 return p,m,fr
def key(triangle):return tuple(sorted(tuple(x) for x in triangle))
for folder,delta,scenario in [('latest-divided','4',5),('previous-custom','5.75',1)]:
 root=R/f'artifacts/material-remap/shared/draw02-gap60/{folder}';meta=json.loads((root/'actual-root-section.json').read_text())
 for lod in range(3):
  actual=set()
  for item in json.loads((root/'attributes/index.json').read_text()):
   names=[b['prefab'] for b in item['bindings'] if b['prefab'].startswith('Mechanism') and (('LOD' not in b['prefab']) if lod==0 else f'LOD{lod}' in b['prefab'])]
   if not names:continue
   data=json.loads((root/f'attributes/{item["cid"]}.attributes.json').read_text())
   for mesh in data['meshes']:
    v=np.asarray(mesh['positions']);actual.update(key(t) for t in v[np.asarray(mesh['faces'])])
  src=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');faces=src['mesh0_indices'].reshape(-1,3)
  p,m,fr=read(O/f'baseline60-lod{lod}-delta{delta}-case{scenario}.bin')
  relevant=[int(face) for r in recipes if r['lod']==lod for face,kind in r['faceKinds'].items() if kind!='inner']
  matched=0;missing=[];triangles=0
  for face in relevant:
   ids=fr.get(face,faces[face].tolist() if not m[face] else [])
   if not ids:missing.append(face);continue
   tris=p[ids].reshape(-1,3,3);triangles+=len(tris)
   if all(key(t) in actual for t in tris):matched+=1
   else:missing.append(face)
  report.append(dict(owner=meta['owner'],folder=folder,lod=lod,sourceCaps=len(relevant),matchedCurrentClippedOrWholeCaps=matched,currentTriangles=triangles,unmatched=missing))
(O/'actual59r2-cap-match.json').write_text(json.dumps(report,indent=2));print(report)
