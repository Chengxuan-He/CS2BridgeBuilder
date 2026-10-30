"""Embed measured finite far charts, after binary16 rounding-cell validation."""
import hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01'
a=json.loads((OUT/'far-appearance.json').read_text());b=json.loads((OUT/'far-half-precision.json').read_text())
rows={r['group']:r for r in b['rows'] if r['domain']=='whole'}
f=[]
for r in a['groups']:
 axes=rows[r['group']]['axes'];assert all(v['feasible'] and v['float32CoefficientsInsideRoundingCells'] and v['float32EvaluationInsideRoundingCells'] for v in axes)
 f.extend(axes[0]['affineXz1']+axes[1]['affineXz1'])
 f.extend([r['sourceBounds'][0][0],r['sourceBounds'][1][0],r['sourceBounds'][0][2],r['sourceBounds'][1][2]])
 f.extend(r['uvBounds'][0]+r['uvBounds'][1])
# Existing measured high-detail motion boundaries; source references retained in audit.
p=ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01RoadPartitions_cs.bin'
s=p.read_bytes();counts=struct.unpack_from('<6i',s,8);offset=32+sum(c*z for c,z in zip(counts[:4],[4,4,8,8]));near=struct.unpack_from('<'+'f'*counts[4],s,offset)
f.extend([near[0],near[23],near[22],near[21],near[29],near[28],near[27]])
raw=struct.pack('<4s7i',b'BBN1',1,0,0,0,0,len(f),0)+struct.pack('<'+'f'*len(f),*f)
path=ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_FarAppearanceMeasurements.bin'
path.write_bytes(raw+hashlib.sha256(raw).digest())
print(path,len(f))
