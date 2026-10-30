"""Try actual adjacent middle asphalt affine-chart unions, without generating cuts."""
import ast,itertools,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
tree=ast.parse(Path(__file__).with_name('AuditFarPrecision.py').read_text())
fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='feasible')
exec(compile(ast.Module(body=[fn],type_ignores=[]),'<rounding-cell-solver>','exec'))
recipe=json.loads((OUT/'recipe.json').read_text())
mesh=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz')
p=mesh['mesh0_positions'].astype(float);uv=mesh['mesh0_uv0'].astype(float);faces=mesh['mesh0_faces']
results=[]
for row in recipe['lods'][1]['domains']:
 if row['domain']!='asphalt':continue
 ids=sorted({f for cell in row['cells'] for f in cell['faces']})
 edges={}
 for f in ids:
  for a,b in [(0,1),(1,2),(2,0)]:
   key=tuple(sorted([tuple(p[faces[f,a]]),tuple(p[faces[f,b]])]))
   edges.setdefault(key,[]).append(f)
 adjacent=[(key,a,b) for key,fs in edges.items() for a,b in itertools.combinations(fs,2)]
 groups={f:{f} for f in ids}
 def group(f):return next(g for g in groups.values() if f in g)
 accepted=[];rejected=[]
 for edge,a,b in adjacent:
  ga,gb=group(a),group(b)
  if ga is gb:continue
  union=ga|gb;vertices=np.unique(faces[list(union)])
  axes=feasible(p[vertices],uv[vertices])
  ok=all(x['feasible'] and x['float32CoefficientsInsideRoundingCells'] and x['float32EvaluationInsideRoundingCells'] for x in axes)
  if ok:
   accepted.append(dict(faces=[a,b],edge=edge))
   del groups[next(k for k,g in groups.items() if g is ga)]
   del groups[next(k for k,g in groups.items() if g is gb)]
   groups[min(union)]=union
  else:
   # Record discontinuity directly at the shared positions; zero means a genuine
   # continuous-gradient fold, not a UV wrap reset.
   jumps=[]
   for xyz in edge:
    ta=[uv[i] for i in faces[a] if tuple(p[i])==xyz][0]
    tb=[uv[i] for i in faces[b] if tuple(p[i])==xyz][0]
    jumps.append((ta-tb).tolist())
   rejected.append(dict(faces=[a,b],edge=edge,uvEndpointJumps=jumps,
                        continuousUv=all(np.max(np.abs(j))==0 for j in jumps)))
 results.append(dict(right=row['right'],positive=row['positive'],sourceFaces=len(ids),
   affineGroups=[sorted(g) for g in groups.values()],adjacentMerges=accepted,
   nonmergeableEdges=rejected,continuousProjectionFolds=sum(e['continuousUv'] for e in rejected)))
(OUT/'middle-chart-unions.json').write_text(json.dumps(dict(status='UV_ONLY_LOWER_BOUND_NOT_RUNTIME_RECIPE',
 limitation='Native varying tangent field is not included; no production prototype-triangle Boolean or cuts were generated.',quarters=results),indent=2)+'\n')
print([(r['right'],r['positive'],r['sourceFaces'],len(r['affineGroups']),r['continuousProjectionFolds']) for r in results])
