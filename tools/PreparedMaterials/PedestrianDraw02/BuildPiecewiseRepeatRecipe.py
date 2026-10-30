"""Prepare exact one-period sampling inputs for a piecewise-affine baker.

Output is deliberately not a /3 manifest: /3 currently has only separable bands.
No whole-span image and no runtime triangle recipe is produced.
"""
import hashlib
import json
import numpy as np
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/PedestrianDraw02'


def main():
    path = OUT / 'native-panel-recipe.json'
    source = json.loads(path.read_text())
    level = source['levels'][0]
    units = []
    for face in level['movingUnitFirstFaces']:
        panel = next(p for p in level['panels'] if p['firstFace'] == face)
        pieces = []
        for t in panel['triangles']:
            # Output directions retain native -X/-Z source tangent orientation.
            # Source affine derivatives must be transformed with the coordinates.
            a = t['sourceUvAffine']
            pieces.append(dict(unitTriangle=[[1-q[0],1-q[1]] for q in t['unit']],
                sourceUv=t['sourceUv'],
                sourceDerivativeU=[-v for v in a[0]],
                sourceDerivativeV=[-v for v in a[1]],
                sourceConstant=[a[0][j]+a[1][j]+a[2][j] for j in range(2)],
                originalFace=t['face']))
        units.append(dict(id=f'PedestrianDraw02.NativeHalf.{face}',
            sourceCid=source['sourceCid'], descriptor=source['descriptor'],
            channels=source['channels'], repeatAxes='V',
            physicalSize=panel['physicalSize'], min=panel['sourceUvMin'], max=panel['sourceUvMax'],
            sourceGradientSampling=True, pieces=pieces,
            uncoveredAreaFraction=1-panel['areaFraction'],
            normalFrame='Identity packed-channel sampling; target native frame must be established independently.'))
    result = dict(schema='PedestrianDraw02/PiecewiseRepeatAuthoring/1',
        sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(), units=units,
        requiredBakerSemantics=['Locate q in an offline unit triangle, evaluate its source affine UV.',
            'SampleGrad uses the full two-dimensional affine Jacobian at each output mip.',
            'Finite U guards use the same piecewise source projection; V repeats exactly one period.',
            'Do not silently weld the measured micrometre boundary gaps; report any extension policy.',
            'No packed normal decode/re-encode for identity transforms.'],
        status='REQUIRES_PIECEWISE_BAKER_INTERFACE_AND_NATIVE_VALIDATION')
    (OUT/'piecewise-repeat-recipe.json').write_text(json.dumps(result,indent=2)+'\n')
    manifest_units=[]
    maximum_position=0.;maximum_gradient=0.;samples=0
    for unit in units:
        panels=[]
        for piece in unit['pieces']:
            target=np.asarray(piece['unitTriangle']);uv=np.asarray(piece['sourceUv'])
            solved=np.linalg.solve(np.column_stack([target,np.ones(3)]),uv)
            expected=np.asarray([piece['sourceDerivativeU'],piece['sourceDerivativeV'],piece['sourceConstant']])
            maximum_gradient=max(maximum_gradient,float(np.max(np.abs(solved[:2]-expected[:2]))))
            for bary in ([1/3]*3,[.8,.1,.1],[.1,.8,.1],[.1,.1,.8]):
                q=np.asarray(bary)@target
                actual=np.r_[q,1]@expected
                maximum_position=max(maximum_position,float(np.max(np.abs(actual-np.asarray(bary)@uv))))
                samples+=1
            panels.append(dict(target=target.reshape(-1).tolist(),source=uv.reshape(-1).tolist()))
        manifest_units.append(dict(id=unit['id'],sourceCid=unit['sourceCid'],
            sourceDigest=unit['descriptor']['sha256'],descriptor=unit['descriptor'],
            channels=[dict(c,encoding='BC7') for c in unit['channels']],
            min=unit['min'],max=unit['max'],physicalSize=unit['physicalSize'],repeatAxes='V',
            sourceGradientSampling=True,panels=panels,aliases=[],
            evidence=dict(path=str(path),sha256=result['sourceSha256'])))
    assert maximum_position<1e-12 and maximum_gradient<1e-12
    (OUT/'piecewise-repeat-manifest.json').write_text(json.dumps(dict(
        recipe='BridgeBuilder/RepeatUnits/3',style='PedestrianDraw02',units=manifest_units),indent=2)+'\n')
    (OUT/'piecewise-repeat-affine-check.json').write_text(json.dumps(dict(
        affineStatus='PASS',samples=samples,maxUvResidual=maximum_position,
        maxJacobianResidual=maximum_gradient,
        coverageStatus='Original micrometre boundary gap retained; guard extension requires producer validation.',
        nativeSamplingStatus='NOT_TESTED'),indent=2)+'\n')
    print(json.dumps(dict(units=len(units),pieces=[len(u['pieces']) for u in units],status=result['status'])))


if __name__ == '__main__':
    main()
