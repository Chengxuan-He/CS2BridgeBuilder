"""Measure Lift03 fixed network source-arm splits; stage only, no production writes."""
from pathlib import Path
import ast,json,numpy as np
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'artifacts/material-remap/Lift03/appearance76/fixed-source'
# Reuse the existing source-preserving barycentric splitter implementation.
module=ast.parse((ROOT/'tools/GeometryMetaprogram/Lift03Frames.py').read_text())
functions=[n for n in module.body if isinstance(n,ast.FunctionDef) and n.name in ('opening_profile','clip')]
# The native far end has fewer authored corners than the detailed shell.
for n in ast.walk(functions[0]):
 if isinstance(n,ast.Assert) and ast.unparse(n.test)=='len(result) > 20':n.test=ast.Compare(left=ast.Call(func=ast.Name(id='len',ctx=ast.Load()),args=[ast.Name(id='result',ctx=ast.Load())],keywords=[]),ops=[ast.Gt()],comparators=[ast.Constant(value=1)])
exec(compile(ast.fix_missing_locations(ast.Module(body=functions,type_ignores=[])),'source-frame-functions','exec'))
split=next(n for n in ast.walk(module) if isinstance(n,ast.For) and ast.unparse(n.target)=='ordinal' and ast.unparse(n.iter)=='fi')
split_code=compile(ast.Module(body=[split],type_ignores=[]),'source-frame-split','exec')
results=[]
for lod in range(3):
 m=np.load(O/f'lod{lod}.npz');v=m['positions'].astype(float);f=m['faces'];points=v[f];end=points[(points[:,:,2]==32).all(1)]
 profile=opening_profile(end)
 central=points[(np.ptp(points[:,:,2],axis=1)>4)&(abs(points[:,:,0]).max(1)<3)]
 half=float(abs(central[:,:,0]).max());assert half==(2.08642578125 if lod<2 else 2.62091064453125)
 profile[:,1]=np.minimum(profile[:,1],-half);profile[:,2]=np.maximum(profile[:,2],half)
 retained=[]
 for row in profile:
  retained.append(row)
  while len(retained)>2:
   a,b,c=retained[-3:];expected=a[1:]+(c[1:]-a[1:])*(b[0]-a[0])/(c[0]-a[0])
   if abs(expected-b[1:]).max()>1e-10:break
   retained.pop(-2)
 profile=np.array(retained);ys,lefts,rights=profile.T
 boundary=lambda y,sign:np.interp(y,ys,lefts if sign<0 else rights)
 weight=lambda p:float(np.clip(p[0]/abs(boundary(p[1],-1 if p[0]<0 else 1)),-1,1))
 components=np.load(O/f'lod{lod}-parts.npz')['component'];parts=json.loads((O/f'lod{lod}-parts.json').read_text())['parts'];center_parts=[p['id'] for p in parts if p['min'][0]<=0<=p['max'][0]];center=np.isin(components,center_parts)
 weights={i:(weight(p) if center[i] else float(np.sign(p[0]))) for i,p in enumerate(v)};added=[];added_weights=[];fragments=[];extra=[];edge_recipes={};fi=np.flatnonzero(center[f].all(1))
 exec(split_code)
 order=list(edge_recipes)+[i for i in range(len(added)) if i not in edge_recipes];remap={len(v)+old:len(v)+new for new,old in enumerate(order)}
 fragments=[(i,[remap.get(index,index) for index in inds]) for i,inds in fragments];edges=[edge_recipes[i] for i in edge_recipes];interiors=[added[i] for i in order[len(edges):]];extra=[extra[i] for i in order];added_weights=[added_weights[i] for i in order]
 p=np.concatenate((v,np.array(extra).reshape(-1,3)));w=np.array(list(weights.values())+added_weights);replaced={i for i,_ in fragments};out=np.array([list(face) for i,face in enumerate(f) if i not in replaced]+[inds[k:k+3] for _,inds in fragments for k in range(0,len(inds),3)])
 max_area=0
 for ordinal,inds in fragments:
  src=v[f[ordinal]];tris=p[np.array(inds).reshape(-1,3)];a=np.linalg.norm(np.cross(src[1]-src[0],src[2]-src[0]));b=np.linalg.norm(np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0]),axis=1).sum();max_area=max(max_area,abs(a-b))
 assert max_area<.0002
 checks=[]
 for delta in (0.,2.,8.,-.25,-half,-2*half):
  q=p.copy();q[:,0]+=w*delta;assert np.isfinite(q).all() and np.array_equal(p[:,1:],q[:,1:]);tri=p[out];target=q[out];n=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);nn=np.cross(target[:,1]-target[:,0],target[:,2]-target[:,0]);valid=np.linalg.norm(n,axis=1)>1e-7;bad=int((valid&((n*nn).sum(1)<=0)).sum());checks.append(dict(delta=delta,nonpositiveOrientation=bad))
  if bad:print('ORIENTATION',lod,delta,bad,[(out[i].tolist(),p[out[i]].tolist(),w[out[i]].tolist()) for i in np.flatnonzero(valid&((n*nn).sum(1)<=0))[:2]],flush=True)
  if delta>=-.25:assert bad==0
  for sign in (-1,1):
   rigid=(w[out]==sign).all(1);assert np.allclose(target[rigid]-tri[rigid],(sign*delta,0,0),atol=1e-10,rtol=0)
 data=dict(lod=lod,sourceVertices=len(v),sourceIndices=int(f.size),weights=list(weights.values()),edges=edges,interiors=interiors,extraWeights=added_weights,fragments=fragments,openingProfile=profile.tolist(),centralHalfSpan=half)
 (O/f'plan{lod}.json').write_text(json.dumps(data,separators=(',',':'))+'\n');np.savez_compressed(O/f'plan{lod}.npz',positions=p,indices=out,weights=w)
 contacts={};mismatches=0
 for i,point in enumerate(v):
  key=tuple(point)
  if key in contacts and abs(w[contacts[key]]-w[i])>1e-8:mismatches+=1
  contacts[key]=i
 result=dict(contactMismatches=mismatches,admitted=mismatches==0,lod=lod,sourceFaces=len(f),outputFaces=len(out),splitFaces=len(fragments),constructedVertices=len(extra),profileNodes=len(profile),maximumSourceAreaError=max_area,checks=checks);results.append(result);print(result,flush=True)
(O/'plan-verification.json').write_text(json.dumps(results,indent=2)+'\n')

assert all(row['admitted'] for row in results), 'Candidate is staged only: original contact points separate; do not install.'
