"""Emit native Lift01 far platform subdivision at its measured tangent boundaries.
Usage: python Lift01FarJoinedParts.py RESEARCH_DIRECTORY OUTPUT_CS
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

research, output = map(Path, sys.argv[1:])
data = np.load(research / 'lift01-main-2.npz')
v, faces = data['positions'], data['indices']
assert v.shape == (1110, 3) and faces.shape == (776, 3)
groups = json.loads((research / 'lift01-far-fixed-parts.json').read_text())
platform_vertices = [i for k in (1, 13) for i in groups[k]['vertices']]
assert len(platform_vertices) == len(set(platform_vertices)) == 118
left, right = -9.111083984375, 9.111083984375
for k in (1,13):
    points = v[groups[k]['vertices']]
    assert left in points[:,0] and right in points[:,0]
    assert np.max(abs(points[:,0])) == 16.85595703125
membership = np.isin(faces, platform_vertices)
assert not np.any(membership.any(1) & ~membership.all(1))
platform_faces = set(np.flatnonzero(membership.all(1)))
unrelated = np.array(sorted(set(range(len(v))) - set(platform_vertices)))
name = 'LiftBridge01_LOD2 Mesh.Geometry'
digest = hashlib.sha256((research / 'movable-skin' / name).read_bytes()).hexdigest()
edges, fragments, appended = [], [], []

def clip(poly, points, boundary, keep_left):
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        xa, xb = float(a @ points[:, 0]), float(b @ points[:, 0])
        ina = xa <= boundary if keep_left else xa >= boundary
        inb = xb <= boundary if keep_left else xb >= boundary
        if ina:
            result.append(a)
        if ina != inb:
            result.append(a + (b - a) * ((boundary - xa) / (xb - xa)))
    return result

def index_of(bary, face, points, local):
    nonzero = np.flatnonzero(abs(bary) > 1e-10)
    if len(nonzero) == 1:
        return int(face[nonzero[0]])
    assert len(nonzero) == 2, bary
    a, b = map(int, nonzero)
    fraction = np.float32(bary[b])
    key = (int(face[a]), int(face[b]), float(fraction))
    if key not in local:
        local[key] = len(v) + len(edges)
        edges.append(key)
        appended.append(points[a] + (points[b] - points[a]) * float(fraction))
    return local[key]

for ordinal, face in enumerate(faces):
    if ordinal not in platform_faces:
        continue
    points = v[face].astype(float)
    lo, hi = points[:, 0].min(), points[:, 0].max()
    if not any(lo < cut < hi for cut in (left, right)):
        continue
    polys = [[row for row in np.eye(3)]]
    for cut in (left, right):
        split = []
        for poly in polys:
            xs = [p @ points[:, 0] for p in poly]
            if min(xs) < cut < max(xs):
                split.extend([clip(poly, points, cut, True), clip(poly, points, cut, False)])
            else:
                split.append(poly)
        polys = split
    indices, local = [], {}
    for poly in polys:
        ids = [index_of(p, face, points, local) for p in poly]
        for i in range(1, len(ids) - 1):
            indices.extend((ids[0], ids[i], ids[i + 1]))
    fragments.append((ordinal, indices))

all_v = np.concatenate((v.astype(float), np.array(appended)))
weights = np.zeros(len(all_v), dtype=np.float32)
weights[platform_vertices] = np.clip(all_v[platform_vertices, 0] / right, -1, 1)
# Roundoff in edge interpolation must not introduce a column-width change.
for i in range(len(v), len(all_v)):
    nearest = min((left, right), key=lambda x: abs(x - all_v[i, 0]))
    assert abs(nearest - all_v[i, 0]) < 1e-6
    weights[i] = -1 if nearest == left else 1
replacement = dict(fragments)
all_faces = np.array([tri for i, face in enumerate(faces)
                     for tri in np.array(replacement.get(i, face)).reshape(-1, 3)])
for ordinal, indices in fragments:
    p = v[faces[ordinal]].astype(float)
    original_area = np.linalg.norm(np.cross(p[1] - p[0], p[2] - p[0]))
    q = all_v[np.array(indices).reshape(-1, 3)]
    area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
    assert abs(area - original_area) < 2e-5
for delta in (-2.99, -2.75, -2., -.5, 0., 2., 8.):
    moved = all_v.copy()
    moved[:, 0] += weights.astype(float) * delta
    assert np.array_equal(moved[unrelated], all_v[unrelated])
    for k in (1, 13):
        ids = groups[k]['vertices']
        assert abs(np.ptp(moved[ids, 0]) - np.ptp(v[ids, 0]) - 2 * delta) < 1e-8
    assert np.array_equal(moved[:, 1:], all_v[:, 1:])
    for side, mask in ((-1, weights == -1), (1, weights == 1)):
        assert np.all(weights[mask] == side)
        assert np.allclose(moved[mask] - all_v[mask], [side * delta, 0, 0])
    p, q = all_v[all_faces], moved[all_faces]
    before = np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0])
    after = np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0])
    valid = np.linalg.norm(before, axis=1) > 1e-8
    assert np.all(np.sum(before[valid] * after[valid], axis=1) > 0), delta

def literal(x):
    return format(float(np.float32(x)), '.9g') + 'f'

lines = ['// Native LiftBridge01_LOD2 Mesh, game 1.6.2f1.',
         '// Geometry SHA-256: ' + digest,
         '// Generated by Lift01FarJoinedParts.py. Rounded platform tangents correspond to measured detailed platform rings.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Lift01FarJoinedParts', '{',
         '    internal static MovableStructureModel.Plan Complete(Vector3[] source, Vector3[] positions, int[] indices, float delta)',
         '    {',
         '        if (!float.IsFinite(delta) || source.Length != 1110 || positions.Length != 1110 || indices.Length != 2328)',
         '            throw new InvalidOperationException("Lift01 far platforms require their recorded shell layout.");',
         '        for (var i = 0; i < positions.Length; i++) positions[i].x += Weights[i] * delta;',
         '        var x = new float[Edges.Length];',
         '        for (var i = 0; i < x.Length; i++)',
         '        {', '            var edge = Edges[i];',
         '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction)',
         '                + Weights[source.Length + i] * delta;', '        }',
         '        return new MovableStructureModel.Plan(source, positions, new[] { indices },',
         '            new[] { new bool[776] }, addedVertices: Edges, fragments: Fragments, constructedX: x);',
         '    }', '', '    private static readonly float[] Weights =', '    {']
for start in range(0, len(weights), 12):
    lines.append('        ' + ', '.join(map(literal, weights[start:start + 12])) + ',')
lines += ['    };', '', '    private static readonly MovableStructureModel.EdgeVertex[] Edges =', '    {']
lines += [f'        new({a}, {b}, {literal(t)}),' for a, b, t in edges]
lines += ['    };', '', '    private static readonly MovableStructureModel.Fragment[] Fragments =', '    {']
lines += ['        new(0, ' + str(i) + ', ' + ', '.join(map(str, ids)) + '),' for i, ids in fragments]
lines += ['    };', '}', '']
output.write_text('\n'.join(lines), encoding='utf-8')
np.savez(research / 'lift01-far-platform-widening.npz', positions=all_v,
         indices=all_faces, weights=weights)
print(f'Far shell: {len(fragments)} subdivided faces, {len(edges)} edge vertices; '
      f'{len(all_faces)} triangles. Area, winding, rigid rounded ends and seven-width checks passed.')
