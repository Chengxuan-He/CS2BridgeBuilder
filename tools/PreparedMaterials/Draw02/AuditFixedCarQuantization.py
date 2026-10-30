"""Exact-rational Float32 geometry audit of every frozen fixed-car test output.

No measured error is used to choose a threshold. Midpoint inclusion follows
round-to-nearest, ties-to-even. Negative controls exercise drift and topology.
"""
from collections import Counter
from fractions import Fraction as Q
from pathlib import Path
import json
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'

def point(values): return tuple(Q(float(np.float32(v))) for v in values)
def parse(path): return [[point(p.split(',')) for p in line.split(';')] for line in path.read_text().splitlines()]
def cross(a, b): return a[0]*b[1]-a[1]*b[0]
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def area(poly): return sum(cross(a, b) for a, b in zip(poly, poly[1:]+poly[:1]))/2
def clip(poly, triangle):
    sign = 1 if area(triangle) > 0 else -1
    for a, b in zip(triangle, triangle[1:]+triangle[:1]):
        old, poly = poly, []
        for p, q in zip(old, old[1:]+old[:1]):
            dp, dq = sign*cross(sub(b, a), sub(p, a)), sign*cross(sub(b, a), sub(q, a))
            if dp >= 0: poly.append(p)
            if dp < 0 < dq or dq < 0 < dp:
                t = dp/(dp-dq); poly.append(tuple(x+t*(y-x) for x, y in zip(p, q)))
    return poly

def cell(p):
    result = []
    for value in p:
        f = np.float32(float(value)); bits = struct.unpack('<I', struct.pack('<f', f))[0]
        low = (value+Q(float(np.nextafter(f, np.float32(-np.inf)))))/2
        high = (value+Q(float(np.nextafter(f, np.float32(np.inf)))))/2
        result.append((low, high, bits % 2 == 0))
    return result

def edge_reference(p, source):
    bounds = cell(p)
    if p in source: return p, (Q(0), Q(0)), dict(original=True)
    for edge, (a, b) in enumerate(zip(source, source[1:]+source[:1])):
        lower, upper, lower_closed, upper_closed = Q(0), Q(1), True, True
        valid = True
        for axis, (lo, hi, closed) in enumerate(bounds):
            d = b[axis]-a[axis]
            if not d:
                valid &= lo < a[axis] < hi or closed and (a[axis] == lo or a[axis] == hi)
                continue
            left, right = sorted(((lo-a[axis])/d, (hi-a[axis])/d))
            if left > lower: lower, lower_closed = left, closed
            elif left == lower: lower_closed &= closed
            if right < upper: upper, upper_closed = right, closed
            elif right == upper: upper_closed &= closed
        valid &= lower < upper or lower == upper and lower_closed and upper_closed
        if not valid: continue
        t = (lower+upper)/2
        q = tuple(x+t*(y-x) for x, y in zip(a, b))
        # Independent exact round-to-even membership proof, including midpoint ties.
        for value, (lo, hi, closed) in zip(q, bounds):
            assert lo < value < hi or closed and (value == lo or value == hi)
        eps = tuple(max(p[i]-bounds[i][0], bounds[i][1]-p[i]) for i in (0, 2))
        return q, eps, dict(sourceEdge=edge, exactT=str(t), exactReference=list(map(str, q)))
    raise AssertionError('Boundary vertex has no exact source-edge point rounding to its Float32 bits: '+str(tuple(map(float, p))))

def audit(polys):
    source, triangles = polys[0], polys[1:]
    assert triangles and all(v[1] == source[0][1] for tri in triangles for v in tri), 'Off-plane output'
    flat = [[(v[0], v[2]) for v in tri] for tri in polys]
    source_area = area(flat[0]); assert source_area != 0
    assert all(area(tri)*source_area > 0 for tri in flat[1:]), 'Reversed or degenerate triangle'
    assert len({tuple(sorted(tri)) for tri in triangles}) == len(triangles), 'Duplicate triangle'
    for i, tri in enumerate(flat[1:]):
        for other in flat[i+2:]:
            assert area(clip(tri, other)) == 0, 'Overlapping triangles'
    edges, directed_edges = Counter(), Counter()
    for tri in triangles:
        for a, b in zip(tri, tri[1:]+tri[:1]):
            edges[tuple(sorted((a, b)))] += 1; directed_edges[(a, b)] += 1
    assert all(count <= 2 for count in edges.values()), 'Non-manifold edge'
    for (a, b), count in edges.items():
        if count == 2: assert directed_edges[(a, b)] == directed_edges[(b, a)] == 1
    next_vertex = {}
    for (a, b), count in directed_edges.items():
        if edges[tuple(sorted((a, b)))] == 1:
            assert count == 1 and a not in next_vertex, 'Branched boundary'
            next_vertex[a] = b
    cycle = [next(iter(next_vertex))]
    while next_vertex[cycle[-1]] != cycle[0]:
        cycle.append(next_vertex[cycle[-1]])
        assert len(cycle) <= len(next_vertex), 'Open or multiple boundary'
    assert len(cycle) == len(next_vertex), 'Hole or disconnected output'
    assert all(v in cycle for v in source), 'Original source corner missing'
    references = [edge_reference(p, source) for p in cycle]
    ideal = [(q[0], q[2]) for q, _, _ in references]
    assert area(ideal) == source_area, 'Reference boundary does not exactly cover original triangle'
    budget = Q(0)
    for i, (_, (ex, ez), _) in enumerate(references):
        previous, following = ideal[i-1], ideal[(i+1)%len(ideal)]
        nx, nz = references[(i+1)%len(ideal)][1]
        budget += (ex*abs(following[1]-previous[1])+ez*abs(following[0]-previous[0])+ex*nz+ez*nx)/2
    output_area = sum(area(t) for t in flat[1:]); error = abs(output_area-source_area)
    assert error <= budget, 'Area exceeds independent rounding-cell propagation bound'
    return dict(triangles=len(triangles), originalRelativeError=float(error/abs(source_area)),
        absoluteAreaError=float(error), independentAreaBound=float(budget),
        exactAreaError=str(error), exactAreaBound=str(budget),
        strictRoundToEven=True, exactTopology=True, boundaryProof=[proof for _, _, proof in references])

def main():
    folder = Path(sys.argv[1]); records = []; failures = []
    paths = sorted(folder.glob('case-*.txt'))
    expected = int(sys.argv[2]) if len(sys.argv) > 2 else 96
    assert expected in (96, 192) and len(paths) == expected, 'Incomplete frozen case matrix'
    for path in paths:
        try: records.append(dict(case=path.name, **audit(parse(path))))
        except AssertionError as e: failures.append(dict(case=path.name, error=str(e)))
    base = parse(paths[0]); negative = {}
    for label in ('displacement', 'hole', 'reverse', 'duplicate'):
        bad = [[tuple(v) for v in tri] for tri in base]
        if label == 'displacement':
            victim = next(v for tri in bad[1:] for v in tri if v not in bad[0])
            changed = point([float(victim[0])+.001, float(victim[1]), float(victim[2])+.001])
            bad = [bad[0]]+[[changed if v == victim else v for v in tri] for tri in bad[1:]]
        elif label == 'hole':
            remove = next(i for i, tri in enumerate(bad[1:], 1) if all(v not in bad[0] for v in tri))
            bad.pop(remove)
        elif label == 'reverse': bad[1] = list(reversed(bad[1]))
        else: bad.append(bad[1])
        try: audit(bad); negative[label] = 'INCORRECTLY ACCEPTED'
        except (AssertionError, KeyError, StopIteration) as e: negative[label] = 'rejected: '+str(e)
    assert all(v.startswith('rejected:') for v in negative.values())
    try:
        audit(parse(OUT/'candidate-area-counterexample.txt'))
        negative['candidate09RepeatedRounding'] = 'INCORRECTLY ACCEPTED'
    except AssertionError as e:
        negative['candidate09RepeatedRounding'] = 'rejected: '+str(e)
    assert negative['candidate09RepeatedRounding'].startswith('rejected:')
    report = dict(candidate=folder.name, passed=len(records), failed=len(failures),
        oldRelativeThresholdExceedances=sum(r['originalRelativeError'] > 1e-4 for r in records),
        arithmetic='Exact rational source/output coordinates, midpoint parity and topology; no epsilon.',
        areaBound='0.5 sum(ex_i*abs(qNext.z-qPrev.z)+ez_i*abs(qNext.x-qPrev.x)+ex_i*ezNext+ez_i*exNext), all exact rational.',
        cases=records, failures=failures, negativeControls=negative)
    (folder/'quantization-audit.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k != 'cases'}, indent=2))
    if failures: raise SystemExit(1)

if __name__ == '__main__': main()
