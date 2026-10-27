"""Identify native PedestrianDraw01 UV charts without changing mesh topology.

Edges join only when both native position and UV agree exactly. Shared atlas
regions can have multiple disconnected physical occurrences. This tool records
that distinction; it does not infer seamless repetition from a rectangle alone.
"""
import argparse
from collections import defaultdict, Counter
import hashlib
import json
from pathlib import Path
import struct
import sys

import numpy as np
from ExportRecipes import numbers
sys.path.insert(0, str(Path(__file__).resolve().parents[3]/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays

STYLE = 'PedestrianDraw01'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def normalized_triangle(points):
    return tuple(sorted(tuple(float(x) for x in p[:2]) for p in np.asarray(points, dtype=np.float32)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    root = args.repository
    evidence = root / f'artifacts/prepared-materials/{STYLE}/shared-sampling'
    bindings = json.loads((evidence / 'native-bindings.json').read_text())
    samplers = json.loads((evidence / 'source-map.json').read_text())
    channels = bindings['sourceTextureBindings']
    regions, meshes, occurrences = {}, [], []
    triangle_lookup, rectangle_lookup = defaultdict(set), defaultdict(set)

    for render in bindings['nativeRenderBindings']:
        path = root / 'artifacts/uv-density/native' / (render['render'] + '.npz')
        native = np.load(path)
        for mesh, source in enumerate(render['surfaceCids']):
            faces = native[f'mesh{mesh}_faces'].astype(np.int64)
            positions = native[f'mesh{mesh}_positions'].astype(np.float32)
            uv = native[f'mesh{mesh}_uv0'][:, :2].astype(np.float32)
            parent = list(range(len(faces)))

            def find(i):
                while parent[i] != i:
                    parent[i] = parent[parent[i]]
                    i = parent[i]
                return i

            def join(a, b):
                a, b = find(a), find(b)
                if a != b:
                    parent[b] = a

            # Preserve each physical occurrence; UV overlap alone is not adjacency.
            vertex_keys = [tuple(float(x) for x in row) for row in np.concatenate((positions, uv), axis=1)]
            edges = defaultdict(list)
            for fi, face in enumerate(faces):
                for a, b in zip(face, np.roll(face, -1)):
                    if vertex_keys[a] != vertex_keys[b]:
                        edges[tuple(sorted((vertex_keys[a], vertex_keys[b])))].append(fi)
            for adjacent in edges.values():
                for face in adjacent[1:]:
                    join(adjacent[0], face)
            groups = defaultdict(list)
            for fi in range(len(faces)):
                groups[find(fi)].append(fi)
            face_regions = [None] * len(faces)
            for members in groups.values():
                triangles = sorted(set(normalized_triangle(uv[faces[fi]]) for fi in members))
                uv_edges = Counter()
                area = 0.0
                for tri in triangles:
                    a, b, c = np.array(tri, dtype=np.float64)
                    ab, ac = b - a, c - a
                    area += abs(float(ab[0] * ac[1] - ab[1] * ac[0])) * .5
                    for a, b in ((tri[0], tri[1]), (tri[1], tri[2]), (tri[2], tri[0])):
                        if a != b:
                            uv_edges[tuple(sorted((a, b)))] += 1
                boundary = sorted(edge for edge, count in uv_edges.items() if count == 1)
                pts = uv[faces[members].flatten()].astype(np.float64)
                lo, hi = pts.min(axis=0), pts.max(axis=0)
                rectangle_area = float(np.prod(hi - lo))
                rectangle = (bool(boundary) and rectangle_area > 0 and area == rectangle_area and
                    all(a[0] == b[0] or a[1] == b[1] for a, b in boundary) and
                    all(a[0] in (lo[0], hi[0]) and b[0] in (lo[0], hi[0]) if a[0] == b[0] else
                        a[1] in (lo[1], hi[1]) and b[1] in (lo[1], hi[1]) for a, b in boundary))
                texture_sources = sorted(channels.get(source, {}).values()) or [source]
                identity = dict(textureSources=texture_sources, boundary=boundary, triangles=triangles if not boundary else [])
                key = sha(json.dumps(identity, sort_keys=True, separators=(',', ':')).encode())
                if key not in regions:
                    regions[key] = dict(key=key, textureSources=texture_sources, bounds=[*lo, *hi],
                        boundary=boundary, uniqueUvTriangleCount=len(triangles), summedTriangleArea=area,
                        exactRectangle=rectangle, nativeRepeatVerified=False, sourceSurfaces=[])
                if source not in regions[key]['sourceSurfaces']:
                    regions[key]['sourceSurfaces'].append(source)
                for fi in members:
                    face_regions[fi] = key
                occurrence = dict(render=render['render'], mesh=mesh, sourceCid=source, chartKey=key, faces=members)
                occurrences.append(occurrence)
                for triangle in triangles:
                    triangle_lookup[source, triangle].add(key)
                if rectangle:
                    rectangle_lookup[source, tuple([*lo, *hi])].add(key)
            assert all(face_regions)
            domain_path = root / f'assets/HardcodedNumbers/{STYLE}' / ('UvDomain_' + render['render'].replace(' ', '_') + f'_{mesh}.bin')
            values = numbers(domain_path)
            ints, floats = (a.tolist() for a in decode_arrays(values['I32'], values['F32']))
            assert ints[0] == -2
            at, fp, domain_rows = 2, 0, []
            for domain_index in range(ints[1]):
                a, b, c, count = ints[at:at + 4]
                at += 4
                triangle = count < 0
                members = ints[at:at + abs(count)]
                at += abs(count)
                chart_keys = sorted({face_regions[face] for face in members})
                if triangle:
                    triangle_lookup[source, normalized_triangle(uv[[a, b, c]])].update(chart_keys)
                else:
                    bounds = floats[fp:fp + 4]
                    fp += 4
                    rectangle_lookup[source, tuple(bounds)].update(chart_keys)
                domain_rows.append(dict(domain=domain_index, triangle=triangle, chartKeys=chart_keys))
            assert at == len(ints) and fp == len(floats)
            meshes.append(dict(render=render['render'], mesh=mesh, sourceCid=source,
                nativeMeshSHA256=sha(path.read_bytes()), faces=len(faces), physicalChartOccurrences=len(groups),
                uniqueCharts=len(set(face_regions)), faceCharts=face_regions, domainCharts=domain_rows,
                measuredDomainSHA256=sha(domain_path.read_bytes())))

    mapped, unresolved = [], []
    for sampler in samplers['samplers']:
        if sampler['method'] == 'triangle-reflection-repeat':
            keys = triangle_lookup[sampler['sourceCid'], normalized_triangle([sampler['a'], sampler['b'], sampler['c']])]
        else:
            bounds = sampler.get('bounds', sampler.get('minimum', []) + sampler.get('maximum', []))
            keys = rectangle_lookup[sampler['sourceCid'], tuple(float(x) for x in np.asarray(bounds, dtype=np.float32))]
        row = dict(key=sampler['key'], sourceCid=sampler['sourceCid'], family=sampler['family'], chartKeys=sorted(keys))
        (mapped if keys else unresolved).append(row)
    result = dict(schema='BridgeBuilder/NativeUvCharts/1', style=STYLE,
        sourceBindingsSHA256=sha((evidence/'native-bindings.json').read_bytes()),
        charts=list(regions.values()), occurrences=occurrences, meshes=meshes,
        samplerChartMappings=mapped, unmatchedSamplers=unresolved,
        summary=dict(renderInputs=len(bindings['nativeRenderBindings']), meshStreams=len(meshes),
            nativeFaces=sum(m['faces'] for m in meshes), physicalChartOccurrences=len(occurrences),
            sharedUvCharts=len(regions), rectangleCharts=sum(c['exactRectangle'] for c in regions.values()),
            mappedSamplerRecords=len(mapped), unmatchedSamplerRecords=len(unresolved)),
        limits='Exact native adjacency and atlas region geometry only. Rectangular shape does not establish a periodic image. No generated topology, UV, source pixels or native mip data changed.')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, separators=(',', ':'))+'\n', encoding='utf-8')
    # Bridge-specific measured numeric data, ready for the shared reader to embed.
    # Keep the candidate outside production arrays until its consumer is verified.
    ordered = sorted(regions)
    chart_indices = {key: i for i, key in enumerate(ordered)}
    ints, floats = [1, len(ordered), len(meshes)], []
    for key in ordered:
        chart = regions[key]
        ints.extend((int(chart['exactRectangle']), len(chart['boundary'])))
        floats.extend(chart['bounds'])
        for a, b in chart['boundary']:
            floats.extend((*a, *b))
    for mesh in meshes:
        ints.append(mesh['faces'])
        ints.extend(chart_indices[key] for key in mesh['faceCharts'])
    raw = b'BBN1' + struct.pack('<7i', 1, len(ints), 0, 0, 0, len(floats), 0)
    raw += struct.pack('<' + 'i' * len(ints), *ints)
    raw += struct.pack('<' + 'f' * len(floats), *floats)
    binary = args.output.with_suffix('.bin')
    binary.write_bytes(raw + hashlib.sha256(raw).digest())
    index = dict(schema='BridgeBuilder/NativeUvCharts/1', binarySHA256=sha(binary.read_bytes()),
        sourceJsonSHA256=sha(args.output.read_bytes()), chartKeys=ordered,
        meshKeys=[dict(render=m['render'], mesh=m['mesh'], sourceCid=m['sourceCid']) for m in meshes],
        layout='I32: [1, chartCount, meshCount], [exactRectangle, boundaryEdgeCount] per chart, [faceCount, chartIndex per source face] per mesh. F32: [minU,minV,maxU,maxV] then boundary [aU,aV,bU,bV] per chart. Names/CIDs remain in this index. BBN1 checksum covers all preceding bytes.',
        productionIntegrated=False)
    args.output.with_name('native-chart-index.json').write_text(json.dumps(index, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result['summary']))


if __name__ == '__main__':
    main()
