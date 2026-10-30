"""Validate the measured source-to-target frame conversion algebra offline.

Does not establish pixel/filter equivalence or replace native rendering checks.
"""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    recipes=json.loads((OUT/'finite-panel-recipes.json').read_text())
    tested=0
    max_error=0.
    max_angle=0.
    normal=np.array([0.,1.,0.])
    vectors=np.array([[0.,0.,1.],[.6,0.,.8],[0.,.6,.8],[.3,.4,np.sqrt(.75)]])
    for product in recipes['products']:
        target=np.asarray(product['targetTangent'])
        t=target[:3]
        b=np.cross(normal,t)*target[3]
        basis=np.stack([t,b,normal],axis=1)
        assert abs(np.linalg.det(basis)+1)<1e-12
        for triangle in product['triangles']:
            relative=np.asarray(triangle['sourceTangents'])
            assert (relative[:,3]==-1).all()
            for weights in ([1,0,0],[0,1,0],[0,0,1],[.2,.3,.5]):
                source_t=np.asarray(weights)@relative[:,:3]
                source_t/=np.linalg.norm(source_t)
                assert abs(source_t[2])<1e-10
                source_world=basis@source_t
                source_b=np.cross(normal,source_world)*-1
                # Independently form the source->world and local 2D rotation.
                expected=vectors@np.stack([source_world,source_b,normal],axis=1).T
                a,c=source_t[:2]
                transformed=vectors@np.array([[a,-c,0],[c,a,0],[0,0,1]]).T
                actual=transformed@basis.T
                max_error=max(max_error,float(abs(actual-expected).max()))
                max_angle=max(max_angle,float(np.degrees(np.arccos(np.clip(a,-1,1)))))
                tested+=len(vectors)
    assert max_error<1e-12
    result=dict(status='PASS_FRAME_ALGEBRA_ONLY',normalVectors=tested,
        maxWorldNormalComponentError=max_error,maxMeasuredFrameRotationDegrees=max_angle,
        limits=['Uses actual native decoded per-vertex tangent data and barycentric interpolation.',
                'Normal texture reconstruction, filtered mips and native output rendering remain unverified.'])
    (OUT/'finite-frame-algebra.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':main()
