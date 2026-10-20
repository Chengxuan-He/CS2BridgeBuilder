"""Emit measured rigid sides and crossing spans for Lift05 tower-top rings.
Usage: python Lift05RoofRings.py RESEARCH_DIRECTORY OUTPUT_CS
"""
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
for lod in range(3):
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
 parts=((70,464),(70,307),(6,25))[lod]
 ids=np.flatnonzero(np.isin(data['component'],parts));selected=np.zeros(len(v),dtype=bool);selected[ids]=True
 assert np.all(selected[f].any(1)==selected[f].all(1))
 kept=np.flatnonzero(selected[f].all(1));assert np.all(bones[kept]==0)
 # The two native inner corners are X +/-11.25 at all LODs. The bevelled
 # side pieces stay rigid; only the straight transverse span between them grows.
 left,right=-11.25,11.25
 assert left in v[ids,0] and right in v[ids,0]
 weights={int(i):float(np.clip(v[i,0]/11.25,-1,1)) for i in ids}
 edges=[];extra=[];fragments=[]
 for fid in kept:
  face=f[fid];points=v[face].astype(float)
  if not any(points[:,0].min()<cut<points[:,0].max() for cut in (left,right)):continue
  polys=[list(np.eye(3))]
  for cut in (left,right):
   result=[]
   for poly in polys:
    xs=[p@points[:,0] for p in poly]
    if min(xs)<cut<max(xs):result += [clip(poly,points,cut,True),clip(poly,points,cut,False)]
    else:result.append(poly)
   polys=result
  indices=[];local={}
  for poly in polys:
   polygon=[]
   for bary in poly:
    nz=np.flatnonzero(abs(bary)>1e-10)
    if len(nz)==1:polygon.append(int(face[nz[0]]))
    else:
     assert len(nz)==2
     a,b=map(int,nz);fraction=float(np.float32(bary[b]));key=(int(face[a]),int(face[b]),fraction)
     if key not in local:
      local[key]=len(v)+len(edges);point=points[a]+(points[b]-points[a])*fraction
      assert min(abs(point[0]-left),abs(point[0]-right))<1e-6
      edges.append((*key,-1 if point[0]<0 else 1));extra.append(point)
     polygon.append(local[key])
   for i in range(1,len(polygon)-1):indices += [polygon[0],polygon[i],polygon[i+1]]
  fragments.append((int(fid),indices))
 replaced={fid for fid,_ in fragments}
 complete=np.array([f[i] for i in kept if i not in replaced]+[ii[j:j+3] for _,ii in fragments for j in range(0,len(ii),3)])
 allv=np.concatenate((v.astype(float),np.array(extra).reshape(-1,3)))
 for fid,ii in fragments:
  p=v[f[fid]].astype(float);q=allv[np.array(ii).reshape(-1,3)]
  a=np.linalg.norm(np.cross(p[1]-p[0],p[2]-p[0]));b=np.linalg.norm(np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]),axis=1).sum()
  assert abs(a-b)<3e-5
 for delta in (-12,-8,-4,-1,-.25,0,2,8):
  moved=allv.copy()
  for i,weight in weights.items():moved[i,0]+=weight*delta
  for i,edge in enumerate(edges):moved[len(v)+i,0]+=edge[3]*delta
  assert np.array_equal(moved[:,1:],allv[:,1:])
  a,b=allv[complete],moved[complete];n0=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);n1=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0])
  valid=np.linalg.norm(n0,axis=1)>1e-8
  assert np.isfinite(moved).all()
  if delta >= -.25: assert np.all(np.sum(n0[valid]*n1[valid],axis=1)>0)
  print(f'LOD{lod} delta={delta}: {np.count_nonzero(np.sum(n0[valid]*n1[valid],axis=1)<0)} inverted triangles; signed mapping retained')
  # Entire side fragments, including new boundary vertices, translate rigidly.
  for side in (-1,1):
   ss=np.all(side*allv[complete][:,:,0]>=11.25-1e-6,axis=1)
   if np.any(ss):assert np.allclose(b[ss]-a[ss],np.array([side*delta,0,0]),atol=1e-6)
  if delta==8:np.savez(r/f'lift05-roof-rings-{lod}-8.npz',positions=moved,source=allv,indices=complete)
 levels.append((record,len(v),len(f),weights,edges,fragments))
 print(f'LOD{lod}: {len(ids)} ring vertices; {len(kept)} -> {len(complete)} faces; {len(edges)} edge vertices; source area, eight signed widths, side rigidity and positive-span winding verified')

lines=['// Native LiftBridge05 tower-top rings, generated by Lift05RoofRings.py.',
 '// Fixed bone 0; side corners translate, measured transverse spans stretch.',
 'using System;','using UnityEngine;','','namespace BridgeBuilder.Bridges;','',
 'internal static class Lift05RoofRings','{',
 '    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)','    {',
 '        var data = lod switch','        {']
for lod,(record,count,fc,weights,edges,fragments) in enumerate(levels):
 lines.append(f'            {lod} => (Count: {count}, Faces: {fc}, Motion: Motion{lod}, Edges: Edges{lod}, Fragments: Fragments{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))','        };',
 '        if (source.Length != data.Count || indices.Length != data.Faces * 3 ||',
 '            !float.IsFinite(delta))',
 '            throw new InvalidOperationException("Lift05 roof rings require their native source.");',
 '        var positions = (Vector3[])source.Clone();',
 '        foreach (var item in data.Motion)',
 '            for (var i = item.First; i <= item.Last; i++)',
 '                positions[i].x = source[i].x + item.Weight * delta;',
 '        var edges = new MovableStructureModel.EdgeVertex[data.Edges.Length];',
 '        var x = new float[edges.Length];',
 '        for (var i = 0; i < edges.Length; i++)','        {','            var edge = data.Edges[i];',
 '            edges[i] = new MovableStructureModel.EdgeVertex(edge.A, edge.B, edge.Fraction);',
 '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) + edge.Weight * delta;',
 '        }',
 '        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[data.Faces] },',
 '            addedVertices: edges, fragments: data.Fragments, constructedX: x);','    }','']
for lod,(record,count,fc,weights,edges,fragments) in enumerate(levels):
 lines+=['    // Geometry SHA-256: '+record['sha256'],
 f'    private static readonly (int First, int Last, float Weight)[] Motion{lod} =','    {']
 ranges=[]
 for i,w in weights.items():
  if w==0:continue
  if ranges and ranges[-1][1]+1==i and ranges[-1][2]==w:ranges[-1][1]=i
  else:ranges.append([i,i,w])
 lines += [f'        ({a}, {b}, {w:.9g}f),' for a,b,w in ranges]
 lines+=['    };','',f'    private static readonly (int A, int B, float Fraction, int Weight)[] Edges{lod} =','    {']
 lines += [f'        ({a}, {b}, {frac:.9g}f, {w}),' for a,b,frac,w in edges]
 lines+=['    };','',f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} =','    {']
 lines += [f'        new(0, {fid}, '+', '.join(map(str,ii))+'),' for fid,ii in fragments]
 lines+=['    };','']
lines+=['}',''];output.write_text('\n'.join(lines),encoding='utf-8')
