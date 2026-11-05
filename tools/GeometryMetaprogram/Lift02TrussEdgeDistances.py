"""Complete source/new triangle-surface minima with interior edge/edge witnesses."""
from pathlib import Path
import sys,json,numpy as np
r=Path(sys.argv[1]);out=r/'artifacts/material-remap/Lift02/truss69';filename=sys.argv[2] if len(sys.argv)>2 else 'surface-connections-r3.json'; candidate=sys.argv[3] if len(sys.argv)>3 else 'new69r3';rows=json.loads((out/filename).read_text())
for lod in range(3):
 name='LiftBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh';data=np.load(r/'artifacts/uv-density/native'/(name+'.npz'));faces=data['mesh0_faces'];source=data['mesh0_positions'].astype(float);new=np.frombuffer((out/candidate/f'lod{lod}-delta8.5.bin').read_bytes(),'<f4',offset=4).reshape(-1,3).astype(float)
 parts={p['component']:p for p in json.loads((r/f'artifacts/material-remap/Lift02/truss68/complete-native-members-lod{lod}.json').read_text())['parts']};cache={}
 def edges(i):
  if i not in cache:cache[i]=np.unique(np.sort(faces[parts[i]['faces']][:,((0,1),(1,2),(2,0))].reshape(-1,2),axis=1),axis=0)
  return cache[i]
 def refine(pos,a,b,upper):
  if upper==0:return 0.
  best=upper*upper;ea=edges(a);eb=edges(b);q=pos[eb[:,0]];v=pos[eb[:,1]]-q;c=np.einsum('ij,ij->i',v,v)
  # Endpoints/edges are already included by vertex/triangle minimization.
  # Interior/interior edge pairs supply the remaining closest-point class.
  for edge in ea:
   p=pos[edge[0]];u=pos[edge[1]]-p;w=p-q;aa=u@u;bb=v@u;dd=w@u;ee=np.einsum('ij,ij->i',v,w);den=aa*c-bb*bb;valid=den!=0
   ss=np.divide(bb*ee-c*dd,den,out=np.zeros(len(den)),where=valid);tt=np.divide(aa*ee-bb*dd,den,out=np.zeros(len(den)),where=valid);valid&=(ss>=0)&(ss<=1)&(tt>=0)&(tt<=1)
   if valid.any():
    delta=w[valid]+ss[valid,None]*u-tt[valid,None]*v[valid];best=min(best,float(np.einsum('ij,ij->i',delta,delta).min()))
    if best==0:return 0.
  # Source distances initially used source vertex/face witnesses only. Check
  # non-coplanar crossings too; final new distances already included this test.
  for first,second in ((a,b),(b,a)):
   tris=pos[faces[parts[second]['faces']]];fp=pos[parts[first]['vertices']];tris=tris[((tris.min(1)<=fp.max(0))&(tris.max(1)>=fp.min(0))).all(1)]
   if not len(tris):continue
   e1=tris[:,1]-tris[:,0];e2=tris[:,2]-tris[:,0]
   for edge in edges(first):
    start,end=pos[edge];direction=end-start;pv=np.cross(direction,e2);det=np.einsum('ij,ij->i',e1,pv);inv=np.divide(1,det,out=np.zeros(len(det)),where=det!=0);tv=start-tris[:,0];u=np.einsum('ij,ij->i',tv,pv)*inv;qv=np.cross(tv,e1);vv=qv@direction*inv;t=np.einsum('ij,ij->i',e2,qv)*inv
    if np.any((det!=0)&(u>=0)&(vv>=0)&(u+vv<=1)&(t>=0)&(t<=1)):return 0.
  return float(np.sqrt(best))
 for row in rows:
  if row['lod']!=lod:continue
  row['sourceVertexFaceDistance']=row['sourceDistance'];row['newVertexFaceOrIntersectionDistance']=row['newSurfaceDistance']
  row['sourceDistance']=refine(source,row['a'],row['b'],row['sourceDistance']);row['newSurfaceDistance']=refine(new,row['a'],row['b'],row['newSurfaceDistance']);row['edgeEdgeRefined']=True
 print('complete lod',lod,flush=True)
(out/filename).write_text(json.dumps(rows,indent=2))
