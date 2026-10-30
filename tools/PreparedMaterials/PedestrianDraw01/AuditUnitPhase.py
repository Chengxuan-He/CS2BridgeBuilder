"""Compare original near/mid face UVs with measured single-unit projections."""
import argparse
import json
from pathlib import Path
import numpy as np


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository',type=Path);p.add_argument('manifest',type=Path)
    a=p.parse_args()
    audit=json.loads((a.repository/'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json').read_text())
    units={u['id']:u for u in json.loads(a.manifest.read_text())['units']}
    rows=[]
    for level in audit['levels'][:2]:
        for f in level['walkwayFaces']:
            positions=np.array(f['positions']);uv=np.array(f['uv0'])
            identity=('moving' if f['bone'] else 'fixed')+('-positive' if positions[:,2].mean()>0 else '-negative')
            unit=units[identity];width,period=unit['physicalSize'];origin=unit['phase']['zOrigin']
            for weights in ([1/3]*3,[.9998,.0001,.0001],[.0001,.9998,.0001],[.0001,.0001,.9998]):
                pos=np.array(weights)@positions;expected=np.array(weights)@uv
                q=(pos[0]/width+.5,(pos[2]-origin)/period%1)
                band=next(b for b in unit['bands'] if b['start']<=q[0]<=b['end'])
                low,high=np.array(band['min']),np.array(band['max'])
                actual=low+np.array([(q[0]-band['start'])/(band['end']-band['start']),q[1]])*(high-low)
                error=abs(actual-expected)*4096
                # Wrap distance only in the authored longitudinal repeat, never
                # across the finite width or between unrelated atlas regions.
                span=abs(high[1]-low[1])*4096
                error[1]=min(error[1],abs(error[1]-span))
                rows.append(dict(lod=level['lod'],face=f['face'],unit=identity,position=pos.tolist(),sourceUv=expected.tolist(),unitUv=actual.tolist(),sourceTexelError=error.tolist()))
    result=dict(samples=len(rows),maximumSourceTexelError=np.max([r['sourceTexelError'] for r in rows],axis=0).tolist(),
        worst=sorted(rows,key=lambda r:max(r['sourceTexelError']),reverse=True)[:32],
        qualification='Phase measurement only. Filtered color/normal/mask comparisons, partial-end charts and runtime binding require separate validation.')
    a.manifest.with_name('phase-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='worst'}))


if __name__=='__main__':main()
