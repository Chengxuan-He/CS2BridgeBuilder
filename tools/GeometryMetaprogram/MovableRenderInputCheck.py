"""Check recorded movable render inputs against production and installed resources.

Usage: python MovableRenderInputCheck.py RESEARCH_DIRECTORY GAME_DATA_DIRECTORY
Does not launch the game, modify assets, or establish generated-output persistence.
"""
from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
from tempfile import TemporaryDirectory
from zipfile import ZipFile


def cid(value):
    raw = struct.pack("<4i", *(value[f"m_GuidPart{i}"] for i in range(1, 5)))
    return "".join(f"{b & 15:x}{b >> 4:x}" for b in raw)


def check_virtual_textures(game, surfaces):
    # Native SurfaceAsset v1: 7-byte prefix, 276-byte SurfaceAssetVTHeader,
    # then nullable VTSurface reference. PerStackData is 136 bytes; the four
    # preprocessed Hash128 fields start at byte 72. Original Texture GUIDs
    # are authoring provenance and need not be shipped as Texture assets.
    references = {}
    for identity, raw in surfaces.items():
        assert struct.unpack_from("<H", raw)[0] == 1, (identity, "Surface version")
        if not raw[6]:
            continue
        stacks = struct.unpack_from("<i", raw, 7)[0]
        assert 0 <= stacks <= 2 and len(raw) >= 284, identity
        for stack in range(stacks):
            for layer in range(4):
                offset = 11 + stack * 136 + 72 + layer * 16
                value = raw[offset:offset + 16]
                if any(value):
                    key = "".join(f"{b & 15:x}{b >> 4:x}" for b in value)
                    references.setdefault(key, set()).add((identity, ".VTTexture"))
        assert raw[283] in (0, 255), (identity, "nullable VT reference")
        if raw[283] == 255:
            assert len(raw) >= 300, identity
            key = "".join(f"{b & 15:x}{b >> 4:x}" for b in raw[284:300])
            references.setdefault(key, set()).add((identity, ".VTSurface"))

    found = set()
    total_bytes = 0
    # These recorded inputs are built-in Game/BridgesAndPorts resources.
    with ExitStack() as stack:
        for relative in ("Content/Game/VT.cok", "Content/BridgesAndPorts/VT.cok"):
            archive = stack.enter_context(ZipFile(game / relative))
            for sidecar in archive.namelist():
                if not sidecar.endswith((".VTTexture.cid", ".VTSurface.cid")):
                    continue
                identity = archive.read(sidecar).decode("utf-8-sig").strip()
                if identity not in references:
                    continue
                assert identity not in found, (identity, "duplicate VT identity")
                entry = sidecar[:-4]
                assert all(entry.endswith(kind) for _, kind in references[identity]), entry
                # Full ZIP reads validate CRC, including payload beyond the header.
                raw = archive.read(entry)
                assert struct.unpack_from("<H", raw)[0] == 1, (entry, "VT version")
                if entry.endswith(".VTSurface"):
                    assert len(raw) >= 498, entry
                    owner = "".join(f"{b & 15:x}{b >> 4:x}" for b in raw[2:18])
                    assert owner in {source for source, _ in references[identity]}, (
                        entry, "VTSurface material back-reference")
                    assert 0 < struct.unpack_from("<i", raw, 490)[0] <= 2, entry
                else:
                    assert len(raw) >= 29 and raw[2] == 255, entry
                    reserved, tile, width, height, _ = struct.unpack_from("<5H", raw, 19)
                    assert reserved == 0 and tile > 0 and width > 0 and height > 0, entry
                found.add(identity)
                total_bytes += len(raw)
    assert found == set(references), ("Missing native VT resources", set(references) - found)
    print(f"PASS {len(found)} native VT payloads ({total_bytes} bytes): references, headers and ZIP CRC")


def main():
    research, game = map(Path, sys.argv[1:])
    repo = Path(__file__).resolve().parents[2]
    audit_path = research / "all-movable-audit.json"
    audit = json.loads(audit_path.read_text(encoding="utf-8"))
    styles = ["Draw02", "Draw03"] + [f"Lift{i:02}" for i in range(1, 6)]
    styles += ["PedestrianDraw01", "PedestrianDraw02"]
    resources = {}
    descriptors = 0
    with TemporaryDirectory(prefix="bridge-render-input-check-") as temporary:
        scratch = Path(temporary)
        for style in styles:
            relative = Path("src/BridgeBuilder/Bridges/Types") / style
            (scratch / relative).mkdir(parents=True)
            shutil.copy2(repo / relative / f"{style}RenderData.g.cs", scratch / relative)
        subprocess.run([
            sys.executable, str(Path(__file__).with_name("MovableRenderSources.py")),
            str(audit_path.resolve()), str(scratch),
        ], check=True)
        inputs = []
        for style in styles:
            folder = repo / "src/BridgeBuilder/Bridges/Types" / style
            for source in sorted(folder.glob("*.cs")):
                production = source.read_text(encoding="utf-8")
                if ": MovableSourceRenders" not in production:
                    continue
                relative = source.relative_to(repo)
                if source.name != f"{style}SourceRenders.cs":
                    emitter = re.search(
                        r"// Generated by (tools/GeometryMetaprogram/\w+\.py)\.",
                        production)
                    assert emitter, f"{source.name}: no recorded source emitter"
                    subprocess.run([
                        sys.executable, str(repo / emitter.group(1)),
                        str(audit_path.resolve()), str(scratch / relative),
                    ], check=True)
                inputs.append(relative)
        # Some approach emitters also write section/component data. Check those
        # outputs too, so a passing descriptor comparison cannot hide stale peers.
        for generated in sorted((scratch / "src").rglob("*.cs")):
            relative = generated.relative_to(scratch)
            assert (repo / relative).is_file(), f"Unintegrated generated source: {relative}"
            assert generated.read_text(encoding="utf-8") == (repo / relative).read_text(encoding="utf-8"), (
                f"Generated source differs from production: {relative}")
        for relative in inputs:
            production = (repo / relative).read_text(encoding="utf-8")
            assert production == (scratch / relative).read_text(encoding="utf-8"), (
                f"{relative.name}: recorded production descriptors differ from native regeneration")
            names = re.findall(r'Create(?:<\w+>)?\("([^"]+)"', production)
            assert names, f"{relative.name}: no render inputs checked"
            for name in names:
                matches = [
                    item["data"] for item in audit["selected"].values()
                    if item["class"] in ("RenderPrefab", "NetPiecePrefab")
                    and item["data"].get("m_Name") == name
                ]
                assert len(matches) == 1, (relative.name, name, "ambiguous native render")
                data = matches[0]
                geometry = audit["assets"][cid(data["m_GeometryAsset"])]
                meshes = geometry["meshes"]
                assert len(meshes) == data["m_MeshCount"] == len(data["m_SurfaceAssets"]), name
                assert sum(m["vertices"] for m in meshes) == data["m_VertexCount"], name
                assert sum(m["indices"] for m in meshes) == data["m_IndexCount"], name
                assert all(m["submeshes"] == 1 for m in meshes), name
                for reference in [data["m_GeometryAsset"]] + data["m_SurfaceAssets"]:
                    identity = cid(reference)
                    resources[identity] = audit["assets"][identity]
            descriptors += len(names)
            print(f"PASS {relative.name}: {len(names)} native descriptors and mesh/surface slots")

    surfaces = {}
    with ExitStack() as stack:
        archives = {}
        for identity, resource in resources.items():
            package = resource["package"]
            if package not in archives:
                archives[package] = stack.enter_context(ZipFile(game / package))
            archive = archives[package]
            raw = archive.read(resource["entry"])
            actual_cid = archive.read(resource["entry"] + ".cid").decode("utf-8-sig").strip()
            assert actual_cid == identity, (identity, "package CID mismatch")
            assert len(raw) == resource["size"], (identity, "package size mismatch")
            assert hashlib.sha256(raw).hexdigest() == resource["sha256"], (
                identity, "package payload mismatch")
            if resource["entry"].endswith(".Surface"):
                surfaces[identity] = raw
    print(f"PASS {descriptors} descriptors; {len(resources)} direct native resources verified")
    check_virtual_textures(game, surfaces)
    print("Scope excludes native VT decoding/streaming, generated output, animation and cold loading.")


if __name__ == "__main__":
    main()
