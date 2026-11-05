"""Emit reviewed Draw02 central girder and moving sidewalk underside membership."""
from pathlib import Path
import json,struct,hashlib,numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/underside52'
ints=[0]*32;floats=[3.5,-9.5,6.0]
for lod in range(2):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 center=json.loads((O/f'lod{lod}-center-members.json').read_text());walks=json.loads((O/f'lod{lod}-walk-members.json').read_text())
 joins=json.loads((R/f'artifacts/material-remap/Draw02/edge55/lod{lod}-center-joins.json').read_text())
 fixed=sorted(set(center['vertices']).union(joins['vertices']))
 row=[len(v),len(f),len(ints),len(fixed)];ints.extend(fixed)
 for walk in walks:
  vertices=walk['vertices'];faces=walk['faces'];assert set(np.flatnonzero(np.isin(f,vertices).any(1)))==set(faces)
  row.extend([len(ints),len(faces)]);ints.extend(faces);row.extend([len(ints),len(vertices),len(floats)]);ints.extend(vertices)
  minimum=-9.5 if walk['side']<0 else 6.
  floats.extend(((v[vertices,0]-np.float32(minimum))/np.float32(3.5)).tolist())
 outer=json.loads((R/f'artifacts/material-remap/Draw02/edge55/lod{lod}-outer-members.json').read_text())['removedFaces']
 row.extend([len(ints),len(outer)]);ints.extend(outer)
 assert len(row)==16;ints[lod*16:(lod+1)*16]=row
body=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+np.array(ints,dtype='<i4').tobytes()+np.array(floats,dtype='<f4').tobytes();body+=hashlib.sha256(body).digest()
p=R/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02Underside_cs.bin';p.write_bytes(body);print(len(body),hashlib.sha256(body).hexdigest())
