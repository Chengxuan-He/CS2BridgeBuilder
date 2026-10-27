"""Extract measured planar UV domains; never classify prototype parts at runtime.

Usage: python MovableUvRecipes.py SOURCE_NPZ_DIRECTORY OUTPUT_DIRECTORY [REPOSITORY]
Append --repack to reuse the measured domain NPZ files in OUTPUT_DIRECTORY while
rebuilding embedded sampling bounds/anchors from the original source meshes.
Without REPOSITORY the files are offline construction evidence only. With it,
nonconstant domains become checksum-validated embedded arrays and an exact-key
runtime catalogue. Nonrectangular domains use their existing source triangles;
rank-one UVs are retained, while constant UVs need no density compensation.
Connected domains with incompatible planes/UV gradients keep separate source-face
frames. Complementary triangles that cover an exact measured UV rectangle share
sampler bounds even across duplicated vertices, while retaining independent frames. No new physical triangle or vertex
position is made.
"""
from pathlib import Path
import json
import sys
import struct
import hashlib
import numpy as np
from UvDensityAudit import jacobians


def frame(p, uv, anchors):
    a, b, c = p[anchors]
    edge = b-a
    length = np.linalg.norm(edge)
    normal = np.cross(edge, c-a)
    area = np.linalg.norm(normal)
    x = edge/length
    y = np.cross(normal/area, x)
    du = (uv[anchors[1]]-uv[anchors[0]])/length
    dv = (uv[anchors[2]]-uv[anchors[0]]-du*np.dot(c-a, x))/(area/length)
    return a, x, y, du, dv


def components(faces):
    parent = np.arange(int(faces.max())+1)
    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for face in faces:
        first = root(face[0])
        for vertex in face[1:]:
            parent[root(vertex)] = first
    groups = {}
    for i, face in enumerate(faces):
        groups.setdefault(int(root(face[0])), []).append(i)
    return list(groups.values())


def recipe(p, uv, faces):
    ids, js = jacobians(p, uv, faces)
    # Rank-one UVs have a measurable directional density too. Only constant UVs
    # have no resolution to compensate; physical degeneracy is filtered above.
    eligible = set(ids[np.any(js != 0, axis=(1, 2))].tolist())
    domains, split_components = [], 0
    for component in components(faces):
        selected = [i for i in component if i in eligible]
        if not selected:
            continue
        vertex_ids = np.unique(faces[selected])
        triangles = p[faces[selected]]
        areas = np.linalg.norm(np.cross(triangles[:, 1]-triangles[:, 0], triangles[:, 2]-triangles[:, 0]), axis=1)
        anchors = faces[selected[int(areas.argmax())]]
        origin, x, y, du, dv = frame(p, uv, anchors)
        delta = p[vertex_ids]-origin
        normal_error = np.max(abs(delta@np.cross(x, y)))
        fitted = uv[anchors[0]]+(delta@x)[:, None]*du+(delta@y)[:, None]*dv
        uv_error = np.max(abs(fitted-uv[vertex_ids]))
        # These are offline measurement tolerances, never runtime part boundaries.
        # Non-affine native UVs retain separate face frames instead of smoothing
        # away their authored density. Float32-level roundoff alone is tolerated.
        if normal_error <= 1e-6 and uv_error <= 1e-7:
            domains.append((anchors, selected))
        else:
            split_components += 1
            domains.extend((faces[i], [i]) for i in selected)
    rows, members, offsets, bounds, rectangles = [], [], [0], [], []
    maximum_error = 0.
    transform = np.array([[2., .3, 0], [0, .7, .2], [.1, 0, 1.4]])
    target = p@transform.T
    for anchors, selected in domains:
        rows.append(anchors)
        members.extend(selected)
        offsets.append(len(members))
        vertex_ids = np.unique(faces[selected])
        bounds.append(np.concatenate([uv[vertex_ids].min(0), uv[vertex_ids].max(0)]))
        q = uv[faces[selected]]
        area = np.abs(np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])).sum() / 2
        box_area = np.prod(bounds[-1][2:]-bounds[-1][:2])
        rectangles.append(box_area > 0 and abs(area-box_area) <= max(1e-12, box_area*1e-5))
        # Exercise the recipe with a non-rigid XYZ affine transform. This tests
        # density math, not a particular bridge's recorded width deformation.
        o, x, y, du, dv = frame(p, uv, anchors)
        to, tx, ty, _, _ = frame(target, uv, anchors)
        original_delta, target_delta = p[vertex_ids]-o, target[vertex_ids]-to
        mapped = uv[vertex_ids]+((target_delta@tx-original_delta@x)[:, None]*du
                                 +(target_delta@ty-original_delta@y)[:, None]*dv)
        local = {int(index): i for i, index in enumerate(vertex_ids)}
        local_faces = np.array([[local[int(i)] for i in face] for face in faces[selected]])
        _, before = jacobians(p[vertex_ids], uv[vertex_ids], local_faces)
        _, after = jacobians(target[vertex_ids], mapped, local_faces)
        before_metric = before@before.transpose(0,2,1)
        after_metric = after@after.transpose(0,2,1)
        error = float(np.max(np.linalg.norm(after_metric-before_metric, axis=(1,2)) /
                             np.maximum(1e-30,np.linalg.norm(before_metric,axis=(1,2)))))
        maximum_error = max(maximum_error, error)
    if maximum_error > .001:
        raise ValueError(f'Measured domain lost density under affine test: {maximum_error}')
    return {'anchors': np.asarray(rows, dtype=np.int32).reshape(-1, 3),
            'offsets': np.asarray(offsets, dtype=np.int32), 'faces': np.asarray(members, dtype=np.int32),
            'bounds': np.asarray(bounds, dtype=np.float32).reshape(-1, 4),
            'rectangles': np.asarray(rectangles)}, {
                'domains': len(domains), 'covered_faces': len(members), 'ineligible_faces': len(faces)-len(eligible),
                'split_nonaffine_components': split_components, 'affine_test_max_density_error': maximum_error}


def underside_uv(path, prefix, uv, root):
    # Existing measured underside repair must be the frame/crop reference too.
    if prefix != 'mesh0_' or root is None:
        return uv
    lod = {'DrawBridge01 Mesh.npz': 0, 'DrawBridge01_LOD1 Mesh.npz': 1}.get(path.name)
    if lod is None:
        return uv
    blob = (root/'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01UndersideUv_cs.bin').read_bytes()
    counts = struct.unpack_from('<6i', blob, 8)
    ints = np.frombuffer(blob, '<i4', counts[0], 32)
    float_offset = 32+4*(counts[0]+counts[1])+8*(counts[2]+counts[3])
    values = np.frombuffer(blob, '<f4', counts[4], float_offset)
    index, start, count = (2, 0, 176) if lod == 0 else (178, 352, 158)
    uv = uv.copy()
    uv[ints[index:index+count]] = values[start:start+count*2].reshape(-1, 2)
    return uv


def rectangle_pairs(source_faces, uv):
    """Measure complete rectangular source patches in UV space.

    Each face keeps its own physical frame. Only its sampler bounds are shared,
    so independent part transforms do not get forced into one target UV plane.
    Duplicated geometry vertices and disconnected copies of the same atlas patch
    do not imply different textures. Complementary authored half-rectangles prove
    coverage; equal area or a common bounding box alone does not.
    """
    halves, rectangles = {}, {}
    for face, vertices in enumerate(source_faces):
        q = uv[vertices]
        low, high = q.min(0), q.max(0)
        span = high-low
        if np.any(span <= 0) or not np.all(np.minimum(abs(q-low), abs(q-high)) <= 1e-7):
            continue
        corners = (q > (low+high)/2).astype(int)
        ids = set((corners[:, 0]+2*corners[:, 1]).tolist())
        if len(ids) != 3:
            continue
        u, v = q[1]-q[0], q[2]-q[0]
        twice_area = abs(u[0]*v[1]-u[1]*v[0])
        if abs(twice_area-np.prod(span)) > max(1e-12, np.prod(span)*1e-5):
            continue
        missing = next(iter(set(range(4))-ids))
        key = tuple(np.concatenate((low, high)))
        halves.setdefault(key, {}).setdefault(missing, []).append(face)
    for bounds, patches in halves.items():
        for missing, faces in patches.items():
            if 3-missing in patches:
                for face in faces:
                    rectangles[face] = np.asarray(bounds)
    return rectangles


def tessellated_rectangles(source_faces, uv):
    """Prove rectangular UV coverage from existing edges, without changing geometry.

    Exact coincident UV triangles are sampling aliases, not extra coverage. Interior
    edges must cancel with opposite orientation. The remaining edges must partition
    all four rectangle sides exactly once. Ambiguous overlaps, holes, T-junctions
    and nonrectangular boundaries are rejected rather than guessed or repaired.
    """
    points, vertex_ids = np.unique(uv, axis=0, return_inverse=True)
    triangles, aliases, unique = [], [], {}
    for face, indices in enumerate(vertex_ids[source_faces]):
        key = tuple(sorted(map(int, indices)))
        if len(set(key)) != 3:
            continue
        if key in unique:
            aliases[unique[key]].append(face)
            continue
        q = points[list(key)]
        u, v = q[1]-q[0], q[2]-q[0]
        cross = u[0]*v[1]-u[1]*v[0]
        if not np.isfinite(cross) or cross == 0:
            continue
        unique[key] = len(triangles)
        triangles.append(key if cross > 0 else (key[0], key[2], key[1]))
        aliases.append([face])
    edges = {}
    for index, triangle in enumerate(triangles):
        for a, b in zip(triangle, (*triangle[1:], triangle[0])):
            edges.setdefault((min(a, b), max(a, b)), []).append((index, a, b))
    parents = list(range(len(triangles)))
    def root(i):
        while parents[i] != i:
            parents[i] = parents[parents[i]]
            i = parents[i]
        return i
    for uses in edges.values():
        if len(uses) == 2 and uses[0][1:] == uses[1][1:][::-1]:
            parents[root(uses[1][0])] = root(uses[0][0])
    groups = {}
    for index in range(len(triangles)):
        groups.setdefault(root(index), []).append(index)
    rectangles = {}
    for group in groups.values():
        if len(group) < 2:
            continue
        boundary, local = [], {}
        for index in group:
            triangle = triangles[index]
            for a, b in zip(triangle, (*triangle[1:], triangle[0])):
                local.setdefault((min(a, b), max(a, b)), []).append((a, b))
        valid = True
        for uses in local.values():
            if len(uses) == 1:
                boundary.append(uses[0])
            elif len(uses) != 2 or uses[0] != uses[1][::-1]:
                valid = False
                break
        if not valid or not boundary:
            continue
        selected = points[np.unique(np.asarray([triangles[i] for i in group]))]
        low, high = selected.min(0), selected.max(0)
        if np.any(high <= low):
            continue
        sides = [[], [], [], []]
        for a, b in boundary:
            p, q = points[a], points[b]
            if p[0] == q[0] and p[0] in (low[0], high[0]):
                side, axis = (0 if p[0] == low[0] else 1), 1
            elif p[1] == q[1] and p[1] in (low[1], high[1]):
                side, axis = (2 if p[1] == low[1] else 3), 0
            else:
                valid = False
                break
            sides[side].append((min(p[axis], q[axis]), max(p[axis], q[axis])))
        if not valid:
            continue
        for side, intervals in enumerate(sides):
            axis = 1 if side < 2 else 0
            cursor = low[axis]
            for start, end in sorted(intervals):
                if start != cursor or end <= start:
                    valid = False
                    break
                cursor = end
            if cursor != high[axis]:
                valid = False
        if not valid:
            continue
        # Consistent orientation plus the exact simple boundary proves coverage;
        # also check its signed area to catch malformed numeric input explicitly.
        q = points[np.asarray([triangles[i] for i in group])]
        u, v = q[:, 1]-q[:, 0], q[:, 2]-q[:, 0]
        area = np.sum(u[:, 0]*v[:, 1]-u[:, 1]*v[:, 0])/2
        expected = np.prod(high-low)
        if abs(area-expected) > max(1e-14, expected*1e-10):
            continue
        bounds = np.r_[low, high]
        for index in group:
            for face in aliases[index]:
                rectangles[face] = bounds
    return rectangles


def pack(root, name, payload, source_faces, source_positions, source_uv):
    paired = rectangle_pairs(source_faces, source_uv)
    paired.update(tessellated_rectangles(source_faces, source_uv))
    records=[]
    for index in range(len(payload['anchors'])):
        faces = payload['faces'][payload['offsets'][index]:payload['offsets'][index+1]]
        if payload['rectangles'][index]:
            bounds = paired.get(int(faces[0]))
            if bounds is None or any(face not in paired or not np.array_equal(paired[face], bounds) for face in faces):
                bounds = payload['bounds'][index]
            records.append((payload['anchors'][index], faces, bounds))
        else:
            for face in faces:
                anchors = source_faces[face]
                # A long thin source triangle must not allocate two long texture
                # axes. Cyclically choose its shortest physical edge, also used
                # by the runtime's recorded-fragment precision conditioning.
                # This changes no index winding, geometry, membership or density.
                p = source_positions[anchors].astype(np.float64)
                lengths = np.linalg.norm(np.roll(p, -1, axis=0)-p, axis=1)
                pivot = int(np.argmin(lengths))
                records.append((np.roll(anchors, -pivot), [face], paired.get(face)))
    # Payload schema 2: negative face counts denote triangular repeat units.
    # Triangles do not redundantly store atlas bounds or UVs already in native data.
    ints, floats = [-2,len(records)], []
    for anchors,faces,bounds in records:
        ints.extend([*anchors, len(faces) if bounds is not None else -len(faces), *faces])
        if bounds is not None:floats.extend(bounds)
    from MovableUvDomainFormat import encode_arrays, write_bbn
    data = write_bbn(*encode_arrays(ints, floats))
    import re
    bridge = re.match(r'(PedestrianDraw|Draw|Lift)Bridge(\d{2})', name)
    if bridge is None:
        raise ValueError('Missing bridge type for measured UV resource: '+name)
    folder = root/'assets/HardcodedNumbers'/(''.join(bridge.groups()))
    folder.mkdir(parents=True, exist_ok=True)
    (folder/('UvDomain_'+name+'.bin')).write_bytes(data)


def catalogue(root):
    names = sorted(p.name[:-4] for p in (root/'assets/HardcodedNumbers').rglob('UvDomain_*.bin'))
    lines = ['// Generated by tools/GeometryMetaprogram/MovableUvRecipes.py.',
             '// Embedded numerical arrays: measured UV domains',
             '#nullable enable',
             'namespace BridgeBuilder.Bridges;', 'internal static class MovableUvCatalogue', '{',
             '    internal static HardcodedNumbers? Load(string key) => key switch', '    {']
    lines.extend(f'        "{name}" => HardcodedNumbers.Load("{name}"),' for name in names)
    lines.extend(['        _ => null', '    };', '}', ''])
    (root/'src/BridgeBuilder/Bridges/Common/Movable/MovableUvCatalogue.g.cs').write_text('\n'.join(lines), encoding='utf-8')


def main():
    source, output = map(Path, sys.argv[1:3])
    root = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else None
    output.mkdir(parents=True, exist_ok=True)
    if '--repack' in sys.argv:
        if root is None:
            raise ValueError('Repacking requires the repository path.')
        for path in sorted(source.glob('*.npz')):
            native = np.load(path)
            measured = np.load(output/path.name)
            for key in measured.files:
                if not key.endswith('_anchors'):
                    continue
                prefix = key[:-len('anchors')]
                payload = {field: measured[prefix+field] for field in ('anchors','offsets','faces','bounds','rectangles')}
                uv = underside_uv(path, prefix, native[prefix+'uv0'][:, :2].astype(float), root)
                pack(root, path.stem.replace(' ', '_')+'_'+prefix.removesuffix('_').removeprefix('mesh'),
                     payload, native[prefix+'faces'], native[prefix+'positions'], uv)
            print('Repacked '+path.name, flush=True)
        catalogue(root)
        return
    records = []
    for path in sorted(source.glob('*.npz')):
        data = np.load(path)
        arrays, summary = {}, []
        for key in data.files:
            if not key.endswith('_positions'):
                continue
            prefix = key[:-len('positions')]
            if prefix+'uv0' not in data or data[prefix+'uv0'].shape[1] < 2:
                summary.append({'mesh': prefix, 'status': 'no two-dimensional UV0'})
                continue
            uv = underside_uv(path, prefix, data[prefix+'uv0'][:, :2].astype(float), root)
            payload, report = recipe(data[key].astype(float), uv, data[prefix+'faces'])
            if root is not None:
                pack(root, path.stem.replace(' ', '_')+'_'+prefix.removesuffix('_').removeprefix('mesh'), payload, data[prefix+'faces'], data[key], uv)
            arrays.update({prefix+k: v for k, v in payload.items()})
            summary.append({'mesh': prefix, **report})
        np.savez_compressed(output/path.name, **arrays)
        records.append({'source': path.name, 'meshes': summary})
        print(f'{path.name}: {sum(s.get("domains", 0) for s in summary)} domains', flush=True)
    (output/'recipes.json').write_text(json.dumps({'runtime_path': 'MovableStructureRetile' if root else None,
        'generated_output_verified': False, 'sources': records}, indent=2), encoding='utf-8')
    if root is not None:
        catalogue(root)
    print(f'Extracted measured UV domains from {len(records)} source files; affine density checks passed; generated output not certified.')


if __name__ == '__main__':
    main()
