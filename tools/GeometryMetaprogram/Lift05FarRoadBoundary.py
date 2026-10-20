"""Reconstruct Lift05 far fixed-road openings from measured detailed boundaries."""
from pathlib import Path
import ctypes as c
import hashlib, json, struct, sys
import numpy as np
r,output=map(Path,sys.argv[1:])
records=json.loads((r/'lift-native-road-plane-candidates.json').read_text())
dll=c.CDLL(r'D:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Plugins\x86_64\PipelinePlugin.dll')
for name,args,result in (
 ('decompress_calc_bound',[c.c_void_p,c.c_int64,c.c_int],c.c_int64),
 ('decompress_data',[c.c_void_p,c.c_int64,c.c_void_p,c.c_int64,c.c_int],c.c_int64),
 ('decompress_meshopt_index_buffer',[c.c_void_p,c.c_int64,c.c_int,c.c_int,c.c_void_p],c.c_int),
 ('decompress_meshopt_vertex_attribute',[c.c_void_p,c.c_int64,c.c_int,c.c_int,c.c_void_p],c.c_int)):
 method=getattr(dll,name);method.argtypes=args;method.restype=result
def clip(poly, points, boundary, keep_left):
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        xa, xb = a @ points[:, 0], b @ points[:, 0]
        ina = xa <= boundary if keep_left else xa >= boundary
        inb = xb <= boundary if keep_left else xb >= boundary
        if ina:
            result.append(a)
        if ina != inb:
            result.append(a + (b-a) * ((boundary-xa)/(xb-xa)))
    clean = []
    for point in result:
        if not clean or np.max(abs(point-clean[-1])) > 1e-10:
            clean.append(point)
    if len(clean) > 1 and np.max(abs(clean[0]-clean[-1])) <= 1e-10:
        clean.pop()
    return clean


levels=[]
inventory=json.loads((r/'lift05-main-parts.json').read_text())
for lod in (2,):
 record=next(x for x in records if x['style']==5 and x['lod']==lod)
 path=r/'movable-skin'/record['geometry'];raw=path.read_bytes()
 assert hashlib.sha256(raw).hexdigest()==record['sha256']
 version,meshes,flags=struct.unpack_from('<HII',raw)
 assert version==1 and meshes==(1 if lod==2 else 2)
 headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(meshes)];h=headers[0]
 def decode(block,count,stride,index=False):
  if flags&2:
   size=dll.decompress_calc_bound(block,len(block),0);assert size>0
   buf=c.create_string_buffer(size);assert dll.decompress_data(block,len(block),buf,size,0)==size;block=buf.raw
  if flags&1:
   buf=c.create_string_buffer(count*stride)
   method=dll.decompress_meshopt_index_buffer if index else dll.decompress_meshopt_vertex_attribute
   assert method(block,len(block),count,stride,buf)==0;block=buf.raw
  assert len(block)==count*stride
  return block
 offset=10+meshes*88+sum(x[-1]*40 for x in headers)
 stride=4 if h[17]&1 else 2
 f=np.frombuffer(decode(raw[offset:offset+h[16]],h[19],stride,True),dtype='<u4' if stride==4 else '<u2').reshape(-1,3)
 offset+=sum(x[16] for x in headers)
 assert h[0]&15==0 and h[1]&3==2
 v=np.frombuffer(decode(raw[offset:offset+h[2]],h[18],12),dtype='<f4').reshape(-1,3)
 faces=[];bones=[];bone=-1
 for line in path.with_suffix('.mesh-0.obj').read_text().splitlines():
  fields=line.split()
  if not fields:continue
  if fields[0]=='g':bone=int(fields[1][5:]) if fields[1].startswith('bone_') else -1
  elif fields[0]=='f':faces.append([int(x.split('/')[0])-1 for x in fields[1:]]);bones.append(bone)
 assert np.array_equal(f,faces)
 bones=np.array(bones);p=v[f].astype(float)
 normal=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0])
 data=np.load(r/f'lift05-main-{lod}-0.npz')
 assert np.array_equal(f,data['indices']) and np.allclose(v,data['positions'],atol=1e-5,rtol=1e-7)

v=v.astype(float)
selected=np.isin(data['component'][f[:,0]],(0,20))&(v[f][:,:,1].max(1)<=10.00001)
top=selected&(np.ptp(v[f][:,:,1],axis=1)<1e-6)&(abs(v[f][:,0,1]-10)<1e-6)
ids=set(map(int,np.flatnonzero(selected)));tops=set(map(int,np.flatnonzero(top)))
assert len(ids)==60 and len(tops)==12
def clip(poly,points,x,left):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  xa,xb=a@points[:,0],b@points[:,0];ia=xa<=x if left else xa>=x;ib=xb<=x if left else xb>=x
  if ia:out.append(a)
  if ia!=ib:out.append(a+(b-a)*(x-xa)/(xb-xa))
 return out
out=[];owners=[];fragments={};pos=v.tolist();recipes=[];removed=[]
for fid in sorted(ids):
 face=f[fid];p=v[face];polys=[list(np.eye(3))]
 for x in (-4,4):
  next=[]
  for poly in polys:
   xs=np.array(poly)@p[:,0]
   if min(xs)<x<max(xs):next += [clip(poly,p,x,True),clip(poly,p,x,False)]
   else:next.append(poly)
  polys=next
 for poly in polys:
  points=np.array(poly)@p
  if fid in tops and abs(points[:,0].mean())<4-1e-6:removed.append(fid);continue
  ii=[]
  for bary in poly:
   nz=np.flatnonzero(abs(bary)>1e-9)
   if len(nz)==1:ii.append(int(face[nz[0]]));continue
   assert len(nz)==2
   q=bary@p;target=q.copy()
   if abs(q[1]-10)<1e-6:
    # Native detailed receiver boundary; preserve outer authored base corners.
    target[1]=9.800048828125
    target[2]=np.sign(q[2])*(62.580078125+(abs(q[2])-62.5)*(84-62.580078125)/(84.25-62.5))
   ii.append(len(pos));pos.append(target.tolist());recipes.append({'face':fid,'source':q.tolist(),'target':target.tolist(),'edge':[int(face[nz[0]]),int(face[nz[1]])],'fraction':float(bary[nz[1]])})
  for j in range(1,len(ii)-1):
   tri=[ii[0],ii[j],ii[j+1]];q=np.array(pos)[tri]
   if np.linalg.norm(np.cross(q[1]-q[0],q[2]-q[0]))>1e-8:
    out.append(tri);owners.append(fid);fragments.setdefault(fid,[]).extend(tri)

positions=np.array(pos);triangles=np.array(out)
for delta in (-.25,0,2,8):
 moved=positions.copy();moved[:,0]+=np.sign(positions[:,0])*delta
 a=v[f[np.array(owners)]];b=moved[triangles]
 n0=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);n1=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0])
 assert np.all(np.sum(n0*n1,axis=1)>0)
print('PASS: 60 native base faces ->',len(out),'faces;',len(recipes),'edge instances; four-width winding')
lines=['// Generated by Lift05FarRoadBoundary.py from the checked native Geometry.',
'using System;','using System.Collections.Generic;','using System.Linq;','using UnityEngine;','',
'namespace BridgeBuilder.Bridges;','', 'internal static class Lift05FarRoadBoundary','{',
'    internal static MovableStructureModel.Plan Apply(MovableStructureModel.Plan plan, float delta)','    {',
'        const int count = 2500;',
'        if (plan.SourcePositions.Length != count || plan.SourceIndices.Length != 1 ||',
'            plan.SourceIndices[0].Length != 5472 || plan.InteriorVertices.Length != 0 || !float.IsFinite(delta))',
'            throw new InvalidOperationException("Lift05 far road requires the measured native main mesh.");',
'        var removed = (bool[])plan.RemoveTriangles[0].Clone();',
'        foreach (var face in Faces)','        {',
'            var i = face.Triangle * 3; var indices = plan.SourceIndices[0];',
'            if (removed[face.Triangle] || indices[i] != face.A || indices[i + 1] != face.B || indices[i + 2] != face.C)',
'                throw new InvalidOperationException("Lift05 far road source topology or fragment ownership changed.");',
'        }',
'        var offset = plan.AddedVertices.Length;',
'        var edges = new List<MovableStructureModel.EdgeVertex>(plan.AddedVertices);',
'        var x = plan.Positions.Skip(count).Select(p => p.x).ToList();',
'        foreach (var edge in Edges)','        {',
'            edges.Add(new MovableStructureModel.EdgeVertex(edge.A, edge.B, edge.Fraction));',
'            x.Add(edge.X + Math.Sign(edge.X) * delta);','        }',
'        var fragments = new List<MovableStructureModel.Fragment>(plan.Fragments);',
'        foreach (var fragment in Fragments)','        {',
'            var indices = fragment.Indices.Select(i => i >= count ? i + offset : i).ToArray();',
'            fragments.Add(new MovableStructureModel.Fragment(0, fragment.Triangle, indices));','        }',
'        var result = new MovableStructureModel.Plan(plan.SourcePositions, plan.Positions.Take(count).ToArray(),',
'            plan.SourceIndices, new[] { removed }, edges.ToArray(), fragments.ToArray(), constructedX: x.ToArray());',
'        // Only new measured boundary vertices move in Y/Z. Every original authored',
'        // vertex stays intact; raw channels interpolate on the recorded source edges.',
'        for (var i = 0; i < Edges.Length; i++)','        {',
'            result.Positions[count + offset + i].y = Edges[i].Y;',
'            result.Positions[count + offset + i].z = Edges[i].Z;','        }',
'        return result;','    }','',
'    private static readonly (int Triangle, int A, int B, int C)[] Faces =','    {']
for fid in sorted(ids):lines.append(f'        ({fid}, '+', '.join(map(str,f[fid]))+'),')
lines+=['    };','', '    private static readonly (int A, int B, float Fraction, float X, float Y, float Z)[] Edges =','    {']
for e in recipes:
 a,b=e['edge'];values=[e['fraction']]+e['target'];lines.append(f'        ({a}, {b}, '+', '.join(f'{x:.12g}f' for x in values)+'),')
lines+=['    };','', '    private static readonly MovableStructureModel.Fragment[] Fragments =','    {']
assert set(fragments)==ids
for fid,ii in sorted(fragments.items()):lines.append(f'        new(0, {fid}, '+', '.join(map(str,ii))+'),')
lines+=['    };','}',''];output.write_text('\n'.join(lines),encoding='utf-8')
