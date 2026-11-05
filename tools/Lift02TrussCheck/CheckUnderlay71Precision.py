"""Find exact Float32 UV witnesses for tiny collapsed triangles, without tolerances."""
from pathlib import Path
import json
import numpy as np

out = Path(__file__).resolve().parents[2] / 'artifacts/material-remap/Lift02/underlay71'
tri_dtype = np.dtype([('face','<i4'),('material','<i4'),('v','<f4',(3,5))])
source_dtype = np.dtype([('material','<i4'),('v','<f4',(3,5))])
results=[]
for label in ['paving','mixed_native','mixed','one_sidewalk']:
    for lod in range(3):
        a=np.frombuffer((out/f'{label}-lod{lod}-triangles.bin').read_bytes(),tri_dtype,offset=4)
        source=np.frombuffer((out/f'{label}-lod{lod}-receivers.bin').read_bytes(),source_dtype,offset=8)
        v=a['v'].astype(float)
        n=np.cross(v[:,1,:3]-v[:,0,:3],v[:,2,:3]-v[:,0,:3])
        du=v[:,1,3:]-v[:,0,3:];dv=v[:,2,3:]-v[:,0,3:]
        collapsed=(n[:,1]==0)&np.any(n!=0,axis=1)&(du[:,0]*dv[:,1]==du[:,1]*dv[:,0])
        materials=(out/f'{label}-lod{lod}-materials.txt').read_text().splitlines()
        records=[]
        for index in np.flatnonzero(collapsed):
            material=materials[a['material'][index]]
            paving=material.endswith('77777777777777777777777777777771')
            selected=source[source['material']==(2 if paving else 0)]['v'].astype(float)
            p=selected[:,:,:3];uv=selected[:,:,3:]
            b=p[:,1]-p[:,0];c=p[:,2]-p[:,0]
            det=b[:,0]*c[:,2]-b[:,2]*c[:,0]
            valid=det!=0;p=p[valid];uv=uv[valid];b=b[valid];c=c[valid];det=det[valid]
            hx=(b[:,1]*c[:,2]-c[:,1]*b[:,2])/det
            hz=(b[:,0]*c[:,1]-c[:,0]*b[:,1])/det
            hc=p[:,0,1]-hx*p[:,0,0]-hz*p[:,0,2]
            triangle=v[index];axis=int(np.argmax(abs(n[index])))
            height=hx[:,None]*triangle[None,:,0]+hz[:,None]*triangle[None,:,2]+hc[:,None]
            depth=triangle[None,:,1]-height
            if paving:depth=-abs(depth)
            witness=None
            for sign in [-1,1]:
                x=np.broadcast_to(triangle[None,:,0],depth.shape).copy()
                z=np.broadcast_to(triangle[None,:,2],depth.shape).copy()
                if axis==0:x+=sign*depth
                else:z+=sign*depth
                if paving:
                    px=x-p[:,None,0,0];pz=z-p[:,None,0,2]
                    wb=(px*c[:,None,2]-pz*c[:,None,0])/det[:,None]
                    wc=(b[:,None,0]*pz-b[:,None,2]*px)/det[:,None]
                    ideal=uv[:,None,0,:]+(uv[:,None,1,:]-uv[:,None,0,:])*wb[:,:,None]+(uv[:,None,2,:]-uv[:,None,0,:])*wc[:,:,None]
                    rounded=ideal.astype(np.float32)
                else:
                    ideal=np.stack((x,z),axis=-1)*float(np.float32(.4))
                    rounded=(np.stack((x,z),axis=-1).astype(np.float32)*np.float32(.4))
                match=np.flatnonzero(np.all(rounded==triangle[None,:,3:].astype(np.float32),axis=(1,2)))
                for candidate in match:
                    q=ideal[candidate,1]-ideal[candidate,0];r=ideal[candidate,2]-ideal[candidate,0]
                    area=abs(q[0]*r[1]-q[1]*r[0])/2
                    if area!=0:
                        witness=dict(receiver=int(candidate),foldSign=sign,idealUvArea=float(area));break
                if witness is not None:break
            area=float(np.linalg.norm(n[index])/2)
            longest=max(np.linalg.norm(triangle[j,:3]-triangle[(j+1)%3,:3]) for j in range(3))
            records.append(dict(triangle=int(index),face=int(a['face'][index]),material=material,
                                physicalArea=area,minimumAltitude=2*area/longest,witness=witness))
        result=dict(input=label,lod=lod,collapsed=len(records),unwitnessed=sum(r['witness'] is None for r in records),
                    maximumArea=max([r['physicalArea'] for r in records]+[0]),
                    maximumMinimumAltitude=max([r['minimumAltitude'] for r in records]+[0]),records=records)
        results.append(result)
        print(label,lod,result['collapsed'],result['unwitnessed'],result['maximumMinimumAltitude'])
(out/'precision-result.json').write_text(json.dumps(results,indent=2))
