"""Verify the emitted appearance station plan at supported width samples.

This checks source coverage/bones and actual serialized Float32 UV arithmetic,
not uploaded selected-road triangle complexity or visual/native load acceptance.
"""
import json
from pathlib import Path
import numpy as np
from ExportRecipes import numbers

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    data=numbers(ROOT/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_AtlasAppearance.bin')
    integers,values=np.array(data['I32']),np.array(data['F32'],dtype=np.float32)
    _,station_count,domain_count,cell_count=integers[:4]
    domains=json.loads((OUT/'native-atlas-domains.json').read_text())['levels'][0]['domains']
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    source_triangles={t['face']:t for p in recipe['levels'][0]['panels'] for t in p['triangles']}
    station_at=domain_count*20
    stations=values[station_at:station_at+station_count*2].reshape(-1,2)
    cells=integers[4+domain_count:].reshape(-1,2)
    limits=values[station_at+station_count*2:].reshape(-1,4)
    assert len(cells)==cell_count==len(limits)
    reports=[]
    for delta in (-.5,0.,2.,8.):
        x=(stations[:,0]+stations[:,1]*np.float32(delta)).astype(np.float32)
        assert np.all(np.diff(x)>0)
        max_error=0.;maximum_roundoff_bound=0.;samples=0;wrong=0
        for domain_index,domain in enumerate(domains):
            for face in domain['sourceFaces']:
                triangle=source_triangles[face]
                for weights in ([1/3,1/3,1/3],[.6,.2,.2],[.2,.6,.2],[.2,.2,.6]):
                    source=np.asarray(weights)@np.asarray(triangle['position'])
                    uv=np.asarray(weights)@np.asarray(triangle['sourceUv'])
                    slab=int(np.searchsorted(stations[:,0],source[0],side='right')-1)
                    if not 0<=slab<station_count-1:raise AssertionError((face,source))
                    ratio=(source[0]-stations[slab,0])/(stations[slab+1,0]-stations[slab,0])
                    px=np.float32(x[slab]+ratio*(x[slab+1]-x[slab]))
                    candidates=[]
                    for cell in np.flatnonzero(cells[:,0]==slab):
                        lo,hi=(1-ratio)*limits[cell,[0,2]]+ratio*limits[cell,[1,3]]
                        if lo-1e-8<=source[2]<=hi+1e-8:candidates.append(int(cells[cell,1]))
                    assert domain_index in candidates,(face,domain_index,candidates)
                    assert all(domains[c]['bone']==domain['bone'] for c in candidates)
                    scale=np.float32((stations[slab+1,0]-stations[slab,0])/(x[slab+1]-x[slab]))
                    offset=np.float32(stations[slab,0]-np.float32(scale*x[slab]))
                    result=[]
                    for channel in (4,7):
                        a,b,c=values[domain_index*20+channel:domain_index*20+channel+3]
                        aa=np.float32(a*scale);cc=np.float32(c+np.float32(a*offset))
                        result.append(float(np.float32(np.float32(aa*px)+np.float32(b*np.float32(source[2]))+cc)))
                        # Standard floating-point forward-error magnitude bound
                        # for the fixed multiply/add sequence, not a UV tolerance.
                        magnitude=abs(float(aa)*float(px))+abs(float(b)*source[2])+abs(float(cc))
                        maximum_roundoff_bound=max(maximum_roundoff_bound,8*np.finfo(np.float32).eps*magnitude*4096)
                    max_error=max(max_error,float(abs(np.asarray(result)-uv).max()*4096));samples+=1
        reports.append(dict(halfWidthDelta=delta,sourceInteriorSamples=samples,
            maxSourceUvDifferencePixels=max_error,maxFloatArithmeticBoundPixels=maximum_roundoff_bound,
            coverageAndBonePass=wrong==0))
    result=dict(status='PASS_SOURCE_PLAN_COVERAGE',stationCount=int(station_count),domainCount=int(domain_count),
        cellCount=int(cell_count),widths=reports,
        limits=['Both source LOD plans were proven equal by BuildAtlasPlan; this executes their shared serialized measurements.',
                'UV differences are measurements; this does not turn them into a universal phase threshold.',
                'Selected input triangulation, all-LOD upload shape/count and full normal interpolation remain pending.'])
    (OUT/'native-atlas-plan-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':main()
