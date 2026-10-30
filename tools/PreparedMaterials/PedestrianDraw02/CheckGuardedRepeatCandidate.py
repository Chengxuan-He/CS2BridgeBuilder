"""Stored BC7 bilinear/trilinear diagnostic with actual per-panel source LOD.

This does not claim to emulate native anisotropic SampleGrad acceptance.
"""
import json
from pathlib import Path
import numpy as np
from CheckNativePanelCandidate import levels,sample

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    manifest=json.loads((OUT/'piecewise-repeat-guarded-manifest.json').read_text())
    report=json.loads((OUT/'panels-v3-guarded-report.json').read_text())
    rows=[]
    cache={}
    for unit,product in zip(manifest['units'],report['units']):
        for channel in unit['channels']:
            output=next(c for c in product['channels'] if c['name']==channel['name'])
            source=cache.setdefault(channel['path'],levels(Path(channel['path'])))
            candidate=levels(OUT/'panels-v3-guarded-candidate/PedestrianDraw02/Shared'/(output['cid']+'.Texture'))
            q=[];uv=[];which=[];edge=[]
            for index,panel in enumerate(unit['panels']):
                for i in range(17):
                    for j in range(17-i):
                        bary=np.array([i,j,16-i-j])/16
                        q.append(bary@np.asarray(panel['target']).reshape(3,2))
                        uv.append(bary@np.asarray(panel['source']).reshape(3,2))
                        which.append(index);edge.append(bool(np.any(bary==0)))
            q,uv,which,edge=np.asarray(q),np.asarray(uv),np.asarray(which),np.asarray(edge)
            mapped=q.copy()
            mapped[:,0]=output['contentMin'][0]+q[:,0]*(output['contentMax'][0]-output['contentMin'][0])
            measurements=[]
            count=output['sourceLodBandCount']
            for mip,pixels in enumerate(candidate):
                actual=sample(pixels,mapped,True);expected=np.zeros_like(actual)
                for panel in range(len(unit['panels'])):
                    mask=which==panel
                    lod=output['sourceLodByMip'][mip*count+panel]
                    lo=min(len(source)-1,max(0,int(np.floor(lod))));hi=min(len(source)-1,lo+1)
                    weight=max(0,lod-lo)
                    expected[mask]=sample(source[lo],uv[mask],False)*(1-weight)+sample(source[hi],uv[mask],False)*weight
                errors=abs(actual-expected)
                measurements.append(dict(mip=mip,maxAbsolute=float(errors.max()),
                    meanAbsolute=float(errors.mean()),edgeMax=float(errors[edge].max()),
                    alphaMax=float(errors[:,3].max())))
            rows.append(dict(unit=unit['id'],channel=channel['name'],mips=measurements))
    result=dict(status='MEASURED_NOT_NATIVE_ACCEPTANCE',rows=rows,
        limitations=['Density-matched trilinear isotropic diagnostic; native anisotropic footprints require GPU comparison.',
            'No error threshold was relaxed; these observations do not approve runtime integration.'])
    (OUT/'panels-v3-guarded-filter-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps([dict(unit=r['unit'],channel=r['channel'],first=r['mips'][0],last=r['mips'][-1]) for r in rows]))


if __name__=='__main__':main()
