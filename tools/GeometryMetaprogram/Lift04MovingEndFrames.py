"""Emit measured subdivisions separating Lift04 moving end frames from side posts.
Usage: python Lift04MovingEndFrames.py RESEARCH_DIRECTORY OUTPUT_CS
The original inner upright surface separates rigid sides from crossing beams.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
tables = []
records=json.loads((r/'lift04-main-parts.json').read_text())
expected=('869158c6d06c9a9717851460adf922838d45c294c4b1e692b7b96e4585422980','4d57e1fe16365bf8702d310191fefbbbbbece9fcafb413b980052640e26d1b6f','ab81a0e0cdd6345fd8e80cdbad7c52b947fc7281aca1c1c37f986af0d49e863d')
for lod, selected_parts in enumerate(((21,1004),(21,933),(12,84))):
    record=next(x for x in records if x['lod']==lod and x['mesh']==0)
    assert hashlib.sha256((r/'movable-skin'/(record['source']+'.Geometry')).read_bytes()).hexdigest()==expected[lod]
    d=np.load(r/f'lift04-main-{lod}-0.npz');v,faces,c=d['positions'],d['indices'],d['component']
    bones=[];bone=None
    for line in (r/'movable-skin'/(record['source']+'.mesh-0.obj')).read_text().splitlines():
        if line.startswith('g '):bone=line.split()[1]
        elif line.startswith('f '):bones.append(bone)
    weights,edges,extra,fragments={},[],[],[]
    for ordinal in selected_parts:
        ids=np.flatnonzero(c==ordinal).tolist()
        assert set(np.array(bones)[np.isin(faces,ids).all(1)])=={'bone_2'}
        # Innermost authored side-post surface; preserve the post profile beyond it.
        left,right=-5.1359863,5.1359863
        assert np.min(v[ids,0])==-5.819702 and np.max(v[ids,0])==5.819702
        member = np.isin(faces, ids)
        assert not np.any(member.any(1) & ~member.all(1))
        assert not set(weights).intersection(ids)
        if ordinal != 84: assert left in v[ids, 0] and right in v[ids, 0]
        for i in ids:
            weights[i] = float(np.clip((2 * float(v[i, 0]) - left - right) / (right - left), -1, 1))
        selected = np.flatnonzero(np.isin(faces, ids).all(1))
        def clip(poly, points, boundary, left):
            result = []
            for a, b in zip(poly, poly[1:] + poly[:1]):
                xa, xb = a @ points[:, 0], b @ points[:, 0]
                ina = xa <= boundary if left else xa >= boundary
                inb = xb <= boundary if left else xb >= boundary
                if ina:
                    result.append(a)
                if ina != inb:
                    result.append(a + (b - a) * ((boundary - xa) / (xb - xa)))
            clean = []
            for p in result:
                if not clean or np.max(abs(p - clean[-1])) > 1e-10:
                    clean.append(p)
            if len(clean) > 1 and np.max(abs(clean[0] - clean[-1])) <= 1e-10:
                clean.pop()
            return clean
        for ordinal in selected:
            face = faces[ordinal]
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
                    nonzero = np.flatnonzero(abs(bary) > 1e-10)
                    if len(nonzero) == 1:
                        polygon.append(int(face[nonzero[0]]))
                    else:
                        assert len(nonzero) == 2
                        a, b = map(int, nonzero)
                        fraction = float(np.float32(bary[b]))
                        key = (int(face[a]), int(face[b]), fraction)
                        if key not in local:
                            local[key] = len(v) + len(edges)
                            p = points[a] + (points[b] - points[a]) * fraction
                            weight = -1 if p[0] < 0 else 1
                            assert min(abs(p[0]-left), abs(p[0]-right)) < 1e-6
                            edges.append((*key, weight))
                            extra.append(p)
                        polygon.append(local[key])
                for i in range(1, len(polygon) - 1):
                    indices += [polygon[0], polygon[i], polygon[i+1]]
            fragments.append((int(ordinal), indices))
    assert len(weights)==(2328,1864,304)[lod]
    # Check every previously emitted map, not only the disconnected side parts.
    maps=['crossbeams','deck-base','machinery','tower-frames','connectors','column-capitals','upper-shells']
    masks=[np.load(r/f'lift04-rigid-{lod}-0.npz')['selected']]
    if lod < 2: maps.append('moving-side-frames')
    masks += [np.load(r/f'lift04-{name}-{lod}.npz')['selected'] for name in maps]
    assert not np.stack(masks).any(0)[list(weights)].any()
    # Every original face touching a frame is wholly owned by it. Validate both
    # untouched original faces and the replacement fragments, not fragments alone.
    replaced = {ordinal for ordinal, _ in fragments}
    frame_faces = np.flatnonzero(np.isin(faces, list(weights)).all(1))
    retained = [faces[i] for i in frame_faces if i not in replaced]
    complete_faces = np.array([list(f) for f in retained] +
        [ids[i:i+3] for _, ids in fragments for i in range(0, len(ids), 3)])
    all_v = np.concatenate((v.astype(float), np.array(extra)))
    for ordinal, indices in fragments:
        p = v[faces[ordinal]].astype(float)
        normal = np.cross(p[1] - p[0], p[2] - p[0])
        q = all_v[np.array(indices).reshape(-1, 3)]
        area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
        assert abs(area - np.linalg.norm(normal)) < 3e-5
    for delta in (-6, -2, -.5, 0, 2, 8):
        moved = all_v.copy()
        for i, w in weights.items():
            moved[i, 0] += w * delta
        for i, edge in enumerate(edges):
            moved[len(v)+i, 0] += edge[3] * delta
        assert np.array_equal(moved[:, 1:], all_v[:, 1:])
        for i, w in weights.items():
            if abs(w) == 1:
                assert abs(moved[i, 0] - all_v[i, 0] - w * delta) < 1e-10
        p, q = all_v[complete_faces], moved[complete_faces]
        before = np.cross(p[:, 1]-p[:, 0], p[:, 2]-p[:, 0])
        after = np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])
        valid = np.linalg.norm(before, axis=1) > 1e-8
        assert np.isfinite(moved).all()
        if delta >= -.5: assert np.all(np.sum(before[valid]*after[valid], axis=1) > 0)
    name = 'LiftBridge04' + (f'_LOD{lod}' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
    np.savez(r/f'lift04-moving-end-frames-{lod}.npz', positions=all_v, indices=complete_faces, moved=moved, selected=np.isin(np.arange(len(v)),list(weights)))
    tables.append((weights, edges, fragments, digest))
    print(f'LOD{lod}: {len(weights)} original vertices, {len(edges)} edge vertices, '
          f'{len(fragments)} subdivided faces; source area and six-width checks passed')

def f(x):
    return format(float(np.float32(x)), '.9g') + 'f'
lines = ['// Measured LiftBridge04 moving end-frame profiles, game 1.6.2f1.',
         '// Generated by Lift04MovingEndFrames.py; no runtime intersections.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Lift04MovingEndFrames', '{',
         '    internal static MovableStructureModel.Plan Complete(int lod, Vector3[] source,',
         '        Vector3[] positions, int[] indices, float delta)', '    {',
         '        var data = lod switch', '        {',
         '            0 => (Count: 60636, Indices: 110130, Vertices: Vertices0, Edges: Edges0, Signs: Signs0, Fragments: Fragments0),',
         '            1 => (Count: 45780, Indices: 66045, Vertices: Vertices1, Edges: Edges1, Signs: Signs1, Fragments: Fragments1),',
         '            2 => (Count: 3212, Indices: 5208, Vertices: Vertices2, Edges: Edges2, Signs: Signs2, Fragments: Fragments2),',
         '            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
         '        if (!float.IsFinite(delta) || source.Length != data.Count ||',
         '            positions.Length != data.Count || indices.Length != data.Indices)',
         '            throw new InvalidOperationException("Lift04 moving end frames require their measured layout.");',
         '        foreach (var vertex in data.Vertices)',
         '            positions[vertex.Index].x = source[vertex.Index].x + vertex.Weight * delta;',
         '        var x = new float[data.Edges.Length];',
         '        for (var i = 0; i < x.Length; i++)', '        {',
         '            var edge = data.Edges[i];',
         '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) + data.Signs[i] * delta;',
         '        }',
         '        return new MovableStructureModel.Plan(source, positions, new[] { indices },',
         '            new[] { new bool[indices.Length / 3] }, data.Edges, data.Fragments, constructedX: x);',
         '    }', '']
for lod, (weights, edges, fragments, digest) in enumerate(tables):
    lines += ['    // Geometry SHA-256: ' + digest,
              f'    private static readonly (int Index, float Weight)[] Vertices{lod} =', '    {']
    lines += [f'        ({i}, {f(w)}),' for i, w in sorted(weights.items())]
    lines += ['    };', f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{lod} =', '    {']
    lines += [f'        new({a}, {b}, {f(t)}),' for a, b, t, _ in edges]
    lines += ['    };', f'    private static readonly float[] Signs{lod} =', '    {',
              '        ' + ', '.join(f(w) for _, _, _, w in edges), '    };',
              f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} =', '    {']
    lines += ['        new(0, ' + str(i) + ', ' + ', '.join(map(str, ids)) + '),' for i, ids in fragments]
    lines += ['    };', '']
lines += ['}', '']
output.write_text('\n'.join(lines), encoding='utf-8')
