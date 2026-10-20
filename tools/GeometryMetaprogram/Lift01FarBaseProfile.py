"""Reconstruct coarse Lift01 base endpoints/inner rim from detailed road boundaries.
Usage: python Lift01FarBaseProfile.py RESEARCH_DIRECTORY OUTPUT_CS
Retains native far topology, caps, simplified underside and raw channels.
"""
from pathlib import Path
from collections import Counter
import hashlib, json, sys
import numpy as np

r, output = map(Path, sys.argv[1:])
digest = hashlib.sha256((r/'movable-skin/LiftBridge01_LOD2 Mesh.Geometry').read_bytes()).hexdigest()
assert digest == 'a634d30d56089487f0a4fbd1d6f7dfbe3644c99f5b447b4a6658e4db282fd925'
d = np.load(r/'lift01-main-2.npz'); v, faces = d['positions'].astype(float), d['indices']
assert v.shape == (1110, 3) and faces.shape == (776, 3)
groups = json.loads((r/'lift01-far-fixed-parts.json').read_text())
near = json.loads((r/'lift01-crossing-0.json').read_text())
middle = json.loads((r/'lift01-crossing-1.json').read_text())
records = json.loads((r/'lift-native-road-plane-candidates.json').read_text())
# Both detailed levels independently establish the exact native receiver boundary.
for lod in (0, 1):
    record = next(x for x in records if x['style'] == 1 and x['lod'] == lod)
    assert hashlib.sha256((r/'movable-skin'/record['geometry']).read_bytes()).hexdigest() == record['sha256']
    data = np.load(r/f'lift01-main-{lod}.npz')
    candidates = [c for c in record['candidates'] if c['bone'] in (0, 4) and c['y'] == 9.800048828125]
    assert len(candidates) == 2
    pts = data['positions'][data['indices'][[i for c in candidates for i in c['faces']]]].reshape(-1, 3)
    assert set(np.unique(pts[:, 1])) == {9.800048828125}
    assert pts[:, 0].min() == -3 and pts[:, 0].max() == 3
road_y, half_width = float(pts[0, 1]), float(pts[:, 0].max())
bones, bone = [], None
for line in (r/'movable-skin/LiftBridge01_LOD2 Mesh.mesh-0.obj').read_text().splitlines():
    fields = line.split()
    if fields and fields[0] == 'g': bone = int(fields[1].removeprefix('bone_'))
    elif fields and fields[0] == 'f': bones.append(bone)
assert len(bones) == 776
bones = np.asarray(bones)
road_faces = {320, 321, 708, 709, 342, 343, 730, 731}
domains = [(groups[8]['vertices'], 3, 0), (groups[20]['vertices'], 13, 0),
           (np.unique(faces[bones == 4]).tolist(), 487, 4)]
positions = v.copy(); used, all_faces, audit = set(), set(), []
for ids, part, owner in domains:
    assert near[part]['minimum'] == middle[part]['minimum']
    assert near[part]['maximum'] == middle[part]['maximum']
    low, high = middle[part]['minimum'][2], middle[part]['maximum'][2]
    membership = np.isin(faces, ids)
    assert not np.any(membership.any(1) & ~membership.all(1))
    fi = np.flatnonzero(membership.all(1))
    assert np.all(bones[fi] == owner) and used.isdisjoint(ids)
    used.update(ids); all_faces.update(map(int, fi))
    stations = sorted(set(v[ids, 2]))
    endpoints = stations if owner == 0 else [z for z in stations if abs(z) > 1]
    assert len(endpoints) == 4
    z_map = {z: low if i < 2 else high for i, z in enumerate(endpoints)}
    for i in ids:
        if v[i, 2] in z_map: positions[i, 2] = z_map[v[i, 2]]
        # Exact corners of the removed top, including cap/inner-wall duplicates.
        if v[i, 1] in (10.2001953125, 10.240234375):
            assert abs(v[i, 0]) in (3.245361328125, 3.28533935546875)
            positions[i, 0] = np.sign(v[i, 0])*half_width
            positions[i, 1] = road_y
    assert positions[ids, 2].min() >= low-1e-12 and positions[ids, 2].max() <= high+1e-12
    for delta in (-2.99, -2.75, -2, -.1, 0, 2, 8):
        q = positions.copy(); q[ids, 0] += np.sign(v[ids, 0])*delta
        a, b = v[faces[fi]], q[faces[fi]]
        n0 = np.cross(a[:, 1]-a[:, 0], a[:, 2]-a[:, 0])
        n1 = np.cross(b[:, 1]-b[:, 0], b[:, 2]-b[:, 0])
        assert np.all(np.sum(n0*n1, axis=1) > 0)
        edges, opened = Counter(), Counter()
        for fid in fi:
            # Attribute seams are welded only for this offline topology check.
            keys = [tuple(p) for p in np.round(q[faces[fid]], 5)]
            for a, b in zip(keys, keys[1:]+keys[:1]):
                edge = tuple(sorted((a, b))); edges[edge] += 1
                if fid not in road_faces: opened[edge] += 1
        assert set(edges.values()) == {2}, (part, delta)
        boundary = [e for e, count in opened.items() if count == 1]
        assert boundary and all(abs(p[1]-road_y) < 1e-5 for edge in boundary for p in edge)
        assert set(opened.values()) <= {1, 2}
    audit.append(dict(part=part, bone=owner, vertices=len(ids), triangles=len(fi),
                      low=low, high=high, open_edges=len(boundary)))
assert len(used) == 176 and len(all_faces) == 100 and road_faces <= all_faces
modified = sorted(i for i in used if not np.array_equal(v[i], positions[i]))
for delta in (-2.99, -2.75, -2, -.1, 0, 2, 8):
    q = positions.copy(); q[list(used), 0] += np.sign(v[list(used), 0])*delta
    np.savez(r/f'lift01-far-base-profile-{delta}.npz', positions=q,
             indices=faces[sorted(all_faces-road_faces)])
text = '''// Generated by Lift01FarBaseProfile.py; native far topology, detailed road boundaries.
// Source SHA-256: a634d30d56089487f0a4fbd1d6f7dfbe3644c99f5b447b4a6658e4db282fd925
// Measured LOD reconstruction; no runtime trim or part classification.
using System;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01FarBaseProfile
{
    internal static MovableStructureModel.Plan Apply(MovableStructureModel.Plan plan, float delta)
    {
        if (plan.SourcePositions.Length != 1110 || plan.SourceIndices.Length != 1 ||
            plan.SourceIndices[0].Length != 2328 || !float.IsFinite(delta))
            throw new InvalidOperationException("Lift01 far base requires its recorded road boundaries.");
        var positions = plan.Positions.Take(1110).ToArray();
        foreach (var point in Profile)
            positions[point.Index] = new Vector3(point.X + point.Direction * delta, point.Y, point.Z);
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices,
            plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices,
            plan.Positions.Skip(1110).Select(p => p.x).ToArray(), reconstructsAuthoredPositions: true);
    }

    private static readonly (int Index, float X, float Y, float Z, int Direction)[] Profile =
    {
'''
for i in modified:
    x, y, z = positions[i]
    text += f'        ({i}, {x:.17g}f, {y:.17g}f, {z:.17g}f, {int(np.sign(v[i, 0]))}),\n'
text += '    };\n}\n'
output.write_text(text, encoding='utf-8')
(r/'lift01-far-base-profile.json').write_text(json.dumps(dict(source=digest, domains=audit,
    profile_vertices=len(modified), retained_nonroad_triangles=92, road_y=road_y,
    added_vertices=0, added_triangles=0), indent=2)+'\n')
print(f'{len(modified)} recorded profile vertices; 92 native non-road triangles, no subdivision; '
      'bone ownership, closed caps, road-only open edges and seven signed-width checks passed.')
