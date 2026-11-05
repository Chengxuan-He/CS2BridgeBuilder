"""Extract exact native attribute fixtures for Draw02 underside regression."""
from pathlib import Path
import ctypes as ct,struct,zipfile,hashlib,json
from UvDensityAudit import NativeGeometry
R=Path(__file__).resolve().parents[2];G=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data');O=R/'artifacts/material-remap/Draw02/underside52'
reader=NativeGeometry(G/'Plugins/x86_64/PipelinePlugin.dll'); receipts=[]
entries=['DrawBridge02_70a2710de5addd5013db1b2da2550089.Geometry','DrawBridge02_LOD1_22180048b038d78cf141cf3bcedb97ae.Geometry','DrawBridge02_LOD2_5137bc3c523b4b6d961694d0b867d31d.Geometry']
with zipfile.ZipFile(G/'Content/BridgesAndPorts/Blob.cok') as archive:
 for lod,entry in enumerate(entries):
  raw=archive.read(entry);version,count,flags=struct.unpack_from('<HII',raw);assert version==1
  headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)];offset=10+count*88+sum(h[-1]*40 for h in headers)
  def block(size,elements,stride,index=False):
   global offset
   data=raw[offset:offset+size];offset+=size
   if flags&2:
    capacity=reader.dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(capacity);assert reader.dll.decompress_data(data,len(data),buffer,capacity,0)==capacity;data=buffer.raw
   if flags&1:
    buffer=ct.create_string_buffer(elements*stride);fn=reader.dll.decompress_meshopt_index_buffer if index else reader.dll.decompress_meshopt_vertex_attribute;assert fn(data,len(data),elements,stride,buffer)==0;data=buffer.raw
   assert len(data)==elements*stride;return data
  indices=[]
  for h in headers:indices.append(block(h[16],h[19],4 if h[17]&1 else 2,True))
  attrs=[[] for _ in headers]
  sizes=[4,2,1,1,2,2,1,1,2,2,4,4]
  for a in range(14):
   for i,h in enumerate(headers):
    size=h[2+a]
    if not size:continue
    fmt=h[0]>>(a*4)&15;dim=(h[1]>>(a*2)&3)+1;payload=block(size,h[18],sizes[fmt]*dim);attrs[i].append((a,fmt,dim,payload))
  h=headers[0]
  with (O/f'lod{lod}-raw.bin').open('wb') as f:
   f.write(struct.pack('<4i',h[18],h[19],4 if h[17]&1 else 2,len(attrs[0])));f.write(indices[0])
   for a,fmt,dim,payload in attrs[0]:f.write(struct.pack('<4i',a,fmt,dim,len(payload)));f.write(payload)
  receipts.append(dict(lod=lod,entry=entry,sha256=hashlib.sha256(raw).hexdigest(),attributes=[dict(attribute=a,format=fmt,dimension=dim,bytes=len(p),sha256=hashlib.sha256(p).hexdigest()) for a,fmt,dim,p in attrs[0]]))
(O/'raw-source-receipt.json').write_text(json.dumps(receipts,indent=2));print([(r['lod'],r['attributes']) for r in receipts])