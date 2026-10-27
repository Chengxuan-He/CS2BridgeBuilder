"""Search every installed built-in Blob archive for this unit's exact authoring CIDs."""
import argparse
import json
from pathlib import Path
import zipfile

from ExportSourceSampling import digest


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("game_data", type=Path)
    p.add_argument("evidence", type=Path)
    p.add_argument("output", type=Path)
    args = p.parse_args()
    evidence = json.loads(args.evidence.read_text())
    unit = evidence["candidates"][0]
    channels = unit["mipEvidence"]["channels"]
    targets = {c["originalTextureCid"]: c for c in channels}
    found = {k: [] for k in targets}
    scanned = []
    for path in sorted((args.game_data / "Content").rglob("Blob*.cok")):
        with zipfile.ZipFile(path) as z:
            sidecars = [n for n in z.namelist() if n.endswith(".Texture.cid")]
            scanned.append({"archive": str(path.resolve()), "bytes": path.stat().st_size,
                            "textureSidecarsChecked": len(sidecars)})
            for name in sidecars:
                identity = z.read(name).decode("utf-8-sig").strip()
                if identity not in targets:
                    continue
                raw = z.read(name[:-4])
                destination = args.output / "authoring" / (identity + ".Texture")
                destination.parent.mkdir(parents=True, exist_ok=True)
                if destination.exists() and destination.read_bytes() != raw:
                    raise ValueError("Conflicting native authoring CID: " + identity)
                destination.write_bytes(raw)
                destination.with_suffix(".Texture.cid").write_text(identity)
                found[identity].append({"archive": str(path.resolve()), "entry": name[:-4],
                    "bytes": len(raw), "sha256": digest(raw), "file": str(destination.resolve())})
    report = {"style": "PedestrianDraw02", "sourceCid": unit["sourceCid"],
        "sourceSHA256": unit["sourceSHA256"], "evidenceSHA256": digest(args.evidence.read_bytes()),
        "archives": scanned, "searchedAuthoringCids": found,
        "nativeChannelInputs": channels, "midMipInputs": unit["mipEvidence"]["midMipInputs"],
        "summary": {"archivesScanned": len(scanned), "textureSidecarsChecked": sum(x["textureSidecarsChecked"] for x in scanned),
                    "wantedChannels": len(targets), "foundChannels": sum(bool(v) for v in found.values())},
        "requiredPublicWork": ["If authoring CIDs are absent, reconstruct this exact source from its original VTTexture and MidMips data.",
            "Do not substitute another Surface's ordinary Texture or regenerate missing levels from mip zero.",
            "Compare extracted unit edges and every mip against original atlas neighborhoods; periodic self-comparison is insufficient."]}
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "authoring-search.json").write_text(json.dumps(report, indent=2)+"\n")
    print(json.dumps(report["summary"]))


if __name__ == "__main__":
    main()
