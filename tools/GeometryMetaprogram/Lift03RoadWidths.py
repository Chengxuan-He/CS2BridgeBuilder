"""Verify Lift03's two elevated road envelopes from the hash-pinned native audit."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("audit", type=Path)
args = parser.parse_args()
payload = args.audit.read_bytes()
assert hashlib.sha256(payload).hexdigest() == (
    "47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070"
), "Unexpected native audit"
assets = json.loads(payload)["selected"]


def active(item, flags):
    return (set(item["m_RequireAll"]) <= flags
            and (not item["m_RequireAny"] or set(item["m_RequireAny"]) & flags)
            and not set(item["m_RequireNone"]) & flags)


# Measured section membership excludes outward Side-layer strips. Repeated
# conditional section occurrences are evaluated, not deduplicated by name.
decks = (
    ("main", 118610, {120893}, {120892: 2, 120890: 4, 120891: 1}, 20),
    ("auxiliary", 118609, {121089}, {121088: 2, 121090: 2, 121087: 1}, 10),
)
for label, root, sides, expected_sections, expected_width in decks:
    for node, fixed in ((False, False), (False, True), (True, False), (True, True)):
        flags = {"Elevated"}
        if node:
            flags.add("Node")
        if fixed:
            flags.add("FixedNodeSize")
        occurrences = Counter()
        width = 0
        for entry in assets[str(root)]["data"]["m_Sections"]:
            if not active(entry, flags):
                continue
            section_id = entry["m_Section"]["m_PathID"]
            if section_id in sides:
                continue
            occurrences[section_id] += 1
            section = assets[str(section_id)]["data"]
            piece_flags = flags | ({"Median"} if entry["m_Median"] else set())
            widths = {
                assets[str(p["m_Piece"]["m_PathID"])]["data"]["m_Width"]
                for p in section["m_Pieces"] if active(p, piece_flags)
            }
            # Surface and bottom are vertically stacked, not lateral siblings.
            # Intersection-only medians contribute no width in this straight span.
            assert len(widths) <= 1, (label, section_id, widths)
            width += next(iter(widths), 0)
        assert occurrences == Counter(expected_sections), (label, flags, occurrences)
        assert width == expected_width, (label, flags, width)
        print(f"PASS {label}: Node={node}, FixedNodeSize={fixed}, width={width:g} m")
