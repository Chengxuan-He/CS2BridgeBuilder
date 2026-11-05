"""Record original sleeper face provenance and continuous authored-board stations.

Identification is offline only. Output preserves each board's native face, vertex,
UV and bone identities. No texture content is generated or modified.
"""
from pathlib import Path
import hashlib, json, struct, sys
import numpy as np
from collections import Counter, defaultdict

def rectangular_parameters(points, triangles, axes):
    """Map the measured face outline to a rectangle, retaining every source triangle.

    Several authored sleeper ends are pentagons, not axis-aligned rectangles.
    Bounding-box UV coordinates alone would leave triangular holes between boards.
    Four native extreme corners anchor the outline; other native boundary vertices
    remain on the corresponding edge. This changes geometry, never texture UVs.
    """
    projected = {int(i): tuple(map(float, points[i, list(axes)])) for i in np.unique(triangles)}
    ids = {q: i for i, q in enumerate(sorted(set(projected.values())))}
    q = np.array(sorted(ids), dtype=float)
    faces = [[ids[projected[int(i)]] for i in t] for t in triangles]
    counts = Counter(tuple(sorted((t[i], t[(i+1) % 3]))) for t in faces for i in range(3))
    graph = defaultdict(list)
    for (a, b), count in counts.items():
        if count == 1: graph[a].append(b); graph[b].append(a)
    assert all(len(v) == 2 for v in graph.values())
    loop = [min(graph)]
    while True:
        candidates = [i for i in graph[loop[-1]] if len(loop) == 1 or i != loop[-2]]
        next_id = candidates[0]
        if next_id == loop[0]: break
        loop.append(next_id)
    values = (q-q.min(0))/(q.max(0)-q.min(0))
    corners = np.array([[0,0],[1,0],[1,1],[0,1]], dtype=float)
    anchors = [min(loop, key=lambda i: float(np.sum((values[i]-corner)**2))) for corner in corners]
    assert len(set(anchors)) == 4
    locations = sorted((loop.index(i), c) for c, i in enumerate(anchors))
    for k, (start, c0) in enumerate(locations):
        end, c1 = locations[(k+1) % 4]
        assert (c1-c0) % 4 in (1, 3)
        path = [loop[j % len(loop)] for j in range(start, (end if end > start else end+len(loop))+1)]
        distance = np.concatenate([[0.], np.cumsum(np.linalg.norm(np.diff(q[path], axis=0), axis=1))])
        for i, t in zip(path, distance/distance[-1]): values[i] = corners[c0]*(1-t)+corners[c1]*t
    area = 0.
    for t in faces:
        a, b, c = values[t]
        determinant = float(np.linalg.det(np.array([b-a,c-a])))
        sa, sb, sc = q[t]
        source_determinant = float(np.linalg.det(np.array([sb-sa,sc-sa])))
        assert determinant * source_determinant > 0, (t, determinant, source_determinant)
        area += abs(determinant)/2
    assert abs(area-1.) < 1e-9, area
    return {i: values[ids[p]].tolist() for i, p in projected.items()}

root = Path(sys.argv[1])
output = root / 'artifacts/material-remap/Lift02/underlay70'
output.mkdir(parents=True, exist_ok=True)
ints, floats, evidence = [], [9.27197265625, 9.2666015625, -106., -82., 82., 106.], []
far_data = np.load(root / 'artifacts/uv-density/native/LiftBridge02_LOD2 Mesh.npz')
far, far_faces = far_data['mesh0_positions'], far_data['mesh0_faces']
farparts = json.loads((root / 'artifacts/material-remap/Lift02/truss68/complete-native-members-lod2.json').read_text())['parts']
for component, top_faces in ((50, [1178,1179]), (78, [1618,1619]), (12, [410,411])):
    q = next(q for q in farparts if q['component'] == component)
    assert set(top_faces).issubset(q['faces'])
    v = far[np.unique(far_faces[top_faces])]
    floats.extend([float(v[:, 0].min()), float(v[:, 0].max())])
gap_start = float(far[far_faces[[402,403]]][:,:,2].max())
gap_end = float(far[far_faces[[404,405]]][:,:,2].min())
floats.extend([-gap_end, -gap_start, gap_start, gap_end])
for lod in (0, 1):
    name = 'LiftBridge02' + (f'_LOD{lod}' if lod else '') + ' Mesh'
    data = np.load(root / f'artifacts/uv-density/native/{name}.npz')
    p, faces = data['mesh0_positions'], data['mesh0_faces']
    parts = json.loads((root / f'artifacts/material-remap/Lift02/truss68/complete-native-members-lod{lod}.json').read_text())
    boards = []
    for part in parts['parts']:
        v = p[part['vertices']]; span = v.max(0) - v.min(0)
        if part['trackRemoved'] and 7.99 < span[0] < 8.01 and .5 < span[1] < .54:
            boards.append((float((v[:, 2].min()+v[:, 2].max())/2), part))
    boards.sort()
    assert len(boards) == 318
    ints.extend([len(p), len(faces), len(boards)])
    groups = [([b for b in boards if b[0] < -82], -106., -82.),
              ([b for b in boards if -82 < b[0] < 82], -82., 82.),
              ([b for b in boards if b[0] > 82], 82., 106.)]
    for group, low, high in groups:
        for k, (center, part) in enumerate(group):
            z0 = low if k == 0 else (group[k-1][0]+center)/2
            z1 = high if k == len(group)-1 else (center+group[k+1][0])/2
            types = [[] for _ in range(5)]
            for face in part['faces']:
                q = p[faces[face]].astype(np.float64)
                normal = np.cross(q[1]-q[0], q[2]-q[0])
                axis = int(np.argmax(np.abs(normal)))
                kind = (0 if normal[1] < 0 else -1) if axis == 1 else ((1 if normal[0] < 0 else 2) if axis == 0 else (3 if normal[2] < 0 else 4))
                if kind >= 0: types[kind].append(face)
            assert all(len(t) >= 2 for t in types)
            ints.extend([int(k == 0), int(k == len(group)-1), 0 if low != -82 else 1])
            floats.extend([z0, z1])
            for kind, fs in enumerate(types):
                ints.append(len(fs))
                axes = (0, 2) if kind == 0 else ((2, 1) if kind < 3 else (0, 1))
                quad = p[np.unique(faces[fs])]
                lo, hi = quad.min(0), quad.max(0)
                parameter_map = rectangular_parameters(p, faces[fs], axes)
                for face in fs:
                    ids = faces[face]; q = p[ids].astype(np.float64)
                    # Actual source triangle basis; inverse is immutable measured data.
                    u, v = q[1]-q[0], q[2]-q[0]
                    n = np.cross(u, v); n /= np.linalg.norm(n)
                    inverse = np.linalg.inv(np.column_stack([u, v, n]))
                    parameters = [value for i in ids for value in parameter_map[int(i)]]
                    ints.extend([int(face), *map(int, ids)])
                    floats.extend(parameters)
                    floats.extend(inverse.astype(np.float32).ravel().tolist())
            evidence.append(dict(lod=lod, component=part['component'], sourceCenter=center,
                                 start=z0, end=z1, bones=part['bones'], sourceFaces=types))
raw = struct.pack('<8i', 0x314e4242, 1, len(ints), 0, 0, 0, len(floats), 0)
raw += np.array(ints, '<i4').tobytes() + np.array(floats, '<f4').tobytes()
raw += hashlib.sha256(raw).digest()
(output / 'boards.json').write_text(json.dumps(evidence, indent=2))
target = root / 'assets/HardcodedNumbers/Lift02/Types_Lift02_RoadUnderlay.bin'
target.write_bytes(raw)
print(str(target), len(raw), hashlib.sha256(raw).hexdigest())
