"""Check Draw02 repeat recipes against native face gradients and runtime routing."""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np
from ExportRecipes import numbers, key, f32


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('repository','native_mesh','inventory','report'):
        p.add_argument(name,type=Path)
    a=p.parse_args()
    inv=json.loads(a.inventory.read_text())
    if len(inv['units'])!=5 or inv['units'][4]['cutout'] or not inv['units'][0]['cutout']:
        raise ValueError('Missing Draw02 opaque/cutout carriageway variants')
    for field in ('sourceCid','sourceDigest','min','max','rows','bands'):
        if inv['units'][0][field]!=inv['units'][4][field]:raise ValueError('Carriageway pixel recipes differ')
    for unit in inv['units']:
        if unit['key']!=key(unit):raise ValueError('Descriptor key differs')
    f=numbers(a.repository/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02RoadAppearance_cs.bin')['F32']
    m=np.load(a.native_mesh)
    pos=m['mesh0_positions'].astype(float);uv=m['mesh0_uv0'].astype(float);faces=m['mesh0_faces']
    # Measured source faces are recorded in Draw02RoadAppearance. Compare both
    # physical axes independently; mirrors retain magnitude but not orientation.
    cases=[(0,1,3680),(0,1,3756),(0,0,3684),(0,2,3760),(0,0,3784),
           (1,None,3634),(1,None,3710),(2,None,2492),(2,None,2512),(3,None,10701),(3,None,10703)]
    gradients=[(f32(f[32]/f[33]),f[22]),(f[25],f[22]),(f32(f[42]/f[43]),f[44]),(f[25],f[51])]
    results=[]
    for index,band,face in cases:
        u=inv['units'][index]
        if key(u)!=u['key']:raise ValueError('Recipe key mismatch')
        rectangle=u if band is None else u['bands'][band]
        extent=np.array(rectangle['max'])-np.array(rectangle['min'])
        gradient=np.diag(extent*np.array(gradients[index]))
        if band is not None:
            gradient[0,0]/=rectangle['end']-rectangle['start'];gradient[1,1]*=rectangle['repeatY']
        ids=faces[face];xz=pos[ids][:,[0,2]]
        native=np.linalg.solve(xz[1:]-xz[0],uv[ids][1:]-uv[ids][0]).T
        metric=native.T@native;emitted=gradient.T@gradient
        relative=float(np.linalg.norm(emitted-metric)/np.linalg.norm(metric))
        # Native half-precision positions quantize symmetric metal strips by one
        # source-coordinate quantum; retain that measured discrepancy explicitly.
        if relative>0.0002:raise ValueError(f'Native density differs: face={face}, relative={relative}')
        results.append(dict(unit=index,band=band,face=face,nativeGradient=native.tolist(),projectedGradient=gradient.tolist(),relativeMetricError=relative))
    runtime=(a.repository/'src/BridgeBuilder/Bridges/Types/Draw02/Draw02RoadSampling.cs').read_text(encoding='utf-8-sig')
    preparation=(a.repository/'src/BridgeBuilder/Bridges/Common/Movable/MovableRoadPreparation.cs').read_text(encoding='utf-8-sig')
    if 'Draw02RoadAppearance.Patches' not in runtime or 'MovableRoadPreparation.Prepare' not in runtime or 'MovableSurfaceAtlas.Compose' not in preparation:
        raise ValueError('Draw02 no longer routes through the measured shared composer')
    report=dict(style='Draw02',recipes=len(inv['units']),checkedNativeFaces=len(results),maxRelativeMetricError=max(r['relativeMetricError'] for r in results),results=results,
                nativeMeshSHA256=hashlib.sha256(a.native_mesh.read_bytes()).hexdigest(),inventorySHA256=hashlib.sha256(a.inventory.read_bytes()).hexdigest(),
                limits='Source geometry/projection evidence only. Prepared texture dimensions, mips, runtime reachability and game appearance remain separate acceptance checks.')
    a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(f"PASS Draw02 source density: {len(results)} measured faces; max relative metric error {report['maxRelativeMetricError']:.9g}; 5 descriptor keys; shared composition route present.")


if __name__=='__main__':main()
