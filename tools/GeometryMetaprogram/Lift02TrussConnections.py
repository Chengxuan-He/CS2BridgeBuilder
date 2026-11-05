"""Audit original cross-component surface contacts against actual Plan coordinates.

Uses every original vertex and triangle of adjacent components, including parts
outside the production membership. The 2 cm source-contact search radius is an
offline diagnostic only; it neither selects runtime members nor changes geometry.
"""
from pathlib import Path
import json,struct,sys
import numpy as np

repo=Path(sys.argv[1]);out=repo/'artifacts/material-remap/Lift02/truss69'
baseline=sys.argv[2] if len(sys.argv)>2 else 'old68'
stem=sys.argv[3] if len(sys.argv)>3 else 'surface-connections'
actual_folder=sys.argv[4] if len(sys.argv)>4 else 'new69r3'
def bbn(path):
 b=path.read_bytes();n=struct.unpack_from('<i',b,8)[0];a=np.frombuffer(b,dtype='<i4',count=n,offset=32);h=np.frombuffer(b,dtype='<f4',count=struct.unpack_from("<i",b,24)[0],offset=32+4*n);return a,h
recipe,h=bbn(repo/'assets/HardcodedNumbers/Lift02/Types_Lift02_CentralTrusses.bin')
def coords(path):
 b=path.read_bytes();return np.frombuffer(b,dtype='<f4',offset=4).reshape(-1,3).copy()
def closest(p,tri):
 # Exact planar projection where barycentric coordinates are inside; otherwise
 # the closest point is on one of the three original closed triangle edges.
 a,b,c=tri[:,0],tri[:,1],tri[:,2];ab=b-a;ac=c-a;ap=p-a
 dot=lambda x,y:np.einsum('ij,ij->i',x,y)
 aa=dot(ab,ab);bb=dot(ab,ac);cc=dot(ac,ac);d=aa*cc-bb*bb
 u=np.divide(cc*dot(ap,ab)-bb*dot(ap,ac),d,out=np.zeros(len(d)),where=d!=0)
 v=np.divide(aa*dot(ap,ac)-bb*dot(ap,ab),d,out=np.zeros(len(d)),where=d!=0)
 w=np.column_stack((1-u-v,u,v));q=np.einsum('ij,ijk->ik',w,tri);dist=dot(q-p,q-p)
 dist[(d==0)|(w.min(1)<0)]=np.inf
 for j,k in ((0,1),(1,2),(2,0)):
  edge=tri[:,k]-tri[:,j];den=dot(edge,edge);t=np.clip(np.divide(dot(p-tri[:,j],edge),den,out=np.zeros(len(den)),where=den!=0),0,1)
  eq=tri[:,j]+t[:,None]*edge;ed=dot(eq-p,eq-p);take=ed<dist
  ew=np.zeros_like(w);ew[:,j]=1-t;ew[:,k]=t;w[take]=ew[take];dist[take]=ed[take]
 i=int(np.argmin(dist));return float(dist[i]),i,w[i]

records=[];summary=[]
for lod in (0,1,2):
 name='LiftBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh'
 d=np.load(repo/'artifacts/uv-density/native'/(name+'.npz'));source=d['mesh0_positions'];faces=d['mesh0_faces']
 parts=json.loads((repo/f'artifacts/material-remap/Lift02/truss68/complete-native-members-lod{lod}.json').read_text())['parts']
 group=np.full(len(source),-1);at=3*len(h)+int(recipe[:lod*len(h)].sum())
 for g in range(len(h)):
  n=recipe[lod*len(h)+g];group[recipe[at:at+n]]=g;at+=n
 pg=[]
 for p in parts:
  gs=np.unique(group[p['vertices']]);pg.append(int(gs[0]) if len(gs)==1 else -2)
 lo=np.array([source[p['vertices']].min(0) for p in parts]);hi=np.array([source[p['vertices']].max(0) for p in parts])
 old=coords(out/baseline/f'lod{lod}-delta8.5.bin');newpath=out/actual_folder/f'lod{lod}-delta8.5.bin'
 actual=newpath.exists();new=coords(newpath) if actual else old.copy()
 if not actual:
  for g in range(len(h)):
   ids=np.flatnonzero(group==g);new[ids,0]=source[ids,0]*np.float32((h[g]+np.float32(8.5))/h[g])
 count=0
 for ai,a in enumerate(parts):
  if pg[ai]<0:continue
  candidates=np.flatnonzero(np.maximum(0,np.maximum(lo[ai]-hi,lo-hi[ai])).max(1)<=.02)
  for bi in candidates:
   b=parts[bi]
   if ai==bi or (pg[bi]>=0 and ai>bi) or a['bones']!=b['bones'] or b['trackRemoved']:continue
   best=(float('inf'),None,None,None)
   for first,second in ((a,b),(b,a)):
    tri=source[faces[second['faces']]].astype(float)
    vertices=np.array(first['vertices']);positions=source[vertices];bounds=source[second['vertices']]
    keep=((positions>=bounds.min(0)-.02)&(positions<=bounds.max(0)+.02)).all(1)
    vertices=vertices[keep];_,unique=np.unique(source[vertices],axis=0,return_index=True)
    for vertex in vertices[unique]:
     dd,j,w=closest(source[vertex].astype(float),tri)
     if dd<best[0]:best=(dd,vertex,second['faces'][j],w)
   dd,vertex,face,w=best
   if dd>.02**2:continue
   srcgap=source[vertex]-w@source[faces[face]];oldgap=old[vertex]-w@old[faces[face]];newgap=new[vertex]-w@new[faces[face]]
   g=pg[ai];expected=srcgap.copy();expected[0]*=float(np.float32((h[g]+np.float32(8.5))/h[g]))
   def final_distance(positions,upper):
    best=upper**2
    for first,second in ((a,b),(b,a)):
     tri=positions[faces[second['faces']]].astype(float);bounds=positions[second['vertices']]
     vertices=np.array(first['vertices']);points=positions[vertices]
     lower=np.maximum(0,np.maximum(bounds.min(0)-points,points-bounds.max(0)))
     vertices=vertices[(lower*lower).sum(1)<=best];_,unique=np.unique(positions[vertices],axis=0,return_index=True)
     for vertex in vertices[unique]:
      distance,_,_=closest(positions[vertex].astype(float),tri);best=min(best,distance)
      if best==0:return 0.
    if best>0:
     # Crossing triangle edges can intersect a face even when none of the
     # vertices lies on that face. Test those intersections before calling a gap.
     for first,second in ((a,b),(b,a)):
      tris=positions[faces[second['faces']]].astype(float)
      first_points=positions[first['vertices']];lo_first=first_points.min(0);hi_first=first_points.max(0)
      tris=tris[((tris.min(1)<=hi_first)&(tris.max(1)>=lo_first)).all(1)]
      if not len(tris):continue
      e1=tris[:,1]-tris[:,0];e2=tris[:,2]-tris[:,0]
      edges=faces[first['faces']][:,((0,1),(1,2),(2,0))].reshape(-1,2)
      for edge in edges:
       start,end=positions[edge].astype(float);direction=end-start;pvec=np.cross(direction,e2);det=np.einsum('ij,ij->i',e1,pvec)
       inv=np.divide(1,det,out=np.zeros(len(det)),where=det!=0);tvec=start-tris[:,0];u=np.einsum('ij,ij->i',tvec,pvec)*inv;qvec=np.cross(tvec,e1);v=qvec@direction*inv;t=np.einsum('ij,ij->i',e2,qvec)*inv
       if np.any((det!=0)&(u>=0)&(v>=0)&(u+v<=1)&(t>=0)&(t<=1)):return 0.
    return float(np.sqrt(best))
   ng=float(np.linalg.norm(newgap));og=float(np.linalg.norm(oldgap))
   row=dict(lod=lod,a=a['component'],b=b['component'],aGroup=pg[ai],bGroup=pg[bi],bRecipe=b['recipe'],sourceDistance=float(np.sqrt(dd)),oldDistance=og,newDistance=ng,newSurfaceDistance=final_distance(new,ng),oldSurfaceDistance=final_distance(old,og),sameGroupResidual=float(np.linalg.norm(newgap-expected)) if pg[ai]==pg[bi] else None,vertex=int(vertex),face=int(face),barycentric=w.tolist(),actualNewPlan=actual)
   records.append(row);count+=1
  if ai%100==0:print('progress',lod,ai,flush=True)
 summary.append(dict(lod=lod,contacts=count,actualNewPlan=actual))
(out/(stem+'.json')).write_text(json.dumps(records,indent=2))
(out/(stem+'-summary.json')).write_text(json.dumps(summary,indent=2));print(summary)
