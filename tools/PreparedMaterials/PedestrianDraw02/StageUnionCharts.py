"""Stage verified authored UV unions without changing production data or pixels."""
import hashlib
import argparse
import json
from pathlib import Path
import sys
from fractions import Fraction
import numpy as np
from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools/GeometryMetaprogram"))
from MovableUvDomainFormat import decode_arrays
BASE = ROOT / "artifacts/prepared-materials/PedestrianDraw02"
sys.path.insert(0, str(ROOT / "artifacts/agent-material-env/PedestrianDraw02/python"))
import shapely
from shapely.geometry import Polygon
from shapely.ops import unary_union


def cross(a, b, p):
    return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])


def on(a, b, p):
    return cross(a, b, p) == 0 and all(min(a[i], b[i]) <= p[i] <= max(a[i], b[i]) for i in range(2))


def build(triangles, allow_exact_intersections=False):
    # Coordinates are original source points or, in the explicit extended mode,
    # exactly representable source-edge intersections. Rational boundary checks
    # reject rounded intersections rather than adding slack.
    positive_triangles = [t for t in triangles if cross(*t) != 0]
    polygons = [Polygon(t) for t in positive_triangles]
    if not polygons:
        return [], {"status": "no-positive-source-triangles"}
    union = unary_union(polygons)
    assert union.is_valid and union.geom_type in ("Polygon", "MultiPolygon")
    source_points = {tuple(float(x) for x in p) for t in triangles for p in t}
    parts = [union] if union.geom_type == "Polygon" else list(union.geoms)
    charts = [[list(r.coords)[:-1] for r in (part.exterior, *part.interiors)] for part in parts]
    output_points = {tuple(p) for loops in charts for ring in loops for p in ring}
    new_points = output_points - source_points
    if new_points and not allow_exact_intersections:
        return [], {"status": "new-intersection-coordinate-rejected"}
    rational = {p: tuple(Fraction(x) for x in p) for p in source_points | output_points}
    edges = [(rational[tuple(a)], rational[tuple(b)]) for t in positive_triangles for a, b in zip(t, np.roll(t, -1, axis=0))]
    if any(not any(on(a, b, rational[p]) for a, b in edges) for p in new_points):
        return [], {"status": "rounded-intersection-coordinate-rejected"}
    for loops in charts:
        for ring in loops:
            for a, b in zip(ring, ring[1:] + ring[:1]):
                a, b = rational[tuple(a)], rational[tuple(b)]
                points = sorted({p for p in rational.values() if on(a, b, p)})
                if not all(any(on(c, d, p) and on(c, d, q) for c, d in edges) for p, q in zip(points, points[1:])):
                    return [], {"status": "boundary-not-on-original-source-edges"}
    assert all(union.covers(p) for p in polygons)
    restored = unary_union([Polygon(c[0], c[1:]) for c in charts])
    assert restored.symmetric_difference(union).area == 0
    intersections = []
    for p in sorted(new_points):
        intersections.append({"position": list(p),
                              "exactRationalCoordinates": [[str(x.numerator), str(x.denominator)] for x in rational[p]],
                              "originalSourceEdges": [[list(map(float, a)), list(map(float, b))]
                                                       for a, b in edges if on(a, b, rational[p])]})
    return charts, {"status": "verified", "sourceTriangles": len(polygons),
                    "sourceTriangleAreaSum": sum(p.area for p in polygons), "unionArea": union.area,
                    "polygons": len(parts), "holes": sum(len(p.interiors) for p in parts),
                    "exactExistingBoundaryCoordinates": not new_points,
                    "exactRepresentableSourceEdgeIntersections": len(new_points), "exactRationalSourceEdgeCoverage": True,
                    "derivedIntersections": intersections,
                    "allSourceTrianglesCovered": True, "roundTripSymmetricDifferenceArea": 0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exact-intersections", action="store_true", help="Write the separate exact-intersection candidate directory; no production writes.")
    args = parser.parse_args()
    meta_path = BASE / "source-consolidation/native-charts.json"
    npz_path = BASE / "source-consolidation/native-charts.npz"
    meta = json.loads(meta_path.read_text())
    data = np.load(npz_path)
    evidence_path = BASE / "source-consolidation/embedded-chart-report.json"
    existing = {(r["name"], r["mesh"]): r["domains"] for r in json.loads(evidence_path.read_text())["streams"]}
    domains, recipe_hashes = [], {}
    for record in meta["records"]:
        render, mesh, prefix = record["name"], record["mesh"], record["arrayPrefix"]
        recipe = ROOT / "assets/HardcodedNumbers/PedestrianDraw02" / ("UvDomain_" + render.replace(" ", "_") + "_" + str(mesh) + ".bin")
        recipe_hashes[str(recipe.relative_to(ROOT))] = hashlib.sha256(recipe.read_bytes()).hexdigest()
        values = numbers(recipe)
        ints, _bounds = decode_arrays(values["I32"], values["F32"])
        assert ints[0] == -2 and len(existing[render, mesh]) == ints[1]
        at = 2
        for domain in range(ints[1]):
            count = ints[at+3]
            at += 4
            members = ints[at:at+abs(count)]
            at += abs(count)
            if not existing[render, mesh][domain]:
                chart_ids = sorted({int(data[prefix + "_face_chart"][f]) for f in members})
                domains.append({"render": render, "mesh": mesh, "originalDomain": domain, "originalChartIds": chart_ids})
        assert at == len(ints)
    records = {(r["name"], r["mesh"]): r for r in meta["records"]}
    cache, audit, overrides = {}, [], []
    for d in domains:
        record = records[d["render"], d["mesh"]]
        prefix = record["arrayPrefix"]
        candidates, rejected = [], []
        for chart_id in d["originalChartIds"]:
            key = (d["render"], d["mesh"], chart_id)
            if key not in cache:
                members = record["charts"][chart_id]["faces"]
                triangles = data[prefix + "_uv"][data[prefix + "_faces"][members]].astype(float)
                charts, proof = build(triangles, args.exact_intersections)
                cache[key] = charts
                audit.append({"render": d["render"], "mesh": d["mesh"], "chart": chart_id,
                              "sourceFaceIds": members, "sourceDigest": record["sourceGraph"]["sha256"], **proof})
            if not cache[key]:
                rejected.append(chart_id)
            candidates.extend(cache[key])
        # Do not publish a partial chart set for a domain that requires a rejected
        # chart; retain its existing behavior until complete source coverage exists.
        if candidates and not rejected:
            overrides.append({"style": "PedestrianDraw02", "render": d["render"], "mesh": d["mesh"],
                              "originalDomain": d["originalDomain"], "charts": candidates})
    out = BASE / ("exact-union-chart-candidate" if args.exact_intersections else "union-chart-candidate")
    out.mkdir(exist_ok=True)
    (out / "chart-overrides.json").write_text(json.dumps(overrides, separators=(",", ":")) + "\n")
    report = {"style": "PedestrianDraw02", "tool": "Shapely " + shapely.__version__,
              "allowExactRepresentableIntersections": args.exact_intersections,
              "domainOverrides": len(overrides), "originalChartCount": len(audit), "recipeHashes": recipe_hashes,
              "verifiedCharts": sum(a["status"] == "verified" for a in audit),
              "overrideSha256": hashlib.sha256((out / "chart-overrides.json").read_bytes()).hexdigest(),
              "inputs": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in
                         (meta_path, npz_path, evidence_path)}, "charts": audit,
              "selection": "Only domains with absent original candidates; source-shape verification, not scenario gains, determines admission.",
              "limits": "Staged UV coverage only. Original native channel/mip references are unchanged. Production mapping, native rendering and integration remain unverified."}
    (out / "source-evidence.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: report[k] for k in ("domainOverrides", "originalChartCount", "verifiedCharts")}))


if __name__ == "__main__":
    main()
