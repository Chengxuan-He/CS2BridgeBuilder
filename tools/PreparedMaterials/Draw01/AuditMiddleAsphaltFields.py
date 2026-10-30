"""Read-only affine/periodic native tangent and UV evidence for all middle asphalt quarters."""
import ast,json,itertools,numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
tree=ast.parse(Path(__file__).with_name('BuildMiddleEdgeFields.py').read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='decode');exec(compile(ast.Module(body=[fn],type_ignores=[]),'<native tangent decode>','exec'))
r=json.loads((OUT/'recipe.json').read_text());d=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz');p=d['mesh0_positions'].astype(float);uv=d['mesh0_uv0'].astype(float);f=d['mesh0_faces'];bits=np.load(OUT/'middle-tangent-bits.npy');ts=np.array([decode(b) for b in bits]);rows=[]
def field(fid):
 vi=f[fid];A=np.column_stack([p[vi,0],p[vi,2],np.ones(3)]);return np.linalg.solve(A,ts[vi])
def evaluate(c,ps):return np.column_stack([ps[:,0],ps[:,2],np.ones(len(ps))])@c
for q in r['lods'][1]['domains']:
 if q['domain']!='asphalt':continue
 ids=sorted({i for c in q['cells'] for i in c['faces']});coeff={i:field(i) for i in ids};allvi=np.unique(f[ids]);A=np.column_stack([p[allvi,0],p[allvi,2],np.ones(len(allvi))]);globalfit=np.linalg.lstsq(A,ts[allvi],rcond=None)[0];globalerr=np.linalg.norm(A@globalfit-ts[allvi],axis=1)
 records=[]
 for i in ids:
  vi=f[i];hands=np.where((bits[vi].astype(np.uint64)&(1<<31))!=0,-1,1)
  records.append(dict(face=i,zRange=[float(abs(p[vi,2]).min()),float(abs(p[vi,2]).max())],packedTangents=bits[vi].tolist(),handedness=hands.tolist(),affineXz1ToTxyz=coeff[i].tolist(),maxVertexResidual=float(np.max(abs(evaluate(coeff[i],p[vi])-ts[vi])))))
 adjacent=[];parents={i:i for i in ids}
 def root(i):
  while parents[i]!=i:i=parents[i]
  return i
 for a,b in itertools.combinations(ids,2):
  shared=set(map(tuple,p[f[a]]))&set(map(tuple,p[f[b]]))
  if len(shared)!=2:continue
  vi=np.unique(np.r_[f[a],f[b]]);err=float(np.max(abs(evaluate(coeff[a],p[vi])-ts[vi])));handsa=set(((bits[f[a]].astype(np.uint64)>>31)&1).tolist());handsb=set(((bits[f[b]].astype(np.uint64)>>31)&1).tolist());same=err<1e-12 and handsa==handsb and len(handsa)==1
  if same:parents[root(b)]=root(a)
  edge=np.array(list(shared));seam=float(np.max(abs(evaluate(coeff[a],edge)-evaluate(coeff[b],edge))))
  uvedge=[]
  for point in edge:
   va=next(v for v in f[a] if np.array_equal(p[v],point));vb=next(v for v in f[b] if np.array_equal(p[v],point));uvedge.append((uv[va]-uv[vb]).tolist())
  adjacent.append(dict(uvSharedEdgeDifferences=uvedge,maxUvSharedEdgeDiscontinuity=float(np.max(np.abs(uvedge))),a=a,b=b,sameAffineField=same,crossFaceResidual=err,sharedEdgeFieldDiscontinuity=seam,edge=edge.tolist()))
 groups={}
 for i in ids:groups.setdefault(root(i),[]).append(i)
 sign=1 if q['positive'] else -1;translations=[]
 for a,b in itertools.permutations(ids,2):
  pb=p[f[b]].copy();pb[:,2]-=8*sign;lookup={tuple(x):i for i,x in enumerate(pb)}
  if not all(tuple(x) in lookup for x in p[f[a]]):continue
  order=[lookup[tuple(x)] for x in p[f[a]]];err=float(np.max(abs(ts[f[a]]-ts[f[b]][order])));translations.append(dict(a=a,b=b,deltaZ=8*sign,decodedTMaxError=err,packedTIdentical=bool(np.array_equal(bits[f[a]],bits[f[b]][order])),uvMaxDifference=float(np.max(abs(uv[f[a]]-uv[f[b]][order])))))
 worst=int(allvi[int(np.argmax(globalerr))]);rows.append(dict(right=q['right'],positive=q['positive'],faces=records,allQuarterAffineMaxVectorResidual=float(globalerr.max()),allQuarterAffineWorstVertex=worst,exactAdjacentAffineGroups=list(groups.values()),adjacency=adjacent,translated8mFields=translations))
(OUT/'middle-asphalt-affine-fields.json').write_text(json.dumps(dict(status='READ_ONLY_SOURCE_FIELDS_NOT_RUNTIME_DOMAIN_LICENSE',roundoffOnlyComparison=1e-12,quarters=rows,limitations='Per-triangle affine existence follows native interpolation and is not permission to cut receivers by every source triangle. Grouping only measures exact decoded fields; UV chart compatibility, phase, independent material boundaries and native re-encoding remain separate.'),indent=2))
for q in rows:print(q['right'],q['positive'],'global error',q['allQuarterAffineMaxVectorResidual'],'T groups',len(q['exactAdjacentAffineGroups']),'discontinuities',sum(e['sharedEdgeFieldDiscontinuity']>1e-12 for e in q['adjacency']),'8m same',sum(t['packedTIdentical'] for t in q['translated8mFields']),len(q['translated8mFields']))
