"""Embed original half-floor U panel boundaries and native tangent ownership."""
import hashlib
import json
import struct
from pathlib import Path
import numpy as np
from ExportRecipes import numbers
ROOT=Path(__file__).resolve().parents[3]


def main():
    audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift01/outputs/native-repeat-mechanism/audit.json').read_text())
    frames=json.loads((ROOT/'artifacts/material-remap/Lift01/floor-native-frames.json').read_text())
    lookup={(f['lod'],f['face']):f for f in frames['faces']}
    original=numbers(ROOT/'assets/HardcodedNumbers/Lift01/Types_Lift01_Lift01RoadAppearance_cs.bin')['F32']
    # Source measurements from the complete near half-floor, not invented bands.
    first=lookup[0,259060];outer=lookup[0,259058]
    u0=min(t[0] for t in first['uv']);u1=max(t[0] for t in first['uv']);u2=max(t[0] for t in outer['uv'])
    v0=min(t[1] for t in first['uv']);v1=max(t[1] for t in first['uv'])
    inner=max(abs(p[0]) for p in first['positions']);half=max(abs(p[0]) for p in outer['positions'])
    period=max(p[2] for p in first['positions'])-min(p[2] for p in first['positions'])
    values=[u0,u1,u2,v0,v1,inner,half,period,original[13]/original[14],original[15]]
    checks=[]
    for lod in (0,1):
        groups=[[] for _ in range(8)]
        for f in audit['reports'][lod]['sourceFaces']:
            p=np.array(f['xyz']);bone=f['bones'][0]
            assert len(set(f['bones']))==1 and bone in (0,4)
            moving=int(bone==4);negative=int(p[:,2].mean()<0);side=int(p[:,0].mean()>0)
            groups[moving*4+negative*2+side].append(lookup[lod,f['face']])
        for group,faces in enumerate(groups):
            tangents=np.unique(np.concatenate([f['tangents'] for f in faces]),axis=0)
            assert tangents.shape==(1,4),(lod,group,tangents)
            values.extend(tangents[0])
            maximum=0
            for face in faces:
                p=np.array(face['positions']);uv=np.array(face['uv'])
                lateral=abs(p[:,0])
                predicted=np.where(lateral<=inner,u0+lateral*(u1-u0)/inner,u1+(lateral-inner)*(u2-u1)/(half-inner)) if lod==0 else u0+lateral*(u2-u0)/half
                maximum=max(maximum,float(abs(predicted-uv[:,0]).max()))
            assert maximum<1e-12,(lod,group,maximum)
            checks.append(dict(lod=lod,appearanceGroup=group,faces=len(faces),nativeTangent=tangents[0].tolist(),sourceUPixelError4096=maximum*4096))
    # Only the three authored terminal types differ from the regular 4 m rate.
    spans=[(0,80.02734375,83.779296875),(0,83.779296875,91.779296875),
           (0,91.779296875,95.99609375),(4,0.,76.),(4,76.,79.9921875)]
    segments=[]
    for index,(bone,lo,hi) in enumerate(spans):
        if index in (1,3):rate,bias=-1/period,values[8+int(bone==4)]
        else:
            candidates=[f for f in audit['reports'][0]['sourceFaces'] if f['bones'][0]==bone and
                min(p[2] for p in f['xyz'])==lo and max(p[2] for p in f['xyz'])==hi]
            assert candidates
            face=candidates[0];z=np.array(face['xyz'])[:,2];v=(np.array(face['uv'])[:,1]-v0)/(v1-v0)
            pair=[int(np.argmin(z)),int(np.argmax(z))]
            rate=(v[pair[1]]-v[pair[0]])/(hi-lo);bias=v[pair[0]]-rate*lo
        values.extend([lo,hi,rate,bias]);segments.append(dict(bone=bone,min=lo,max=hi,rate=rate,bias=bias,finite=index not in(1,3)))
    phase_checks=[]
    for lod in (0,1):
        for face in audit['reports'][lod]['sourceFaces']:
            z=abs(np.array(face['xyz'])[:,2]);bone=face['bones'][0]
            selected=[s for s in segments if s['bone']==bone and z.min()>=s['min'] and z.max()<=s['max']]
            assert len(selected)==1,(lod,face['face'],z)
            s=selected[0];original_v=(np.array(face['uv'])[:,1]-v0)/(v1-v0);predicted=z*s['rate']+s['bias']
            delta=predicted-original_v
            if not s['finite']:delta=(delta+.5)%1-.5
            error=float(abs(delta).max());assert error<1e-10,(lod,face['face'],error)
            phase_checks.append(dict(lod=lod,face=face['face'],finite=s['finite'],normalizedVError=error))
    raw=struct.pack('<II6i',0x314e4242,1,0,0,0,0,len(values),0)+np.array(values,'<f4').tobytes()
    target=ROOT/'assets/HardcodedNumbers/Lift01/Types_Lift01_HalfFloorDomains.bin'
    target.write_bytes(raw+hashlib.sha256(raw).digest())
    out=ROOT/'artifacts/material-remap/Lift01/half-floor-domains.json'
    out.write_text(json.dumps(dict(checks=checks,phaseChecks=phase_checks,segments=segments,nearDomainCount=40,middleDomainCount=20,
        sourceHalfWidth=half,sourceInnerWidth=inner,sourceU=[u0,u1,u2],period=period,
        textureCount='One full original half-floor source-grid unit shared by near and middle and every appearance group; no per-sampler copies.',
        scope='Original U panel boundary, actual V terminal phase, and uploaded tangent ownership verified over 576 faces. Does not certify texture/mip equivalence or a finished repeat resource binding.',
        payloadSHA256=hashlib.sha256(target.read_bytes()).hexdigest()),indent=2))
    print(out)


if __name__=='__main__':main()
