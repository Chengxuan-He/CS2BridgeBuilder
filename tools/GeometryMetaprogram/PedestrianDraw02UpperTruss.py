"""Emit complete upper PedestrianDraw02 trusses. ROOT RESEARCH OUTPUT arguments."""
import sys,json,hashlib,struct,numpy as np
from pathlib import Path
root,r,out=map(Path,sys.argv[1:]);out.mkdir(parents=True,exist_ok=True);ints=[];floats=[];record=[]
for lod in range(2):
 d=np.load(r/f'pedestrian02-main-{lod}.npz');v=d['positions'];faces=d['indices'];parts=json.loads((r/f'pedestrian02-main-{lod}-parts.json').read_text())
 # Three repeated full overhead X bays on each side, including all connection plates/collars.
 selected=[i for i,q in enumerate(parts) if q['minimum'][1]>=27.5 and q['maximum'][1]<=29.91 and q['minimum'][0]>=-4.5 and q['maximum'][0]<=4.5]
 assert len(selected)==120
 ids=sorted(i for p in selected for i in parts[p]['vertices']);assert len(ids)==len(set(ids));lo=float(v[ids,0].min());hi=float(v[ids,0].max());assert lo==-hi
 hit=np.isin(faces,ids).sum(1);assert not np.any((hit>0)&(hit<3));ff=np.flatnonzero(hit==3).tolist()
 ints +=[len(v),len(ids)]+ids;floats += [hi]
 record.append(dict(lod=lod,parts=selected,vertices=ids,faces=ff,halfspan=hi,sourceHash=hashlib.sha256((r/f'pedestrian02-main-{lod}.npz').read_bytes()).hexdigest()))
b=struct.pack('<8i',0x314e4242,1,len(ints),0,0,0,len(floats),0)+struct.pack('<%di'%len(ints),*ints)+struct.pack('<%df'%len(floats),*floats);b+=hashlib.sha256(b).digest()
p=root/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_PedestrianDraw02UpperTruss_cs.bin';p.write_bytes(b);(out/'upper-membership.json').write_text(json.dumps(record,indent=2))
print([(x['lod'],len(x['vertices']),len(x['faces']),x['halfspan']) for x in record])
