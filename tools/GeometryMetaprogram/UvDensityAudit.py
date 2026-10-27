"""Decode native Geometry and retain per-face UV Jacobians for density comparisons.

This is offline evidence, not a runtime part classifier. Singular values here are
UV units/metre unless explicit texture dimensions are supplied to jacobians().
Never describe this source inventory as an audit of generated bridge output.
"""
import argparse
import ctypes as ct
import hashlib
import json
from pathlib import Path
import struct
import numpy as np


def jacobians(positions, uv, faces, dimensions=(1, 1)):
    p = positions[faces].astype(np.float64)
    q = uv[faces, :2].astype(np.float64) * np.asarray(dimensions)
    e = p[:, 1] - p[:, 0]
    f = p[:, 2] - p[:, 0]
    length = np.linalg.norm(e, axis=1)
    area2 = np.linalg.norm(np.cross(e, f), axis=1)
    valid = (length > 0) & (area2 > 0)
    valid &= np.isfinite(p).all(axis=(1, 2)) & np.isfinite(q).all(axis=(1, 2))
    ids = np.flatnonzero(valid)
    x = np.sum(e[ids] * f[ids], axis=1) / length[ids]
    y = area2[ids] / length[ids]
    j = np.empty((len(ids), 2, 2))
    j[:, :, 0] = (q[ids, 1] - q[ids, 0]) / length[ids, None]
    j[:, :, 1] = ((q[ids, 2] - q[ids, 0]) - j[:, :, 0] * x[:, None]) / y[:, None]
    return ids, j


class NativeGeometry:
    def __init__(self, plugin):
        self.dll = ct.CDLL(str(plugin))
        for name, args, result in (
            ('decompress_calc_bound', [ct.c_void_p, ct.c_int64, ct.c_int], ct.c_int64),
            ('decompress_data', [ct.c_void_p, ct.c_int64, ct.c_void_p, ct.c_int64, ct.c_int], ct.c_int64),
            ('decompress_meshopt_vertex_attribute', [ct.c_void_p, ct.c_int64, ct.c_int, ct.c_int, ct.c_void_p], ct.c_int),
            ('decompress_meshopt_index_buffer', [ct.c_void_p, ct.c_int64, ct.c_int, ct.c_int, ct.c_void_p], ct.c_int)):
            fn = getattr(self.dll, name)
            fn.argtypes, fn.restype = args, result

    def read(self, path, include_colors=False):
        raw = path.read_bytes()
        version, count, flags = struct.unpack_from('<HII', raw)
        if version != 1:
            raise ValueError(f'Unsupported Geometry version {version}: {path}')
        headers = [struct.unpack_from('<QI14i5I', raw, 10 + i * 88) for i in range(count)]
        offset = 10 + count * 88 + sum(h[-1] * 40 for h in headers)

        def block(size, elements, stride, indices=False):
            nonlocal offset
            data = raw[offset:offset + size]
            offset += size
            if flags & 2:
                capacity = self.dll.decompress_calc_bound(data, len(data), 0)
                if capacity <= 0:
                    raise ValueError('Invalid native compression length')
                buffer = ct.create_string_buffer(capacity)
                if self.dll.decompress_data(data, len(data), buffer, capacity, 0) != capacity:
                    raise ValueError('Native decompression length mismatch')
                data = buffer.raw
            if flags & 1:
                buffer = ct.create_string_buffer(elements * stride)
                decode = self.dll.decompress_meshopt_index_buffer if indices else self.dll.decompress_meshopt_vertex_attribute
                if decode(data, len(data), elements, stride, buffer) != 0:
                    raise ValueError('Native meshopt decoding failed')
                data = buffer.raw
            if len(data) != elements * stride:
                raise ValueError('Native attribute length mismatch')
            return data

        meshes = [{} for _ in headers]
        for mesh, h in zip(meshes, headers):
            stride = 4 if h[17] & 1 else 2
            mesh['faces'] = np.frombuffer(block(h[16], h[19], stride, True),
                                          dtype='<u4' if stride == 4 else '<u2').reshape(-1, 3)
        for attribute in range(14):
            for mesh, h in zip(meshes, headers):
                size = h[2 + attribute]
                if not size:
                    continue
                if attribute != 0 and not 4 <= attribute <= 11 and not (attribute == 3 and include_colors):
                    offset += size
                    continue
                fmt = (h[0] >> (attribute * 4)) & 15
                dim = ((h[1] >> (attribute * 2)) & 3) + 1
                if attribute == 3 and fmt == 2:
                    mesh['color_bytes'] = np.frombuffer(block(size, h[18], dim), dtype='u1').reshape(-1, dim)
                    continue
                if fmt not in (0, 1):
                    raise ValueError(f'Unmeasured coordinate format {fmt} at channel {attribute}: {path}')
                dtype = np.dtype('<f4' if fmt == 0 else '<f2')
                mesh['positions' if attribute == 0 else 'color' if attribute == 3 else f'uv{attribute - 4}'] = np.frombuffer(
                    block(size, h[18], dtype.itemsize * dim), dtype=dtype).reshape(-1, dim)
        return meshes


def self_test():
    p = np.array([[0., 0, 0], [2, 0, 0], [0, 0, 4]])
    uv = np.array([[0., 0], [1, 0], [0, 1]])
    f = np.array([[0, 1, 2]])
    _, j = jacobians(p, uv, f, (1024, 2048))
    np.testing.assert_allclose(np.linalg.svd(j, compute_uv=False), [[512, 512]])
    # Equal UV/physical areas are insufficient: reciprocal stretch still fails.
    changed = p * [2, 1, .5]
    _, j2 = jacobians(changed, uv, f, (1024, 2048))
    np.testing.assert_allclose(np.linalg.svd(j2, compute_uv=False), [[1024, 256]])
    np.testing.assert_allclose(abs(np.linalg.det(j)), abs(np.linalg.det(j2)))
    # Rigid rotation/translation must not be reported as lost density.
    rotated = p[:, [2, 0, 1]] + [17, -3, 9]
    _, jr = jacobians(rotated, uv, f, (1024, 2048))
    np.testing.assert_allclose(np.linalg.svd(jr, compute_uv=False), [[512, 512]])
    source = {'positions': p, 'uv0': uv, 'faces': f}
    moved = {'positions': changed, 'uv0': uv, 'faces': f}
    result = compare([source], [moved])['meshes'][0]
    assert result['unique_matches'] == 1 and result['changed_density_faces'] == 1
    np.testing.assert_allclose(result['violations'][0]['density_ratios'], [2, .5])
    assert compare([source], [{'positions': rotated, 'uv0': uv, 'faces': f}])['meshes'][0]['changed_density_faces'] == 0
    ambiguous = dict(source, faces=np.concatenate([f, f]))
    assert compare([ambiguous], [moved])['meshes'][0]['unique_matches'] == 0
    print('PASS anisotropic density, equal-area false positive, native dimensions and rigid transforms.')


def compare(reference, generated, unchanged_axes=()):
    """Compare uniquely corresponding UV triangles, never guess ambiguous matches.

    This ratio is independent of texture dimensions only when the material and
    its texture dimensions are unchanged. Converted road surfaces require their
    own source mapping and are deliberately not certified by this comparison.
    """
    results = []
    for mesh_index, (source, target) in enumerate(zip(reference, generated)):
        if 'uv0' not in source or 'uv0' not in target:
            continue
        si, sj = jacobians(source['positions'], source['uv0'], source['faces'])
        ti, tj = jacobians(target['positions'], target['uv0'], target['faces'])
        lookup = {}
        def key_for(mesh, face):
            indices = mesh['faces'][face]
            values = mesh['uv0'][indices, :2].ravel().astype(float).tolist()
            if unchanged_axes:
                values += mesh['positions'][indices][:, unchanged_axes].ravel().astype(float).tolist()
            return tuple(values)
        for face, j in zip(si, sj):
            key = key_for(source, face)
            if key in lookup:
                lookup[key] = None
            else:
                lookup[key] = (int(face), j)
        records, skipped = [], 0
        for face, j in zip(ti, tj):
            key = key_for(target, face)
            item = lookup.get(key)
            if item is None or abs(np.linalg.det(item[1])) < 1e-15:
                skipped += 1
                continue
            # Rotating either triangle's local physical frame cannot affect these
            # singular values. Same-material dimensions cancel from the ratio.
            ratios = np.linalg.svd(np.linalg.solve(item[1], j), compute_uv=False)
            records.append({'source_face': item[0], 'generated_face': int(face),
                            'density_ratios': ratios.tolist()})
        failed = [r for r in records if min(r['density_ratios']) < .99 or max(r['density_ratios']) > 1.01]
        results.append({'mesh': mesh_index, 'unique_matches': len(records), 'unmatched_or_ambiguous': skipped,
                        'changed_density_faces': len(failed), 'violations': failed})
    return {'requires_unchanged_material_and_texture_dimensions': True,
            'additional_unchanged_position_axes': list(unchanged_axes),
            'unmatched_faces_are_not_verified': True, 'meshes': results}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--plugin', type=Path)
    parser.add_argument('--self-test', action='store_true')
    parser.add_argument('--reference', type=Path)
    parser.add_argument('--generated', type=Path)
    parser.add_argument('--match-yz', action='store_true', help='Only for recipes already proven to retain Y/Z; disambiguates repeated UVs offline')
    args = parser.parse_args()
    if args.self_test:
        self_test()
    if args.reference is not None:
        if args.generated is None or args.plugin is None or args.output is None:
            parser.error('--reference requires --generated, --plugin and --output')
        reader = NativeGeometry(args.plugin)
        result = compare(reader.read(args.reference), reader.read(args.generated), (1, 2) if args.match_yz else ())
        result['reference'], result['generated'] = str(args.reference), str(args.generated)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
        print(json.dumps([{k: v for k, v in m.items() if k != 'violations'} for m in result['meshes']]))
        return
    if args.input is None:
        return
    if args.output is None or args.plugin is None:
        parser.error('--input requires --output and --plugin')
    reader = NativeGeometry(args.plugin)
    args.output.mkdir(parents=True, exist_ok=True)
    records = []
    for path in sorted(args.input.glob('*.Geometry')):
        archive, summaries = {}, []
        for index, mesh in enumerate(reader.read(path)):
            summary = {'mesh': index, 'vertices': len(mesh['positions']), 'triangles': len(mesh['faces']),
                       'uv_channels': [key for key in mesh if key.startswith('uv')]}
            if 'uv0' in mesh and mesh['uv0'].shape[1] >= 2:
                ids, j = jacobians(mesh['positions'], mesh['uv0'], mesh['faces'])
                singular = np.linalg.svd(j, compute_uv=False)
                mesh['metric_faces'], mesh['uv0_jacobian'] = ids, j
                summary.update(valid_geometry_faces=len(ids), singular_uv_faces=int((singular[:, 1] == 0).sum()),
                               uv_units_per_metre_min=singular.min(axis=0).tolist() if len(ids) else None,
                               uv_units_per_metre_max=singular.max(axis=0).tolist() if len(ids) else None)
            archive.update({f'mesh{index}_{key}': value for key, value in mesh.items()})
            summaries.append(summary)
        np.savez_compressed(args.output / (path.stem + '.npz'), **archive)
        records.append({'source': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'meshes': summaries})
    (args.output / 'inventory.json').write_text(json.dumps({'units': 'UV units/metre; texture dimensions not supplied',
        'generated_output_verified': False, 'sources': records}, indent=2), encoding='utf-8')
    print(f'Decoded {len(records)} native Geometry files; source metrics retained, generated output not certified.')


if __name__ == '__main__':
    main()
