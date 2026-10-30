"""Find actual translated LOD1 asphalt facets with identical native frame fields."""
import ast,itertools,json,hashlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
tree=ast.parse(Path(__file__).with_name('AuditFarPrecision.py').read_text())
fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='feasible')
exec(compile(ast.Module(body=[fn],type_ignores=[]),'<rounding-cells>','exec'))
recipe=json.loads((OUT/'recipe.json').read_text())
mesh=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz')
p=mesh['mesh0_positions'].astype(float);uv=mesh['mesh0_uv0'].astype(float);f=mesh['mesh0_faces']
t=np.load(OUT/'middle-tangent-bits.npy')
rows=[]
for r in recipe['lods'][1]['domains']:
    if r['domain']!='asphalt':continue
    ids=sorted({i for c in r['cells'] for i in c['faces']})
    sign=1 if r['positive'] else -1
    matches=[]
    for a,b in itertools.permutations(ids,2):
        pa=p[f[a]].copy();pb=p[f[b]].copy();pb[:,2]-=8*sign
        lookup={tuple(x):i for i,x in enumerate(pb)}
        if not all(tuple(x) in lookup for x in pa):continue
        order=[lookup[tuple(x)] for x in pa]
        tb=t[f[b]][order];ua=uv[f[a]];ub=uv[f[b]][order]
        local=pa.copy();local[:,2]-=local[:,2].min()
        axes=feasible(np.vstack([local,local]),np.vstack([ua,ub]))
        compatible=all(x['feasible'] and x['float32CoefficientsInsideRoundingCells'] and x['float32EvaluationInsideRoundingCells'] for x in axes)
        matches.append({'sourceFace':a,'translatedFace':b,'zRange':sorted((pa[:,2]*sign).tolist()),'translationMeters':8,'identicalPackedTangents':bool(np.array_equal(t[f[a]],tb)), 'constantPackedTangent':len(set(t[f[a]].tolist()))==1,'packedTangents':t[f[a]].astype(int).tolist(),'binary16CompatibleUv':compatible,'axes':axes})
    rows.append({'right':r['right'],'positive':r['positive'],'matches':matches})
result={'status':'LOCAL_REPEAT_EVIDENCE_NOT_COMPLETE_MIDDLE_RECIPE','sourceGeometrySha256':hashlib.sha256((ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz').read_bytes()).hexdigest(),'quarters':rows,'limitations':'Each result proves translated source facets only; rectangular unit coverage, end/transition domains, original frame decode and full PBR sampling remain separate checks. No new runtime cuts or texture assets are generated.'}
(OUT/'middle-eight-meter-repeat.json').write_text(json.dumps(result,indent=2))
for row in rows:print(row['right'],row['positive'],len(row['matches']),sum(m['identicalPackedTangents'] and m['constantPackedTangent'] and m['binary16CompatibleUv'] for m in row['matches']))
