"""Extract exact native source inputs without consulting obsolete generated materials.

This is an authoring input manifest, not a production material manifest. Shared
native MidMips archives stay read-only and are recorded as external input locations.
No images are decoded, resized, sampled, compressed or baked.
"""
import argparse
from contextlib import ExitStack
import json
from pathlib import Path
import re
import struct
import zipfile

from ExportSourceSampling import STYLE, cid, descriptor, digest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("game_data", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    code = (args.repository / "src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02SourceRenders.cs").read_text()
    uses = []
    for match in re.finditer(r'Create\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+) \}', code):
        uses.append({"render": match[1], "meshCid": match[2], "surfaceCids": re.findall(r'"([a-f0-9]+)"', match[3])})
    if len(uses) != 7:
        raise ValueError("Unexpected prototype render inventory")
    args.output.mkdir(parents=True, exist_ok=True)
    assets, surfaces, mip_inputs = {}, [], []
    with ExitStack() as stack:
        archives = [stack.enter_context(zipfile.ZipFile(p)) for family in ("Game", "BridgesAndPorts")
                    for p in sorted((args.game_data / "Content" / family).glob("*.cok"))
                    if p.name.startswith(("Blob", "VT", "MidMips"))]
        entries = {}
        for archive in archives:
            for name in archive.namelist():
                if name.endswith(".cid"):
                    identity = archive.read(name).decode("utf-8-sig").strip()
                    entries.setdefault(identity, []).append((archive, name[:-4]))
        def read(identity):
            matches = entries.get(identity, [])
            if not matches:
                raise ValueError("Missing original CID " + identity)
            data = matches[0][0].read(matches[0][1])
            if any(a.read(n) != data for a, n in matches[1:]):
                raise ValueError("Conflicting original CID " + identity)
            return data, matches
        def save(identity):
            data, matches = read(identity)
            if identity not in assets:
                ext = Path(matches[0][1]).suffix
                path = args.output / "native" / (identity + ext)
                path.parent.mkdir(parents=True, exist_ok=True)
                if path.exists() and path.read_bytes() != data:
                    raise ValueError("Existing source copy differs: " + str(path))
                path.write_bytes(data)
                path.with_suffix(ext + ".cid").write_text(identity, encoding="utf-8")
                assets[identity] = {"cid": identity, "sha256": digest(data), "bytes": len(data),
                                    "file": str(path.resolve()), "kind": ext[1:], "builtin": True,
                                    "locations": [{"archive": str(a.filename), "entry": n} for a,n in matches]}
            return data
        for identity in sorted({s for use in uses for s in use["surfaceCids"]}):
            data = save(identity)
            template, header, vt, textures = descriptor(data)
            record = {"cid": identity, "template": template, "sha256": digest(data),
                      "textureBindings": textures, "vtSurfaceCid": vt, "vtTextureCids": []}
            if vt:
                save(vt)
            if header:
                for s in range(2):
                    for layer in range(4):
                        identity2 = cid(header[76+s*136+layer*16:92+s*136+layer*16])
                        if identity2 != "0"*32:
                            raw = save(identity2)
                            record["vtTextureCids"].append({"stack": s, "layer": layer, "cid": identity2,
                                "width": struct.unpack_from("<H", raw, 23)[0],
                                "height": struct.unpack_from("<H", raw, 25)[0],
                                "mipCount": raw[29], "graphicsFormat": raw[30], "originalTextureCid": cid(raw[3:19])})
            record["unavailableTextureBindings"] = {}
            for channel, other in textures.items():
                if other in entries:
                    save(other)
                elif header is not None:
                    # Native VT descriptors retain original authoring Texture CIDs
                    # whose data can exist only in the VT payload. Do not fabricate
                    # those files or treat their absence as a verified channel map.
                    record["unavailableTextureBindings"][channel] = other
                else:
                    raise ValueError("Missing non-VT texture " + other)
            surfaces.append(record)
        for archive in archives:
            if not Path(archive.filename).name.startswith("MidMips"):
                continue
            for name in archive.namelist():
                if name.endswith(".MidMips"):
                    mip_inputs.append({"archive": str(archive.filename), "entry": name,
                        "cid": archive.read(name+".cid").decode("utf-8-sig").strip(),
                        "bytes": archive.getinfo(name).file_size, "zipCRC32": f"{archive.getinfo(name).CRC:08x}",
                        "disposition": "original builtin cache; shared read-only input, not a package copy"})
    report = {"schema": "BridgeBuilder/PedestrianDraw02/NativeSourceInputs/1", "style": STYLE,
        "renderUses": uses, "sources": surfaces, "assets": list(assets.values()), "midMipInputs": mip_inputs,
        "summary": {"renders": len(uses), "surfaces": len(surfaces), "nativeFiles": len(assets),
                    "nativeBytes": sum(a["bytes"] for a in assets.values()),
                    "pixelPayloadFiles": sum(a["kind"] != "Surface" for a in assets.values())},
        "verification": {"nativeBytesExact": True, "nativePayloadCidsResolved": True,
            "originalTextureBindingsAllResolved": not any(s["unavailableTextureBindings"] for s in surfaces),
            "pixelReencoding": False, "nativeSamplingValidated": False,
            "fullMipCacheMembershipValidated": False},
        "limits": ["Authoring inputs only; public native preparation schema and execution are pending.",
                   "Original MidMips cache provenance is retained; per-source membership and native cold loading require the public validator.",
                   "Native source references are separate from unsupported historical sampler-fold semantics."]}
    (args.output / "source-inputs.json").write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(report["summary"], indent=2))


if __name__ == "__main__":
    main()
