import struct,zlib,json,hashlib
from pathlib import Path
import numpy as np
root=Path('C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
totals={k:0 for k in ['baseline','column','deltaA','deltaFaceGlobal','sparseA','sparseFaces']}
for path in sorted((root/'assets/HardcodedNumbers').rglob('UvDomain_*.bin')):
 raw=path.read_bytes();counts=struct.unpack_from('<6i',raw,8);assert not any(counts[i] for i in [1,2,3,5]);v=np.frombuffer(raw,dtype='<i4',count=counts[0],offset=32);f=np.frombuffer(raw,dtype='<u4',count=counts[4],offset=32+4*counts[0]);assert v[0]==-2
 n=int(v[1]);at=2;coord=0;cols=[[],[],[],[]];fs=[];bounds=[];unique=[];lookup={}
 for i in range(n):
  a,b,c,k=map(int,v[at:at+4]);at+=4;cols[0].append(a);cols[1].append(b-a);cols[2].append(c-a);cols[3].append(k);faces=v[at:at+abs(k)];at+=abs(k);fs.extend([int(faces[0]),*np.diff(faces).tolist()])
  if k>0:
   key=f[coord:coord+4].tobytes();coord+=4
   if key not in lookup:lookup[key]=len(unique);unique.append(key)
   bounds.append(lookup[key])
 assert at==len(v) and coord==len(f)
 floats=b''.join(unique)
 def payload(columns,faces,extra=()):
  ints=[-3,n,len(fs),len(unique),*extra,*columns[0],*columns[1],*columns[2],*columns[3],*bounds,*faces]
  body=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(floats)//4,0)+np.asarray(ints,dtype='<i4').tobytes()+floats
  return body+hashlib.sha256(body).digest()
 totals['baseline']+=len(zlib.compress(raw,9));totals['column']+=len(zlib.compress(payload(cols,fs),9))
 da=[cols[0][0],*np.diff(cols[0]).tolist()];dcols=[da,*cols[1:]]
 totals['deltaA']+=len(zlib.compress(payload(dcols,fs),9))
 # Across concatenated face sequence, reconstruct per-domain first entries before global differences.
 full=[];at=2
 for i in range(n):
  k=abs(int(v[at+3]));at+=4;full.extend(v[at:at+k].tolist());at+=k
 df=[full[0],*np.diff(full).tolist()]
 totals['deltaFaceGlobal']+=len(zlib.compress(payload(dcols,df),9))
 def sparse(values,default):
  pairs=[(i,x) for i,x in enumerate(values) if x!=default];return [len(pairs),*[i for i,x in pairs],*[x for i,x in pairs]]
 totals['sparseA']+=len(zlib.compress(payload([sparse(da,0),*cols[1:]],df),9))
 totals['sparseFaces']+=len(zlib.compress(payload(dcols,sparse(df,1)),9))
print(json.dumps(totals,indent=2))
(root/'artifacts/prepared-materials/shared-polar-final/numeric-layout-estimates.json').write_text(json.dumps(totals,indent=2)+'\n')
