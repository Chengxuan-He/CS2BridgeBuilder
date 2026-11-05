from pathlib import Path
import numpy as np,json
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/underside52'
for lod,cid in enumerate(['792796a81ca9354398b7ff63a93386d8','1583c7b680b9a0d3de9810c6ec409e7b']):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];uv=d['mesh0_uv'];f=d['mesh0_indices'].reshape(-1,3);members=json.loads((O/f'lod{lod}-center-members.json').read_text())
 a=json.loads((R/f'artifacts/material-remap/shared/draw02-layout50/attributes/{cid}.attributes.json').read_text());m=a['meshes'][0];print(m.keys());p=np.array(m['positions']);q=np.array(m['uv0']);t=np.array(m['faces']);lookup={}
 def key(points,uvs):return tuple(sorted(tuple(x) for x in np.column_stack((points[:,1:],uvs[:,:2]))))
 for face in t:lookup.setdefault(key(p[face],q[face]),[]).append(face)
 rows=[]
 for fi in members['faces']:
  face=f[fi];matches=lookup.get(key(v[face],uv[face]),[])
  if len(matches)==1:
   g=matches[0];rows.append(dict(sourceFace=fi,sourceX=v[face,0].tolist(),actualX=p[g,0].tolist()))
 print('actual matching centerfaces',lod,len(rows),'cross-source examples',[r for r in rows if min(r['sourceX'])<0<max(r['sourceX'])][:2]);(O/f'actual50-center-lod{lod}.json').write_text(json.dumps(rows,indent=2))
from MovableUvDomainFormat import decode_arrays,read_bbn
v,_=read_bbn((R/'assets/HardcodedNumbers/Draw02/Draw02_UvSampling.bin').read_bytes());at=2;flags=[]
for i in range(v[1]):n=v[at];at+=1;flags.append(v[at:at+n]);at+=n
for lod in range(2):
 name='DrawBridge02'+('_LOD1' if lod else '')+'_Mesh_0'
 ints,_=decode_arrays(*read_bbn((R/f'assets/HardcodedNumbers/Draw02/UvDomain_{name}.bin').read_bytes()));at=2;walks=json.loads((O/f'lod{lod}-walk-members.json').read_text());selected={f for w in walks for f in w['faces']};covered=set();rows=[]
 for domain in range(ints[1]):
  n=abs(ints[at+3]);at+=4;faces=set(ints[at:at+n].tolist());at+=n;hit=faces&selected
  if hit:covered.update(hit);rows.append(dict(domain=int(domain),faces=sorted(hit),retainedAxes=int(flags[lod*2][domain])*3))
 print('walk domains',lod,'covered',len(covered),'retained',[r['retainedAxes'] for r in rows], 'missing',len(selected-covered));(O/f'walk-sampling-lod{lod}.json').write_text(json.dumps(rows,indent=2))
