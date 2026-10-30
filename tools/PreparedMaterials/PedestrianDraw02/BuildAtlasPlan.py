"""Emit source-identity appearance station rules, never prototype face geometry."""
import json
from pathlib import Path
import sys
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import write_bbn


def main():
    measured=json.loads((OUT/('continuous-atlas-domains.json' if '--continuous' in sys.argv else 'native-atlas-domains.json')).read_text())
    fields=('bone','repeatV','boundary','sourceUvMin','sourceUvMax','u','v','tangentCoefficients','tangentHandedness')
    first=measured['levels'][0]['domains']
    second=measured['levels'][1]['domains']
    assert [{k:d[k] for k in fields} for d in first]==[{k:d[k] for k in fields} for d in second]
    assert all(d['u']['halfRoundingCompatible'] and d['v']['halfRoundingCompatible'] for d in first)
    stations=sorted({p[0] for d in first for outline in d.get('outlinePieces',[d['boundary']]) for p in outline})
    # Same reviewed centre/side membership as the native physical partition.
    weights=[0 if abs(x)<.0001 else (-1 if x<0 else 1) for x in stations]
    cells=[]
    for slab,(left,right) in enumerate(zip(stations,stations[1:])):
        x=(left+right)/2
        intervals=[]
        for index,domain in enumerate(first):
            pieces=[]
            for points in domain.get('outlinePieces',[domain['boundary']]):
                crossings=[]
                for a,b in zip(points,points[1:]+points[:1]):
                    if min(a[0],b[0])<x<max(a[0],b[0]):
                        def z(at):return a[1]+(b[1]-a[1])*(at-a[0])/(b[0]-a[0])
                        crossings.append((z(x),z(left),z(right)))
                if not crossings:continue
                assert len(crossings)==2
                crossings.sort();lo,hi=crossings
                pieces.append((lo[0],hi[0],index,lo[1],lo[2],hi[1],hi[2]))
            pieces.sort();merged=[]
            for entry in pieces:
                # Remove only shared internal boundaries, never bridge a gap.
                if merged and merged[-1][5:7]==entry[3:5]:
                    previous=merged.pop()
                    merged.append((previous[0],entry[1],index,*previous[3:5],*entry[5:7]))
                else:merged.append(entry)
            intervals.extend(merged)
        intervals.sort()
        assert all(a[1]<=b[0]+1e-9 for a,b in zip(intervals,intervals[1:])), (left,right,[(a,b) for a,b in zip(intervals,intervals[1:]) if a[1]>b[0]+1e-9])
        cells.extend((slab,*entry[2:]) for entry in intervals)
    ints=[1,len(stations),len(first),len(cells)]
    floats=[]
    for d in first:
        ints.append(d.get('preparedUnitIndex',0))
        floats.extend([*d['sourceUvMin'],*d['sourceUvMax'],*d['u']['coefficients'],*d['v']['coefficients'],
            *np.asarray(d['tangentCoefficients']).reshape(-1).tolist(),d['tangentHandedness']])
    for x,w in zip(stations,weights):floats.extend([x,w])
    for slab,domain,ll,lr,ul,ur in cells:
        ints.extend([slab,domain]);floats.extend([ll,lr,ul,ur])
    payload=write_bbn(ints,floats)
    if '--continuous' in sys.argv:
        groups={};runtime=[]
        for cell,(slab,domain,ll,lr,ul,ur) in enumerate(cells):
            left,right=stations[slab:slab+2];wl,wr=weights[slab:slab+2]
            mapping=(wl,) if wl==wr else (wl,wr,left,right)
            key=(domain,mapping)
            if key not in groups:
                groups[key]=len(runtime)
                runtime.append(dict(bone=first[domain]['bone'],representativeCell=cell,
                    sourceDomain=domain,widthAffine=list(mapping),boundary=[],outlinePieces=[]))
            runtime[groups[key]]['outlinePieces'].append([[left,ll],[right,lr],[right,ur],[left,ul]])
        for d in runtime:d['boundary']=d['outlinePieces'][0]
        (OUT/'continuous-runtime-domains.json').write_text(json.dumps(dict(levels=[dict(domains=runtime)]),indent=2)+'\n')
    target=ROOT/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_AtlasAppearance.bin'
    target.write_bytes(payload)
    report=dict(schema=1,lods=2,sharedMeasuredPlan=True,stations=len(stations),domains=len(first),cells=len(cells),
        bytes=len(payload),originalRoadTrianglesPerLod=276,
        measuredAdditionalHingeProjectionSeamsPerLod=4,
        limits=['Cell count includes existing physical X-station intersections; it is not final uploaded triangle count.',
                'Final selected-input shape, coverage, winding, source mip and tangent checks remain mandatory.'])
    (OUT/'native-atlas-plan.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report))


if __name__=='__main__':main()
