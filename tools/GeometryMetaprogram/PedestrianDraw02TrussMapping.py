"""Emit measured pedestrian02 truss memberships. Usage: script ROOT RESEARCH EVIDENCE_DIRECTORY."""
from pathlib import Path
import json,numpy as np,hashlib,struct,sys
root=Path(sys.argv[1]);r=Path(sys.argv[2]); output=Path(sys.argv[3]);output.mkdir(parents=True,exist_ok=True)
ints=[];floats=[];evidence=[]
for lod in range(2):
 p=json.loads((r/f'pedestrian02-main-{lod}-parts.json').read_text());v=np.load(r/f'pedestrian02-main-{lod}.npz')['positions']
 groups=[i for i,q in enumerate(p) if q['minimum'][1]>=7.5 and q['maximum'][1]<=8.6 and q['minimum'][0]>=-2.55 and q['maximum'][0]<=2.55]
 ribs=[i for i,q in enumerate(p) if q['minimum'][1]==9.49658203125 and q['maximum'][1]==9.699951171875 and q['maximum'][2]-q['minimum'][2]>50]
 assert len(groups)==240 and len(ribs)==14
 a=sorted(i for g in groups for i in p[g]['vertices']);b=[];w=[]
 for g in ribs:
  q=p[g]; centre=(q['minimum'][0]+q['maximum'][0])/2; weight=round(centre)/4
  b+=q['vertices'];w += [weight]*len(q['vertices'])
 assert len(set(a))==len(a) and len(set(b))==len(b) and not set(a)&set(b)
 lo=float(v[a,0].min());hi=float(v[a,0].max());assert lo==-hi
 ints += [len(v),len(a),len(b)]+a+b;floats += [hi]+w
 evidence.append(dict(lod=lod,assemblyParts=groups,ribParts=ribs,assemblyVertices=len(a),ribVertices=len(b),span=[lo,hi],sourceHash=hashlib.sha256((r/f'pedestrian02-main-{lod}.npz').read_bytes()).hexdigest()))
b=struct.pack('<8i',0x314e4242,1,len(ints),0,0,0,len(floats),0)+struct.pack('<%di'%len(ints),*ints)+struct.pack('<%df'%len(floats),*floats);b+=hashlib.sha256(b).digest()
out=root/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_PedestrianDraw02TrussMapping_cs.bin';out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(b)
(output/'membership.json').write_text(json.dumps(evidence,indent=2))
print([(e['assemblyVertices'],e['ribVertices'],e['span']) for e in evidence])
