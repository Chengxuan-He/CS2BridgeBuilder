"""Audit a finite semantic grouping of the complete original structural domain inventory.

This checks provenance and coverage bookkeeping, not visual periodicity or runtime generation.
Usage: python AuditStructureUnits.py manifest.json report.json
"""
import hashlib
import json
import math
from pathlib import Path
import sys


def audit(manifest_path):
    manifest_path = Path(manifest_path).resolve()
    root = manifest_path.parent
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest.get("schema") != "BridgeBuilder/StructureUnits/1":
        raise ValueError("Unsupported structure grouping schema")

    def verified(reference):
        path = Path(reference["path"])
        path = path if path.is_absolute() else root / path
        data = path.read_bytes()
        if hashlib.sha256(data).hexdigest() != reference["sha256"].lower():
            raise ValueError(f"Evidence digest mismatch: {path}")
        return data

    inventory = json.loads(verified(manifest["inventory"]).decode("utf-8-sig"))
    if inventory.get("schema") != "BridgeBuilder/StructureDomains/1" or inventory["style"] != manifest["style"]:
        raise ValueError("Inventory schema/style mismatch")
    expected = {}
    for domain in inventory["domains"]:
        identity = domain["id"]
        if identity in expected or not identity or len(domain["sourceDigest"]) != 64:
            raise ValueError("Duplicate/invalid original domain identity")
        if not isinstance(domain["receivers"], int) or domain["receivers"] < 0:
            raise ValueError("Invalid receiver count")
        expected[identity] = domain
    units = {}
    for unit in manifest["units"]:
        identity = unit["id"]
        if identity in units:
            raise ValueError("Duplicate semantic unit identity")
        bounds, period = unit["bounds"], unit["physicalPeriod"]
        if len(bounds) != 4 or not all(math.isfinite(v) for v in bounds) or not (0 <= bounds[0] < bounds[2] <= 1 and 0 <= bounds[1] < bounds[3] <= 1):
            raise ValueError("Invalid measured source unit bounds")
        if len(period) != 2 or not all(math.isfinite(v) and v > 0 for v in period):
            raise ValueError("Invalid measured physical unit period")
        if not unit.get("semanticPattern") or len(unit["sourceDigest"]) != 64:
            raise ValueError("Missing original semantic pattern/source identity")
        verified(unit["evidence"])
        verified(unit["mipEvidence"])
        units[identity] = unit
    seen, used, group_ids, groups = set(), set(), set(), []
    kinds = {"ORIGINAL", "REPEAT_BASE", "REPEAT_DETAIL", "NONREPEATING"}
    for group in manifest["groups"]:
        if group["id"] in group_ids or group["kind"] not in kinds or not group["domains"]:
            raise ValueError("Duplicate/invalid semantic group")
        group_ids.add(group["id"])
        verified(group["evidence"])
        repeat = group["kind"].startswith("REPEAT_")
        if repeat:
            unit = units[group["unit"]]
            if unit["sourceDigest"] != group["sourceDigest"]:
                raise ValueError("Unit/group source mismatch")
            used.add(group["unit"])
        elif not group.get("reason"):
            raise ValueError("Original/nonrepeating groups require a concrete disposition")
        count = 0
        for identity in group["domains"]:
            if identity not in expected or identity in seen:
                raise ValueError(f"Unknown or multiply assigned domain: {identity}")
            domain = expected[identity]
            if domain["sourceDigest"] != group["sourceDigest"]:
                raise ValueError("Domain/group source mismatch")
            count += domain["receivers"]
            seen.add(identity)
        groups.append({"id": group["id"], "kind": group["kind"], "domains": len(group["domains"]), "receivers": count})
    missing = sorted(set(expected) - seen)
    if missing:
        raise ValueError(f"Incomplete original inventory grouping: {len(missing)} missing; first={missing[:5]}")
    if set(units) - used:
        raise ValueError("Unreferenced proposed units; do not bake unused resources")
    unresolved = sum(g["receivers"] for g in groups if g["kind"] == "NONREPEATING")
    return {"status": "GROUPING_COMPLETE_WITH_UNRESOLVED_CLASSES" if unresolved else "GROUPING_COMPLETE",
            "style": manifest["style"], "originalDomains": len(expected), "genuineUnits": len(units),
            "groups": groups, "unresolvedNonrepeatingReceivers": unresolved,
            "productionGenerationVerified": False,
            "limitation": "Verified evidence hashes and complete grouping only; semantic provenance, native mapping and complete saved-owner closure are separate checks."}


if __name__ == "__main__":
    result = audit(sys.argv[1])
    Path(sys.argv[2]).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: result[k] for k in ("status", "style", "originalDomains", "genuineUnits", "unresolvedNonrepeatingReceivers")}))
