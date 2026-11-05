"""Measure one real original rail/road contact plane on existing upper-wall faces."""
from pathlib import Path
import json,struct,hashlib
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'artifacts/material-remap/Draw02/gap59'
records=json.loads((O/'source-members.json').read_text());out=[]
height=np.float32(9.699951171875)
for rec in records:
 lod=rec['lod'];d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3)
 # Existing authored under-road base/upper-closure boundary, below the car surface.
 assert np.any((np.abs(v[:,0])==np.float32(5.800048828125))&(v[:,1]==height))
 edges=[];lookup={};fragments=[]
 def vertex(a,b,upper):
  if a>b:a,b=b,a
  t=np.float32((float(height)-float(v[a,1]))/(float(v[b,1])-float(v[a,1])))
  key=(int(a),int(b),float(t),upper)
  if key not in lookup:lookup[key]=len(edges);edges.append(dict(a=int(a),b=int(b),fraction=float(t),upper=upper))
  return -lookup[key]-1
 for face in rec['wallFaces']:
  ids=f[face].tolist();tri=[]
  for upper in (False,True):
   poly=[]
   for a,b in zip(ids,ids[1:]+ids[:1]):
    da=float(v[a,1]-height);db=float(v[b,1]-height)
    if (da>=0)==upper:poly.append(a)
    if da*db<0:poly.append(vertex(a,b,upper))
   for i in range(1,len(poly)-1):tri.extend([poly[0],poly[i],poly[i+1]])
  assert len(tri)==9
  fragments.append(dict(face=face,indices=tri))
 out.append(dict(lod=lod,side=rec['side'],height=float(height),edges=edges,fragments=fragments))
(O/'road-contact-split.json').write_text(json.dumps(out,indent=2));print([(x['lod'],x['side'],len(x['edges']),len(x['fragments'])) for x in out])
ints=[0]*16;floats=[float(height)]
for n,rec in enumerate(out):
 row=[len(ints),len(rec['edges'])]
 for e in rec['edges']:
  ints.extend([e['a'],e['b'],len(floats),int(e['upper'])]);floats.append(e['fraction'])
 row.extend([len(ints),len(rec['fragments'])])
 for f in rec['fragments']:ints.extend([f['face'],len(f['indices'])]+f['indices'])
 ints[n*4:n*4+4]=row
blob=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+np.array(ints,dtype='<i4').tobytes()+np.array(floats,dtype='<f4').tobytes();blob+=hashlib.sha256(blob).digest()
(R/'assets/HardcodedNumbers/Draw02/Types_Draw02_RailFootContactSplitMeasurements.bin').write_bytes(blob)
print(len(blob),hashlib.sha256(blob).hexdigest())
