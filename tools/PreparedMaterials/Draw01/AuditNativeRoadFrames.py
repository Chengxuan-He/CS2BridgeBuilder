"""Read native packed frames on the exact measured road cells."""
import ctypes as ct, hashlib, json, struct, sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry
out=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
recipe=json.loads((out/'recipe.json').read_text())
graph=json.loads((ROOT/'artifacts/layer-decoupling/Draw01/outputs/native-archetype-audit/native-source-graph.json').read_text())
decoder=NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
rows=[]
for lod in recipe['lods']:
 raw=(ROOT/'artifacts/layer-decoupling/Draw01/cache'/(graph['mainMovingLODs'][lod['lod']]['geometryCid']+'.Geometry')).read_bytes()
 version,count,flags=struct.unpack_from('<HII',raw);assert version==1 and flags==3
 headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
 offset=10+count*88+sum(h[-1]*40 for h in headers)+sum(h[16] for h in headers)
 attrs={}
 for attr in range(14):
  for mi,h in enumerate(headers):
   size=h[2+attr];data=raw[offset:offset+size];offset+=size
   if mi!=0 or attr not in [1,2,13]:continue
   fmt=(h[0]>>(attr*4))&15;dim=((h[1]>>(attr*2))&3)+1
   assert (fmt,dim) == ({1:(5,2),2:(0,1),13:(10,1)}[attr])
   capacity=decoder.dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(capacity)
   assert decoder.dll.decompress_data(data,len(data),buffer,capacity,0)==capacity
   decoded=ct.create_string_buffer(h[18]*4)
   assert decoder.dll.decompress_meshopt_vertex_attribute(buffer,capacity,h[18],4,decoded)==0
   attrs[attr]=np.frombuffer(decoded.raw,dtype='<u4')
 if lod['lod']==1:np.save(out/'middle-tangent-bits.npy',attrs[2])
 mesh=np.load(ROOT/'artifacts/uv-density/native'/lod['sourceFile'])
 for row in lod['domains']:
  for cell in row['cells']:
   ids=np.unique(mesh['mesh0_faces'][cell['faces']])
   cell['normalBits']=np.unique(attrs[1][ids]).astype(int).tolist()
   cell['tangentBits']=np.unique(attrs[2][ids]).astype(int).tolist()
   cell['boneIndices']=np.unique(attrs[13][ids]).astype(int).tolist()
  rows.append(dict(lod=lod['lod'],domain=row['domain'],right=row['right'],positive=row['positive'],
    normalBits=sorted({v for c in row['cells'] for v in c['normalBits']}),
    tangentBits=sorted({v for c in row['cells'] for v in c['tangentBits']}),
    bones=sorted({v for c in row['cells'] for v in c['boneIndices']})))
(out/'recipe.json').write_text(json.dumps(recipe,indent=2)+'\n')
(out/'frames.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps(rows[:16],indent=2))
