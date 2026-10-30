"""Read native packed tangent frames for original Lift03 road faces."""
import ctypes as C,hashlib,json,struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Lift03';RESEARCH=Path(r'C:\Users\admin\Documents\Codex\2026-10-15\c-users-admin-documents-codex-cs2bridgebuilder\work\bridge-research')
dll=C.CDLL(r'D:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Plugins\x86_64\PipelinePlugin.dll')
bound=dll.decompress_calc_bound;bound.argtypes=[C.c_void_p,C.c_int64,C.c_int];bound.restype=C.c_int64
decode=dll.decompress_data;decode.argtypes=[C.c_void_p,C.c_int64,C.c_void_p,C.c_int64,C.c_int];decode.restype=C.c_int64
vertex=dll.decompress_meshopt_vertex_attribute;vertex.argtypes=[C.c_void_p,C.c_int64,C.c_int,C.c_int,C.c_void_p];vertex.restype=C.c_int
rows=[];units=[]
for lod in range(3):
 name='LiftBridge03'+(f'_LOD{lod}' if lod else '')+' Mesh';raw=(RESEARCH/'movable-skin'/(name+'.Geometry')).read_bytes();version,n,flags=struct.unpack_from('<HII',raw);hs=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(n)];off=10+88*n+sum(h[-1]*40+h[16] for h in hs);channels={}
 for attr in range(14):
  for mesh,h in enumerate(hs):
   size=h[2+attr];b=raw[off:off+size];off+=size
   if mesh!=0 or attr not in [1,2]:continue
   fmt=(h[0]>>(attr*4))&15;dim=((h[1]>>(attr*2))&3)+1;assert (fmt,dim)==((5,2) if attr==1 else (0,1))
   if flags&2:
    count=bound(b,len(b),0);out=C.create_string_buffer(count);assert decode(b,len(b),out,count,0)==count;b=out.raw
   if flags&1:
    out=C.create_string_buffer(h[18]*4);assert vertex(b,len(b),h[18],4,out)==0;b=out.raw
   channels[attr]=np.frombuffer(b,'<u4')
 mesh=np.load(ROOT/'artifacts/uv-density/native'/(name+'.npz'));faces=mesh['mesh0_faces'];xyz=mesh['mesh0_positions'];audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift03/outputs/native-repeat-mechanism/audit.json').read_text())
 for report in [r for r in audit['reports'] if r['lod']==lod]:
  ids=np.array(report['faceIds']);p=xyz[faces[ids]]
  if lod<2:
   period=8 if report['deck']=='Main' else 4;u=ids[np.all(p[:,:,2]>=40,axis=1)&np.all(p[:,:,2]<=40+period,axis=1)&np.all(p[:,:,0]<=0,axis=1)];v=np.unique(faces[u]);pairs,counts=np.unique(np.stack([channels[1][v],channels[2][v]],axis=-1),axis=0,return_counts=True)
   units.append(dict(lod=lod,deck=report['deck'],faceIds=u.tolist(),frames=[dict(normalBits=int(a),tangentBits=int(b),vertices=int(c)) for (a,b),c in zip(pairs,counts)]))
  for xside in [-1,1]:
   for zside in [-1,1]:
    fids=ids[np.all(p[:,:,0]*xside>=0,axis=1)&np.all(p[:,:,2]*zside>=0,axis=1)];verts=np.unique(faces[fids]);pairs,counts=np.unique(np.stack([channels[1][verts],channels[2][verts]],axis=-1),axis=0,return_counts=True)
    rows.append(dict(lod=lod,deck=report['deck'],xSide=xside,zSide=zside,sourceGeometrySHA256=hashlib.sha256(raw).hexdigest(),faceCount=len(fids),frames=[dict(normalBits=int(a),tangentBits=int(b),vertices=int(c)) for (a,b),c in zip(pairs,counts)]))
result=dict(rows=rows,units=units)
(OUT/'native-road-packed-frames.json').write_text(json.dumps(result,indent=2))
for kind,symbol,count in [('normal','math_array_oct16_to_fp32normals',3),('tangent','math_array_oct15_to_fp32tangents',4)]:
 native=getattr(dll,symbol);native.argtypes=[C.c_void_p,C.c_int,C.c_void_p];native.restype=None
 for row in rows+units:
  for frame in row['frames']:
   src=(C.c_uint*1)(frame[kind+'Bits']);dst=(C.c_float*count)();native(src,1,dst);frame[kind]=list(dst)
result['decoder']='Original PipelinePlugin math_array_oct16_to_fp32normals / math_array_oct15_to_fp32tangents, matching Game.Prefabs.MeshNormal/MeshTangent.Unpack paths.'
(OUT/'native-road-decoded-frames.json').write_text(json.dumps(result,indent=2))
print(json.dumps(units,indent=2))
