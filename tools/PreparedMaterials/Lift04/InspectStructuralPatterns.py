"""One finite full-channel comparison of the original beam profile strips."""
import json
from pathlib import Path
import numpy as np
from InspectStructureCounterexample import bc7_image

ROOT=Path(__file__).resolve().parents[3]


def main():
    output=ROOT/'artifacts/prepared-materials/Lift04/structure-inventory'
    manifest=json.loads((ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/extracted-source/manifest.json').read_text())
    images={c['name']:np.asarray(bc7_image(Path(c['path']).read_bytes())).astype(np.int16) for c in manifest['units'][0]['channels']}
    # Bounds are original chart records, not receiver triangles or width variants.
    profiles=[('outer-flange',[2210,2076,2226,3342]),('inner-web',[2029,2076,2072,2932]),
              ('wide-beam-profile',[1910,2076,2176,2932]),('long-beam-profile',[1990,2076,2176,3342])]
    charts=json.loads((ROOT/'artifacts/prepared-materials/Lift04/shared-sampling/native-charts.json').read_text())
    records=[]
    for name,bounds in profiles:
        members=[]
        for m in charts['meshes']:
            for chart in m['charts']:
                if np.array_equal(np.array(chart['bounds'])*4096,bounds):members.append(dict(render=m['render'],mesh=m['mesh'],chart=chart['id'],faces=chart['faces'],exactRectangle=chart['exactRectangle']))
        assert members
        channels=[]
        x0,y0,x1,y1=bounds
        for channel,image in images.items():
            strip=image[y0:y1,x0:x1];unit=strip[:404]
            diff=np.abs(strip[404:]-strip[:-404]);seam=np.abs(unit[0]-unit[-1]);adjacent=np.abs(np.diff(strip,axis=0))
            horizontal=np.abs(unit[:,0]-unit[:,-1])
            channels.append(dict(name=channel,period404RGBA_MAE=float(diff.mean()),period404RGBA_Max=int(diff.max()),
                                 verticalUnitSeamMAE=float(seam.mean()),verticalUnitSeamMax=int(seam.max()),
                                 originalAdjacentMAE=float(adjacent.mean()),originalAdjacentMax=int(adjacent.max()),
                                 horizontalUnitSeamMAE=float(horizontal.mean()),horizontalUnitSeamMax=int(horizontal.max()),
                                 minRGBA=strip.min(axis=(0,1)).tolist(),maxRGBA=strip.max(axis=(0,1)).tolist()))
        records.append(dict(id=name,coveragePixels=bounds,unitPixels=[x1-x0,404],sourceChartMembership=members,channels=channels,
                            note='Original chart profile repeats along source V; cross-profile U is not assumed periodic. Original Normal/Mask edge details remain in the complete U profile. No production unit is implied by this diagnostic alone.'))
    (output/'beam-profile-patterns.json').write_text(json.dumps(dict(source=manifest['units'][0]['sourceDigest'],profiles=records),indent=2)+'\n')
    for record in records:
        print(record['id'],record['unitPixels'],len(record['sourceChartMembership']))
        for c in record['channels']:print(c['name'],{k:round(c[k],4) for k in ('period404RGBA_MAE','period404RGBA_Max','verticalUnitSeamMAE','verticalUnitSeamMax','originalAdjacentMax','horizontalUnitSeamMax')})


if __name__=='__main__':main()
