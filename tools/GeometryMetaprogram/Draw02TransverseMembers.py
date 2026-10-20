"""Record Draw02 transverse braces, hinge axles and directly-under-road base strips.
Usage: python Draw02TransverseMembers.py RESEARCH_DIRECTORY OUTPUT_CS
Only LOD0/LOD1 retain these independent authored members. Far shell mapping is separate.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

research, output = map(Path, sys.argv[1:])
planes = json.loads((research / 'draw02-road-plane-candidates.json').read_text())
reference_spans = {}
tables = []
for lod in range(2):
    data = np.load(research / f'draw02-main-lod{lod}-uv.npz')
    v = data['positions'].astype(np.float32)
    triangles = data['indices'].reshape(-1, 3)
    removed = [face for p in planes[lod]['candidates'] if p['y'] in (9.800049, 10.0)
               for face in p['triangle_ordinals']]
    faces = np.delete(triangles, removed, axis=0)
    parent = list(range(len(v)))
    def root(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    def join(a, b):
        parent[root(int(a))] = root(int(b))
    for a, b, c in faces:
        join(a, b)
        join(a, c)
    coincident = {}
    for i, p in enumerate(v):
        key = tuple(p)
        if key in coincident:
            join(i, coincident[key])
        else:
            coincident[key] = i
    groups = {}
    for i in np.unique(faces):
        groups.setdefault(root(int(i)), []).append(int(i))
    members = {}
    for ids in groups.values():
        p = v[ids]
        lo, hi = p.min(0), p.max(0)
        axle = (len(ids) == (720 if lod == 0 else 296) and
                lo[0] == np.float32(-10.656005859375) and hi[0] == -lo[0] and
                lo[1] == np.float32(3.74884033203125) and hi[1] == np.float32(7.078857421875))
        transition = (len(ids) == 58 and lo[0] == -6 and hi[0] == 6 and
                      lo[1] == np.float32(9.3642578125) and hi[1] == np.float32(9.800048828125))
        bottom = (len(ids) == 40 and lo[0] == np.float32(-10.800048828125) and hi[0] == -lo[0] and
                  lo[1] == hi[1] == np.float32(.5500030517578125))
        linkage = (len(ids) == (212 if lod == 0 else 73) and lo[0] == np.float32(-.200000762939453125) and hi[0] == -lo[0] and
                   lo[1] == np.float32(2.07904052734375) and hi[1] == np.float32(4.20556640625))
        if not (axle or transition or bottom or linkage) and not (lo[0] < 0 < hi[0] and hi[2] - lo[2] < .51 and 9.5 < hi[1] < 9.75):
            continue
        station = round(float((lo[2] + hi[2]) / 2))
        if axle:
            assert abs(station) == 74
            assert np.allclose(sorted(abs(p[:,2]).tolist())[::len(p)-1], [72.314453125,75.64453125])
        elif transition:
            assert abs(station) == 79
            assert np.allclose([abs(p[:,2]).min(),abs(p[:,2]).max()], [78.7421875,80.01171875])
        elif bottom:
            assert abs(station) == 76
            assert np.allclose([abs(p[:,2]).min(),abs(p[:,2]).max()], [70.607421875,80.77734375])
        elif linkage:
            assert abs(station) == 74
            assert np.allclose([abs(p[:,2]).min(),abs(p[:,2]).max()],
                               [70.876953125,77.08203125 if lod == 0 else 77.060546875])
        else:
            assert abs(station) in range(4, 69, 8)
            assert abs(float((lo[2] + hi[2]) / 2) - station) < .001
        base = transition or not (axle or bottom or linkage) and hi[1] > 9.65
        role = 'linkage' if linkage else 'transition' if transition else 'bottom' if bottom else 'axle' if axle else 'base' if base else 'brace'
        key = (role, station)
        assert key not in members
        if lod == 0:
            assert lo[0] == -hi[0]
            reference_spans[key] = float(hi[0])
        # The middle LOD sometimes loses a 5 cm outer cap. Keep the near member's
        # measured span rather than normalizing a simplified remnant to another width.
        weights = np.sign(p[:, 0]) if base else p[:, 0] / reference_spans[key]
        members[key] = (ids, weights.astype(np.float32))
    assert len(members) == 44
    assert set(members) == ({(role, sign * station) for role in ('base', 'brace')
                            for sign in (-1, 1) for station in range(4, 69, 8)} |
                           {(role,sign*station) for role,station in (('axle',74),('transition',79),('bottom',76),('linkage',74))
                            for sign in (-1,1)})
    coefficients = {}
    seen = set()
    for key, (ids, weights) in members.items():
        assert not seen.intersection(ids)
        seen.update(ids)
        for i, weight in zip(ids, weights):
            coefficients.setdefault(float(weight), []).append(i)
        for delta in ((-.1, 0, 2, 8) if key[0] == 'linkage' else (-.5, 0, 2, 8)):
            moved = v[ids].copy()
            moved[:, 0] += weights * np.float32(delta)
            assert np.array_equal(moved[:, 1:], v[ids, 1:])
            assert np.isfinite(moved).all()
            if delta == 0:
                assert np.array_equal(moved, v[ids])
            if key[0] == 'linkage':
                assert np.allclose(moved[:,0],v[ids,0]*(1+delta/reference_spans[key]),atol=2e-6)
                assert reference_spans[key]+delta > 0
            if key[0] in ('base','transition'):
                for sign in (-1, 1):
                    mask = np.sign(v[ids, 0]) == sign
                    assert np.allclose(moved[mask, 0] - v[ids, 0][mask], sign * delta, atol=1e-6)
    name = 'DrawBridge02' + ('_LOD1' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((research / 'movable-skin' / name).read_bytes()).hexdigest()
    tables.append((len(v), coefficients, digest))
    print(f'LOD{lod}: 44 measured members including both central linkages; {len(seen)} source vertices; movement invariants passed')

lines = ['// DrawBridge02 measured transverse members, game 1.6.2f1.',
         '// Generated by tools/GeometryMetaprogram/Draw02TransverseMembers.py.',
         '// This partial mapping covers isolated braces/base strips, not the complete mechanism.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Draw02TransverseMembers', '{',
         '    // Native central linkage half-span; the mandatory own-span transform degenerates at this delta.',
         '    internal const float MinimumHalfWidthDelta = -0.200000762939453125f;', '',
         '    internal static void Apply(int lod, Vector3[] source, Vector3[] positions, float delta)',
         '    {', '        var expected = lod switch', '        {',
         '            0 => 44370, 1 => 31007,',
         '            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
         '        if (source.Length != expected || positions.Length != expected)',
         '            throw new InvalidOperationException("Draw02 transverse mapping requires its recorded main mesh.");',
         '        foreach (var group in lod == 0 ? Near : Middle)',
         '            foreach (var index in group.Vertices)',
         '                positions[index].x = source[index].x + group.Weight * delta;', '    }', '']
for (count, table, digest), name in zip(tables, ('Near', 'Middle')):
    lines += ['    // Geometry SHA-256: ' + digest,
              f'    private static readonly (float Weight, int[] Vertices)[] {name} =', '    {']
    for weight, ids in sorted(table.items()):
        lines += [f'        ({weight:.9g}f, new[]', '        {']
        for i in range(0, len(ids), 18):
            lines += ['            ' + ', '.join(map(str, sorted(ids)[i:i+18])) + ',']
        lines += ['        }),']
    lines += ['    };', '']
lines += ['}', '']
output.write_text('\n'.join(lines), encoding='utf-8')
