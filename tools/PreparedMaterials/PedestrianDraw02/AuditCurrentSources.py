"""Verify retained original source bytes and the current prepared style subset.

This does not measure current production receiver coverage or a generated owner.
It performs no baking and never modifies native inputs or packaged assets.
"""
from collections import Counter
from contextlib import ExitStack
import json
from pathlib import Path
import re

from ExportSourceSampling import descriptor, digest


ROOT = Path(__file__).resolve().parents[3]
STYLE = "PedestrianDraw02"
BASE = ROOT / "artifacts/prepared-materials" / STYLE
ENV = ROOT / "artifacts/agent-material-env" / STYLE


def main():
    import zipfile

    manifest_path = ENV / "source-input/source-inputs.json"
    closure_path = BASE / "published-native-v2/closure.json"
    corpus_path = ROOT / "artifacts/uv-density/structure-tessellated-sources.json"
    renders_path = ROOT / "src/BridgeBuilder/Bridges/Types" / STYLE / (STYLE + "SourceRenders.cs")
    legacy_path = ROOT / "tools/MovablePlanUvCheck/Program.cs"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    closure = json.loads(closure_path.read_text(encoding="utf-8"))
    corpus = json.loads(corpus_path.read_text(encoding="utf-8"))
    uses = [{"render": m[1], "meshCid": m[2], "surfaceCids": re.findall(r'"([a-f0-9]+)"', m[3])}
            for m in re.finditer(r'Create\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+) \}',
                                 renders_path.read_text(encoding="utf-8-sig"))]
    assert uses == manifest["renderUses"], "Source render inventory changed"
    verified = []
    with ExitStack() as stack:
        archives = {}
        for asset in manifest["assets"]:
            payload = Path(asset["file"]).read_bytes()
            assert len(payload) == asset["bytes"] and digest(payload) == asset["sha256"], asset["cid"]
            assert Path(asset["file"] + ".cid").read_text(encoding="utf-8-sig").strip() == asset["cid"]
            for location in asset["locations"]:
                archive = archives.get(location["archive"])
                if archive is None:
                    archive = archives[location["archive"]] = stack.enter_context(zipfile.ZipFile(location["archive"]))
                assert archive.read(location["entry"]) == payload, location
                assert archive.read(location["entry"] + ".cid").decode("utf-8-sig").strip() == asset["cid"]
            verified.append({k: asset[k] for k in ("cid", "kind", "bytes", "sha256", "locations")})
    historical = [u for u in corpus["units"] if u["style"] == STYLE]
    sources = []
    for source in manifest["sources"]:
        payload = next(a for a in manifest["assets"] if a["cid"] == source["cid"])
        template, header, vt, bindings = descriptor(Path(payload["file"]).read_bytes())
        assert template == source["template"] and vt == source["vtSurfaceCid"] and bindings == source["textureBindings"]
        requests = [u for u in historical if u["sourceCid"] == source["cid"]]
        assert all(u["sourceDigest"] == source["sha256"] for u in requests)
        sources.append({**source, "historicalRequests": len(requests),
                        "uses": [{"render": u["render"], "mesh": i} for u in uses
                                 for i, cid in enumerate(u["surfaceCids"]) if cid == source["cid"]]})
    formal = ROOT / "assets/Materials" / STYLE
    files = sorted(p for p in formal.rglob("*") if p.is_file())
    hashes = {str(p.relative_to(formal)).replace("/", "\\"): digest(p.read_bytes()) for p in files}
    assert hashes == closure["fileHashes"], "Prepared material bytes differ from verified publication"
    empty = [str(p.relative_to(formal)) for p in formal.rglob("*") if p.is_dir() and not any(p.iterdir())]
    assert not empty, empty
    snapshot_differences = {}
    for snapshot in ("baker", "baker-current"):
        differences = []
        for p in sorted((ENV / snapshot / "tools/GpuBake").glob("*")):
            original = ROOT / "tools/GpuBake" / p.name
            if p.is_file() and original.is_file() and digest(p.read_bytes()) != digest(original.read_bytes()):
                differences.append(p.name)
        snapshot_differences[snapshot] = differences
    code = legacy_path.read_text(encoding="utf-8-sig")
    dll = ROOT / "src/BridgeBuilder/bin/Release/BridgeBuilder.dll"
    report = {
        "schema": "PedestrianDraw02CurrentSources/1",
        "status": "source-and-prepared-subset-verified; production-coverage-pending",
        "inputs": {str(p.relative_to(ROOT)): digest(p.read_bytes()) for p in
                   (manifest_path, closure_path, corpus_path, renders_path, legacy_path, dll)},
        "summary": {"renders": len(uses), "meshStreams": sum(len(u["surfaceCids"]) for u in uses),
                    "surfaceCids": len(sources), "surfaceDigestFamilies": len({s["sha256"] for s in sources}),
                    "verifiedOriginalFiles": len(verified), "verifiedOriginalBytes": sum(a["bytes"] for a in verified),
                    "historicalRequests": len(historical), "preparedFiles": len(files),
                    "preparedBytes": sum(p.stat().st_size for p in files),
                    "preparedKinds": dict(Counter(p.suffix for p in files))},
        "sources": sources, "verifiedOriginalAssets": verified,
        "legacyCheckerExactFieldMentions": {k: k in code for k in ("ExactCoordinates", "ExactPoints", "ExactSourceUvs")},
        "privateGpuBakeSnapshotDifferences": snapshot_differences,
        "preparedFileHashes": hashes,
        "generatedOwnerMaterialBytes": None,
        "limits": [
            "Historical requests and legacy region counts are not current production failure counts.",
            "Legacy chartUnits lose exact fields and original domain provenance; parent Select lacks Charts.",
            "Source-family equality does not establish source periodicity or native representability.",
            "Archive byte equality does not validate mip membership or native filtered rendering.",
            "Prepared subset bytes do not prove complete generated-owner or SDK package budgets.",
            "Synchronize required private producer files before any new baking; no baking was performed.",
        ],
    }
    (BASE / "current-source-audit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["summary"]))
    print(json.dumps(snapshot_differences))


if __name__ == "__main__":
    main()
