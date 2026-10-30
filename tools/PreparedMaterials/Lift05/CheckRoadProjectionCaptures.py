"""Independent source-triangle interpolation check of actual C# road outputs."""
import hashlib
import json
import struct
import numpy as np
from ReviewRoadProjectionBoundaries import ROOT,SOURCE

def main():
    plan=json.loads((ROOT/'artifacts/material-remap/Lift05/road-projection-plan.json').read_text())
    cases=[]
    for lod in range(3):
        source=json.loads((SOURCE/f'road-lod{lod}.json').read_text());faces={f['id']:f for f in source['faces']}
        level=plan['levels'][lod];resolution=512 if lod==2 else 4096
        for delta in (0,3,12):
            for pose in range(3):
                path=ROOT/f'artifacts/material-remap/Lift05/native-wrap-inputs/road-capture-{lod}-{delta}-{pose}.bin'
                raw=path.read_bytes();count=struct.unpack_from('<i',raw)[0];offset=4;errors=[];unsupported=[];escapes=[]
                for _ in range(count):
                    domain=struct.unpack_from('<i',raw,offset)[0];offset+=4
                    for _ in range(3):
                        values=struct.unpack_from('<17f',raw,offset);offset+=68
                        xyz=np.array(values[14:17],float);actual=np.array(values[3:5])
                        assert values[5:7]==(7,9)
                        if domain==0:
                            unsupported.append(float(np.max(np.abs(actual-xyz[[0,2]]))));continue
                        patch=level['patches'][domain-1];face=faces[patch['faces'][0]]
                        center=(patch['sourceMin'][0]+patch['sourceMax'][0])/2
                        if center<-3:xyz[0]+=delta
                        elif center>3:xyz[0]-=delta
                        else:xyz[0]*=3/(3+delta)
                        points=np.array(face['positions'])[:,[0,2]]
                        weights=np.linalg.solve(np.vstack((points.T,np.ones(3))),np.array([xyz[0],xyz[2],1]))
                        expected=weights@np.array(face['uv'])
                        errors.append(float(np.max(np.abs(expected-actual)))*resolution)
                        low=np.array(patch['uvMin']);high=np.array(patch['uvMax'])
                        escapes.append(float(max(0,np.max(low-actual),np.max(actual-high)))*resolution)
                assert offset==len(raw)
                cases.append(dict(lod=lod,delta=delta,pose=pose,outputTriangles=count,
                    maximumOriginalInterpolationErrorTexels=max(errors),maximumFiniteBoundsEscapeTexels=max(escapes),
                    maximumUnsupportedCoordinateRoundtripError=max(unsupported),sha256=hashlib.sha256(raw).hexdigest()))
    output=ROOT/'artifacts/material-remap/Lift05/road-native-coordinate-measurements.json'
    output.write_text(json.dumps(dict(cases=cases,scope='Measurements against original stored source positions/UV via independent barycentric solve. Includes Float32 pose roundtrip. No material/filter tolerance is asserted; C# separately verifies exact unchanged unsupported source fields.'),indent=2)+'\n')
    print('27 captures measured; maximum source interpolation error:',max(c['maximumOriginalInterpolationErrorTexels'] for c in cases),'texels')
    print('Maximum finite bounds escape:',max(c['maximumFiniteBoundsEscapeTexels'] for c in cases),'texels')

if __name__=='__main__':main()
