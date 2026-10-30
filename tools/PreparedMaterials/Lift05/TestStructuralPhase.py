"""Regression checks for the authorized Lift05-only source-edge phase transport."""
import json
import numpy as np
from ReviewStructuralSeams import ROOT, phases, motion


def main():
    policy=json.loads((ROOT/'artifacts/prepared-materials/Lift05/non-tiling/classification.json').read_text())
    cases=[]
    for lod in range(2):
        data=np.load(ROOT/f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz')
        p=data['mesh0_positions'].astype(float);uv=data['mesh0_uv0'].astype(float);f=data['mesh0_indices']
        render='LiftBridge05 Mesh' if lod==0 else 'LiftBridge05_LOD1 Mesh'
        parts=next(m for m in policy['meshes'] if m['render']==render and m['mesh']==0)['mixedProfileEvidence']
        weights,_=motion(lod,len(p))
        for delta in (0,1,3,6,12):
            target=p.copy();target[:,0]=(p[:,0].astype(np.float32)+weights*np.float32(delta)).astype(float)
            offsets,evidence=phases(parts,p,uv,f,target)
            errors=[];naive=[]
            for edge in evidence['constraints']:
                a,b=edge['first'],edge['second']
                errors.append(abs(offsets[b]-offsets[a]-edge['offsetDifference']))
                naive.append(abs(edge['offsetDifference']))
            if delta==0:assert all(value==0 for value in offsets.values())
            assert max(errors,default=0)<1e-10
            if delta==3:assert max(naive)>.2, 'Negative control must reject independent per-domain anchors.'
            cases.append(dict(lod=lod,delta=delta,domains=len(parts),
                connectedComponents=len(evidence['components']),edges=len(errors),
                maximumPhaseConstraintError=max(errors,default=0),
                naiveAnchorMaximumPhaseMismatch=max(naive,default=0),
                coveredSourceTriangles=sum(len(p['faces']) for p in parts)))
    assert {c['domains'] for c in cases}=={80,49}
    out=ROOT/'artifacts/material-remap/Lift05/structural-phase-tests.json'
    out.write_text(json.dumps(dict(passed=True,cases=cases,
        scope='Offline source-edge phase constraints at actual binary32 width recipes; no native upload, normals, mips or game acceptance claim.'),indent=2)+'\n')
    print('PASS 10 original-identity/width/phase cases and rejected independent-anchor negative control.')


if __name__=='__main__':main()
