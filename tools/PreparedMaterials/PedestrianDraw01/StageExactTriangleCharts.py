"""Stage source-exact coverage for domains without reliable charts.

The production BBN is never modified. Each added chart is one positive-area
original UV triangle, not a bounding box or inferred repeat region. Separate
charts use OR containment, so overlaps cannot cancel and holes cannot be filled.
Existing reliable domain charts are not replaced.
With --native-union, stage full physical-source unions only when every contour
vertex is already a native UV vertex and every contour edge is exactly covered
by original collinear triangle edges. Preserve disconnected regions and holes.
With --exact-intersections, also admit exactly representable intersections of
nonparallel original edges, verifying rational union area independently.
"""
from collections import Counter
from fractions import Fraction
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys

import numpy as np
from ExportRecipes import numbers
from ExportNativeCharts import normalized_triangle
sys.path.insert(0, str(Path(__file__).resolve().parents[3]/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repository', type=Path, default=Path(__file__).resolve().parents[3])
parser.add_argument('--native-union', action='store_true')
parser.add_argument('--exact-intersections', action='store_true')
options = parser.parse_args()
ROOT = options.repository.resolve()
STYLE = 'PedestrianDraw01'
OUT = ROOT / 'artifacts/prepared-materials' / STYLE / 'exact-triangle-charts'
UNION = options.native_union or options.exact_intersections
if UNION:
    OUT = OUT.with_name('exact-source-intersections' if options.exact_intersections else 'exact-source-union')
    sys.path.insert(0, str(ROOT/'artifacts/agent-material-env'/STYLE/'python-deps'))
    from shapely.geometry import Polygon
    from shapely.ops import unary_union


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cross(t):
    a, b, c = t
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def exact_boundary_edges(loops, triangles):
    edges = [(a, b) for t in triangles for a, b in zip(t, t[1:]+t[:1])]
    edges = [(tuple(map(Fraction, a)), tuple(map(Fraction, b))) for a, b in edges]
    checked = 0
    for loop in loops:
        for af, bf in zip(loop, loop[1:]+loop[:1]):
            a, b = tuple(map(Fraction, af)), tuple(map(Fraction, bf))
            axis = 0 if a[0] != b[0] else 1
            assert a != b
            intervals = []
            for p, q in edges:
                if cross((a, b, p)) != 0 or cross((a, b, q)) != 0:
                    continue
                t, u = sorted(((p[axis]-a[axis])/(b[axis]-a[axis]),
                               (q[axis]-a[axis])/(b[axis]-a[axis])))
                lo, hi = max(Fraction(0), t), min(Fraction(1), u)
                if lo < hi:
                    intervals.append((lo, hi))
            covered = Fraction(0)
            for lo, hi in sorted(intervals):
                if lo > covered:
                    return None
                covered = max(covered, hi)
            if covered != 1:
                return None
            checked += 1
    return checked


def exact_intersections(points, triangles):
    """Admit derived points only at exact intersections of original edge lines."""
    edges = [(tuple(map(Fraction, a)), tuple(map(Fraction, b)))
             for t in triangles for a, b in zip(t, t[1:]+t[:1])]
    result = []
    for original in sorted(points):
        p = tuple(map(Fraction, original))
        supporting = [(a, b) for a, b in edges if cross((a, b, p)) == 0
                      and all(min(a[i], b[i]) <= p[i] <= max(a[i], b[i]) for i in (0, 1))]
        distinct_lines = any((b[0]-a[0])*(d[1]-c[1]) != (b[1]-a[1])*(d[0]-c[0])
                             for a, b in supporting for c, d in supporting)
        if not distinct_lines:
            return None
        result.append(dict(position=original,
                           rational=[[str(x.numerator), str(x.denominator)] for x in p],
                           originalEdges=[[list(map(float, a)), list(map(float, b))] for a, b in supporting]))
    return result


def exact_union_area(triangles):
    """Integrate triangle-union vertical intervals between exact edge events."""
    triangles = [tuple(tuple(map(Fraction, p)) for p in t) for t in triangles]
    edges = sorted({tuple(sorted((a, b))) for t in triangles for a, b in zip(t, t[1:]+t[:1])})
    events = {p[0] for t in triangles for p in t}
    for i, (a, b) in enumerate(edges):
        dx, dy = b[0]-a[0], b[1]-a[1]
        for c, d in edges[i+1:]:
            if any(max(a[j], b[j]) < min(c[j], d[j]) or max(c[j], d[j]) < min(a[j], b[j]) for j in (0, 1)):
                continue
            ex, ey = d[0]-c[0], d[1]-c[1]
            determinant = dx*ey-dy*ex
            if determinant == 0:
                continue
            t = ((c[0]-a[0])*ey-(c[1]-a[1])*ex)/determinant
            u = ((c[0]-a[0])*dy-(c[1]-a[1])*dx)/determinant
            if 0 <= t <= 1 and 0 <= u <= 1:
                events.add(a[0]+t*dx)
    ordered = sorted(events)
    area = Fraction(0)
    for left, right in zip(ordered, ordered[1:]):
        x = (left+right)/2
        intervals = []
        for triangle in triangles:
            ys = [a[1]+(x-a[0])*(b[1]-a[1])/(b[0]-a[0])
                  for a, b in zip(triangle, triangle[1:]+triangle[:1])
                  if min(a[0], b[0]) < x < max(a[0], b[0])]
            if ys:
                intervals.append((min(ys), max(ys)))
        merged = []
        for lo, hi in sorted(intervals):
            if merged and lo <= merged[-1][1]:
                merged[-1] = (merged[-1][0], max(merged[-1][1], hi))
            else:
                merged.append((lo, hi))
        area += (right-left)*sum((hi-lo for lo, hi in merged), Fraction(0))
    return area


def exact_ring_area(coordinates):
    points = [tuple(map(Fraction, p)) for p in list(coordinates)[:-1]]
    return abs(sum((a[0]*b[1]-a[1]*b[0] for a, b in zip(points, points[1:]+points[:1])), Fraction(0))/2)


def main():
    evidence = ROOT / 'artifacts/prepared-materials' / STYLE / 'shared-sampling/native-charts.json'
    source = json.loads(evidence.read_text())
    bbn = ROOT / 'assets/HardcodedNumbers' / STYLE / ('UvCharts_'+STYLE+'.bin')
    values = numbers(bbn)
    ints = values['I32']
    doubles = list(values['F64'])
    assert ints[0] == len(source['meshes'])
    at = 2 + ints[1]
    empty, original_domains = set(), []
    for mesh in source['meshes']:
        mesh_domains = []
        assert ints[at] == len(mesh['domainCharts'])
        at += 1
        for domain in mesh['domainCharts']:
            count = ints[at]
            at += 1
            if count == 0:
                empty.add((mesh['render'], mesh['mesh'], domain['domain']))
            mesh_domains.append(list(ints[at:at+count]))
            at += count
        original_domains.append(mesh_domains)
    assert at == ints[2]

    needed = {key for mesh in source['meshes'] for d in mesh['domainCharts']
              if (mesh['render'], mesh['mesh'], d['domain']) in empty for key in d['chartKeys']}
    triangles, native, source_hashes, occurrences = {}, {}, {}, Counter()
    union_charts, union_rejected = {}, []
    intersection_proofs, rounded_rejected, edge_rejected = {}, [], []
    exact_area_proofs = {}
    checked_boundary_edges = 0
    face_occurrences = {}
    expected = {m['render']: m['nativeMeshSHA256'] for m in source['meshes']}
    for oi, occurrence in enumerate(source['occurrences']):
        key = occurrence['chartKey']
        if key not in needed:
            continue
        name, mesh = occurrence['render'], occurrence['mesh']
        if name not in native:
            path = ROOT / 'artifacts/uv-density/native' / (name+'.npz')
            source_hashes[name] = digest(path)
            assert source_hashes[name] == expected[name]
            native[name] = np.load(path)
        data = native[name]
        uv, faces = data[f'mesh{mesh}_uv0'], data[f'mesh{mesh}_faces']
        original = sorted(set(normalized_triangle(uv[faces[f]]) for f in occurrence['faces']))
        positive = [t for t in original if cross(t) != 0]
        triangles[oi] = positive
        if UNION and positive:
            shape = unary_union([Polygon(t) for t in positive])
            polygons = list(shape.geoms) if shape.geom_type == 'MultiPolygon' else [shape]
            assert shape.is_valid and all(p.geom_type == 'Polygon' for p in polygons)
            loops = [list(r.coords)[:-1] for p in polygons for r in [p.exterior, *p.interiors]]
            native_vertices = {p for t in positive for p in t}
            derived = {p for loop in loops for p in loop} - native_vertices
            proof = exact_intersections(derived, positive) if options.exact_intersections and derived else []
            boundary_count = exact_boundary_edges(loops, positive) if not derived or options.exact_intersections else None
            if derived and options.exact_intersections and proof is None:
                rounded_rejected.append(oi)
            elif (not derived or options.exact_intersections) and boundary_count is not None:
                checked_boundary_edges += boundary_count
                restored_shape = Polygon()
                for loop in loops:
                    restored_shape = restored_shape.symmetric_difference(Polygon(loop))
                assert restored_shape.equals(shape)
                assert all(Polygon(t).difference(shape).is_empty for t in positive)
                union_charts[oi] = loops
                if derived:
                    original_area = exact_union_area(positive)
                    represented_area = sum((exact_ring_area(p.exterior.coords)-sum(
                        (exact_ring_area(r.coords) for r in p.interiors), Fraction(0)) for p in polygons), Fraction(0))
                    assert original_area == represented_area
                    intersection_proofs[oi] = proof
                    exact_area_proofs[oi] = dict(numerator=str(original_area.numerator),
                                                denominator=str(original_area.denominator),
                                                triangleUnionEqualsContourArea=True)
            else:
                union_rejected.append(oi)
                if not derived or options.exact_intersections:
                    edge_rejected.append(oi)
        for face in occurrence['faces']:
            fk = (name, mesh, face)
            assert fk not in face_occurrences
            face_occurrences[fk] = oi
        occurrences[key] += 1
    assert needed == set(occurrences)

    overrides, per_mesh = [], Counter()
    for mesh in source['meshes']:
        domain_path = ROOT/'assets/HardcodedNumbers'/STYLE/('UvDomain_'+mesh['render'].replace(' ', '_')+'_'+str(mesh['mesh'])+'.bin')
        encoded_domain = numbers(domain_path)
        measured = decode_arrays(encoded_domain['I32'], encoded_domain['F32'])[0].tolist()
        assert measured[0] == -2 and measured[1] == len(mesh['domainCharts'])
        face_at = 2
        for domain in mesh['domainCharts']:
            count = abs(measured[face_at+3])
            face_at += 4
            faces = measured[face_at:face_at+count]
            face_at += count
            identity = (mesh['render'], mesh['mesh'], domain['domain'])
            if identity not in empty:
                continue
            physical = {face_occurrences[(mesh['render'], mesh['mesh'], face)] for face in faces}
            assert {source['occurrences'][oi]['chartKey'] for oi in physical} == set(domain['chartKeys'])
            unique = sorted({t for oi in physical for t in triangles[oi]})
            if not unique:
                continue
            charts = ([union_charts[oi] for oi in sorted(physical) if oi in union_charts]
                      if UNION else [[list(t)] for t in unique])
            if not charts:
                continue
            overrides.append(dict(style=STYLE, render=mesh['render'], mesh=mesh['mesh'],
                                  originalDomain=domain['domain'], charts=charts))
            per_mesh[mesh['render']+'|'+str(mesh['mesh'])] += 1
        assert face_at == len(measured)
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / 'overrides.json'
    path.write_text(json.dumps(overrides, separators=(',', ':'))+'\n', encoding='utf-8')
    # Stage a deduplicated BBN with the existing schema. Existing chart loops and
    # references remain intact; only previously empty domain lists receive refs.
    chart_records = []
    for ci in range(ints[1]):
        start = ints[2+ci]
        chart_records.append(list(ints[start:start+1+2*ints[start]]))
    candidate_domains = [[list(d) for d in m] for m in original_domains]
    mesh_indices = {(m['render'], m['mesh']): i for i, m in enumerate(source['meshes'])}
    triangle_ids = {}
    for override in overrides:
        mi = mesh_indices[override['render'], override['mesh']]
        refs = candidate_domains[mi][override['originalDomain']]
        assert not refs
        for chart in override['charts']:
            key = json.dumps(chart, separators=(',', ':'))
            if key not in triangle_ids:
                triangle_ids[key] = len(chart_records)
                record = [len(chart)]
                for loop in chart:
                    record.extend([len(doubles), len(loop)])
                    doubles.extend(x for p in loop for x in p)
                chart_records.append(record)
            refs.append(triangle_ids[key])
    encoded = [len(candidate_domains), len(chart_records)] + [0]*len(chart_records)
    for mesh in candidate_domains:
        encoded.append(len(mesh))
        for refs in mesh:
            encoded.extend([len(refs), *refs])
    for ci, record in enumerate(chart_records):
        encoded[2+ci] = len(encoded)
        encoded.extend(record)
    raw = b'BBN1'+struct.pack('<7i', 1, len(encoded), 0, 0, 0, 0, len(doubles))
    raw += struct.pack('<'+'i'*len(encoded), *encoded)+struct.pack('<'+'d'*len(doubles), *doubles)
    candidate_path = OUT / ('UvCharts_'+STYLE+'.bin')
    candidate_path.write_bytes(raw+hashlib.sha256(raw).digest())
    restored = numbers(candidate_path)
    assert list(restored['I32']) == encoded
    assert struct.pack('<'+'d'*len(values['F64']), *restored['F64'][:len(values['F64'])]) == struct.pack('<'+'d'*len(values['F64']), *values['F64'])
    report = dict(sourceChartsSHA256=digest(evidence), baselineBBNSHA256=digest(bbn),
                  nativeNPZSHA256=source_hashes, existingEmptyDomains=len(empty),
                  candidateDomains=len(overrides), candidateCharts=sum(len(r['charts']) for r in overrides),
                  remainingEmptyDomains=len(empty)-len(overrides), perMesh=dict(per_mesh),
                  checkedPhysicalOccurrences=sum(occurrences.values()),
                  candidateBBNSHA256=digest(candidate_path), candidateBBNBytes=candidate_path.stat().st_size,
                  newUniqueCharts=len(triangle_ids),
                  nativeVertexUnionMode=UNION and not options.exact_intersections,
                  unionOccurrencesRejectedForNewVertices=union_rejected,
                  exactIntersectionsAllowed=options.exact_intersections,
                  exactIntersectionProofs=intersection_proofs,
                  exactRationalUnionAreaProofs=exact_area_proofs,
                  roundedIntersectionOccurrencesRejected=rounded_rejected,
                  unsupportedBoundaryOccurrencesRejected=edge_rejected,
                  exactRationalBoundaryEdgesVerified=checked_boundary_edges,
                  overridesSHA256=digest(path), overridesBytes=path.stat().st_size,
                  existingReliableDomainChartsUnchanged=True, productionModified=False,
                  scope='Exact original source coverage only; no repeat inference, pixel conversion, or physical mesh cuts.')
    (OUT/'stage.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
