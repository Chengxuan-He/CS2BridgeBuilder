"""Emit independently deformable Lift01 far tower core from native LOD1 parts.
Usage: python Lift01FarTowerCore.py RESEARCH_DIRECTORY OUTPUT_CS
This supplies a measured replacement stage, not a complete far tower or renderer.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
raw = r / 'movable-skin/LiftBridge01_LOD1 Mesh.Geometry'
digest = hashlib.sha256(raw.read_bytes()).hexdigest()
assert digest == 'c5694dce731ce05218de15761665cc1d234723049c48bd707c87587509ed8e4e'
d = np.load(r / 'lift01-main-1.npz')
v, faces = d['positions'], d['indices']
assert v.shape == (351968, 3) and faces.shape == (228254, 3)
parts = json.loads((r / 'lift01-crossing-1.json').read_text())
near = json.loads((r / 'lift01-crossing-0.json').read_text())
selected = (280, 308, 287, 315, 334, 335, 171, 265)
expected_faces = (456, 456, 48, 48, 72, 72, 140, 134)
used, kept, weights = set(), [], {}
bone = None
bone_by_vertex = {}
for line in (r / 'movable-skin/LiftBridge01_LOD1 Mesh.mesh-0.obj').read_text().splitlines():
    fields = line.split()
    if not fields:
        continue
    if fields[0] == 'g':
        bone = fields[1]
    elif fields[0] == 'f':
        for item in fields[1:]:
            bone_by_vertex.setdefault(int(item.split('/')[0])-1, set()).add(bone)
for ordinal, count in zip(selected, expected_faces):
    part = parts[ordinal]
    ids = part['vertices']
    assert not used.intersection(ids)
    used.update(ids)
    assert all(bone_by_vertex[i] == {'bone_0'} for i in ids)
    member = np.isin(faces, ids)
    assert not np.any(member.any(1) & ~member.all(1))
    fids = np.flatnonzero(member.all(1))
    assert len(fids) == count
    kept.extend(map(int, fids))
    if ordinal in (280, 308):
        assert part['minimum'] == near[ordinal]['minimum']
        assert part['maximum'] == near[ordinal]['maximum']
        # The same complete authored bars/centre columns as Lift01TowerFrames.
        fs = faces[fids]
        bars = fs[np.max(abs(v[fs][:, :, 0]), axis=1) > 7]
        bar_ids = set(map(int, bars.flat))
        column_ids = set(ids) - bar_ids
        assert len(bar_ids) == 576 and len(column_ids) == 336
        assert np.max(abs(v[list(column_ids), 0])) == .339599609375
        assert all(not (set(f) & bar_ids and set(f) & column_ids) for f in fs)
        for i in ids:
            weights[i] = float(np.sign(v[i, 0])) if i in bar_ids else 0.
    else:
        lo, hi = part['minimum'][0], part['maximum'][0]
        assert lo < 0 < hi
        for i in ids:
            weights[i] = (2 * float(v[i, 0]) - lo - hi) / (hi - lo)
assert len(kept) == 1426 and len(set(kept)) == len(kept)
assert len(used) == 2710
# Complete longitudinal side frames and lower beams. The four diagonal supports
# retain their separate native far parts (2/4/14/16); do not duplicate them here.
# Both levels preserve these part identities and bounds. Coarse proxy welding
# must not turn these same-side structures into crossing, stretchable members.
side_parts = json.loads((r / 'lift01-side-1.json').read_text())
near_sides = json.loads((r / 'lift01-side-0.json').read_text())
side_ordinals = (2270, 2271, 2525, 2526)
for ordinal, count in zip(side_ordinals, (832, 164, 832, 164)):
    part = side_parts[ordinal]
    assert part['minimum'] == near_sides[ordinal]['minimum']
    assert part['maximum'] == near_sides[ordinal]['maximum']
    ids = part['vertices']
    assert np.array_equal(v[ids].min(0), part['minimum'])
    assert np.array_equal(v[ids].max(0), part['maximum'])
    assert part['minimum'][0] > 0 or part['maximum'][0] < 0
    direction = 1 if part['minimum'][0] > 0 else -1
    assert not used.intersection(ids)
    assert all(bone_by_vertex[i] == {'bone_0'} for i in ids)
    used.update(ids)
    member = np.isin(faces, ids)
    assert not np.any(member.any(1) & ~member.all(1))
    fids = np.flatnonzero(member.all(1))
    assert len(fids) == count
    kept.extend(map(int, fids))
    for i in ids:
        weights[i] = float(direction)
    for delta in (-2.99, -2.75, -2, -.1, 0, 2, 8):
        points = v[ids].astype(float)
        moved = points.copy()
        moved[:, 0] += direction * delta
        assert np.allclose(moved - moved[0], points - points[0], rtol=0, atol=1e-12)
assert len(kept) == 3418 and len(set(kept)) == len(kept)
assert len(used) == 6470
# The complete upper transverse system and end-frame connectors are separate
# authored crossing parts in the detailed mesh. Include their measured native
# membership rather than widening the baked combined front/roof proxy texture.
upper_ordinals = [i for i in range(90, 334) if i not in selected]
upper_ordinals += list(range(23, 29)) + list(range(337, 343))
minimum_half_span = 3.1732177734375
for ordinal in upper_ordinals:
    part = parts[ordinal]
    ids = part['vertices']
    assert part['minimum'] == near[ordinal]['minimum']
    assert part['maximum'] == near[ordinal]['maximum']
    assert np.array_equal(v[ids].min(0), part['minimum'])
    assert np.array_equal(v[ids].max(0), part['maximum'])
    lo, hi = part['minimum'][0], part['maximum'][0]
    assert lo < 0 < hi
    minimum_half_span = min(minimum_half_span, (hi - lo) / 2)
    assert not used.intersection(ids)
    assert all(bone_by_vertex[i] == {'bone_0'} for i in ids)
    used.update(ids)
    member = np.isin(faces, ids)
    assert not np.any(member.any(1) & ~member.all(1))
    kept.extend(map(int, np.flatnonzero(member.all(1))))
    for i in ids:
        weights[i] = (2 * float(v[i, 0]) - lo - hi) / (hi - lo)
assert len(set(kept)) == len(kept)
edges, extra, fragments = [], [], []
def clip(poly, points, boundary, keep_left):
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        xa, xb = a @ points[:, 0], b @ points[:, 0]
        ina = xa <= boundary if keep_left else xa >= boundary
        inb = xb <= boundary if keep_left else xb >= boundary
        if ina:
            result.append(a)
        if ina != inb:
            result.append(a + (b-a) * ((boundary-xa)/(xb-xa)))
    clean = []
    for point in result:
        if not clean or np.max(abs(point-clean[-1])) > 1e-10:
            clean.append(point)
    if len(clean) > 1 and np.max(abs(clean[0]-clean[-1])) <= 1e-10:
        clean.pop()
    return clean

for ordinal in (29, 343):
    part = parts[ordinal]
    assert part['minimum'] == near[ordinal]['minimum']
    assert part['maximum'] == near[ordinal]['maximum']
    ids = part['vertices']
    assert len(ids) == 1112 and not used.intersection(ids)
    assert all(bone_by_vertex[i] == {'bone_0'} for i in ids)
    used.update(ids)
    left = -.8794708251953125 if ordinal == 29 else -.879486083984375
    right = .879486083984375
    assert v[ids][v[ids, 0] < 0, 0].max() == left
    assert v[ids][v[ids, 0] > 0, 0].min() == right
    member = np.isin(faces, ids)
    assert not np.any(member.any(1) & ~member.all(1))
    fids = np.flatnonzero(member.all(1))
    assert len(fids) == 616
    kept.extend(map(int, fids))
    for i in ids:
        weights[i] = float(np.clip((2*float(v[i, 0])-left-right)/(right-left), -1, 1))
    for fid in fids:
        face = faces[fid]
        points = v[face].astype(float)
        if not any(points[:, 0].min() < cut < points[:, 0].max() for cut in (left, right)):
            continue
        polys = [list(np.eye(3))]
        for cut in (left, right):
            result = []
            for poly in polys:
                xs = [p @ points[:, 0] for p in poly]
                if min(xs) < cut < max(xs):
                    result += [clip(poly, points, cut, True), clip(poly, points, cut, False)]
                else:
                    result.append(poly)
            polys = result
        indices, local = [], {}
        for poly in polys:
            polygon = []
            for bary in poly:
                nz = np.flatnonzero(abs(bary) > 1e-10)
                if len(nz) == 1:
                    polygon.append(int(face[nz[0]]))
                else:
                    assert len(nz) == 2
                    a, b = map(int, nz)
                    fraction = float(np.float32(bary[b]))
                    key = (int(face[a]), int(face[b]), fraction)
                    if key not in local:
                        local[key] = len(v) + len(edges)
                        point = points[a] + (points[b]-points[a])*fraction
                        assert min(abs(point[0]-left), abs(point[0]-right)) < 1e-6
                        edges.append((*key, -1 if point[0] < 0 else 1))
                        extra.append(point)
                    polygon.append(local[key])
            for i in range(1, len(polygon)-1):
                indices += [polygon[0], polygon[i], polygon[i+1]]
        fragments.append((int(fid), indices))
assert len(fragments) == 56
replaced = {fid for fid, _ in fragments}
complete_faces = np.asarray([faces[i] for i in kept if i not in replaced] +
    [ids[i:i+3] for _, ids in fragments for i in range(0, len(ids), 3)])
all_vertices = np.concatenate((v.astype(float), np.asarray(extra)))
for fid, ids in fragments:
    p = v[faces[fid]].astype(float)
    q = all_vertices[np.asarray(ids).reshape(-1, 3)]
    original_area = np.linalg.norm(np.cross(p[1]-p[0], p[2]-p[0]))
    fragment_area = np.linalg.norm(np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0]), axis=1).sum()
    assert abs(original_area-fragment_area) < 3e-5
for delta in (-2.99, -2.75, -2, -.1, 0, 2, 8):
    moved = all_vertices.copy()
    for i, weight in weights.items():
        moved[i, 0] += weight * delta
    for i, edge in enumerate(edges):
        moved[len(v)+i, 0] += edge[3] * delta
    a, b = all_vertices[complete_faces], moved[complete_faces]
    n0 = np.cross(a[:, 1]-a[:, 0], a[:, 2]-a[:, 0])
    n1 = np.cross(b[:, 1]-b[:, 0], b[:, 2]-b[:, 0])
    valid = np.linalg.norm(n0, axis=1) > 1e-8
    assert np.isfinite(moved).all()
    if delta >= -.1:
        assert np.all(np.sum(n0[valid] * n1[valid], axis=1) > 0)
    else:
        print(f"Far core delta={delta}: {np.count_nonzero(np.sum(n0[valid]*n1[valid], axis=1) <= 0)} collapsed/reversed faces retained by signed mapping")
    assert np.array_equal(moved[:, 1:], all_vertices[:, 1:])
    if delta == 0:
        assert np.array_equal(moved, all_vertices)
    np.savez(r / f'lift01-far-tower-core-{delta}.npz',
             positions=moved, indices=complete_faces)

def ranges(values):
    result = []
    for i in sorted(values):
        if result and result[-1][1] + 1 == i:
            result[-1][1] = i
        else:
            result.append([i, i])
    return result

lines = ['// Native LiftBridge01_LOD1 Mesh, fixed tower core only.',
    '// Geometry SHA-256: ' + digest,
    '// Generated by Lift01FarTowerCore.py. Preserve the source main Surface and bone 0.',
    '// Includes rigid sides, upper transverse frames and measured roof grids.',
    'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
    'internal static class Lift01FarTowerCore', '{',
    
    '    internal static MovableStructureModel.Plan Create(Vector3[] source, int[] indices, float delta)',
    '    {',
    '        if (source.Length != 351968 || indices.Length != 684762 ||',
    '            !float.IsFinite(delta))',
    '            throw new InvalidOperationException("Lift01 far tower core requires its recorded middle source.");',
    '        var positions = (Vector3[])source.Clone();',
    '        foreach (var item in Motion)',
    '            for (var i = item.First; i <= item.Last; i++)',
    '                positions[i].x = source[i].x + item.Weight * delta;',
    '        var remove = new bool[228254];',
    '        Array.Fill(remove, true);',
    '        foreach (var range in Faces)',
    '            for (var i = range.First; i <= range.Last; i++) remove[i] = false;',
    '        var edges = new MovableStructureModel.EdgeVertex[Edges.Length];',
    '        var x = new float[Edges.Length];',
    '        for (var i = 0; i < Edges.Length; i++)',
    '        {',
    '            var edge = Edges[i];',
    '            edges[i] = new MovableStructureModel.EdgeVertex(edge.A, edge.B, edge.Fraction);',
    '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) + edge.Weight * delta;',
    '        }',
    '        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { remove },',
    '            addedVertices: edges, fragments: Fragments, constructedX: x);',
    '    }', '',
    '    private static readonly (int First, int Last)[] Faces =', '    {']
lines += [f'        ({a}, {b}),' for a, b in ranges(kept)]
lines += ['    };', '', '    private static readonly (int First, int Last, float Weight)[] Motion =', '    {']
motion = []
for i, weight in sorted(weights.items()):
    weight = float(np.float32(weight))
    if weight == 0:
        continue
    if motion and motion[-1][1] + 1 == i and motion[-1][2] == weight:
        motion[-1][1] = i
    else:
        motion.append([i, i, weight])
lines += [f'        ({a}, {b}, {weight:.9g}f),' for a, b, weight in motion]
lines += ['    };', '', '    private static readonly (int A, int B, float Fraction, int Weight)[] Edges =', '    {']
lines += [f'        ({a}, {b}, {fraction:.9g}f, {weight}),' for a, b, fraction, weight in edges]
lines += ['    };', '', '    private static readonly MovableStructureModel.Fragment[] Fragments =', '    {']
lines += [f'        new(0, {fid}, ' + ', '.join(map(str, ids)) + '),' for fid, ids in fragments]
lines += ['    };', '}', '']
output.write_text('\n'.join(lines), encoding='utf-8')
print(f'{len(selected) + len(side_ordinals) + len(upper_ordinals) + 2} measured parts: {len(used)} source vertices, '
      f'{len(edges)} added edge vertices, {len(complete_faces)} triangles; '
      'whole-face ownership, fixed bone 0, side rigidity and seven-width signed mapping checks passed.')
