from pathlib import Path
import numpy as np,json
from MovableUvDomainFormat import decode_arrays,read_bbn
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/closure53';v,_=read_bbn((R/'assets/HardcodedNumbers/Draw02/Draw02_UvSampling.bin').read_bytes());at=2;flags=[]
for i in range(v[1]):n=v[at];at+=1;flags.append(v[at:at+n]);at+=n
for lod in range(2):
 name='DrawBridge02'+('_LOD1' if lod else '')+'_Mesh_0';ints,_=decode_arrays(*read_bbn((R/f'assets/HardcodedNumbers/Draw02/UvDomain_{name}.bin').read_bytes()));at=2;selected=set(json.loads((O/f'lod{lod}-outer-members.json').read_text())['removedFaces']);covered=set();rows=[]
 for domain in range(ints[1]):
  n=abs(ints[at+3]);at+=4;faces=set(ints[at:at+n].tolist());at+=n;hit=faces&selected
  if hit:covered.update(hit);rows.append(dict(domain=int(domain),faces=sorted(hit),retainedAxes=int(flags[lod*2][domain])*3))
 assert covered==selected
 print(lod,'domains',len(rows),'axes',sorted(set(r['retainedAxes'] for r in rows)))
 (O/f'outer-sampling-lod{lod}.json').write_text(json.dumps(rows,indent=2))
