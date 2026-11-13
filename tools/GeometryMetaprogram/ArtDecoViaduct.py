"""Extract the measured Art Deco pylon's native transverse profile and frame domains.
Input JSON is decoded from e2418d88bc0ec23a8ca39af6f6f66dcc, never a runtime mesh.
"""
import hashlib,json,pathlib,struct,sys
import numpy as np
root=pathlib.Path(__file__).resolve().parents[2]
src=pathlib.Path(sys.argv[1]);m=json.loads(src.read_text());p=np.array(m['positions'],dtype=np.float64);tri=np.array(m['indices']).reshape(-1,3)
assert len(p)==1610 and len(tri)==1022
# All source transverse profile bands end at their measured side-pier junction or
# the centre axis. The side piers and their stepped ends translate rigidly;
# interpolation across each original transverse band retains its own source reach.
coef=np.sign(p[:,0]); candidates=[[] for _ in p]
for face in tri:
 a,b,c=face;edges=np.array([p[b]-p[a],p[c]-p[a]])
 gradient=np.linalg.lstsq(edges,np.array([coef[b]-coef[a],coef[c]-coef[a]]),rcond=None)[0]
 assert np.max(np.abs(edges@gradient-[coef[b]-coef[a],coef[c]-coef[a]]))<1e-9
 area=np.linalg.norm(np.cross(edges[0],edges[1]))
 for i in face:candidates[i].append((area,gradient))
# Original shared-vertex shading uses one frame. Freeze its largest incident
# source-face domain; equal-area ties retain source triangle order. Axis-aligned
# tangents at shared domains remain collinear across all positive-scale domains.
g=np.array([max(v,key=lambda x:x[0])[1] if v else np.zeros(3) for v in candidates])
out=root/'assets/HardcodedNumbers/ArtDecoViaduct';out.mkdir(parents=True,exist_ok=True)
for key,values in [('PylonDisplacements',coef),('PylonFrameX',g[:,0]),('PylonFrameY',g[:,1]),('PylonFrameZ',g[:,2])]:
 values=np.asarray(values,dtype='<f4');data=struct.pack('<8i',0x314e4242,1,0,0,0,0,len(values),0)+values.tobytes();(out/f'Types_ArtDecoViaduct_{key}.bin').write_bytes(data+hashlib.sha256(data).digest())
for delta in [0.,-2.,5.,20.]:
 q=p.copy();q[:,0]+=coef*delta
 assert np.array_equal(q[:,1:],p[:,1:])
 if delta==0:assert np.array_equal(q,p)
 for side in (-1,1):
  # The complete outer shafts keep their original X distances.
  mask=(p[:,0]*side>=11.199951)&(p[:,1]<=82)
  assert np.max(np.abs((q[mask,0]-p[mask,0])-side*delta))<1e-10
print('ArtDeco: 1610 source vertices, 1022 triangles, identity/YZ/outer-shaft checks passed; shared-frame domain decisions retained explicitly.')
# Native packed frame equivalence is tested when the optional original channels
# accompany the JSON. A Jacobian ambiguity is acceptable only when it maps the
# actual normal and tangent to the same normalized directions.
normal_path=src.with_name('attribute1.bin');tangent_path=src.with_name('attribute2.bin')
if normal_path.exists() and tangent_path.exists():
 normals=struct.unpack('<1610I',normal_path.read_bytes());tangents=struct.unpack('<1610I',tangent_path.read_bytes())
 def octa(v,bits):
  mask=(1<<bits)-1;half=1<<(bits-1);x=v&mask;y=(v>>16)&mask
  x=(x-(1<<bits) if x>=half else x)/(half-1);y=(y-(1<<bits) if y>=half else y)/(half-1);z=1-abs(x)-abs(y)
  if z<0:x,y=(1-abs(y))*(1 if x>=0 else -1),(1-abs(x))*(1 if y>=0 else -1)
  v=np.array([x,y,z]);return v/np.linalg.norm(v)
 maximum=0.
 for delta in (-2.,5.,20.):
  for vertex,domains in enumerate(candidates):
   directions=[]
   for _,derivative in domains:
    j=np.eye(3);j[0]+=delta*derivative
    n=np.linalg.inv(j).T@octa(normals[vertex],16);n/=np.linalg.norm(n)
    t=j@octa(tangents[vertex],15);t-=n*np.dot(n,t);t/=np.linalg.norm(t)
    directions.append(np.r_[n,t])
   if len(directions)>1:maximum=max(maximum,float(np.max(np.abs(np.array(directions)-directions[0]))))
 assert maximum<1e-10,maximum
 print('Native normal/tangent incident-domain maximum difference:',maximum)
