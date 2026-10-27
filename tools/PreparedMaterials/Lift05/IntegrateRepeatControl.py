"""Publish only the independently verified Lift05 control-reference candidates."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil


def digest(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def cid_bytes(cid):
    require(len(cid) == 32 and all(c in "0123456789abcdef" for c in cid), "Invalid CID")
    return bytes(int(cid[i:i + 2][::-1], 16) for i in range(0, 32, 2))


def inside(path, root):
    path = path.resolve()
    require(path.is_relative_to(root.resolve()), f"Path escaped Lift05 scope: {path}")
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("repo", type=Path)
    args = parser.parse_args()
    repo = args.repo.resolve()
    style = repo / "assets/Materials/Lift05"
    artifacts = repo / "artifacts/prepared-materials/Lift05"
    candidates = artifacts / "repeat-control-final"
    backup = repo / "artifacts/backups/Lift05/repeat-zero-control-integration"
    report_path = artifacts / "repeat-zero-control-integration.json"
    require(not backup.exists() and not report_path.exists(), "Integration evidence already exists")
    audit_bytes = (candidates / "audit.json").read_bytes()
    audit = json.loads(audit_bytes)
    require(audit["status"] == "PASS" and audit["readbackPositiveControl"], "Incomplete GPU audit")
    require(audit["producedTextures"] == audit["encodedTextures"] == 0, "Unexpected texture generation")
    require(all(not s["authoredControl"] for s in audit["sources"]), "Authored control is ineligible")
    require(len(audit["changes"]) == 1 and len(audit["textures"]) == 2, "Unexpected audited scope")
    require(all(m["transparentZero"] for t in audit["textures"] for m in t["mips"]), "Nonzero mip")
    log = (artifacts / "repeat-control-final.log").read_text(encoding="utf-8-sig")
    require("PASS Lift05 repeat control audit:" in log and "return code 0" in log, "Native check failed")
    zero = style / "Road/ZeroControl.Texture"
    zero_cid = zero.with_suffix(".Texture.cid").read_text().strip()
    zero_proof = next(t for t in audit["textures"] if t["cid"] == zero_cid)
    require(digest(zero.read_bytes()) == zero_proof["sha256"], "Road zero changed since GPU audit")
    new_shared = inside(style / "Shared" / (zero_cid + ".Texture"), style)
    payloads = {p: p.read_bytes() for p in style.rglob("*") if p.is_file()}
    before_size = sum(map(len, payloads.values()))
    replacements = {}
    surfaces = []
    for change in audit["changes"]:
        require(change["onlyControlHashChanged"] and change["otherChannelsPreserved"] and change["nativeDiskReadback"], "Unverified candidate")
        relative = Path("Repeat") / change["key"] / "Repeat.Surface"
        target = inside(style / relative, style)
        source = inside(candidates / relative, candidates)
        old, new = target.read_bytes(), source.read_bytes()
        offset = change["offset"]
        require(digest(old) == change["oldSha256"] and digest(new) == change["newSha256"], "Descriptor changed since audit")
        require(target.with_suffix(".Surface.cid").read_text().strip() == change["oldSurfaceCid"], "Original Surface identity changed")
        require(len(old) == len(new) and old[:offset] == new[:offset] and old[offset + 16:] == new[offset + 16:], "Non-control bytes differ")
        require(new[offset:offset + 16] == cid_bytes(zero_cid), "Candidate does not bind Road zero")
        before_control = next(b for b in change["before"] if b["property"] == "_ControlMask")
        require(old[offset:offset + 16] == cid_bytes(before_control["cid"]), "Original control CID differs")
        expected_cid = digest((audit["recipe"] + "/" + digest(new)).encode())[:32]
        require(source.with_suffix(".Surface.cid").read_text().strip() == change["newSurfaceCid"] == expected_cid, "PreparedBakeStore CID mismatch")
        for binding in change["before"]:
            texture = inside(Path(binding["path"]), style)
            require(digest(texture.read_bytes()) == binding["sha256"], "Original texture changed")
            require(texture.with_suffix(".Texture.cid").read_text().strip() == binding["cid"], "Texture CID changed")
        for binding in change["after"]:
            if binding["property"] != "_ControlMask":
                previous = next(b for b in change["before"] if b["property"] == binding["property"])
                require(previous["cid"] == binding["cid"] and previous["sha256"] == binding["sha256"], "Other channel differs")
        replacements[target] = new
        replacements[target.with_suffix(".Surface.cid")] = source.with_suffix(".Surface.cid").read_bytes()
        surfaces.append({"path": str(relative), "beforeSha256": digest(old), "afterSha256": digest(new), "oldCid": change["oldSurfaceCid"], "newCid": expected_cid, "controlOffset": offset})
    require({p for p in style.glob("Repeat/*/Repeat.Surface")} == {p for p in replacements if p.suffix == ".Surface"}, "Unaccounted Repeat descriptor")
    # Check the complete prospective style Surface set, not only changed Repeat files.
    style_surfaces = {p: replacements.get(p, data) for p, data in payloads.items() if p.suffix == ".Surface"}
    removals = []
    reference_checks = []
    for cid in audit["removableControlCids"]:
        hits = [str(p.relative_to(style)) for p, data in style_surfaces.items() if cid_bytes(cid) in data]
        require(not hits, f"Old control remains referenced: {cid}: {hits}")
        path = inside(style / "Shared" / (cid + ".Texture"), style)
        proof = next(t for t in audit["textures"] if t["cid"] == cid)
        require(digest(path.read_bytes()) == proof["sha256"], "Old control changed before deletion")
        require(path.with_suffix(".Texture.cid").read_text().strip() == cid, "Old control sidecar changed")
        removals.extend([path, path.with_suffix(".Texture.cid")])
        reference_checks.append({"cid": cid, "scannedSurfaceCount": len(style_surfaces), "remainingReferences": hits})
    shared_sources = {new_shared: zero, new_shared.with_suffix(".Texture.cid"): zero.with_suffix(".Texture.cid")}
    for target, source in shared_sources.items():
        if target.exists():
            require(target.read_bytes() == source.read_bytes(), "Existing Shared zero differs")
    # Backup contains original files only. Evidence lives outside the backup tree.
    originals = sorted(set(replacements) | set(removals))
    for original in originals:
        destination = backup / original.relative_to(style)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(original, destination)
        require(destination.read_bytes() == payloads[original], "Backup verification failed")
    for target, source in shared_sources.items():
        if not target.exists():
            shutil.copy2(source, target)
    for target, data in replacements.items():
        target.write_bytes(data)
        require(target.read_bytes() == data, "Descriptor publication verification failed")
    # Re-prove no live packaged Surface reference before deleting specific files.
    after_surfaces = {p: p.read_bytes() for p in style.rglob("*.Surface")}
    for check in reference_checks:
        require(all(cid_bytes(check["cid"]) not in data for data in after_surfaces.values()), "Control still referenced after publication")
    for path in removals:
        inside(path, style / "Shared")
        require((backup / path.relative_to(style)).read_bytes() == path.read_bytes(), "Backup changed before removal")
        path.unlink()
        require(not path.exists(), "Old control removal failed")
    after = {p: p.read_bytes() for p in style.rglob("*") if p.is_file()}
    allowed = set(replacements) | set(removals) | set(shared_sources)
    require(all(after.get(p) == data for p, data in payloads.items() if p not in allowed), "Unrelated material payload changed")
    require(not any(p not in payloads and p not in allowed for p in after), "Unexpected new material file")
    require(new_shared.read_bytes() == zero.read_bytes(), "Shared zero bytes differ")
    report = {"status": "PASS", "auditSha256": digest(audit_bytes), "backup": str(backup.relative_to(repo)),
              "backupOriginalFiles": len(originals), "publishedSurfaces": surfaces, "sharedZeroCid": zero_cid,
              "sharedZeroSha256": digest(new_shared.read_bytes()), "oldControlReferenceChecks": reference_checks,
              "removedFiles": [str(p.relative_to(style)) for p in removals], "unchangedNonControlChannels": True,
              "unchangedUnrelatedFiles": True, "beforeStyleFiles": len(payloads), "afterStyleFiles": len(after),
              "beforeStyleBytes": before_size, "afterStyleBytes": sum(map(len, after.values())),
              "netStyleBytesSaved": before_size - sum(map(len, after.values())),
              "noBaking": True, "noUserVisibleTextChanged": True}
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"PASS Lift05 control integration: {len(surfaces)} descriptors; {len(originals)} originals backed up; {len(removals)} obsolete files removed; {report['netStyleBytesSaved']} net bytes saved.")


if __name__ == "__main__":
    main()
