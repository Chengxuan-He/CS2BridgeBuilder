"""Stream actual emitted units into compact per-style evidence and counts."""
import argparse
import gzip
import json
from collections import defaultdict
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("input", type=Path)
parser.add_argument("output", type=Path)
parser.add_argument("--source-uv", type=Path, help="Include retained-domain scenarios and explicit zero-density-unit styles.")
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
counts = defaultdict(lambda: {"units": 0, "canUseSource": 0, "repeatOnly": 0, "triangles": 0})
streams = {}
try:
    with args.input.open("rb") as source:
        for line in source:
            unit = json.loads(line)
            style = unit["style"]
            if style not in streams:
                streams[style] = gzip.open(args.output / (style + ".units.ndjson.gz"), "wb", compresslevel=1)
            streams[style].write(line)
            count = counts[style]
            count["units"] += 1
            count["canUseSource"] += unit["canUseSource"]
            count["repeatOnly"] += unit["repeatOnly"]
            count["triangles"] += len(unit["points"]) // 3
finally:
    for stream in streams.values():
        stream.close()
retained = defaultdict(lambda: {"plans": 0, "domainOccurrences": 0, "sourceFaceOccurrences": 0})
native = defaultdict(lambda: {"plans": 0, "domainOccurrences": 0})
if args.source_uv:
    with args.source_uv.open() as source:
        for line in source:
            row = json.loads(line)
            style = row["style"]
            counts[style]
            retained[style]["plans"] += 1
            retained[style]["domainOccurrences"] += len(row["sourceUvDomains"])
            retained[style]["sourceFaceOccurrences"] += row["sourceFaces"]
            if row.get("nativeNetwork", False):
                native[style]["plans"] += 1
                native[style]["domainOccurrences"] += len(row["nativeNetworkDomains"])
    for style in counts:
        if style not in streams:
            with gzip.open(args.output / (style + ".units.ndjson.gz"), "wb"):
                pass
(args.output / "summary.json").write_text(json.dumps({
    "input": str(args.input.resolve()), "styles": dict(sorted(counts.items())),
    "retainedSourceUv": dict(sorted(retained.items())),
    "nativeNetworkSampling": dict(sorted(native.items())),
    "scope": "Emitted changed Units only; unchanged source faces omitted by production. CanUseSource is original chart/gradient capability, not total prepared repeat-region coverage or human acceptance."
}, indent=2) + "\n", encoding="utf-8")
print(json.dumps(counts, indent=2))
