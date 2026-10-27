import gzip,json,sys,collections
from pathlib import Path
import numpy as np
root=Path.cwd();sys.path.insert(0,str(root/'tools/PreparedMaterials/PedestrianDraw02'))
from BuildUvSampling import overlaps
base=root/'artifacts/prepared-materials/PedestrianDraw02'; audit=json.loads((base/'non-tiling/classification.json').read_text());charts=json.loads((base/'source-consolidation/native-charts.json').read_text());native=np.load(base/'source-consolidation/native-charts.npz');region=audit['genuineRepeatRegion']
p=root/'artifacts/prepared-materials/shared-nontiling/actual-final/styles'; lines=[json.loads(x) for x in gzip.open(p/'PedestrianDraw02.regions.json.classifications.ndjson.gz','rt')]; remaining=[r for r in lines if r['status']!='original']; print('status',collections.Counter(r['status'] for r in lines));print('remaining',len(remaining));print('stream',collections.Counter((r['render'],r['mesh']) for r in remaining));print('delta',collections.Counter(r['delta'] for r in remaining))
reasons=collections.Counter();precise=collections.Counter();details={}
for r in remaining:
    record=next(x for x in audit['meshes'] if (x['render'],x['mesh'])==(r['render'],r['mesh']));reason=next(k for k,v in record['classifications'].items() if r['originalDomain'] in v);reasons[reason]+=1
    nr=next(x for x in charts['records'] if (x['name'],x['mesh'])==(r['render'],r['mesh']));prefix=nr['arrayPrefix'];uv=native[prefix+'_uv'];f=native[prefix+'_faces'];members=[pair[0] for pair in r['sourceFaces']];tri=uv[f[members]].astype(float);hits=[overlaps(t,region) for t in tri];key='source-face-repeat-overlap' if any(hits) else 'chart-neighbor-only-repeat-exclusion';precise[key]+=1;details[(r['render'],r['originalDomain'])]=(key,members,tri.reshape(-1,2).min(0).tolist(),tri.reshape(-1,2).max(0).tolist())
print('flagReasons',reasons);print('actualSourceFaces',precise);print('unique',len(details),collections.Counter(x[0] for x in details.values()));print('details',json.dumps([dict(render=k[0],domain=k[1],reason=v[0],faces=v[1],uvMin=v[2],uvMax=v[3]) for k,v in details.items()],indent=2))
