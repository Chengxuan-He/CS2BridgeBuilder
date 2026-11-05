"""Independently decode the serialized Lift03 products and compare source texels."""
from pathlib import Path
import ctypes,struct,hashlib,json,sys,numpy as np
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'tools/PreparedMaterials/Draw03'));from ComposeRoadPeriods import bc7
O=R/'artifacts/material-remap/Lift03/appearance76';report=json.loads((O/'prepared.json').read_text());codec=ctypes.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll');decode=codec.decompress_data;decode.argtypes=[ctypes.c_void_p,ctypes.c_longlong,ctypes.c_void_p,ctypes.c_longlong,ctypes.c_int];decode.restype=ctypes.c_longlong
sources={}
for prop,item in report['sources'].items():
 raw=(R/'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2/input'/(item['cid']+'.Texture')).read_bytes();assert hashlib.sha256(raw).hexdigest()==item['sha256'];sources[prop]=next(bc7(raw))[1]
rows=[]
for unit in report['units']:
 xy=np.load(O/(unit['name']+'-source-map.npz'));x,y=xy['sourceX'],xy['sourceY'];assert x.shape==y.shape
 for prop,meta in unit['channels'].items():
  raw=(O/'Materials/Lift03/Shared'/(unit['bindings'][prop]+'.Texture')).read_bytes();version,w,h,depth,mips,fmt,dim,filtering,wrap,aniso,size=struct.unpack_from('<4H6Bi',raw);assert (version,depth,dim,filtering,wrap,aniso)==(5,1,2,2,0,16);assert fmt==(4 if prop=='_BaseColorMap' else 8);assert len(raw)==18+size
  count=sum(max(1,w>>i)*max(1,h>>i)*4 for i in range(mips));buf=ctypes.create_string_buffer(count);assert decode(raw[18:],size,buf,count,0)==count;data=buf.raw;at=0;first=None
  for level in range(mips):
   width,height=max(1,w>>level),max(1,h>>level);pixels=np.frombuffer(data[at:at+width*height*4],dtype='u1').reshape(height,width,4);at+=pixels.nbytes;assert hashlib.sha256(pixels.tobytes()).hexdigest()==meta['mips'][level]['sha256']
   if level==0:first=pixels
  assert at==len(data);expected=sources[prop][y,x];assert np.array_equal(first,expected)
  rows.append(dict(unit=unit['name'],channel=prop,originalMipZeroBytes=first.nbytes,exactSourceTexels=True,verifiedSerializedMips=mips))
(O/'serialized-source-verification.json').write_text(json.dumps(rows,indent=2)+'\n');print('PASS',len(rows),'aligned source channels;',sum(r['verifiedSerializedMips'] for r in rows),'serialized mips; exact original decoded mip-zero texels')
