"""Embed actual near road atlas slabs; never infer the simplified middle asphalt."""
import hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
d=json.loads((ROOT/'artifacts/material-remap/Draw01/native-atlas-road/recipe.json').read_text())
names=['asphalt','grating','fixed-sidewalk','fixed-carriageway']
values=[]
for r in d['lods'][0]['domains']:
 for c in r['cells']:
  assert all(a['feasible'] and a['float32CoefficientsInsideRoundingCells'] and a['float32EvaluationInsideRoundingCells'] for a in c['affineAxes'])
  x=[p[0] for p in c['positions']]
  values.extend([names.index(r['domain']),int(r['right']),int(r['positive']),*c['zRange'],min(x),max(x),*c['uvMin'],*c['uvMax'],*c['affineAxes'][0]['affineXz1'],*c['affineAxes'][1]['affineXz1']])
assert len(values)%17==0
raw=struct.pack('<4s7i',b'BBN1',1,0,0,0,0,len(values),0)+struct.pack('<'+'f'*len(values),*values)
p=ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_NativeAtlasRoadMeasurements.bin'
p.write_bytes(raw+hashlib.sha256(raw).digest())
print(len(values)//17,'measured cells',len(raw)+32,'bytes')
