"""Prepare Draw01 native chart mappings and replay finite seam subdivision.

This is an offline producer, not a texture bake. Each output vertex carries
barycentric source weights for interpolation of the original mesh attributes.
Only true rectangular authored regions enter the periodic seam path. Region
topology is not evidence of periodic pixel continuity; that remains a separate
native-rendering acceptance check.
"""
import collections
import hashlib
import json
import math
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Draw01/native-charts'


def signature(record):
    triangle = record.get('triangle', record.get('Triangle'))
    fields = ('a', 'b', 'c') if triangle else ('bounds',)
    return (triangle, tuple(np.float32(x).tobytes() for field in fields for x in record[field]))


def clip(vertices, axis, boundary, positive):
    """Clip [chartU,chartV,baryA,baryB,baryC] without altering source shape."""
    result = []
    if not vertices:
        return result
    previous = vertices[-1]
    dp = (previous[axis] - boundary) * positive
    for current in vertices:
        dc = (current[axis] - boundary) * positive
        if (dp < 0) != (dc < 0):
            t = dp / (dp - dc)
            intersection = previous + t * (current - previous)
            intersection[axis] = boundary
            result.append(intersection)
        if dc >= 0:
            result.append(current)
        previous, dp = current, dc
    return result


def split_triangle(chart_uv, maximum_cells=256):
    """Return affine pieces; integral translations occur only across real seams.

    Maximum cells is an offline experiment limit. An exceeded observation is
    reported as unsupported; this is not a production fallback or a hidden clamp.
    Unlike vertex frac, a crossing triangle is split before any UV translation.
    """
    low = np.floor(chart_uv.min(axis=0)).astype(int)
    high = np.ceil(chart_uv.max(axis=0)).astype(int) - 1
    high = np.maximum(high, low)
    count = int(np.prod(high - low + 1))
    if count > maximum_cells:
        return None, count
    polygon = list(np.column_stack((chart_uv, np.eye(3))))
    result = []
    for x in range(low[0], high[0] + 1):
        for y in range(low[1], high[1] + 1):
            p = clip(polygon, 0, x, 1)
            p = clip(p, 0, x + 1, -1)
            p = clip(p, 1, y, 1)
            p = clip(p, 1, y + 1, -1)
            for i in range(1, len(p) - 1):
                triangle = np.array((p[0], p[i], p[i + 1]))
                # Barycentric area detects duplicate/collinear clipping vertices.
                area = np.linalg.det((triangle[1:, 3:5] - triangle[0, 3:5]).T)
                if abs(area) > 1e-14:
                    result.append((triangle[:, :2] - (x, y), triangle[:, 2:], (x, y)))
    return result, count


def main():
    charts = json.loads((OUT / 'charts.json').read_text())
    source_map = json.loads((ROOT / 'artifacts/prepared-materials/Draw01/source-sharing/sampling-map.json').read_text())
    regions = {r['id']: r for r in charts['regions']}
    mappings, by_shape = [], collections.defaultdict(list)
    for sampler in charts['samplers']:
        region = regions[sampler['chart']]
        if not region['rectangle']:
            continue
        original = sampler['originalSampling']
        if original['triangle']:
            a, b, c = (np.array(original[field], dtype=np.float32).astype(float) for field in ('a', 'b', 'c'))
            matrix = np.column_stack((b - a, c - a))
        else:
            bounds = np.array(original['bounds'], dtype=np.float32).astype(float)
            a, matrix = bounds[:2], np.diag(bounds[2:] - bounds[:2])
        bounds = np.array(region['bounds'])
        span = bounds[2:] - bounds[:2]
        mapping = dict(key=sampler['key'], sourceMaterial=sampler['sourceMaterial'],
            sourceDigest=sampler['sourceDigest'], chart=region['id'], bounds=region['bounds'],
            # Row-major 2x2; q = matrix * receiverUv + offset.
            receiverToChart=(matrix / span[:, None]).tolist(),
            offset=((a - bounds[:2]) / span).tolist(),
            atlasMatrix=matrix.tolist(), atlasOffset=a.tolist(),
            policy='native-source-rectangular-chart-with-finite-seams')
        mappings.append(mapping)
        by_shape[signature(original)].append(mapping)
    observations = json.loads((ROOT / 'artifacts/prepared-materials/Draw01/receiver-plans.json.receivers.json').read_text())
    evidence = []
    maximum_area_error = maximum_mapping_error = 0.0
    for observation in observations:
        if observation['name'] not in ('DrawBridge01 Mesh', 'DrawBridge01_LOD1 Mesh') or observation['mesh'] != 0:
            continue
        for mapping in by_shape.get(signature(observation), ()):
            uv = np.asarray(observation['points'], dtype=np.float32).astype(float)
            matrix, offset = np.array(mapping['receiverToChart']), np.array(mapping['offset'])
            q = uv @ matrix.T + offset
            bounds = np.array(mapping['bounds']); span = bounds[2:] - bounds[:2]
            triangles_in = len(q) // 3
            triangles_out = cells_max = rejected = 0
            for i in range(0, len(q), 3):
                triangle = q[i:i + 3]
                pieces, cells = split_triangle(triangle)
                cells_max = max(cells_max, cells)
                if pieces is None:
                    rejected += 1
                    continue
                signed_area = 0.0
                for wrapped, weights, cell in pieces:
                    signed_area += np.linalg.det((weights[1:, 1:] - weights[0, 1:]).T)
                    # Translation is constant on every piece; the atlas gradient
                    # and tangent orientation are unchanged, never reflected.
                    reconstructed = weights @ triangle
                    error = np.max(np.abs(reconstructed - (wrapped + cell)))
                    maximum_mapping_error = max(maximum_mapping_error, float(error))
                    assert np.all(wrapped >= -1e-10) and np.all(wrapped <= 1 + 1e-10)
                if abs(np.linalg.det((triangle[1:] - triangle[0]).T)) > 1e-16:
                    maximum_area_error = max(maximum_area_error, abs(signed_area - 1))
                triangles_out += len(pieces)
            evidence.append(dict(key=mapping['key'], chart=mapping['chart'], render=observation['name'],
                lod=observation['lod'], delta=observation['delta'], scenario=observation['scenario'],
                left=observation['left'], right=observation['right'], repeatOnly=observation['RepeatOnly'],
                inputTriangles=triangles_in, outputTriangles=triangles_out,
                maximumBoundingCells=cells_max, unsupportedTriangles=rejected))
    assert maximum_area_error < 1e-8, maximum_area_error
    assert maximum_mapping_error < 1e-8, maximum_mapping_error
    keys = {m['key'] for m in mappings}
    candidate_cids = {cid for s in source_map['samplers'] if s['key'] in keys for cid in s['oldPreparedTextures'].values()}
    still_used = {cid for s in source_map['samplers'] if s['key'] not in keys for cid in s['oldPreparedTextures'].values()}
    exclusive = candidate_cids - still_used
    texture_dir = ROOT / 'assets/Materials/Draw01/Shared'
    texture_bytes = sum((texture_dir / (cid + '.Texture')).stat().st_size for cid in exclusive)
    result = dict(schema='Draw01/NativeRectangularChartSampling/1', sourceMaterial=charts['sourceMaterial'],
        sourceTextures=charts['textureResources'], samplers=mappings,
        summary=dict(mappedSamplers=len(mappings), charts=len({m['chart'] for m in mappings}),
            receiverObservations=len(evidence), reachedSamplers=len({e['key'] for e in evidence}),
            inputTriangles=sum(e['inputTriangles'] for e in evidence),
            outputTriangles=sum(e['outputTriangles'] for e in evidence),
            maximumBoundingCells=max(e['maximumBoundingCells'] for e in evidence),
            unsupportedTriangles=sum(e['unsupportedTriangles'] for e in evidence),
            maximumBarycentricAreaError=maximum_area_error,maximumAffineMappingError=maximum_mapping_error,
            candidateExclusiveOldTextures=len(exclusive),candidateExclusiveOldTextureBytes=texture_bytes,
            newTexturePayloads=0),
        observations=evidence,
        implementation='split_triangle preserves barycentric attributes and maps each clipped piece to one actual source rectangle; native Surface and original mip graph remain unchanged.',
        acceptance='Geometry/UV arithmetic only. RepeatOnly origins, native chart boundary appearance, filtered mip seams and complete producer integration must be confirmed before activation. Old assets were not changed.')
    (OUT / 'rectangular-sampling.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result['summary'], indent=2))


if __name__ == '__main__':
    main()
