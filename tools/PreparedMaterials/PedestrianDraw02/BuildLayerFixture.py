"""Export measured PedestrianDraw02 patch inputs for independent-layer regression.

This does not classify selected roads or authorize a replacement error tolerance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import numpy as np
from ExportRecipes import numbers


def sha(b):return hashlib.sha256(b).hexdigest()


def key(p,uv):return tuple(sorted(tuple(float(x) for x in row) for row in np.concatenate((p,uv),axis=1)))


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();root=a.repository.resolve();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
    base=root/'artifacts/prepared-materials/PedestrianDraw02'
    meta=json.loads((base/'source-consolidation/native-charts.json').read_text())
    fixture=base/'source-consolidation/native-charts.npz'
    proof=json.loads((root/'tools/PreparedMaterials/PedestrianDraw02/ClosureUvParts.json').read_text())
    assert sha(fixture.read_bytes())==proof['nativeChartsSha256']
    arrays=np.load(fixture)
    records=[r for r in meta['records'] if r['mesh']==0 and r['name'] in ('PedestrianDrawBridge02 Mesh','PedestrianDrawBridge02_LOD1 Mesh')]
    assert len(records)==2
    near=next(r for r in records if r['lod']==0);source=near['sourceGraph']
    appearance=root/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.bin'
    f=numbers(appearance)['F32'];assert len(f)==31
    pref=near['arrayPrefix'];positions=arrays[pref+'_positions'];uv=arrays[pref+'_uv'];faces=arrays[pref+'_faces']
    units=[]
    for label,ids,low,high,active in [('fixed',(6708,6709),f[6:8],f[8:10],True),('moving-unused',(8556,8557),f[17:19],f[19:21],False)]:
        ps=positions[faces[list(ids)]];us=uv[faces[list(ids)]]
        assert np.array_equal(us.reshape(-1,2).min(axis=0),low)
        assert np.array_equal(us.reshape(-1,2).max(axis=0),high)
        span=np.ptp(ps.reshape(-1,3),axis=0)[[0,2]]
        by_lod=[];source_keys={key(x,y) for x,y in zip(ps,us)}
        for r in records:
            prefix=r['arrayPrefix'];rp=arrays[prefix+'_positions'];ru=arrays[prefix+'_uv'];rf=arrays[prefix+'_faces']
            matched=[i for i,face in enumerate(rf) if key(rp[face],ru[face]) in source_keys]
            assert len(matched)==2,(label,r['name'],matched)
            by_lod.append(dict(render=r['name'],lod=r['lod'],mesh=0,sourceFaces=matched))
        units.append(dict(name=label,active=active,sourceFaces=list(ids),positions=ps.tolist(),uv=us.tolist(),
                          minimum=low,maximum=high,physicalPeriodMetres=span.tolist(),lods=by_lod))
    payloads=[]
    for location in source['locations']:
        with zipfile.ZipFile(location['archive']) as z:b=z.read(location['entry'])
        assert sha(b)==source['sha256']
        target=out/(source['cid']+'.Surface');target.write_bytes(b);Path(str(target)+'.cid').write_text(source['cid'])
        payloads.append(dict(path=str(target),cid=source['cid'],sha256=sha(b),archive=location['archive'],entry=location['entry']))
    # A repeat unit has independent longitudinal/transverse projection axes. Probe
    # binary32 evaluation against the measured binary-exact coordinate span; these
    # are diagnostics, never a universal tolerance selected to pass this example.
    precision=[]
    for axis,period in enumerate(units[0]['physicalPeriodMetres']):
        frequency=np.float32(1)/np.float32(period)
        texels=(units[0]['maximum'][axis]-units[0]['minimum'][axis])*4096
        for repetitions in (1,2,16,64,128):
            distance=np.float32(period*repetitions)
            phase=np.float32(frequency*distance)
            precision.append(dict(axis=axis,repetitions=repetitions,distanceMetres=float(distance),
                frequency=float(frequency),frequencyBits=struct.pack('<f',frequency).hex(),
                localBoundaryErrorSourceTexels=abs(float(frequency)*period-1)*texels,
                cumulativeFrequencyErrorSourceTexels=abs(float(frequency)*float(distance)-repetitions)*texels,
                binary32EvaluatedErrorSourceTexels=abs(float(phase)-repetitions)*texels))
    report=dict(style='PedestrianDraw02',prototypeSurface=source,fixtureSHA256=proof['nativeChartsSha256'],
        appearanceBBNSHA256=sha(appearance.read_bytes()),units=units,payloads=payloads,precision=precision,
        inspectedRuntimeFiles={str(path.relative_to(root)):sha(path.read_bytes()) for path in [
            root/'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02RoadPreparation.cs',
            root/'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02RoadAppearance.cs',
            root/'src/BridgeBuilder/Bridges/Common/Movable/MovableRoadPreparation.cs',
            root/'src/BridgeBuilder/Bridges/Common/Movable/MovableSurfaceAtlas.cs']},
        route=dict(prototypeRoad=False,fixed='prepared prototype repeat; mirrored regions 0 and 1',
            moving='original selected-road projection; no prototype road replacement',
            worldspace='selected-road source dependent; not established by prototype template0',
            blend='no PedestrianDraw02 prototype/selected-road blend; native selected-road layers still require independent representation'),
        limits=['Test repetition counts are numeric probes, not claimed supported receiver lengths.',
                'No universal replacement tolerance is established here.',
                'Native object multilayer skinning, shader sampling and game rendering require shared verification.'])
    (out/'fixture.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('PASS two source quads at both LODs; one active fixed repeat; source Surface hash and BBN preserved')


if __name__=='__main__':main()
