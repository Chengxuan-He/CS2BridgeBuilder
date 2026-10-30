"""Measure full-width walking units from original faces; emit offline-only recipes."""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository',type=Path)
    p.add_argument('output',type=Path)
    a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
    evidence=a.repository/'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json'
    audit=json.loads(evidence.read_text())
    sources=json.loads((a.repository/'artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2/trial-v1/persistent-manifest.json').read_text())['units'][0]
    units=[]
    for identity,bone,z0,z1,half in [('fixed-positive',0,64.,68.,4.),('fixed-negative',0,-68.,-64.,4.),
                                   ('moving-positive',1,9.05517578125,15.849609375,3.68646240234375),
                                   ('moving-negative',2,-15.849609375,-9.05517578125,3.68646240234375)]:
        bands=[];level_faces=[]
        for lod in (0,1):
            faces=[f for f in audit['levels'][lod]['walkwayFaces'] if f['bone']==bone
                   and min(v[2] for v in f['positions'])>=z0 and max(v[2] for v in f['positions'])<=z1]
            assert len(faces)==4
            area=0
            measured=[]
            for start,end in [(-half,0.),(0.,half)]:
                group=[f for f in faces if min(v[0] for v in f['positions'])>=start and max(v[0] for v in f['positions'])<=end]
                assert len(group)==2
                points=np.array([v for f in group for v in f['positions']])
                uv=np.array([v for f in group for v in f['uv0']])
                matrix=np.column_stack([points[:,0],points[:,2],np.ones(len(points))])
                affine=np.linalg.lstsq(matrix,uv,rcond=None)[0]
                assert np.max(abs(matrix@affine-uv))<1e-12
                # Both triangles preserve the original signed x/z projection.
                assert abs(affine[1,0])<1e-12 and abs(affine[0,1])<1e-12
                # Original values are binary half-floats. Recover those exact
                # endpoint bits instead of preserving least-squares roundoff.
                low=(np.array([start,z0,1.])@affine).astype(np.float32).tolist()
                high=(np.array([end,z1,1.])@affine).astype(np.float32).tolist()
                measured.append(dict(start=(start+half)/(2*half),end=(end+half)/(2*half),repeatY=1,min=low,max=high))
                for f in group:
                    v=np.array(f['positions']);area+=abs(np.cross(v[1]-v[0],v[2]-v[0])[1])/2
            assert abs(area-2*half*(z1-z0))<1e-10
            if lod==0:bands=measured
            else:assert np.max(abs(np.array([b['min']+b['max'] for b in bands])-np.array([b['min']+b['max'] for b in measured])))<1e-12
            level_faces.append(dict(lod=lod,faces=[f['face'] for f in faces],geometrySHA256=audit['levels'][lod]['geometrySHA256']))
        channels=[dict(c,encoding='BC7') for c in sources['channels']]
        for c in channels:assert sha(Path(c['path']))==c['sha256']
        unit=dict(id=identity,sourceCid=sources['sourceCid'],sourceDigest=sources['sourceDigest'],
            min=[0.,0.],max=[1.,1.],physicalSize=[2*half,z1-z0],repeatAxes='V',bands=bands,
            aliases=[],channels=channels,descriptor=sources['descriptor'],
            evidence=dict(path=str(evidence.resolve()),sha256=sha(evidence)),
            originalFaces=level_faces,phase=dict(zOrigin=z0,zPeriod=z1-z0),
            mapping='Full finite width with the original center UV reset inside one offline unit; V repeats one original measured cell. Signed projections are retained.')
        units.append(unit)
    result=dict(recipe='BridgeBuilder/RepeatUnits/2',style='PedestrianDraw01',units=units)
    (a.output/'near-road-units.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('PASS: four measured full-width units; original signed UV projections and near/mid corner agreement.')


if __name__=='__main__':main()
