"""Check native far mapping against per-coordinate IEEE binary16 rounding cells."""
import itertools,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01'
d=json.loads((OUT/'far-appearance.json').read_text())
def feasible(p,t):
 a=np.column_stack((p[:,0],p[:,2],np.ones(len(p))))
 result=[]
 for axis in range(2):
  h=t[:,axis].astype(np.float16)
  assert np.array_equal(h.astype(float),t[:,axis])
  lo=(h.astype(float)+np.nextafter(h,np.float16(-np.inf)).astype(float))/2
  hi=(h.astype(float)+np.nextafter(h,np.float16(np.inf)).astype(float))/2
  lhs=np.concatenate((a,-a));rhs=np.concatenate((hi,-lo))
  fit=np.linalg.lstsq(a,t[:,axis],rcond=None)[0]
  ok=bool(np.all(lhs@fit<=rhs+1e-12))
  if not ok or not np.all(lhs@fit.astype(np.float32).astype(float)<=rhs+1e-12):
   feasible_vertices=[]
   for rows in itertools.combinations(range(len(rhs)),3):
    try:c=np.linalg.solve(lhs[list(rows)],rhs[list(rows)])
    except np.linalg.LinAlgError:continue
    if np.all(lhs@c<=rhs+1e-12):feasible_vertices.append(c)
   if feasible_vertices:fit=np.mean(feasible_vertices,axis=0);ok=True
  result.append(dict(feasible=ok,float32CoefficientsInsideRoundingCells=bool(np.all(lhs@fit.astype(np.float32).astype(float)<=rhs+1e-12)),float32EvaluationInsideRoundingCells=bool(np.all(a.astype(np.float32)@fit.astype(np.float32)>=lo-1e-12) and np.all(a.astype(np.float32)@fit.astype(np.float32)<=hi+1e-12)),affineXz1=fit.tolist(),maxResidualTexel=float(np.max(np.abs(a@fit-t[:,axis]))*512)))
 return result
rows=[]
for r in d['groups']:
 p=np.array(r['positions']);t=np.array(r['uv']);zs=np.unique(p[:,2]);groups=[('whole',np.ones(len(p),bool))]
 if r['mergedCarriagewayAndSidewalk']:
  assert len(zs)==3
  groups += [('lower-authored-domain',p[:,2]<=zs[1]),('upper-authored-domain',p[:,2]>=zs[1])]
 for name,mask in groups:
  rows.append(dict(group=r['group'],domain=name,zRange=[float(p[mask,2].min()),float(p[mask,2].max())],vertices=int(sum(mask)),axes=feasible(p[mask],t[mask])))
(OUT/'far-half-precision.json').write_text(json.dumps(dict(sourceSha256=d['sourceSha256'],rounding='Separate binary16 predecessor/successor midpoint for every coordinate; no universal texel threshold.',rows=rows),indent=2)+'\n')
for r in rows:print(r['group'],r['domain'],[a['feasible'] for a in r['axes']])
