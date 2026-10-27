"""Append source-verified UV unions while preserving every existing reliable chart.

Run after StageUnionCharts.py. Default writes a candidate only; --apply also
updates the style BBN. Standard GenerateUvCharts.py runs both steps.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / "artifacts/prepared-materials/PedestrianDraw02"


def decode(path):
    values = numbers(path)
    ints, coords = values["I32"], values["F64"]
    assert ints[0] == 1
    stream_count = ints[1]
    chart_count = ints[2+stream_count]
    streams, charts = [], []
    for index in range(stream_count):
        at = ints[2+index]
        count = ints[at]
        at += 1
        domains = []
        for _ in range(count):
            length = ints[at]
            at += 1
            domains.append(list(ints[at:at+length]))
            at += length
        streams.append(domains)
    for index in range(chart_count):
        at = ints[3+stream_count+index]
        count = ints[at]
        at += 1
        loops = []
        for _ in range(count):
            start, length = ints[at:at+2]
            at += 2
            loops.append([list(coords[start+i*2:start+i*2+2]) for i in range(length)])
        charts.append(loops)
    return streams, charts


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--exact-intersections", action="store_true")
    args = parser.parse_args()
    candidate_dir = BASE / ("exact-union-chart-candidate" if args.exact_intersections else "union-chart-candidate")
    resource = ROOT / "assets/HardcodedNumbers/PedestrianDraw02/PedestrianDraw02UvCharts.bin"
    baseline = json.loads((BASE / "source-consolidation/embedded-chart-report.json").read_text())
    proof = json.loads((candidate_dir / "source-evidence.json").read_text())
    override_path = candidate_dir / "chart-overrides.json"
    assert hashlib.sha256(resource.read_bytes()).hexdigest() == baseline["sha256"], "Regenerate original chart baseline first"
    assert hashlib.sha256(override_path.read_bytes()).hexdigest() == proof["overrideSha256"]
    for relative, sha in {**proof["inputs"], **proof["recipeHashes"]}.items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == sha, relative
    original_streams, original_charts = decode(resource)
    streams = [[list(d) for d in s] for s in original_streams]
    charts = list(original_charts)
    def key(chart):
        return tuple(tuple(tuple(point) for point in ring) for ring in chart)
    identities = {key(c): index for index, c in enumerate(charts)}
    lookup = {(s["name"], s["mesh"]): i for i, s in enumerate(baseline["streams"])}
    overrides = json.loads(override_path.read_text())
    if args.exact_intersections:
        strict_path = BASE / "union-chart-candidate/chart-overrides.json"
        prior = json.loads(strict_path.read_text())
        keys = {(r["render"], r["mesh"], r["originalDomain"]): r["charts"] for r in overrides}
        assert all(keys[r["render"], r["mesh"], r["originalDomain"]] == r["charts"] for r in prior), "Prior union domain changed"
    modified = set()
    for row in overrides:
        assert row["style"] == "PedestrianDraw02"
        stream, domain = lookup[row["render"], row["mesh"]], row["originalDomain"]
        assert (stream, domain) not in modified and not streams[stream][domain]
        modified.add((stream, domain))
        for chart in row["charts"]:
            identity = key(chart)
            if identity not in identities:
                identities[identity] = len(charts)
                charts.append(chart)
            streams[stream][domain].append(identities[identity])
    ints = [1, len(streams)] + [0]*len(streams) + [len(charts)] + [0]*len(charts)
    coords = []
    for index, domains in enumerate(streams):
        ints[2+index] = len(ints)
        ints.append(len(domains))
        for domain in domains:
            ints.extend((len(domain), *domain))
    for index, chart in enumerate(charts):
        ints[3+len(streams)+index] = len(ints)
        ints.append(len(chart))
        for ring in chart:
            ints.extend((len(coords), len(ring)))
            coords.extend(x for point in ring for x in point)
    body = b"BBN1" + struct.pack("<i6i", 1, len(ints), 0, 0, 0, 0, len(coords))
    body += struct.pack("<"+"i"*len(ints), *ints) + struct.pack("<"+"d"*len(coords), *coords)
    raw = body + hashlib.sha256(body).digest()
    candidate = candidate_dir / "PedestrianDraw02UvCharts.bin"
    candidate.write_bytes(raw)
    checked_streams, checked_charts = decode(candidate)
    assert checked_charts[:len(original_charts)] == original_charts
    assert checked_streams == streams and checked_charts == charts
    assert all(checked_streams[s][d] == old for s, domains in enumerate(original_streams)
               for d, old in enumerate(domains) if (s, d) not in modified)
    assert all(not original_streams[s][d] for s, d in modified)
    if args.apply:
        resource.write_bytes(raw)
    report = {"style": "PedestrianDraw02", "applied": args.apply,
              "baselineSha256": baseline["sha256"], "sha256": hashlib.sha256(raw).hexdigest(),
              "baselineBytes": baseline["bytes"], "bytes": len(raw),
              "originalCharts": len(original_charts), "addedCharts": len(charts)-len(original_charts),
              "recoveredDomains": len(modified), "sourceProofSha256": hashlib.sha256((candidate_dir / "source-evidence.json").read_bytes()).hexdigest(),
              "exactOriginalChartsPreserved": True, "allUnmodifiedDomainReferencesPreserved": True,
              "fullTypedArrayRoundTrip": True, "textureOrDescriptorChanges": False}
    (candidate_dir / "integration.json").write_text(json.dumps(report, indent=2)+"\n")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
