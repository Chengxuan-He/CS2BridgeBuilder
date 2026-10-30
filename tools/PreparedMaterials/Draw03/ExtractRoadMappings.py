"""Measure Draw03 far-road atlas projections without inventing repeat units.

Output is offline evidence, not runtime prototype intersections or permission to
subdivide receivers. Only original atlas-domain boundaries may inform runtime
appearance partitions. No texture is changed or resampled by this tool.
"""
from pathlib import Path
import argparse
import hashlib
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    path = ROOT / 'artifacts/uv-density/native/DrawBridge03_LOD2 Mesh.npz'
    data = np.load(path)
    positions = data['mesh0_positions'][:, :3].astype(np.float64)
    faces = data['mesh0_faces']
    uv = data['mesh0_uv0'][:, :2].astype(np.float64)
    assert positions.shape == (684, 3) and faces.shape == (448, 3)
    records = []
    maximum = 0.0
    for ordinal, indices in enumerate(faces):
        p = positions[indices]
        n = np.cross(p[1] - p[0], p[2] - p[0])
        if not (np.all(p[:, 1] == 10.356689453125) and n[1] > 0
                and p[:, 0].min() < 12.5 and p[:, 0].max() > -12.5):
            continue
        # Affine map in original object X/Z. Preserve both atlas directions;
        # these are finite domains, not a near-LOD 8 m repetition recipe.
        design = np.column_stack((p[:, 0], p[:, 2], np.ones(3)))
        projection = np.linalg.solve(design, uv[indices]).astype(np.float32)
        residual = float(np.max(np.abs(design @ projection - uv[indices])) * 512)
        assert residual <= 0.001, (ordinal, residual)
        maximum = max(maximum, residual)
        records.append(dict(face=ordinal, vertices=indices.tolist(),
                            position=p.tolist(), sourceUv=uv[indices].tolist(),
                            objectXzToUv=projection.tolist(),
                            float32MaximumTexelError=residual,
                            mappingMode=['finite', 'finite']))
    assert len(records) == 48
    seams = []
    for i, left in enumerate(records):
        for right in records[i + 1:]:
            pairs = [(a, b) for a, pa in enumerate(left['position'])
                     for b, pb in enumerate(right['position']) if pa == pb]
            if len(pairs) != 2:
                continue
            delta = [np.subtract(left['sourceUv'][a], right['sourceUv'][b])
                     for a, b in pairs]
            difference = float(np.max(np.abs(delta)) * 512)
            if difference > 0:
                seams.append(dict(faces=[left['face'], right['face']],
                                  edge=[left['position'][a] for a, _ in pairs],
                                  atlasDiscontinuityTexels=difference))
    assert seams, 'Expected authored atlas resets were not found'
    report = dict(status='PASS_ORIGINAL_FINITE_UV_MEASUREMENT',
                  scope='Source projection evidence only; no runtime or visual acceptance.',
                  sourceSurface='9212c7ed45a80abe105853ffe71bc8c4',
                  sourceFile=str(path.relative_to(ROOT)),
                  sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                  sourceTextureSize=[512, 512],
                  roadBearingTriangles=len(records),
                  maximumFloat32TexelError=maximum,
                  roadTriangles=records, authoredAtlasSeams=seams,
                  unresolved=['Baked prototype markings must be separated without changing finite sampling.',
                              'Do not use the near/mid-LOD Surface or repeat rectangles at far LOD.',
                              'Appearance types require source texel inspection, not section-name inference.'])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: report[k] for k in ('status', 'roadBearingTriangles',
                                           'maximumFloat32TexelError')}))


if __name__ == '__main__':
    main()
