"""Stage exact authored UV unions. Requires NumPy and Shapely on PYTHONPATH."""
import sys,json,hashlib
from pathlib import Path
from fractions import Fraction
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/PreparedMaterials/Lift03'))
import numpy as np
import shapely
from shapely.geometry import Polygon
from shapely.ops import unary_union
from ExportRecipes import numbers
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


BASE=ROOT/'artifacts/prepared-materials/Lift03'
inputs=BASE/'native-charts'
runtime=json.loads((inputs/'runtime-integration.json').read_text())
audit=[];overrides=[];hashes={}
for stream in runtime['streams']:
 rejected=set(map(int,stream['rejected']))
 if not rejected:continue
 meta_path=inputs/(stream['stream']+'.json');bbn_path=inputs/(stream['stream']+'.bin')
 for p in (meta_path,bbn_path):hashes[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 meta=json.loads(meta_path.read_text());data=numbers(bbn_path)
 uv=np.asarray(data['F32'],dtype=np.float64).reshape(-1,2);ints=data['I32'];faces=np.asarray(ints[4:4+ints[2]*3],dtype=np.int32).reshape(-1,3)
 absent=[d for d in meta['domains'] if d['charts'] and all(c in rejected for c in d['charts'])]
 needed=sorted(set(c for d in absent for c in d['charts']));cache={}
 for c in needed:
  original=meta['charts'][c];triangles=uv[faces[original['triangles']]]
  candidate,proof=build(triangles,True);cache[c]=candidate
  audit.append(dict(render=meta['name'],mesh=meta['mesh'],chart=c,sourceCid=meta['sourceCid'],originalTriangleIds=original['triangles'],originalRejection=stream['rejected'][str(c)],**proof))
 for d in absent:
  if all(cache[c] for c in d['charts']):overrides.append(dict(style='Lift03',render=meta['name'],mesh=meta['mesh'],originalDomain=d['domain'],charts=[chart for c in d['charts'] for chart in cache[c]]))
 print(stream['stream'],len(needed),sum(bool(v) for v in cache.values()),flush=True)
out=BASE/'exact-union-chart-candidate';out.mkdir(exist_ok=True)
(out/'chart-overrides.json').write_text(json.dumps(overrides,separators=(',',':'))+'\n')
report=dict(style='Lift03',domains=len(overrides),charts=len(audit),verifiedCharts=sum(a['status']=='verified' for a in audit),sourceHashes=hashes,audit=audit,productionFilesChanged=False,selection='Every source domain with no accepted original chart; selection independent of receiver scenario gains. Physical chart membership remains unchanged.',scope='Exact source union coverage only. No geometry, pixels, channel identities, mips, gradient thresholds or repeat inference changed.')
(out/'source-evidence.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ('domains','charts','verifiedCharts')}))
