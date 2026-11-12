"""Verify package discovery isolation and byte-complete on-demand inputs.
Traversal follows installed FileSystemDataSource.PopulateFromDirectory: root is
always visited; child names beginning with ~ or . are skipped. This is an offline
packaging regression check, not a native game/cold-load test.
"""
import hashlib
import sys
from pathlib import Path
source, package = map(Path, sys.argv[1:])
prepared = package / "~Materials"
assert prepared.is_dir() and not (package / "Materials").exists()
def discovered(root):
    pending = [root]
    while pending:
        for item in pending.pop().iterdir():
            if item.name.startswith(("~", ".")):
                continue
            if item.is_dir():
                pending.append(item)
            elif item.suffix == ".Surface":
                yield item
assert not list(discovered(package)), "Preparation Surfaces leaked into automatic discovery"
expected = {p.relative_to(source) for p in source.rglob("*") if p.is_file() and (p.suffix in {".Surface", ".Material", ".Texture", ".VTTexture", ".VTSurface", ".MidMips", ".Geometry", ".Prefab"} or p.suffix == ".cid")}
actual = {p.relative_to(prepared) for p in prepared.rglob("*") if p.is_file()}
assert expected == actual, (expected - actual, actual - expected)
for name in expected:
    assert hashlib.sha256((source/name).read_bytes()).digest() == hashlib.sha256((prepared/name).read_bytes()).digest(), name
surfaces = list(discovered(prepared))
assert len(surfaces) == sum(p.suffix == ".Surface" for p in expected)
print(f"PASS: zero auto-discovered preparation Surfaces; {len(surfaces)} accessible from explicit root; {len(expected)} payload/sidecar hashes identical")
