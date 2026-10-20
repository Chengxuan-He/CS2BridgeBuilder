"""Emit native Lift03 far-shell openings and measured width rules.
Usage: python Lift03FarStructure.py RESEARCH_DIRECTORY OUTPUT_CS
Regenerate Lift03Frames.py joined-shells first for the reviewed near opening.
"""
from pathlib import Path
import json,sys,ast,hashlib,struct
import ctypes as c
import numpy as np
r,output=map(Path,sys.argv[1:3])
source=Path(__file__).with_name('Lift03Frames.py').read_text()
node=next(n for n in ast.parse(source).body if isinstance(n,ast.FunctionDef) and n.name=='clip');exec(ast.get_source_segment(source,node))
d=np.load(r/'lift03-far-native.npz')
raw=(r/'movable-skin/LiftBridge03_LOD2 Mesh.Geometry').read_bytes()
assert hashlib.sha256(raw).hexdigest()=='4dd846ac20f020fb45de94549d66e0bbafda4c575554a5b235a93e7d5a6008e4'
decoder=Path(__file__).with_name('Lift05Counterweights.py').read_text()
exec(decoder[decoder.index('dll = c.CDLL'):decoder.index('tables=[]')])
version,meshes,flags=struct.unpack_from('<HII',raw);assert version==1 and meshes==1
headers=[struct.unpack_from('<QI14i5I',raw,10)];h=headers[0]
section=decoder[decoder.index('    def decode('):decoder.index('    cached=np.load')]
exec('\n'.join(line[4:] if line.startswith('    ') else line for line in section.splitlines()))
assert np.array_equal(v,d['positions']) and np.array_equal(f,d['indices'])
v=v.astype(float);comp=d['component'];selected=np.flatnonzero(np.isin(comp[f],(3,4,5)).all(1));tops=set(np.load(r/'lift03-far-road-tops.npz')['faces'].tolist())
profile=np.array(json.loads((r/'lift03-joined-shells-opening-profiles.json').read_text())['1']);rawprofile=profile.copy();profile[:,1]=np.minimum(profile[:,1],-2.08642578125);profile[:,2]=np.maximum(profile[:,2],2.08642578125)
keep=[]
for row in profile:
 keep.append(row)
 while len(keep)>2:
  a,b,last=keep[-3:];estimate=a[1:]+(last[1:]-a[1:])*(b[0]-a[0])/(last[0]-a[0])
  if max(abs(estimate-b[1:]))>1e-10:break
  keep.pop(-2)
profile=np.array(keep);ys,left,right=profile.T; rawkeep=[]
for row in rawprofile:
 rawkeep.append(row)
 while len(rawkeep)>2:
  a,b,last=rawkeep[-3:];estimate=a[1:]+(last[1:]-a[1:])*(b[0]-a[0])/(last[0]-a[0])
  if max(abs(estimate-b[1:]))>1e-10:break
  rawkeep.pop(-2)
rawprofile=np.array(rawkeep)
split_heights=np.unique(np.r_[ys,rawprofile[:,0]])
boundary=lambda y,sign:np.interp(y,ys,left if sign<0 else right)
cut_boundary=lambda y,sign:np.interp(y,rawprofile[:,0],rawprofile[:,1 if sign<0 else 2])
weight=lambda p:float(np.clip(p[0]/abs(boundary(p[1],-1 if p[0]<0 else 1)),-1,1))
verts=v.tolist();weights=[0.]*len(v);triangles=[];removed=[];recipes=[];fragments=[]
for i in np.flatnonzero(np.isin(comp,(3,4,5))):weights[i]=weight(v[i])
for ordinal in selected:
 if ordinal in tops:removed.append(int(ordinal));continue
 face=f[ordinal];points=v[face];cap=np.ptp(points[:,2])<1e-8;polys=[list(np.eye(3))]
 fw=np.array(weights)[face]
 if not cap and (np.all(fw==1) or np.all(fw==-1)):
  triangles.append(face.tolist());continue
 for y in split_heights[(split_heights>points[:,1].min()+1e-9)&(split_heights<points[:,1].max()-1e-9)]:
  out=[]
  for poly in polys:
   dist=lambda b:float(b@points[:,1]-y);ds=[dist(b) for b in poly]
   out.extend([clip(poly,dist,True),clip(poly,dist,False)] if min(ds)<-1e-9 and max(ds)>1e-9 else [poly])
  polys=out
 for sign,boundary_fn in ((-1,boundary),(1,boundary),(-1,cut_boundary),(1,cut_boundary)):
  out=[]
  for poly in polys:
   dist=lambda b:float((b@points)[0]-boundary_fn((b@points)[1],sign));ds=[dist(b) for b in poly]
   out.extend([clip(poly,dist,True),clip(poly,dist,False)] if min(ds)<-1e-9 and max(ds)>1e-9 else [poly])
  polys=out
 kept=[]
 for poly in polys:
  centre=np.mean(poly,axis=0)@points
  if cap and centre[1]>ys[0]+1e-8 and cut_boundary(centre[1],-1)+1e-8<centre[0]<cut_boundary(centre[1],1)-1e-8:continue
  kept.append(poly)
 if len(kept)==1 and len(polys)==1:
  triangles.append(face.tolist());continue
 removed.append(int(ordinal));local={};out=[]
 for poly in kept:
  polygon=[]
  for bary in poly:
   nz=np.flatnonzero(abs(bary)>1e-8)
   if len(nz)==1:idx=int(face[nz[0]])
   else:
    key=tuple(np.round(bary,10))
    if key not in local:
     wb,wc=map(float,np.float32(bary[1:]));p=points[0]*(1-wb-wc)+points[1]*wb+points[2]*wc
     if len(nz)==2:
      ia,ib=map(int,nz);fraction=float(np.float32(bary[ib]/sum(bary[nz])));p=points[ia]+(points[ib]-points[ia])*fraction
      recipe=('edge',int(face[ia]),int(face[ib]),fraction)
     else:recipe=('interior',*map(int,face),wb,wc)
     local[key]=len(verts);verts.append(p.tolist());weights.append(weight(p));recipes.append(recipe)
    idx=local[key]
   polygon.append(idx)
  out.extend([[polygon[0],polygon[k],polygon[k+1]] for k in range(1,len(polygon)-1)])
 if out:fragments.append((int(ordinal),out));triangles.extend(out)
p=np.array(verts);t=np.array(triangles);w=np.array(weights)
for delta in (-.25,0,2,8):
 q=p.copy();q[:,0]+=w*delta;a=p[t];b=q[t];na=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);nb=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);valid=np.linalg.norm(na,axis=1)>1e-7
 assert np.all((na[valid]*nb[valid]).sum(1)>0),(delta,'winding')
np.savez(r/'lift03-far-shell-candidate.npz',positions=p,indices=t,weights=w)
(r/'lift03-far-shell-candidate-recipes.json').write_text(json.dumps(dict(removed=removed,fragments=fragments,recipes=recipes)))
print('PASS four widths',len(t),'shell triangles',len(recipes),'constructed vertices',len(removed),'removed/replaced original faces',len(fragments),'fragments')

# Preserve native attribute interpolation while numbering every edge before interiors.
order=[i for i,item in enumerate(recipes) if item[0]=='edge']
edge_count=len(order)
order += [i for i,item in enumerate(recipes) if item[0]=='interior']
remap={len(v)+old:len(v)+new for new,old in enumerate(order)}
edges=[recipes[i][1:] for i in order[:edge_count]]
interiors=[recipes[i][1:] for i in order[edge_count:]]
for a,b,fraction in edges:
 assert 0<fraction<1
for a,b,cc,wb,wc in interiors:
 assert wb>0 and wc>0 and np.float32(wb)+np.float32(wc)<1
original_ids=np.flatnonzero(np.isin(comp,(3,4,5)))
assert len(original_ids)==348 and set(removed)-{i for i,_ in fragments}==tops
def number(value):return format(float(np.float32(value)),'.9g')+'f'
lines=[
 '// Measured Lift03 native far-shell openings; generated by Lift03FarStructure.py.',
 '// Source Geometry SHA-256: 4dd846ac20f020fb45de94549d66e0bbafda4c575554a5b235a93e7d5a6008e4',
 'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
 'internal static class Lift03FarStructure', '{',
 '    internal static MovableStructureModel.Plan Create(Vector3[] source, int[] indices, float delta)',
 '    {',
 '        if (source.Length != 996 || indices.Length != 852 * 3 || !float.IsFinite(delta))',
 '            throw new InvalidOperationException("Lift03 far structure requires its recorded native layout.");',
 '        var positions = (Vector3[])source.Clone();',
 '        Lift03RigidParts.Apply(2, source, positions, delta);',
 '        foreach (var item in Weights)',
 '            positions[item.Index].x = source[item.Index].x + item.Weight * delta;',
 '        var x = new float[ExtraWeights.Length];',
 '        for (var i = 0; i < Edges.Length; i++)',
 '        {',
 '            var edge = Edges[i];',
 '            x[i] = source[edge.A].x + (source[edge.B].x - source[edge.A].x) * edge.Fraction + ExtraWeights[i] * delta;',
 '        }',
 '        for (var i = 0; i < Interior.Length; i++)',
 '        {',
 '            var vertex = Interior[i];',
 '            var index = Edges.Length + i;',
 '            x[index] = source[vertex.A].x + (source[vertex.B].x - source[vertex.A].x) * vertex.WeightB',
 '                + (source[vertex.C].x - source[vertex.A].x) * vertex.WeightC + ExtraWeights[index] * delta;',
 '        }',
 '        var removed = new bool[852];',
 '        Lift03FarRoadTops.Remove(indices, removed);',
 '        Lift03SupportFaces.RemoveFar(indices, removed);',
 '        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { removed },',
 '            addedVertices: Edges, fragments: Fragments, interiorVertices: Interior, constructedX: x);',
 '    }',
 '    private static readonly (int Index, float Weight)[] Weights =', '    {']
lines += [f'        ({i}, {number(w[i])}),' for i in original_ids]+['    };']
lines += ['    private static readonly MovableStructureModel.EdgeVertex[] Edges =','    {']
lines += [f'        new({a}, {b}, {number(fraction)}),' for a,b,fraction in edges]+['    };']
lines += ['    private static readonly MovableStructureModel.TriangleVertex[] Interior =','    {']
lines += [f'        new({a}, {b}, {cc}, {number(wb)}, {number(wc)}),' for a,b,cc,wb,wc in interiors]+['    };']
lines += ['    private static readonly float[] ExtraWeights =','    {',
          '        '+', '.join(number(w[len(v)+i]) for i in order),'    };']
lines += ['    private static readonly MovableStructureModel.Fragment[] Fragments =','    {']
for ordinal,triangles in fragments:
 flat=[remap.get(i,i) for tri in triangles for i in tri]
 lines.append('        new(0, '+str(ordinal)+', '+', '.join(map(str,flat))+'),')
lines += ['    };','}','']
output.write_text('\n'.join(lines),encoding='utf-8')


