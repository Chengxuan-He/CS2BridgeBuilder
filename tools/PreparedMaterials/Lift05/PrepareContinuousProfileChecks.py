"""Independent continuous-unit UV fixtures; no period vertices or face splitting."""
import json
import struct
import numpy as np
from ReviewStructuralSeams import ROOT,phases,motion
from BuildUvCharts import domains

def main():
    policy=json.loads((ROOT/'artifacts/prepared-materials/Lift05/non-tiling/classification.json').read_text())
    output=ROOT/'artifacts/material-remap/Lift05/native-wrap-inputs';reports=[]
    for lod in range(2):
        a=np.load(ROOT/f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz')
        p=a['mesh0_positions'].astype(float);uv=a['mesh0_uv0'].astype(float);faces=a['mesh0_indices']
        name='LiftBridge05 Mesh' if lod==0 else 'LiftBridge05_LOD1 Mesh'
        parts=next(m for m in policy['meshes'] if m['render']==name and m['mesh']==0)['mixedProfileEvidence']
        by_face={f:part for part in parts for f in part['faces']};weights,_=motion(lod,len(p))
        for delta in (0,1,3,6,12):
            target=p.copy();target[:,0]=(p[:,0].astype(np.float32)+weights*np.float32(delta)).astype(float)
            offsets,evidence=phases(parts,p,uv,faces,target);mapped={}
            for part in parts:
                ids=faces[part['faces']].ravel();x=p[ids,0];u=uv[ids,0];lo,hi=u.min(),u.max()
                assert lo==.6162109375 and hi==.748046875
                rate=(u[np.argmax(x)]-u[np.argmin(x)])/(hi-lo)/np.ptp(x);anchor=int(ids[np.argmin(x)])
                offset=offsets[part['seedFace']]-rate*(target[anchor,0]-p[anchor,0])
                for f in part['faces']:
                    for v in faces[f]:
                        vv=.25+(uv[v,1]-.890625)/(.8955078125-.890625)*.5
                        assert .25<=vv<=.75
                        mapped[f,int(v)]=((uv[v,0]-lo)/(hi-lo)+rate*(target[v,0]-p[v,0])+offset,vv)
            records=[]
            if delta:
                for domain,fs in enumerate(domains(name,0)):
                    if not set(fs).intersection(by_face):continue
                    assert set(fs)<=set(by_face)
                    values={}
                    for f in fs:
                        for v in faces[f]:
                            value=mapped[f,int(v)]
                            if int(v) in values:assert np.array_equal(np.float32(values[int(v)]),np.float32(value))
                            values[int(v)]=value
                    records.append((domain,values))
            raw=struct.pack('<i',len(records))
            for domain,values in records:
                raw+=struct.pack('<2i',domain,len(values))
                for vertex,value in sorted(values.items()):raw+=struct.pack('<iff',vertex,*value)
            (output/f'continuous-lod{lod}-delta{delta}.bin').write_bytes(raw)
            reports.append(dict(lod=lod,delta=delta,preparedDomains=len(records),sourceFaces=len(by_face),addedPeriodVertices=0,addedPeriodTriangles=0,
                phaseResidual=max(abs(offsets[e['second']]-offsets[e['first']]-e['offsetDifference']) for e in evidence['constraints'])))
    (output.parent/'continuous-profile-reference.json').write_text(json.dumps(dict(cases=reports,
        limitation='Independent source-array fixture, not compiled consumer/Retile/native quality acceptance.'),indent=2)+'\n')
    print('PASS 10 continuous phase fixtures: finite V, exact domain membership, shared vertex consistency, original-width empty bindings; no topology creation.')

if __name__=='__main__':main()
