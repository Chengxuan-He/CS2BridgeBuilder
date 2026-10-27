"""Stage exact source-triangle charts for captured domains lacking reliable loops.

No production resource is written. Existing valid chart candidates are untouched.
Each added chart is exactly one nondegenerate original UV triangle, never its box.
"""
import gzip
import hashlib
import json
import sys
from pathlib import Path
import numpy as np
from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools/GeometryMetaprogram"))
from MovableUvDomainFormat import decode_arrays
BASE = ROOT / "artifacts/prepared-materials/PedestrianDraw02"


def main():
    capture = ROOT / "artifacts/prepared-materials/shared-current/receivers/PedestrianDraw02.units.ndjson.gz"
    source_file = BASE / "source-consolidation/native-charts.npz"
    metadata_file = BASE / "source-consolidation/native-charts.json"
    metadata = json.loads(metadata_file.read_text())
    data = np.load(source_file)
    needed = {}
    with gzip.open(capture, "rt") as stream:
        for line in stream:
            unit = json.loads(line)
            if not unit["charts"]:
                key = (unit["name"], unit["mesh"], unit["originalDomain"])
                row = needed.setdefault(key, {"sourceFaces": set(), "capturedUnits": 0})
                row["sourceFaces"].update(face[0] for face in unit["sourceFaces"])
                row["capturedUnits"] += 1
    outputs, evidence, recipe_hashes = [], [], {}
    for record in metadata["records"]:
        render, mesh, prefix = record["name"], record["mesh"], record["arrayPrefix"]
        recipe = ROOT / "assets/HardcodedNumbers/PedestrianDraw02" / ("UvDomain_" + render.replace(" ", "_") + "_" + str(mesh) + ".bin")
        recipe_hashes[str(recipe.relative_to(ROOT))] = hashlib.sha256(recipe.read_bytes()).hexdigest()
        values = numbers(recipe)
        ints, _bounds = decode_arrays(values["I32"], values["F32"])
        assert ints[0] == -2
        at = 2
        uv, faces, membership = (data[prefix + "_" + k] for k in ("uv", "faces", "face_chart"))
        for domain in range(ints[1]):
            a, b, c, count = ints[at:at+4]
            at += 4
            members = ints[at:at+abs(count)]
            at += abs(count)
            key = (render, mesh, domain)
            if key not in needed:
                continue
            assert needed[key]["sourceFaces"] <= set(members), key
            chart_ids = sorted({int(membership[f]) for f in members})
            source_faces = sorted({f for ci in chart_ids for f in record["charts"][ci]["faces"]})
            triangles, kept, degenerate = [], [], []
            seen = set()
            for face in source_faces:
                triangle = uv[faces[face]].astype(np.float64)
                p, q, r = triangle
                area2 = (q[0]-p[0])*(r[1]-p[1])-(q[1]-p[1])*(r[0]-p[0])
                if area2 == 0:
                    degenerate.append(face)
                    continue
                assert np.isfinite(triangle).all()
                identity = tuple(sorted(tuple(p) for p in triangle))
                if identity in seen:
                    continue
                seen.add(identity)
                triangles.append([triangle.tolist()])
                kept.append(face)
            if triangles:
                outputs.append({"style": "PedestrianDraw02", "render": render, "mesh": mesh,
                                "originalDomain": domain, "charts": triangles})
            evidence.append({"render": render, "mesh": mesh, "originalDomain": domain,
                             "sourceSurfaceCid": record["sourceSurfaceCid"],
                             "sourceDigest": record["sourceGraph"]["sha256"],
                             "originalChartIds": chart_ids, "sourceTriangleFaces": kept,
                             "omittedDegenerateFaces": degenerate,
                             "capturedUnits": needed[key]["capturedUnits"],
                             "charts": len(triangles)})
        assert at == len(ints)
    out = BASE / "triangle-chart-candidate"
    out.mkdir(exist_ok=True)
    (out / "chart-overrides.json").write_text(json.dumps(outputs, separators=(",", ":")) + "\n")
    report = {"style": "PedestrianDraw02", "scope": "staged exact source triangles for empty captured chart sets only",
              "captureSha256": hashlib.sha256(capture.read_bytes()).hexdigest(),
              "sourceNpzSha256": hashlib.sha256(source_file.read_bytes()).hexdigest(),
              "sourceMetadataSha256": hashlib.sha256(metadata_file.read_bytes()).hexdigest(),
              "recipeHashes": recipe_hashes, "domains": len(evidence),
              "domainsWithCandidate": len(outputs), "capturedUnits": sum(e["capturedUnits"] for e in evidence),
              "triangleCandidates": sum(e["charts"] for e in evidence), "evidence": evidence,
              "verification": {"sourceFaceMembership": True, "originalTriangleCoordinatesUnchanged": True,
                               "nondegenerateFiniteTrianglesOnly": True, "existingValidChartsChanged": False,
                               "runtimeCoverageMeasured": False, "productionFilesChanged": False},
              "limits": ["Each chart has exactly its original triangle area and no added holes or bounding-box coverage.",
                         "This fallback deliberately does not combine triangles into a larger union.",
                         "All original channels, native identities and mips remain original references; no pixels are changed.",
                         "Compare through production CanUseSource before integrating; no tolerance or RepeatOnly bypass is authorized."]}
    (out / "source-evidence.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: report[k] for k in ("domains", "domainsWithCandidate", "capturedUnits", "triangleCandidates")}))


if __name__ == "__main__":
    main()
