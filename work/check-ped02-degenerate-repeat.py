import json,numpy as np
from pathlib import Path
base=Path('artifacts/prepared-materials/PedestrianDraw02')
r=json.loads((base/'non-tiling/classification.json').read_text()); n=json.loads((base/'source-consolidation/native-charts.json').read_text()); a=np.load(base/'source-consolidation/native-charts.npz');lo=np.array(r['genuineRepeatRegion'][:2]);hi=np.array(r['genuineRepeatRegion'][2:]); counts=[]
for rec in r['meshes']:
    if rec['sourceDigest']!='5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219':continue
    nat=next(t for t in n['records'] if (t['name'],t['mesh'])==(rec['render'],rec['mesh']));prefix=nat['arrayPrefix'];uv=a[prefix+'_uv'];faces=a[prefix+'_faces']
    for c in rec['nativeIslands']:
        t=uv[faces[c['faces']]].astype(float);area=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);inside=((t>lo)&(t<hi)).all(axis=2).any(axis=1)
        if np.any((area==0)&inside):counts.append((rec['render'],c['chart']))
print('Accepted charts with degenerate source UV in repeat interior:',counts)
