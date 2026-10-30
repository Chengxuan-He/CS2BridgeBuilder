"""Prepare bounded original fixed/endpoint domains for offline frame-aware baking.

Each recipe represents one original finite panel, not a generated road span.
Only byte-identical normalized source maps share a recipe; mirrored sampling is
metadata. No runtime triangle domains or period cuts are generated here.
"""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    source=json.loads((OUT/'native-panel-recipe.json').read_text())
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    products={}
    uses=[]
    for level,frame_level in zip(source['levels'],frames['levels']):
        assert level['lod']==frame_level['lod']
        by_face={p['firstFace']:p for p in frame_level['panels']}
        for panel in level['panels']:
            if panel['repeatedV']:continue
            frame=by_face[panel['firstFace']]
            target=np.asarray(frame['canonicalTargetTangent'],dtype=np.float64)
            source_frames={t['face']:t for t in frame['sourceFrames']}
            # Orient source UV sampling independently from the native tangent.
            # A left/right mirror is use metadata, not another pixel identity.
            gradients=np.asarray([t['sourceUvAffine'] for t in panel['triangles']])
            flips=[bool(np.median(gradients[:,0,0])<0),bool(np.median(gradients[:,1,1])<0)]
            normal=np.array([0.,1.,0.])
            bitangent=np.cross(normal,target[:3])*target[3]
            triangles=[]
            for triangle in panel['triangles']:
                unit=np.asarray(triangle['unit'])
                for axis in range(2):
                    if flips[axis]:unit[:,axis]=1-unit[:,axis]
                tangent=np.asarray(source_frames[triangle['face']]['tangents'])
                # Store the measured source frame in the target frame; physical
                # rotation of the whole opposite leaf does not duplicate pixels.
                relative=tangent.copy()
                relative[:,:3]=np.stack([tangent[:,:3]@target[:3],tangent[:,:3]@bitangent,
                    tangent[:,:3]@normal],axis=1)
                triangles.append(dict(unit=unit.tolist(),sourceUv=triangle['sourceUv'],
                    sourceTangents=relative.tolist()))
            # Canonicalize triangle/vertex order without changing any scalar.
            canonical=[]
            for triangle in triangles:
                vertices=sorted([list(q)+list(uv)+list(t) for q,uv,t in zip(
                    triangle['unit'],triangle['sourceUv'],triangle['sourceTangents'])])
                canonical.append(vertices)
            canonical.sort()
            identity=dict(triangles=canonical,repeatAxes='None',
                sourceCid=source['sourceCid'])
            key=hashlib.sha256(json.dumps(identity,separators=(',',':')).encode()).hexdigest()
            if key not in products:
                products[key]=dict(key=key,repeatAxes='None',finiteSourceGrid=False,
                    reason='Piecewise original source UVs require explicit bounded resampling; a single affine source-grid lock is inapplicable.',
                    sourceCid=source['sourceCid'],descriptor=source['descriptor'],channels=source['channels'],
                    sourceUvMin=panel['sourceUvMin'],sourceUvMax=panel['sourceUvMax'],
                    physicalSize=panel['physicalSize'],targetTangent=target.tolist(),triangles=triangles,
                    sourceFrameAngleDegrees=frame['originalFrameAngleDegrees'])
            uses.append(dict(lod=level['lod'],firstFace=panel['firstFace'],bone=panel['bone'],
                key=key,physicalMin=panel['physicalMin'],physicalMax=panel['physicalMax'],
                nativeFrame=target.tolist(),flipPhysicalAxes=flips))
    result=dict(schema='PedestrianDraw02/FinitePanel/1',products=list(products.values()),uses=uses,
        status='AUTHORING_RECIPE_NOT_BAKED',
        limits=['Per-vertex source tangent interpolation must be transformed into the recorded target frame offline.',
                'Shared per-mip phase correction and filtered-source equivalence are pending.',
                'Exact-only identity currently leaves distinct mirrored/quantized recipes; no budget acceptance is claimed.',
                'These authoring triangles are never runtime partitions. Only the original fixed/hinge/tip physical domains may divide receivers.'])
    (OUT/'finite-panel-recipes.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(finiteUses=len(uses),exactProducts=len(products),lods=len(source['levels']))))


if __name__=='__main__':main()
