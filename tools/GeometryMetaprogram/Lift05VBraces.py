"""Emit separate native Lift05 V-brace members.
Usage: python Lift05VBraces.py RESEARCH_DIRECTORY OUTPUT_CS
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
 components=range(893,901) if lod==0 else range(556,564) if lod==1 else []
 parts=[];owned=set();modes=np.full(len(v),9,dtype=int)
 for component in components:
  ids=np.flatnonzero(data['component']==component);ff=f[data['component'][f[:,0]]==component]
  parent={int(i):int(i) for i in ids}
  def root(i):
   while parent[i]!=i:i=parent[i]
   return i
  for a,b,cc in ff:parent[root(int(b))]=root(int(a));parent[root(int(cc))]=root(int(a))
  indexed={}
  for i in ids:indexed.setdefault(root(int(i)),[]).append(int(i))
  members={-1:[],0:[],1:[]}
  for indices in indexed.values():
   lo,hi=v[indices,0].min(),v[indices,0].max()
   mode=0 if lo<=0<=hi else (1 if lo>0 else -1)
   members[mode].extend(indices)
  for mode,indices in members.items():
   ids=np.array(sorted(indices));assert len(ids)>0 and owned.isdisjoint(ids);owned.update(ids)
   low,high=float(v[ids,0].min()),float(v[ids,0].max())
   assert (low<0<high) if mode==0 else (low>0 if mode==1 else high<0)
   modes[ids]=mode;parts.append((component,mode,ids,low,high))
 # No source triangle crosses these separately authored member groups.
 selected=modes!=9
 assert np.all(selected[f].any(1)==selected[f].all(1))
 sf=f[selected[f].all(1)]
 assert np.all(modes[sf]==modes[sf[:,0],None])
 if lod==2:
  # Native far mesh omits the small V-brace segments; no substitute is added.
  assert not np.any((abs(v[:,0])<3.7)&(v[:,1]>14.8)&(v[:,1]<17.3))
 for delta in (-8,-4,-1,-.25,0,2,8):
  changed=v.astype(float).copy()
  for component,mode,ids,low,high in parts:
   if mode:changed[ids,0]+=mode*delta
   else:changed[ids,0]+=(2*changed[ids,0]-low-high)/(high-low)*delta
  assert np.array_equal(changed[:,1:],v[:,1:]) and np.array_equal(changed[~selected],v[~selected])
  a=np.cross(v[sf[:,1]].astype(float)-v[sf[:,0]],v[sf[:,2]].astype(float)-v[sf[:,0]])
  b=np.cross(changed[sf[:,1]]-changed[sf[:,0]],changed[sf[:,2]]-changed[sf[:,0]])
  valid=np.einsum('ij,ij->i',a,a)>1e-16
  assert np.isfinite(changed).all()
  if delta >= -.25: assert np.all(np.einsum('ij,ij->i',a[valid],b[valid])>0)
  rigid=sf[modes[sf[:,0]]!=0]
  for i in (1,2):assert np.allclose(changed[rigid[:,i]]-changed[rigid[:,0]],v[rigid[:,i]].astype(float)-v[rigid[:,0]],atol=1e-12,rtol=0)
  if delta==8:np.savez(r/f'lift05-vbraces-{lod}-8.npz',positions=changed,indices=f,selected=selected,modes=modes)
 levels.append((record,len(v),parts))
 print(f'LOD{lod}: {len(parts)} V-brace members, {len(owned)} vertices; rigid sides and seven signed widths and positive-span winding passed')

lines=['// Native LiftBridge05 V-brace members; generated by Lift05VBraces.py.',
 '// Side diagonals translate rigidly; only the separate centre bars stretch.',
 'using System;','using UnityEngine;','','namespace BridgeBuilder.Bridges;','',
 'internal static class Lift05VBraces','{',
 '    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)','    {',
 '        var data = lod switch','        {']
for lod,(record,count,parts) in enumerate(levels):lines.append(f'            {lod} => (Count: {count}, Parts: Parts{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))','        };',
 '        if (source.Length != data.Count || positions.Length != data.Count || !float.IsFinite(delta))',
 '            throw new InvalidOperationException("Lift05 V-brace source layout changed.");',
 '        foreach (var part in data.Parts)','        {','            var span = part.Max - part.Min;',
 '            foreach (var i in part.Vertices)',
 '                positions[i].x = source[i].x + (part.Side != 0 ? part.Side :',
 '                    (2f * source[i].x - part.Min - part.Max) / span) * delta;',
 '        }','    }','']
for lod,(record,count,parts) in enumerate(levels):
 lines+=['    // Geometry SHA-256: '+record['sha256'],
 f'    private static readonly (int Side, float Min, float Max, int[] Vertices)[] Parts{lod} =','    {']
 for component,mode,ids,low,high in parts:
  lines += [f'        // Native component {component}, member side {mode}.',f'        ({mode}, {low:.17g}f, {high:.17g}f, new[]', '        {']
  for start in range(0,len(ids),16):lines.append('            '+', '.join(str(i) for i in ids[start:start+16])+',')
  lines+=['        }),']
 lines+=['    };','']
lines+=['}',''];output.write_text('\n'.join(lines),encoding='utf-8')
