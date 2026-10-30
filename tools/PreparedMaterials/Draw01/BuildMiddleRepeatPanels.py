"""Author one evidenced eight-metre middle asphalt unit, not runtime cuts."""
import hashlib,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
m=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz')
tangents=np.load(OUT/'middle-tangent-bits.npy')
z0=19.9599609375;z1=z0+8;width=3.36602783203125
def decode(bits):
    def signed(v):return (v-32768 if v&16384 else v)/16383
    x=signed(bits&32767);y=signed((bits>>16)&32767)
    n=np.array([x,y,1-abs(x)-abs(y)])
    f=max(-n[2],0);n[0]+=(-f if n[0]>=0 else f);n[1]+=(-f if n[1]>=0 else f)
    return n/np.linalg.norm(n),(-1 if bits&2147483648 else 1)
def clip(poly,value,keep_above):
    output=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        inside_a=a[1]>=value if keep_above else a[1]<=value
        inside_b=b[1]>=value if keep_above else b[1]<=value
        if inside_a:output.append(a)
        if inside_a!=inside_b:output.append(a+(b-a)*((value-a[1])/(b[1]-a[1])))
    return output
panels=[];area=0
normal=np.array([0.,1.,0.]);target_tangent=np.array([1.,0.,0.]);target_hand=-1
target_frame=np.column_stack([target_tangent,np.cross(normal,target_tangent)*target_hand,normal])
for face in [885,886,913,914]:
    ids=m['mesh0_faces'][face];pos=m['mesh0_positions'][ids].astype(float);uv=m['mesh0_uv0'][ids].astype(float)
    bits=set(tangents[ids].astype(int).tolist());assert len(bits)==1
    tangent,hand=decode(next(iter(bits)))
    frame=np.column_stack([tangent,np.cross(normal,tangent)*hand,normal])
    transform=target_frame.T@frame
    assert np.max(abs(transform.T@transform-np.eye(3)))<1e-12
    poly=[np.array([p[0],p[2],*q]) for p,q in zip(pos,uv)]
    poly=clip(clip(poly,z0,True),z1,False)
    for i in range(1,len(poly)-1):
        tri=np.array([poly[0],poly[i],poly[i+1]])
        q=(tri[:,:2]-[-width,z0])/[width,8]
        a=abs(np.linalg.det(np.array([q[1]-q[0],q[2]-q[0]])))/2
        if a==0:continue
        area+=a
        panels.append({'sourceFace':face,'target':q.reshape(-1).tolist(),'source':tri[:,2:].reshape(-1).tolist(),'normalTransform':transform.reshape(-1).tolist(),'sourceTangent':[*tangent.tolist(),hand],'sourcePackedTangent':next(iter(bits))})
assert abs(area-1)<1e-12
codec=ROOT/'artifacts/material-remap/PedestrianDraw02/decompiled-frame/Colossal.Mathematics.MathUtils.decompiled.cs'
result={'status':'AUTHORING_ONLY_NOT_BAKED_OR_CONNECTED','sourceCid':'97291ab7f7938fc35cc670b15f2bca36','lod':1,'physicalSize':[width,8],'referenceBounds':[-width,0,z0,z1],'targetTangent':[1,0,0,-1],'repeatAxes':'V','panels':panels,'unitArea':area,'codecSha256':hashlib.sha256(codec.read_bytes()).hexdigest(),'limitations':['Only positive-left central middle asphalt; other quarters and transition coverage remain to be proven.','Matched translated facets have compatible binary16 source intervals and float32 vertex evaluation, but some ideal real-number evaluations of float32 U coefficients miss the interval; not accepted as exact original filtered sampling.','Native finite-U filtering support, bounded paint composition and all-mip comparison are required before production consumption.','No near-LOD substitution, runtime prototype-face Boolean, period seams or full-span pixels.']}
(OUT/'middle-eight-meter-panels.json').write_text(json.dumps(result,indent=2));print(len(panels),'panels',area,'unit area')
