"""Minimal read-only replay of the candidate-09 geometry counterexample.

The double-carry alternative is a diagnostic, not a production implementation.
Attributes, shared API behavior and other bridge families require their own tests.
"""
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
source = np.array([[float(v) for v in p.split(',')] for p in (OUT/'candidate-area-counterexample.txt').read_text().splitlines()[0].split(';')], np.float32).astype(float)
nodes = {}
for row in (OUT/'candidate-area-partition.csv').read_text().splitlines():
    row = row.split(','); index, negative, positive, bone = map(int, row[:4])
    matrix = np.array([float(v) for v in row[4:]], np.float32).astype(float).reshape(4, 4)
    nodes[index] = (negative, positive, bone, matrix)

def run(round_each):
    output = []
    def split(poly, index):
        if len(poly) < 3: return
        negative, positive, bone, frame = nodes[index]
        if bone >= 0:
            for i in range(1, len(poly)-1):
                tri = np.array([poly[0], poly[i], poly[i+1]], np.float32)
                if np.dot(np.cross(tri[1]-tri[0], tri[2]-tri[0]), np.cross(tri[1]-tri[0], tri[2]-tri[0])) != 0:
                    output.append(tri)
            return
        distances, errors = [], []
        u = 1/16777216; gamma6 = 6*u/(1-6*u)
        for p in poly:
            x, y, z = frame[2, :3]*p; w = frame[2, 3]
            distances.append(x+y+z+w)
            errors.append(gamma6*(abs(x)+abs(y)+abs(z)+abs(w)))
        if any(abs(d) > e for d, e in zip(distances, errors)):
            distances = [0 if abs(d) <= e else d for d, e in zip(distances, errors)]
        below, above = [], []
        has_negative, has_positive = False, False
        for i, a in enumerate(poly):
            b = poly[(i+1)%len(poly)]; da, db = distances[i], distances[(i+1)%len(poly)]
            if da <= 0: below.append(a)
            if da >= 0: above.append(a)
            has_negative |= da < 0; has_positive |= da > 0
            if da < 0 < db or db < 0 < da:
                start, end, ds, de = (a, b, da, db) if tuple(a) < tuple(b) else (b, a, db, da)
                t = ds/(ds-de); point = start+(end-start)*t
                if round_each: point = point.astype(np.float32).astype(float)
                below.append(point); above.append(point)
        if not has_negative and not has_positive: split(poly, positive)
        else:
            if has_negative: split(below, negative)
            if has_positive: split(above, positive)
    split(list(source), 0)
    return output

def signature(tris): return sorted(tuple(sorted(tuple(v.view(np.uint32)) for v in tri)) for tri in tris)
expected = [np.array([[float(v) for v in p.split(',')] for p in line.split(';')], np.float32) for line in (OUT/'candidate-area-counterexample.txt').read_text().splitlines()[1:]]
current, double = run(True), run(False)
assert signature(current) == signature(expected), 'Read-only replay does not match native production counterexample.'
def line(poly): return ';'.join(','.join(format(float(v), '.9g') for v in p) for p in poly)
(OUT/'candidate-area-double-carry.txt').write_text(line(source)+'\n'+'\n'.join(map(line, double))+'\n')
(OUT/'clip-replay.json').write_text(json.dumps(dict(currentReplayBitIdentical=True,
    currentTriangles=len(current), doubleCarryTriangles=len(double),
    change='Keep double interpolation positions through recursive clipping; cast to native Float32 only at final leaf.',
    productionChanged=False, attributesValidated=False), indent=2)+'\n')
print('PASS current replay matches all native output position bits; double-carry diagnostic exported separately.')
