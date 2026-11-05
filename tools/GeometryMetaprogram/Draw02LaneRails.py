"""Emit exact original Draw02 low/high rail membership, never inferred at runtime."""
from pathlib import Path
import json,struct,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
DIR=ROOT/'artifacts/material-remap/Draw02/lane-rails'
low=json.loads((DIR/'original-rail-components.json').read_text())
high=json.loads((DIR/'original-high-separators.json').read_text())
records=[]
for lod in range(2):
 for leaf in (-1,1):
  part=next(c for c in low[lod]['candidates'] if c['nativeLowSeparator'] and c['minimum'][0]<0<c['maximum'][0] and (c['maximum'][2]>0)==(leaf>0))
  records.append(dict(part,sourceLod=lod,anchorX=0.,leaf=leaf,kind='Low',side=0))
 for side in (-1,1):
  for leaf in (-1,1):
   part=next(c for c in high if c['lod']==lod and c['side']==side and c['leaf']==leaf)
   # Align the original car-facing edge, keeping the complete rail outside the lane.
   anchor=part['minimum'][0] if side>0 else part['maximum'][0]
   records.append(dict(part,sourceLod=lod,kind='High',anchorX=anchor))
ints=[0]*(len(records)*4);floats=[]
for i,r in enumerate(records):
 d=np.load(ROOT/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{r["sourceLod"]}-native.npz')
 f=d['mesh0_indices'].reshape(-1,3);ids=np.unique(f[r['faces']]);bones=np.unique(d['mesh0_bones'][ids])
 assert bones.tolist()==[1 if r['leaf']>0 else 2]
 ints[i*4:i*4+4]=[len(d['mesh0_positions']),len(f),len(ints),len(r['faces'])]
 ints.extend(r['faces']);floats.extend([r['anchorX'],r['minimum'][2],r['maximum'][2]])
 r['bone']=int(bones[0]);r['sourceSha256']=hashlib.sha256((ROOT/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{r["sourceLod"]}-native.npz').read_bytes()).hexdigest()
blob=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+struct.pack(f'<{len(ints)}i',*ints)+struct.pack(f'<{len(floats)}f',*floats)
blob+=hashlib.sha256(blob).digest()
path=ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02LaneRails_cs.bin';path.write_bytes(blob)
(DIR/'rail-runtime-records.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(len(records),'rail records;',len(blob),'bytes;',hashlib.sha256(blob).hexdigest())
