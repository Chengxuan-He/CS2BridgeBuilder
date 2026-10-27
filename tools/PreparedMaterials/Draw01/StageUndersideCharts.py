"""Stage missing charts for existing measured underside UV corrections; no formal writes."""
import collections
import hashlib
import json
import sys
from fractions import Fraction
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, read_bbn, domains
from shapely.geometry import Polygon
from shapely.ops import unary_union


def on(a, b, p):
    return ((b[0]-a[0])*(p[1]-a[1]) == (b[1]-a[1])*(p[0]-a[0])
            and all(min(a[i], b[i]) <= p[i] <= max(a[i], b[i]) for i in range(2)))


def verified_union(triangles):
    polygons = [Polygon(t) for t in triangles if Polygon(t).area > 0]
    union = unary_union(polygons)
    assert union.is_valid and union.geom_type in ('Polygon', 'MultiPolygon')
    parts = [union] if union.geom_type == 'Polygon' else list(union.geoms)
    charts = [[list(r.coords)[:-1] for r in (p.exterior, *p.interiors)] for p in parts]
    source_points = {tuple(p) for t in triangles for p in t}
    result_points = {tuple(p) for c in charts for ring in c for p in ring}
    rational = {p: tuple(Fraction(float(x)) for x in p) for p in source_points | result_points}
    edges = [(rational[tuple(a)], rational[tuple(b)]) for t in triangles
             for a, b in zip(t, np.roll(t, -1, axis=0))]
    for p in result_points-source_points:
        if not any(on(a, b, rational[p]) for a, b in edges):
            return [], dict(status='rounded-intersection-rejected')
    for c in charts:
        for ring in c:
            for a, b in zip(ring, ring[1:]+ring[:1]):
                a, b = rational[tuple(a)], rational[tuple(b)]
                split = sorted({p for p in rational.values() if on(a, b, p)})
                if not all(any(on(c, d, p) and on(c, d, q) for c, d in edges)
                           for p, q in zip(split, split[1:])):
                    return [], dict(status='boundary-off-source-edge-rejected')
    if not all(union.covers(p) for p in polygons):
        return [], dict(status='source-triangle-coverage-rejected')
    restored = unary_union([Polygon(c[0], c[1:]) for c in charts])
    assert restored.symmetric_difference(union).area == 0
    return charts, dict(status='verified', sourceTriangles=len(polygons), unionArea=union.area,
                        holes=sum(len(p.interiors) for p in parts),
                        exactRepresentableIntersections=len(result_points-source_points),
                        exactRationalBoundaryOnSourceEdges=True, symmetricDifferenceArea=0)


def main():
    base = ROOT/'artifacts/prepared-materials/Draw01'
    inventory_path = base/'actual-retile/missing-source-charts.json'
    inventory = json.loads(inventory_path.read_text())
    correction_path = ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01UndersideUv_cs.bin'
    assert hashlib.sha256(correction_path.read_bytes()).hexdigest() == inventory['correctionSha256']
    ints, _, _, _, values, _ = read_bbn(correction_path)
    overrides, proofs, recipe_hashes = [], [], {}
    for render, offset, start, count in [('DrawBridge01 Mesh', 2, 0, 176), ('DrawBridge01_LOD1 Mesh', 178, 352, 158)]:
        requested = [d for d in inventory['correctedPositiveDomains'] if d['render'] == render]
        if not requested:
            continue
        recipe = ROOT/'assets/HardcodedNumbers/Draw01'/('UvDomain_'+render.replace(' ', '_')+'_0.bin')
        measured_domains = domains(recipe)
        recipe_hashes[str(recipe.relative_to(ROOT))] = hashlib.sha256(recipe.read_bytes()).hexdigest()
        for d in requested:
            assert measured_domains[d['domain']]['faces'] == d['faces']
        path = ROOT/'artifacts/uv-density/native'/(render+'.npz')
        assert hashlib.sha256(path.read_bytes()).hexdigest() == inventory['nativeHashes'][str(path.relative_to(ROOT))]
        native = np.load(path)
        positions, faces = native['mesh0_positions'], native['mesh0_faces']
        uv = native['mesh0_uv0'][:, :2].astype(float)
        uv[ints[offset:offset+count]] = values[start:start+count*2].reshape(-1, 2)
        vertices = [tuple(map(float, p))+tuple(map(float, q)) for p, q in zip(positions, uv)]
        parent = list(range(len(faces)))
        def find(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        edges = collections.defaultdict(list)
        for f, triangle in enumerate(faces):
            for a, b in zip(triangle, np.roll(triangle, -1)):
                edges[tuple(sorted((vertices[a], vertices[b])))].append(f)
        for connected in edges.values():
            if len(connected) == 2:
                parent[find(connected[1])] = find(connected[0])
        groups = collections.defaultdict(list)
        for f in range(len(faces)):
            groups[find(f)].append(f)
        cache = {}
        for d in requested:
            charts, rejected = [], False
            for component in sorted({find(f) for f in d['faces']}):
                if component not in cache:
                    members = groups[component]
                    candidate, proof = verified_union(uv[faces[members]])
                    cache[component] = candidate
                    proofs.append(dict(render=render, mesh=0, component=component, sourceFaces=members, **proof))
                charts.extend(cache[component])
                rejected |= not cache[component]
            if charts and not rejected:
                overrides.append(dict(style='Draw01', render=render, mesh=0, originalDomain=d['domain'], charts=charts))
    out = base/'underside-chart-candidate'
    out.mkdir(exist_ok=True)
    (out/'chart-overrides.json').write_text(json.dumps(overrides, separators=(',', ':'))+'\n')
    report = dict(status='STAGED', domains=len(overrides), sourceComponents=proofs,
                  domainRecipeHashes=recipe_hashes,
                  inventorySha256=hashlib.sha256(inventory_path.read_bytes()).hexdigest(),
                  overrideSha256=hashlib.sha256((out/'chart-overrides.json').read_bytes()).hexdigest(),
                  formalWrites=0, pixelsBaked=0,
                  scope='Only absent domains with existing measured positive-area underside UV corrections. Connectivity uses identical physical position and corrected UV endpoints; complete component unions preserve holes and exact source edges. Existing valid charts are unchanged.')
    (out/'source-evidence.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(dict(domains=len(overrides), components=len(proofs), statuses=collections.Counter(p['status'] for p in proofs))))


if __name__ == '__main__':
    main()
