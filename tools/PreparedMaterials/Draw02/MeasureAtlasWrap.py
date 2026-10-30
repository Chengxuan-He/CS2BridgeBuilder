"""Measure native-atlas repeat mappings and genuine feature-boundary slabs.

No source triangles are shipped or intersected with generated geometry. Slab
stations are only endpoints of simplified grating/solid appearance boundaries;
the separate four-metre wrap seam rule is handled by the common atlas mapper.
"""
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Draw02/near-cells'
SOURCE = ROOT / 'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'

def main():
    measured = json.loads((OUT / 'appearance-domain-measurements.json').read_text())
    results = []
    candidates = []
    for level in measured:
        mesh = np.load(SOURCE / f"lod{level['lod']}-native.npz")
        p, uv, indices = [mesh['mesh0_' + k] for k in ('positions', 'uv', 'indices')]
        domains = []
        for number, domain in enumerate(level['domains']):
            ids = np.unique(indices[domain['faces']])
            sign = 1 if domain['bone'] == 1 else -1
            z = p[ids, 2].astype(float) * sign
            tex = uv[ids].astype(float)
            lower, upper = float(tex[:, 1].min()), float(tex[:, 1].max())
            period = 4.
            gradient = -(upper - lower) / period
            stages = []
            for label, a, b, repeat in [('centre', float(z.min()), 4., False),
                                       ('repeat', 4., 76., True),
                                       ('hinge', 76., float(z.max()), False)]:
                if b <= a:
                    continue
                selected = (z >= a - 1e-7) & (z <= b + 1e-7)
                if repeat:
                    slope, offset = gradient * sign, upper
                    continuous = z[selected] * gradient + offset
                    # At source seam vertices either chart endpoint is valid.
                    distance = (continuous - tex[selected, 1]) / (upper - lower)
                    error = np.abs(distance - np.round(distance)) * (upper - lower)
                else:
                    at_a = tex[np.abs(z - a) < 1e-7, 1]
                    at_b = tex[np.abs(z - b) < 1e-7, 1]
                    va = upper
                    vb = lower if label == 'centre' else float(at_b[0])
                    assert np.any(at_a == va) and np.any(at_b == vb)
                    slope = (vb - va) / (b - a) * sign
                    offset = va - slope * sign * a
                    actual = p[ids[selected], 2].astype(float) * slope + offset
                    # The adjacent repeated cell owns the opposite endpoint too.
                    error = np.minimum(abs(actual - tex[selected, 1]),
                                       abs(abs(actual - tex[selected, 1]) - (upper - lower)))
                assert np.max(error) < 1e-7, (level['lod'], number, label, error.max())
                stages.append(dict(kind=label, absoluteZ=[a, b], repeatV=repeat,
                                   v=[0., slope, offset], maximumVertexError=float(error.max())))
            domains.append(dict(index=number, bone=domain['bone'], family=domain['family'],
                transverseFeature=domain['transverseFeature'],
                u=domain['actualFloat32IntervalCheck']['coefficients'],
                sourceBounds=[domain['uvMinimum'], domain['uvMaximum']], stages=stages,
                halfIntervalCheck=domain['actualFloat32IntervalCheck']))
        slabs = []
        for bone in (1, 2):
            sign = 1 if bone == 1 else -1
            selected = [(i, d) for i, d in enumerate(level['domains']) if d['bone'] == bone]
            stations = sorted(set(v[2] * sign for _, d in selected
                                  for edge in d['simplifiedBoundaryEdges'] for v in edge))
            for z0, z1 in zip(stations, stations[1:]):
                mid = (z0 + z1) * .5
                intervals = []
                for number, domain in selected:
                    crossings = []
                    for edge in domain['simplifiedBoundaryEdges']:
                        a, b = np.array(edge[0]), np.array(edge[1])
                        az, bz = a[2] * sign, b[2] * sign
                        if not min(az, bz) < mid < max(az, bz):
                            continue
                        slope = (b[0] - a[0]) / (bz - az)
                        offset = a[0] - slope * az
                        crossings.append((slope * mid + offset, slope, offset))
                    crossings.sort()
                    assert len(crossings) % 2 == 0
                    for left, right in zip(crossings[::2], crossings[1::2]):
                        intervals.append(dict(domain=number, left=list(left[1:]), right=list(right[1:])))
                intervals.sort(key=lambda d: d['left'][0] * mid + d['left'][1])
                for left, right in zip(intervals, intervals[1:]):
                    assert left['right'][0] * mid + left['right'][1] <= right['left'][0] * mid + right['left'][1] + 1e-7
                slabs.append(dict(bone=bone, absoluteZ=[z0, z1], intervals=intervals))
        # All original face interiors must map to the same real feature after
        # reducing its boundary graph to coordinate slabs.
        witnesses = 0
        for number, domain in enumerate(level['domains']):
            sign = 1 if domain['bone'] == 1 else -1
            for face in domain['faces']:
                point = p[indices[face]].astype(float).mean(0)
                x, z = point[0], point[2] * sign
                matches = []
                for slab in slabs:
                    if slab['bone'] != domain['bone'] or not slab['absoluteZ'][0] < z < slab['absoluteZ'][1]:
                        continue
                    for interval in slab['intervals']:
                        left, right = interval['left'], interval['right']
                        if left[0] * z + left[1] < x < right[0] * z + right[1]:
                            matches.append(interval['domain'])
                assert matches == [number], (level['lod'], face, number, matches)
                candidates.append(dict(lod=level['lod'], domain=number, x=float(point[0]), z=float(point[2]),
                                       u=float(point[0] * domains[number]['u'][0] + domains[number]['u'][1])))
                witnesses += 1
        results.append(dict(lod=level['lod'], sourceSurface='8a28ab8d9526e258efbcb81c3bbc936b',
                            domains=domains, boundarySlabs=slabs, nativeFaceInteriorChecks=witnesses,
                            supportedType='Asphalt', finiteAxis='U', repeatAxis='V'))
        print(f"LOD{level['lod']}: {len(domains)} domains, {len(slabs)} boundary slabs, {witnesses} source face interiors matched")
    (OUT / 'native-atlas-wrap-plan.json').write_text(json.dumps(results, indent=2) + '\n')
    (OUT / 'moving-atlas-candidates.json').write_text(json.dumps(dict(points=candidates)) + '\n')

if __name__ == '__main__':
    main()
