"""Extract Lift05 authored UV coverage, keeping source identity separate from sampling."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import numpy as np


def main():
    p = argparse.ArgumentParser()
    p.add_argument('repo', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    repo = a.repo.resolve()
    sys.path.insert(0, str(repo/'artifacts/agent-material-env/Lift05/python-deps'))
    from shapely.geometry import Polygon, box
    from shapely.ops import unary_union
    assert not a.output.exists(), 'Use a fresh evidence directory'
    source = (repo/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05SourceRenders.cs').read_text(encoding='utf-8-sig')
    renders = re.findall(r'Create\("([^"]+)", "([0-9a-f]{32})", new\[\] \{ ([^}]+) \}', source)
    assert len(renders) == 9
    catalog = json.loads((repo/'artifacts/prepared-materials/Lift05/native-integration/native-source-sampling.json').read_text())
    source_info = {s['cid']:s for s in catalog['sources']}
    mapping = json.loads((repo/'artifacts/prepared-materials/Lift05/shared-sampling/source-sampling.json').read_text())
    by_source = {}
    native_triangles = {}
    evidence = []
    for name, geometry, entries in renders:
        path = repo/'artifacts/uv-density/native'/f'{name}.npz'
        native = np.load(path)
        for mesh, cid in enumerate(re.findall(r'"([0-9a-f]{32})"', entries)):
            uv = native[f'mesh{mesh}_uv0'][:, :2].astype(np.float64)
            faces = native[f'mesh{mesh}_faces']
            polygons = {}
            degenerates = 0
            for face_index, face in enumerate(faces):
                q = uv[face]
                assert np.isfinite(q).all()
                key = tuple(sorted(map(tuple, q)))
                native_triangles.setdefault(cid, {}).setdefault(key,
                    dict(render=name, mesh=mesh, face=face_index, vertexIndices=face.tolist()))
                poly = Polygon(q)
                if poly.area == 0:
                    degenerates += 1
                    continue
                assert poly.is_valid
                polygons.setdefault(key, poly)
            by_source.setdefault(cid, []).extend(polygons.values())
            evidence.append(dict(render=name, geometryCid=geometry, materialSlot=mesh,
                sourceSurfaceCid=cid, sourceNpZSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                triangles=len(faces), degenerateUvTriangles=degenerates,
                uniqueNondegenerateUvTriangles=len(polygons)))
    assert by_source.keys() == source_info.keys()
    regions = []
    shapes = {}
    for cid, polygons in sorted(by_source.items()):
        coverage = unary_union(polygons)
        assert coverage.is_valid
        parts = list(coverage.geoms) if coverage.geom_type == 'MultiPolygon' else [coverage]
        parts.sort(key=lambda q: (q.bounds, q.area))
        shapes[cid] = []
        for poly in parts:
            index = len(regions)
            regions.append(dict(id=index, sourceSurfaceCid=cid, bounds=list(poly.bounds),
                rectangle=poly.equals(box(*poly.bounds)), area=poly.area,
                exterior=[list(q) for q in poly.exterior.coords],
                holes=[list(map(list, ring.coords)) for ring in poly.interiors]))
            shapes[cid].append((index, poly))
    records = []
    for item in mapping['structureSamplers']:
        sample = item['originalSampling']
        native_face = None
        if sample['triangle']:
            q = np.asarray([sample[k] for k in ('a','b','c')], dtype=np.float32).astype(np.float64)
            shape = Polygon(q)
            native_face = native_triangles[item['sourceCid']].get(tuple(sorted(map(tuple, q))))
        else:
            shape = box(*np.asarray(sample['bounds'], dtype=np.float32).astype(np.float64))
        candidates = [index for index, poly in shapes[item['sourceCid']] if poly.covers(shape)]
        union_covers = bool(candidates)
        if not candidates and native_face is not None and shape.area > 0:
            # Exact source-triangle membership is authoritative when GEOS union
            # intersection rounding loses a boundary sliver. No epsilon or source
            # coordinate movement is introduced. Associate its interior region.
            candidates = [index for index, poly in shapes[item['sourceCid']]
                          if poly.covers(shape.representative_point())]
            assert len(candidates) == 1
        records.append(dict(key=item['key'], kind='structure', sourceSurfaceCid=item['sourceCid'],
            chartIds=candidates, coveredByNativeSource=bool(candidates) or native_face is not None,
            strictUnionCovers=union_covers, exactNativeFace=native_face,
            degenerateNativeUv=shape.area == 0, originalSampling=sample))
    for item in mapping['repeatSamplers']:
        shape = box(*(item['min']+item['max']))
        candidates = [index for index, poly in shapes[item['sourceCid']] if poly.covers(shape)]
        records.append(dict(kind='fixed-road-repeat', sourceSurfaceCid=item['sourceCid'],
            chartIds=candidates, coveredByNativeSource=bool(candidates), originalSampling=item))
    # Polygon unions may create non-Float32 intersection coordinates. F64 keeps
    # those computed boundaries, rather than silently moving them to float32.
    source_order = sorted(source_info)
    source_index = {cid:index for index,cid in enumerate(source_order)}
    # Fixed source/sampler hash tables are uint32 arrays; region/face/ring indices
    # are int32; computed polygon boundaries are float64. No string initializer.
    words = []
    for cid in source_order:
        words.extend(struct.unpack('<4I', bytes.fromhex(cid)))
    ints = [2, len(source_order), len(regions), len(records)]
    doubles = []
    for region in regions:
        rings = [region['exterior']] + region['holes']
        ints.extend([region['id'], source_index[region['sourceSurfaceCid']], int(region['rectangle']), len(rings)])
        for ring in rings:
            ints.extend([len(doubles)//2, len(ring)])
            doubles.extend(v for point in ring for v in point)
    for record in records:
        key = record.get('key')
        words.extend(struct.unpack('<8I', bytes.fromhex(key)) if key else [0]*8)
        native_face = record.get('exactNativeFace')
        mesh_entry = next((i for i,e in enumerate(evidence) if native_face is not None
                          and e['render']==native_face['render']
                          and e['materialSlot']==native_face['mesh']), -1)
        ints.extend([source_index[record['sourceSurfaceCid']],
                     int(record['kind']=='fixed-road-repeat'), mesh_entry,
                     native_face['face'] if native_face is not None else -1])
        ints.append(len(record['chartIds']))
        ints.extend(record['chartIds'])
    data = struct.pack('<II6i', 0x314e4242, 1, len(ints), len(words), 0, 0, 0, len(doubles))
    data += np.asarray(ints, dtype='<i4').tobytes()+np.asarray(words, dtype='<u4').tobytes()+np.asarray(doubles, dtype='<f8').tobytes()
    data += hashlib.sha256(data).digest()
    a.output.mkdir(parents=True)
    (a.output/'Lift05NativeCharts.bin').write_bytes(data)
    summary = dict(sourceSurfaces=len(by_source), renderInputs=len(renders), materialSlots=len(evidence),
        sourceRegions=len(regions), rectangularRegions=sum(r['rectangle'] for r in regions),
        samplers=len(records), coveredSamplers=sum(r['coveredByNativeSource'] for r in records),
        uncoveredSamplers=sum(not r['coveredByNativeSource'] for r in records),
        exactNativeFaceSamplers=sum(r.get('exactNativeFace') is not None for r in records),
        degenerateNativeUvSamplers=sum(r.get('degenerateNativeUv', False) for r in records),
        newTexturePayloads=0, nativeBbnBytes=len(data))
    result = dict(schema='Lift05/NativeUvCharts/2', sourceOrder=source_order, sources=list(source_info.values()),
        evidence=evidence, regions=regions, samplers=records, summary=summary,
        semantics='Exact authored UV triangle coverage union per original Surface identity, not mesh connectivity. Overlapping islands share texels. Holes are retained. No bounding-box expansion or coordinate snapping.',
        integration='Retain original Surface/Texture/VT graph. Map original sampler to recorded source coverage, then apply actual receiver mapping. Split only required true UV seams. Rectangle coverage is not evidence of seamless pixel repetition.',
        limits='These are source coverage facts, not native filtered sampling or persisted rendering acceptance. Uncovered sampler regions require original-part mapping; the former triangle-fold bake is not prototype evidence.')
    (a.output/'charts.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
