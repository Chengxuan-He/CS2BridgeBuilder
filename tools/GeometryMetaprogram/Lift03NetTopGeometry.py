"""Emit the measured Lift03 ordinary-pillar U-shaped top at every LOD.
Usage: python Lift03NetTopGeometry.py RESEARCH_DIRECTORY OUTPUT_CS
"""
from pathlib import Path
import hashlib,json,sys
import numpy as np
r,output=map(Path,sys.argv[1:])
hashes=('8776c1a9d158c4df3a85b41af4ef287d397ddd02bdfda6d580a52fd27f9dc936','dcad6b10b8324f3a95a44236c48c69bc84b29d643986bcec5b03f05329906b3b','e9e4af98912739537b870b002b26c86a6a9e650404d1fa03969ad072d4f4911e')
levels=[]
def opening_profile(points):
 result=[]
 for y in np.unique(points[:,:,1]):
  candidates=[]
  for sample in (y-1e-5,y+1e-5):
   spans=[]
   for tri in points[(points[:,:,1].min(1)<=sample)&(points[:,:,1].max(1)>=sample)]:
    hits=[]
    for a,b in zip(tri,np.roll(tri,-1,axis=0)):
     if (a[1]<=sample<=b[1] or b[1]<=sample<=a[1]) and a[1]!=b[1]:
      hits.append(a[0]+(b[0]-a[0])*(sample-a[1])/(b[1]-a[1]))
    if hits:spans.append((min(hits),max(hits)))
   merged=[]
   for lo,hi in sorted(spans):
    if merged and lo<=merged[-1][1]+1e-7:merged[-1][1]=max(hi,merged[-1][1])
    else:merged.append([lo,hi])
   if any(lo<=0<=hi for lo,hi in merged):continue
   pos=[lo for lo,hi in merged if lo>0];neg=[hi for lo,hi in merged if hi<0]
   if pos and neg:
    candidates.append((max(neg),min(pos)))
  if candidates:
   left_candidate,right_candidate=min(candidates,key=lambda pair:pair[1]-pair[0])
   # Recover the exact authored corner; the offset probes only identify its side.
   intersections=[]
   for tri in points[(points[:,:,1].min(1)<=y)&(points[:,:,1].max(1)>=y)]:
    for a,b in zip(tri,np.roll(tri,-1,axis=0)):
     if a[1]==y:intersections.append(a[0])
     if min(a[1],b[1])<y<max(a[1],b[1]):
      intersections.append(a[0]+(b[0]-a[0])*(y-a[1])/(b[1]-a[1]))
   xs=np.array(intersections);left=xs[np.argmin(abs(xs-left_candidate))];right=xs[np.argmin(abs(xs-right_candidate))]
   assert abs(left-left_candidate)<.001 and abs(right-right_candidate)<.001
   result.append((float(y),float(left),float(right)))
 assert len(result)>20
 return np.array(result)
def clip(poly, distances, keep_negative):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da,db=distances(a),distances(b)
  ina=da<=1e-10 if keep_negative else da>=-1e-10
  inb=db<=1e-10 if keep_negative else db>=-1e-10
  if ina:result.append(a)
  if ina!=inb:result.append(a+(b-a)*(da/(da-db)))
 clean=[]
 for p in result:
  if not clean or max(abs(p-clean[-1]))>1e-9:clean.append(p)
 if len(clean)>1 and max(abs(clean[0]-clean[-1]))<1e-9:clean.pop()
 return clean
for lod in range(3):
 name='LiftBridge03NetPillarTop'+(f'_LOD{lod}' if lod else '')+' Mesh.Geometry'
 assert hashlib.sha256((r/'lift03-network-pillar'/name).read_bytes()).hexdigest()==hashes[lod]
 data=np.load(r/f'lift03-netpillar-0-{lod}.npz');v=data['positions'].astype(float);f=data['indices']
 assert (len(v),len(f))==((4864,8088),(4796,4852),(384,400))[lod]
 if lod==0:
  native_profile=opening_profile(v[f]);profile=native_profile.copy()
  # At the bottom, the connection meets the measured column contour.
  profile[:,1]=np.minimum(profile[:,1],-3.3563232421875)
  profile[:,2]=np.maximum(profile[:,2],3.3563232421875)
  retained=[]
  for row in profile:
   retained.append(row)
   while len(retained)>2:
    a,b,c=retained[-3:];expected=a[1:]+(c[1:]-a[1:])*((b[0]-a[0])/(c[0]-a[0]))
    if np.max(abs(expected-b[1:]))>1e-10:break
    retained.pop(-2)
  profile=np.array(retained)
  (r/'lift03-nettop-opening-profile.json').write_text(json.dumps({'native':native_profile.tolist(),'width':profile.tolist()},indent=2)+'\n')
 ys,lefts,rights=profile.T
 boundary=lambda y,sign:np.interp(y,ys,lefts if sign<0 else rights)
 weight=lambda p:float(np.clip(p[0]/abs(boundary(p[1],-1 if p[0]<0 else 1)),-1,1))
 weights={i:weight(p) for i,p in enumerate(v)}
 added=[];added_weights=[];fragments=[];extra=[];edge_recipes={};selected=list(range(len(f)));fi=selected
 for ordinal in fi:
  face=f[ordinal];points=v[face];polys=[list(np.eye(3))]
  face_weights=[weights[int(i)] for i in face]
  # A coarse side-arm chord must remain the same authored side part.
  # Do not insert the detailed curved opening into its interior.
  if all(x==1 for x in face_weights) or all(x==-1 for x in face_weights):continue
  # Split only at measured profile corners and the two arm boundaries.
  for y in ys[(ys>points[:,1].min()+1e-9)&(ys<points[:,1].max()-1e-9)]:
   next_polys=[]
   for poly in polys:
    dist=lambda b:float(b@points[:,1]-y)
    ds=[dist(b) for b in poly]
    if min(ds)<-1e-9 and max(ds)>1e-9:next_polys += [clip(poly,dist,True),clip(poly,dist,False)]
    else:next_polys.append(poly)
   polys=next_polys
  for sign in (-1,1):
   next_polys=[]
   for poly in polys:
    dist=lambda b:float((b@points)[0]-boundary((b@points)[1],sign))
    ds=[dist(b) for b in poly]
    if min(ds)<-1e-9 and max(ds)>1e-9:next_polys += [clip(poly,dist,True),clip(poly,dist,False)]
    else:next_polys.append(poly)
   polys=next_polys
  if len(polys)==1:continue
  local={};indices=[]
  for poly in polys:
   polygon=[]
   for bary in poly:
    nz=np.flatnonzero(abs(bary)>1e-8)
    if len(nz)==1:idx=int(face[nz[0]])
    else:
     key=tuple(np.round(bary,10))
     if key not in local:
      wb,wc=map(float,np.float32(bary[1:]));pos=points[0]*(1-wb-wc)+points[1]*wb+points[2]*wc
      if len(nz)==2:
       ia,ib=map(int,nz);fraction=float(np.float32(bary[ib]/(bary[ia]+bary[ib])))
       assert 0<fraction<1
       edge_recipes[len(added)]=(int(face[ia]),int(face[ib]),fraction)
       pos=points[ia]+(points[ib]-points[ia])*fraction
      local[key]=len(v)+len(added);added.append((*map(int,face),wb,wc));extra.append(pos);added_weights.append(weight(pos))
     idx=local[key]
    polygon.append(idx)
   for k in range(1,len(polygon)-1):indices.extend((polygon[0],polygon[k],polygon[k+1]))
  fragments.append((int(ordinal),indices))
 # Native Plan stores edge vertices before strictly interior barycentric vertices.
 order=list(edge_recipes)+[i for i in range(len(added)) if i not in edge_recipes]
 remap={len(v)+old:len(v)+new for new,old in enumerate(order)}
 fragments=[(i,[remap.get(index,index) for index in inds]) for i,inds in fragments]
 edges=[edge_recipes[i] for i in edge_recipes]
 interiors=[added[i] for i in order[len(edges):]]
 for _,_,_,wb,wc in interiors:assert wb>0 and wc>0 and np.float32(wb)+np.float32(wc)<1
 extra=[extra[i] for i in order];added_weights=[added_weights[i] for i in order]
 all_v=np.concatenate((v,np.array(extra).reshape(-1,3)));w=np.zeros(len(all_v))
 for i,value in weights.items():w[i]=value
 w[len(v):]=added_weights
 assert np.all(np.isfinite(all_v)) and np.all(np.isfinite(w)), 'Non-finite structural construction is not valid output'
 replaced={i for i,_ in fragments};sf=np.array([list(f[i]) for i in selected if i not in replaced]+[inds[k:k+3] for _,inds in fragments for k in range(0,len(inds),3)])
 for ordinal,inds in fragments:
  p=v[f[ordinal]];q=all_v[np.array(inds).reshape(-1,3)]
  area=np.linalg.norm(np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]),axis=1).sum();original=np.linalg.norm(np.cross(p[1]-p[0],p[2]-p[0]))
  assert abs(area-original)<.0002,(lod,ordinal,area,original)
 for delta in (-.25,0,2,8):
  q=all_v.copy();q[:,0]+=w*delta
  a,b=all_v[sf],q[sf];na=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);nb=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);valid=np.linalg.norm(na,axis=1)>1e-7
  failed=valid & ((na*nb).sum(1)<=0)
  if failed.any():
   first=int(np.flatnonzero(failed)[0]);print('WINDING FAILURE',lod,delta,int(failed.sum()),a[first],w[sf[first]],na[first],nb[first])
   for ordinal,inds in fragments:
    if set(sf[first])<=set(inds):
     print('SOURCE',ordinal,v[f[ordinal]],w[f[ordinal]])
  assert not failed.any()
  assert np.array_equal(q[:,1:],all_v[:,1:])
  for sign in (-1,1):
   rigid=np.all(w[sf]==sign,axis=1)
   assert np.allclose(b[rigid]-a[rigid],(sign*delta,0,0),rtol=0,atol=1e-10)
 levels.append((len(v),len(f)*3,weights,edges,interiors,added_weights,fragments))
 np.savez(r/f'lift03-nettop-{lod}.npz',positions=all_v,indices=sf,weights=w)
 print(f'PASS top LOD{lod}: {len(sf)} triangles; {len(edges)} edge/{len(interiors)} interior vertices; source area, rigid sides and four widths')
def number(x):return format(float(np.float32(x)),'.9g')+'f'
lines=['// Measured Lift03 U-frame arm boundaries, generated by Lift03NetTopGeometry.py.','using System;','using UnityEngine;','','namespace BridgeBuilder.Bridges;','','internal static class Lift03NetTopGeometry','{','    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)','    {','        var positions = (Vector3[])source.Clone();','        var data = lod switch','        {']
for lod,(n,ic,*_) in enumerate(levels):lines.append(f'            {lod} => (Count: {n}, IndexCount: {ic}, Weights: Weights{lod}, Edges: Edges{lod}, Interior: Interior{lod}, ExtraWeights: ExtraWeights{lod}, Fragments: Fragments{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))','        };','        if (source.Length != data.Count || positions.Length != data.Count || indices.Length != data.IndexCount || !float.IsFinite(delta))','            throw new InvalidOperationException("Lift03 ordinary top requires its recorded native layout.");','        foreach (var item in data.Weights)','            positions[item.Index].x = source[item.Index].x + item.Weight * delta;','        var x = new float[data.ExtraWeights.Length];','        for (var i = 0; i < data.Edges.Length; i++)','        {','            var edge = data.Edges[i];','            x[i] = source[edge.A].x + (source[edge.B].x - source[edge.A].x) * edge.Fraction + data.ExtraWeights[i] * delta;','        }','        for (var i = 0; i < data.Interior.Length; i++)','        {','            var a = data.Interior[i];','            var index = data.Edges.Length + i;','            x[index] = source[a.A].x + (source[a.B].x - source[a.A].x) * a.WeightB + (source[a.C].x - source[a.A].x) * a.WeightC + data.ExtraWeights[index] * delta;','        }','        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[indices.Length / 3] },','            addedVertices: data.Edges, fragments: data.Fragments, interiorVertices: data.Interior, constructedX: x);','    }']
for lod,(_,_,weights,edges,interior,extra,frags) in enumerate(levels):
 lines += ['    // Geometry SHA-256: '+hashes[lod],f'    private static readonly (int Index, float Weight)[] Weights{lod} =','    {']+[f'        ({i}, {number(w)}),' for i,w in sorted(weights.items())]+['    };']
 lines += [f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{lod} =','    {']+[f'        new({a}, {b}, {number(t)}),' for a,b,t in edges]+['    };']
 lines += [f'    private static readonly MovableStructureModel.TriangleVertex[] Interior{lod} =','    {']+[f'        new({a}, {b}, {cc}, {number(wb)}, {number(wc)}),' for a,b,cc,wb,wc in interior]+['    };']
 lines += [f'    private static readonly float[] ExtraWeights{lod} =','    {','        '+', '.join(map(number,extra)),'    };']
 lines += [f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} =','    {']+['        new(0, '+str(i)+', '+', '.join(map(str,inds))+'),' for i,inds in frags]+['    };']
lines+=['}',''];output.write_text('\n'.join(lines),encoding='utf-8')

