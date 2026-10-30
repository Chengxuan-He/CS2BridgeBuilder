"""Compare every output texel/mip with the identical unmodified-grid control."""
import ctypes as ct,json,struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
dll=ct.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
dll.decompress_data.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p,ct.c_int64,ct.c_int];dll.decompress_data.restype=ct.c_int64
report=json.loads((OUT/'bounded-paired-report.json').read_text());changed,control=report['units']
def load(product):
 data=(OUT/'bounded-paired-products/Draw01/Shared'/(product['cid']+'.Texture')).read_bytes()
 assert struct.unpack_from('<H',data)[0]==5
 count=struct.unpack_from('<i',data,14)[0]
 expected=sum(w*h*4 for w,h in zip(product['mipWidths'],product['mipHeights']))
 if count==-1:raw=data[18:]
 else:
  buf=ct.create_string_buffer(expected)
  assert dll.decompress_data(data[18:],count,buf,expected,0)==expected
  raw=buf.raw
 assert len(raw)==expected
 return np.frombuffer(raw,dtype=np.uint8)
rows=[]
for a,b in zip(changed['channels'],control['channels']):
 assert a['name']==b['name'] and a['width']==b['width'] and a['height']==b['height']
 aa,bb=load(a),load(b);offset=0
 for mip,(w,h,lod) in enumerate(zip(a['mipWidths'],a['mipHeights'],a['sourceLodByMip'])):
  size=w*h*4;x=aa[offset:offset+size].reshape(h,w,4);y=bb[offset:offset+size].reshape(h,w,4);offset+=size
  u=a['sourceOriginPixels'][0]+(np.arange(w)+.5)*a['sourceRegionPixels'][0]/w
  footprint=.5*2**lod
  mask=(u>=787-footprint-1e-4)&(u<=810+footprint+1e-4)
  diff=np.abs(x.astype(int)-y.astype(int));outside=diff[:,~mask,:]
  row=dict(channel=a['name'],mip=mip,width=w,height=h,sourceLod=lod,sourceMaskFootprintPixels=footprint,
    changedTexels=int(np.any(diff!=0,axis=2).sum()),maskColumns=int(mask.sum()),
    outsideChangedTexels=int(np.any(outside!=0,axis=2).sum()),outsideMaxByteError=int(outside.max()) if outside.size else 0,
    maximumComponentDelta=diff.max(axis=(0,1)).tolist(),
    alphaHalfCrossings=int(((x[:,:,3]>=128)!=(y[:,:,3]>=128)).sum()))
  if a['name']=='_NormalMap':
   def normal(v):
    t=v.astype(float)/255;xx=t[:,:,0]*t[:,:,3]*2-1;yy=t[:,:,1]*2-1
    n=np.stack([xx,yy,np.sqrt(np.maximum(0,1-xx*xx-yy*yy))],axis=2)
    return n/np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-12)
   cosine=np.clip((normal(x)*normal(y)).sum(axis=2),-1,1)
   row['maximumNormalChangeDegrees']=float(np.degrees(np.arccos(cosine)).max())
  rows.append(row)
result=dict(status='MASK_ISOLATION_PASS' if all(r['outsideChangedTexels']==0 for r in rows) else 'MASK_ISOLATION_FAIL',
 scope='All output texels and authored-input mips against identical unmodified-grid control; not original-vs-unit sampling acceptance.',
 operation='Defined same-source donor composition, not recovered unknown substrate.',rows=rows,
 changedProductBytes=sum(c['bytes'] for c in changed['channels']))
(OUT/'bounded-paired-byte-check.json').write_text(json.dumps(result,indent=2)+'\n')
print(result['status'],len(rows),'channel/mip rows','outside changed',sum(r['outsideChangedTexels'] for r in rows),'bytes',result['changedProductBytes'])
