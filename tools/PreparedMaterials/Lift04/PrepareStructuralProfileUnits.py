"""Prepare a finite two-profile full-channel bake; no broad atlas fallback."""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]


def main():
    evidence=ROOT/'artifacts/prepared-materials/Lift04/structure-inventory'
    inputs=ROOT/'artifacts/agent-material-env/Lift04/structural-profiles/inputs'
    inputs.mkdir(parents=True,exist_ok=True)
    patterns=json.loads((evidence/'beam-profile-patterns.json').read_text())
    original=json.loads((ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/extracted-source/manifest.json').read_text())['units'][0]
    units=[];proofs=[]
    for name in ('wide-beam-profile','outer-flange'):
        profile=next(p for p in patterns['profiles'] if p['id']==name)
        x0,y0,x1,y1=profile['coveragePixels'];member=profile['sourceChartMembership'][0]
        source=ROOT/'artifacts/uv-density/native'/(member['render']+'.npz')
        mesh=np.load(source);mi=member['mesh'];faces=mesh[f'mesh{mi}_faces'];uv=mesh[f'mesh{mi}_uv0'];xyz=mesh[f'mesh{mi}_positions']
        frames=[]
        for face in member['faces']:
            indices=faces[face];q=uv[indices].astype(float);p=xyz[indices].astype(float)
            matrix=(q[1:]-q[0])
            if abs(np.linalg.det(matrix))<1e-14:continue
            derivative=np.linalg.solve(matrix,p[1:]-p[0])
            physical=[float(np.linalg.norm(derivative[0])*(x1-x0)/4096),float(np.linalg.norm(derivative[1])*404/4096)]
            frames.append(dict(face=face,physicalPeriodUV=physical,sourceDensityUV=[(x1-x0)/physical[0],404/physical[1]]))
        assert frames
        proof=dict(id=name,sourceDigest=original['sourceDigest'],unitBounds=[x0/4096,y0/4096,x1/4096,(y0+404)/4096],
                   coverageBounds=[x0/4096,y0/4096,x1/4096,y1/4096],sourceChartMembership=profile['sourceChartMembership'],
                   sourceMeshSha256=hashlib.sha256(source.read_bytes()).hexdigest(),physicalFrames=frames,channels=profile['channels'],
                   constraint='Longitudinal V pattern only. Preserve full original cross-profile U detail; do not extrapolate/repeat U. Shared native sampler/coverage constraint must be verified before installation.')
        proof_path=evidence/(name+'-evidence.json');proof_path.write_text(json.dumps(proof,indent=2)+'\n');proofs.append(proof)
        units.append(dict(id='Lift04-'+name,sourceCid=original['sourceCid'],sourceDigest=original['sourceDigest'],
                          min=proof['unitBounds'][:2],max=proof['unitBounds'][2:],physicalSize=frames[0]['physicalPeriodUV'],
                          repeatAxes="V",aliases=[],evidence=dict(path=str(proof_path),sha256=hashlib.sha256(proof_path.read_bytes()).hexdigest()),
                          descriptor=original['descriptor'],channels=original['channels']))
    manifest=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift04',units=units)
    (inputs/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (evidence/'profile-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(inputs/'manifest.json')


if __name__=='__main__':main()
