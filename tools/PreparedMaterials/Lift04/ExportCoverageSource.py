"""Export real Lift04 native mesh bytes for the isolated coverage probe."""
import ctypes as c,struct,zipfile,json,hashlib
from pathlib import Path
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Lift04/coverage54';O.mkdir(exist_ok=True)
G=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
ns={'__name__':'fixture'};exec((R/'tools/GeometryMetaprogram/UvDensityAudit.py').read_text(),ns);dll=ns['NativeGeometry'](G/'Plugins/x86_64/PipelinePlugin.dll').dll
wanted=['37c9d971785164e3ffad27e43316fb1e','cfacdca3e966b81c975f0c598dce68ed','02e211c2a51037ea43d6727acdd28922'];reports=[]
with zipfile.ZipFile(G/'Content/BridgesAndPorts/Blob.cok') as z:
 for entry in z.namelist():
  if not entry.endswith('.Geometry.cid'):continue
  cid=z.read(entry).decode().strip()
  if cid not in wanted:continue
  lod=wanted.index(cid);raw=z.read(entry[:-4]);(O/f'lod{lod}.Geometry').write_bytes(raw)
  version,count,flags=struct.unpack_from('<HII',raw);assert version==1
  hs=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)];offset=10+count*88+sum(h[-1]*40 for h in hs)
  def block(size,n,stride,index=False):
   global offset
   b=raw[offset:offset+size];offset+=size
   if flags&2:
    cap=dll.decompress_calc_bound(b,len(b),0);buf=c.create_string_buffer(cap);assert dll.decompress_data(b,len(b),buf,cap,0)==cap;b=buf.raw
   if flags&1:
    buf=c.create_string_buffer(n*stride);fn=dll.decompress_meshopt_index_buffer if index else dll.decompress_meshopt_vertex_attribute;assert fn(b,len(b),n,stride,buf)==0;b=buf.raw
   assert len(b)==n*stride;return b
  ix=[block(h[16],h[19],4 if h[17]&1 else 2,True) for h in hs];attrs=[[] for h in hs]
  for a in range(14):
   for m,h in enumerate(hs):
    if not h[2+a]:continue
    fmt=(h[0]>>(a*4))&15;dim=((h[1]>>(a*2))&3)+1;stride=[4,2,1,1,2,2,1,1,2,2,4,4][fmt]*dim
    attrs[m].append((a,fmt,dim,block(h[2+a],h[18],stride)))
  for m,h in enumerate(hs):
   stem=f'lod{lod}-mesh{m}'; stride=4 if h[17]&1 else 2
   with (O/(stem+'-raw.bin')).open('wb') as f:
    f.write(struct.pack('<4i',h[18],h[19],stride,len(attrs[m])));f.write(ix[m])
    for a,fmt,dim,b in attrs[m]:f.write(struct.pack('<4i',a,fmt,dim,len(b)));f.write(b)
   pos=next(b for a,fmt,dim,b in attrs[m] if a==0);assert len(pos)==h[18]*12
   indices=struct.unpack('<'+('I' if stride==4 else 'H')*h[19],ix[m])
   (O/(stem+'-fixture.bin')).write_bytes(struct.pack('<2i',h[18],h[19])+pos+struct.pack('<'+'i'*h[19],*indices))
  reports.append(dict(lod=lod,cid=cid,sha256=hashlib.sha256(raw).hexdigest(),meshes=[dict(vertices=h[18],faces=h[19]//3) for h in hs]))
assert len(reports)==3
(O/'source.json').write_text(json.dumps(reports,indent=2));print(reports)
