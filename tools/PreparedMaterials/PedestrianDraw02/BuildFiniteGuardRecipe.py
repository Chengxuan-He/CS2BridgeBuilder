"""Explicit extension of the two measured finite outer contours only."""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    manifest=json.loads((OUT/'piecewise-repeat-manifest.json').read_text())
    result=[]
    for unit in manifest['units']:
        panels=unit['panels']
        adjacent=3
        p=np.asarray(panels[adjacent]['target']).reshape(3,2)
        uv=np.asarray(panels[adjacent]['source']).reshape(3,2)
        affine=np.linalg.solve(np.column_stack([p,np.ones(3)]),uv)
        if unit['id'].endswith('.8556'):
            contour=[p[2].tolist(),p[1].tolist()]
            guard=np.array([contour[0],[1.,0.],contour[1]])
            exterior='x > 1 - epsilon * (1-y); finite right contour'
        else:
            contour=[p[2].tolist(),p[0].tolist()]
            guard=np.array([contour[0],[0.,1.],contour[1]])
            exterior='x < epsilon * y; finite left contour'
        mapped=np.column_stack([guard,np.ones(3)])@affine
        # The shared contour reproduces the neighbouring source mapping exactly.
        checks=[]
        for q in contour:
            source=np.r_[q,1]@affine
            checks.append(dict(target=q,source=source.tolist()))
        area=abs(float(np.linalg.det(np.stack([guard[1]-guard[0],guard[2]-guard[0]]))))/2
        assert 0<area<2e-7
        result.append(dict(id=unit['id'],guardPanels=[dict(adjacentPanel=adjacent,
            target=guard.reshape(-1).tolist(),source=mapped.reshape(-1).tolist())],
            finiteOuterContour=contour,exteriorCondition=exterior,
            unitArea=area,sharedContourChecks=checks,
            sourceAffine=affine.tolist(),normalFrame='Same omitted identity transform as adjacent panel',
            sampler='Original source sampler and full affine derivatives; no constant UV edge clamp',
            scope='Only measured finite outside wedge within unit rectangle; no internal hole or V extension'))
    (OUT/'finite-guard-recipe.json').write_text(json.dumps(dict(schema=1,units=result),indent=2)+'\n')
    print(json.dumps(dict(units=len(result),areas=[u['unitArea'] for u in result])))


if __name__=='__main__':main()
