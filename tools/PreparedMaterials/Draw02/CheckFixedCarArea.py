"""Analyze actual Float32 production output without relaxing the acceptance check."""
import json
from pathlib import Path
from collections import Counter
import numpy as np
import sys

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
suffix = sys.argv[1] if len(sys.argv) > 1 else 'counterexample'
lines = (OUT/f'candidate-area-{suffix}.txt').read_text().splitlines()
polys = [np.array([[float(v) for v in point.split(',')] for point in line.split(';')], np.float32).astype(float) for line in lines]
source, triangles = polys[0], polys[1:]
def cross(a, b): return float(a[0]*b[1]-a[1]*b[0])
def signed(poly): return sum(cross(a, b) for a, b in zip(poly, np.roll(poly, -1, axis=0)))/2
def clip(poly, triangle):
    sign = 1 if signed(triangle) > 0 else -1
    result = list(poly)
    for a, b in zip(triangle, np.roll(triangle, -1, axis=0)):
        original, result = result, []
        if not original: break
        for p, q in zip(original, original[1:]+original[:1]):
            dp, dq = sign*cross(b-a, p-a), sign*cross(b-a, q-a)
            if dp >= 0: result.append(p)
            if (dp < 0 < dq) or (dq < 0 < dp): result.append(p+(q-p)*(dp/(dp-dq)))
    return np.array(result)
flat = [p[:, [0, 2]] for p in polys]
areas = [abs(signed(p)) for p in flat]
intersection = sum(abs(signed(clip(p, flat[0]))) if len(clip(p, flat[0])) >= 3 else 0 for p in flat[1:])
overlap = 0
for i, a in enumerate(flat[1:]):
    for b in flat[i+2:]:
        intersection_poly = clip(a, b)
        if len(intersection_poly) >= 3: overlap += abs(signed(intersection_poly))
edges = Counter()
for tri in triangles:
    for a, b in zip(tri, np.roll(tri, -1, axis=0)):
        edges[tuple(sorted((tuple(a), tuple(b))))] += 1
boundary = sorted({point for edge, count in edges.items() if count == 1 for point in edge})
vertices = []
for point in boundary:
    p = np.array(point); best = None
    for a, b in zip(source, np.roll(source, -1, axis=0)):
        delta = b-a; t = np.clip(np.dot(p-a, delta)/np.dot(delta, delta), 0, 1)
        projected = a+t*delta; distance = float(np.linalg.norm(p-projected))
        lower = (p+np.nextafter(p.astype(np.float32), np.float32(-np.inf)).astype(float))/2
        upper = (p+np.nextafter(p.astype(np.float32), np.float32(np.inf)).astype(float))/2
        lower_t, upper_t = 0., 1.
        for axis in range(3):
            if delta[axis] == 0:
                if not lower[axis] <= a[axis] <= upper[axis]: lower_t = 2
            else:
                interval = sorted(((lower[axis]-a[axis])/delta[axis], (upper[axis]-a[axis])/delta[axis]))
                lower_t, upper_t = max(lower_t, interval[0]), min(upper_t, interval[1])
        ideal = a+(lower_t+upper_t)*.5*delta if lower_t <= upper_t else None
        same_bits = ideal is not None and np.array_equal(ideal.astype(np.float32), p.astype(np.float32))
        item = dict(position=list(point), sourceEdgeDistance=distance,
            sourceEdgePointRoundsToThisVertex=bool(same_bits),
            sourceEdgeIntersectsClosedRoundingCell=bool(lower_t <= upper_t),
            sourceEdgeParameterInterval=[float(lower_t), float(upper_t)],
            nearestSourcePoint=projected.tolist(), halfUlpExtent=np.maximum(p-lower, upper-p).tolist(),
            idealSourceEdgePoint=ideal.tolist() if ideal is not None else None)
        if any(np.array_equal(p, v) for v in source):
            item['idealSourceEdgePoint'] = p.tolist()
            item['halfUlpExtent'] = [0., 0., 0.]
            item['sourceEdgePointRoundsToThisVertex'] = True
        if best is None or distance < best['sourceEdgeDistance']: best = item
    vertices.append(best)
directed = {}
for tri in triangles:
    for a, b in zip(tri, np.roll(tri, -1, axis=0)):
        if edges[tuple(sorted((tuple(a), tuple(b))))] == 1:
            assert tuple(a) not in directed
            directed[tuple(a)] = tuple(b)
cycle = [next(iter(directed))]
while directed[cycle[-1]] != cycle[0]:
    cycle.append(directed[cycle[-1]])
    assert len(cycle) <= len(directed)
assert len(cycle) == len(directed)
lookup = {tuple(v['position']): v for v in vertices}
area_budget = None
if all(v['sourceEdgeIntersectsClosedRoundingCell'] for v in vertices):
    ideal = np.array([lookup[v]['idealSourceEdgePoint'] for v in cycle])[:, [0, 2]]
    radii = np.array([np.linalg.norm(np.array(lookup[v]['halfUlpExtent'])[[0, 2]]) for v in cycle])
    budget = .5*sum(radii[i]*np.linalg.norm(ideal[(i+1)%len(ideal)]-ideal[i-1]) for i in range(len(ideal)))
    budget += .5*sum(radii*np.roll(radii, -1))
    area_budget = dict(idealSourceEdgePolygonArea=abs(signed(ideal)),
        nativeFloat32BoundaryAreaBound=float(budget),
        measuredDifferenceWithinBound=abs(sum(areas[1:])-areas[0]) <= budget,
        allBoundaryPointsReencodeSameBits=all(v['sourceEdgePointRoundsToThisVertex'] for v in vertices),
        formula='0.5 sum(r_i * |q_next-q_previous| + r_i*r_next); q_i lies on original source edge within the CLOSED cell of p_i; r_i is radius of that Float32 cell. Original vertices have r_i=0.',
        scope='Closed-cell geometric bound only. Midpoint tie-to-even may prevent q_i from reencoding to p_i. This is NOT acceptance when strict same-bit rounding fails; no relative threshold is changed.')
report = dict(sourceVertices=source.tolist(), triangles=[p.tolist() for p in triangles],
    sourceEdgeLengths=[float(np.linalg.norm(a-b)) for a, b in zip(source, np.roll(source, -1, axis=0))],
    exactDoubleSourceArea=areas[0], exactDoubleOutputArea=sum(areas[1:]),
    absoluteAreaDifference=sum(areas[1:])-areas[0], relativeAreaDifference=(sum(areas[1:])-areas[0])/areas[0],
    outputPairOverlapArea=overlap, outsideSourceArea=sum(areas[1:])-intersection,
    uncoveredSourceArea=areas[0]-intersection, reversedTriangles=sum(signed(p)*signed(flat[0]) < 0 for p in flat[1:]),
    duplicateTriangles=len(triangles)-len({tuple(sorted(map(tuple, p))) for p in triangles}),
    maximumPlaneDistance=max(abs(float(v[1]-source[0, 1])) for p in triangles for v in p),
    boundaryVertices=vertices,
    roundingCellAreaBudget=area_budget,
    status='Diagnostic only. Existing acceptance failure is retained; no tolerance changed.')
(OUT/('candidate-area-analysis.json' if suffix == 'counterexample' else f'candidate-area-{suffix}-analysis.json')).write_text(json.dumps(report, indent=2, default=lambda v: v.item())+'\n')
print(json.dumps({k:v for k,v in report.items() if k not in ('triangles','boundaryVertices','sourceVertices')}, indent=2, default=lambda v: v.item()))
print('Boundary distance max:', max(v['sourceEdgeDistance'] for v in vertices),
      'vertices not one-round representable:', sum(not v['sourceEdgePointRoundsToThisVertex'] for v in vertices))
