"""Compare final merged-domain tangent fits with native packed precision cells."""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def decoded(x,y):
    z=1-abs(x)-abs(y)
    if z<0:x,y=(1-abs(y))*(1 if x>=0 else -1),(1-abs(x))*(1 if y>=0 else -1)
    v=np.array([x,y,z]);return v/np.linalg.norm(v)


def main():
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    domains=json.loads((OUT/'native-atlas-domains.json').read_text())
    native_bits=sorted({b for l in frames['levels'] for p in l['panels'] for b in p['tangentBits']})
    precision=[]
    for bits in native_bits:
        x,y=bits&32767,(bits>>16)&32767
        x=x-32768 if x>=16384 else x;y=y-32768 if y>=16384 else y
        base=decoded(x/16383,y/16383)
        angles=[]
        for a in (-.5,0,.5):
            for b in (-.5,0,.5):
                v=decoded(np.clip((x+a)/16383,-1,1),np.clip((y+b)/16383,-1,1))
                angles.append(float(np.degrees(np.arccos(np.clip(np.dot(v,base),-1,1)))))
        precision.append(dict(bits=bits,maxSampledCellAngleDegrees=max(angles)))
    maximum=max(p['maxSampledCellAngleDegrees'] for p in precision)
    residuals=[dict(lod=l['lod'],faces=d['sourceFaces'],angle=d['tangentAffineMaxAngleDegrees'])
        for l in domains['levels'] for d in l['domains'] if d['tangentAffineMaxAngleDegrees']>maximum]
    report=dict(status='UNRESOLVED_NATIVE_FRAME_INTERPOLATION',packedCellSamples=precision,
        maxSampledPackedCellAngleDegrees=maximum,mergedDomainsExceedingSampledPackedCell=residuals,
        conclusion='A generic affine tangent field alone does not exactly preserve these merged original hinge frames.',
        limits=['These precision-cell samples are not a new acceptance tolerance.',
                'Do not split further prototype triangles or normalize away these differences without resolving the representation.'])
    (OUT/'native-tangent-affine-check.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(maxSampledPackedCellAngleDegrees=maximum,unresolvedDomains=len(residuals),
        maxAffineResidual=max(r['angle'] for r in residuals))))


if __name__=='__main__':main()
