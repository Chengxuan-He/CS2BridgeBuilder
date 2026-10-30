"""Extract original walking UV-reset stations, never prototype-face intersections."""
import argparse
import hashlib
import json
import struct
from pathlib import Path
import numpy as np


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('repository',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();evidence=a.repository/'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json'
    audit=json.loads(evidence.read_text());records=[];counts=[]
    for level in audit['levels'][:2]:
        start=len(records)
        for parent,bone,sign in [(0,0,1),(1,0,-1),(2,1,1),(3,2,-1)]:
            domain=[f for f in level['walkwayFaces'] if f['bone']==bone and np.mean(np.array(f['positions'])[:,2])*sign>0]
            lo=min(v[2] for f in domain for v in f['positions']);hi=max(v[2] for f in domain for v in f['positions'])
            resets=sorted({s['edge'][0][2] for s in level['uvSeams'] if s['bones']==[bone,bone]
                and abs(s['edge'][0][2]-s['edge'][1][2])<1e-7 and max(abs(d[1]) for d in s['delta'])>.1
                and lo<s['edge'][0][2]<hi})
            bounds=[lo,*resets,hi]
            for side in (0,1):
                half=4. if bone==0 else 3.68646240234375
                for z0,z1 in zip(bounds,bounds[1:]):
                    faces=[f for f in domain if (np.mean(np.array(f['positions'])[:,0])>0)==bool(side)
                           and z0<=np.mean(np.array(f['positions'])[:,2])<=z1]
                    assert faces
                    xyz=np.array([v for f in faces for v in f['positions']]);uv=np.array([v for f in faces for v in f['uv0']])
                    assert xyz[:,2].min()>=z0 and xyz[:,2].max()<=z1
                    edge=xyz[:,0]==(half if side else -half)
                    stations=xyz[:,2];tex=uv[:,1]
                    assert stations.min()==z0 and stations.max()==z1
                    v0=np.unique(tex[stations==z0]);v1=np.unique(tex[stations==z1]);assert len(v0)==len(v1)==1,(level['lod'],parent,side,z0,z1,v0,v1)
                    slope=(v1[0]-v0[0])/(z1-z0)
                    # Complete original half-width chart, including extrapolation
                    # only over selected-road geometry where prototype cutouts occur.
                    u=[-.1875/half,0.,0.,.5712890625 if side else .3837890625]
                    v=[0.,0.,slope,float(v0[0]-z0*slope)]
                    u=np.array(u,dtype=np.float32).astype(float).tolist();v=np.array(v,dtype=np.float32).astype(float).tolist()
                    projected=np.column_stack([xyz[:,0]*u[0]+u[3],xyz[:,2]*v[2]+v[3]])
                    error=np.max(abs(projected-uv),axis=0)*4096
                    records.append(dict(lod=level['lod'],parent=parent,side=side,z0=z0,z1=z1,halfWidth=half,
                        min=[.3837890625,float(min(v0[0],v1[0]))],max=[.5712890625,float(max(v0[0],v1[0]))],
                        u=u,v=v,tangent=[-1.,0.,0.,-1.],faces=[f['face'] for f in faces],maximumSourceTexelError=error.tolist()))
        counts.append(len(records)-start)
    ints=[1,*counts]+[n for r in records for n in [r['parent'],r['side']]]
    floats=[v for r in records for v in [r['z0'],r['z1'],r['halfWidth'],0.,*r['min'],*r['max'],*r['u'],*r['v'],*r['tangent']]]
    raw=b'BBN1'+struct.pack('<7i',1,len(ints),0,0,0,len(floats),0)+struct.pack('<'+'i'*len(ints),*ints)+struct.pack('<'+'f'*len(floats),*floats)
    raw+=hashlib.sha256(raw).digest()
    (a.repository/'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_NativeRoadCells.bin').write_bytes(raw)
    a.output.mkdir(parents=True,exist_ok=True)
    result=dict(sourceSHA256=hashlib.sha256(evidence.read_bytes()).hexdigest(),counts=counts,records=records,
        construction='Measured original reset stations plus original x=0 boundary; one affine sampler per finite original cell. No Boolean operation or prototype-face subdivision.',
        limitations='Fixed cutout vertices contain small non-affine authored UV deviations. Reported residuals are not a filtered appearance acceptance claim.')
    (a.output/'native-road-cells.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(counts=counts,maximumSourceTexelError=np.max([r['maximumSourceTexelError'] for r in records],axis=0).tolist())))


if __name__=='__main__':main()
