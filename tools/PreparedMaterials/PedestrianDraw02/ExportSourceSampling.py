"""Inventory PedestrianDraw02 source payloads separately from sampling records.

Read-only with respect to native and packaged assets. No baking or asset replacement.
The report is authoring evidence, not a runtime manifest or a completed migration.
"""
import argparse
from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import struct
import zipfile

STYLE = "PedestrianDraw02"
INVENTORY_SHA = "dc72be70e2058c6fa79f8c459ae3c8fe3b2215110edd8b04decc559f58fad92b"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    return "".join(f"{b & 15:x}{b >> 4:x}" for b in data)


def descriptor(data):
    if data[:2] != b"\x01\0":
        raise ValueError("Unsupported Surface version")
    offset = 7
    header, surface = None, None
    if data[6]:
        header, offset = data[7:283], 283
        marker = data[offset]
        offset += 1
        if marker == 255:
            surface, offset = cid(data[offset:offset + 16]), offset + 16
        elif marker != 0:
            raise ValueError("Invalid VTSurface presence")

    def count():
        nonlocal offset
        marker = data[offset]
        offset += 1
        if marker == 0:
            return 0
        if marker != 255:
            raise ValueError("Invalid native dictionary marker")
        result = struct.unpack_from("<i", data, offset)[0]
        offset += 4
        if result < 0:
            raise ValueError("Negative native dictionary count")
        return result

    def string():
        nonlocal offset
        size, shift = 0, 0
        while True:
            b = data[offset]
            offset += 1
            size |= (b & 127) << shift
            if b < 128:
                break
            shift += 7
            if shift > 28:
                raise ValueError("Invalid string length")
        result = data[offset:offset + size].decode("utf-8")
        offset += size
        return result

    for width in (4, 4, 16, 16):
        for _ in range(count()):
            string()
            offset += width
    textures = {}
    for _ in range(count()):
        name = string()
        marker = data[offset]
        offset += 1
        if marker == 255:
            textures[name] = cid(data[offset:offset + 16])
            offset += 16
        elif marker != 0:
            raise ValueError("Invalid texture presence")
    for _ in range(count()):
        string()
    if offset != len(data):
        raise ValueError("Unconsumed Surface descriptor bytes")
    return struct.unpack_from("<i", data, 2)[0], header, surface, textures


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("game_data", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    root = args.repository
    inventory_path = root / "artifacts/uv-density/structure-tessellated-sources.json"
    if digest(inventory_path.read_bytes()) != INVENTORY_SHA:
        raise ValueError("Authoritative structural inventory changed; review before migration")
    inventory = json.loads(inventory_path.read_text(encoding="utf-8-sig"))
    units = [dict(u, family="Structure") for u in inventory["units"] if u["style"] == STYLE]
    repeat_path = root / "artifacts/prepared-materials" / STYLE / "repeat-inventory.json"
    repeat = json.loads(repeat_path.read_text(encoding="utf-8-sig"))
    units += [dict(u, family="Repeat") for u in repeat["units"]]
    style_root = root / "assets/Materials" / STYLE
    expected = {u["key"] for u in units if u["family"] == "Structure"}
    actual = {p.name for p in (style_root / "Structure").iterdir() if p.is_dir()}
    if expected != actual:
        raise ValueError(f"Structure key mismatch: missing={expected-actual}, extra={actual-expected}")
    descriptions = {s["cid"]: s for s in inventory["sources"]}
    payloads, sources = {}, []
    with ExitStack() as stack:
        archives = [stack.enter_context(zipfile.ZipFile(p)) for family in ("Game", "BridgesAndPorts")
                    for p in sorted((args.game_data / "Content" / family).glob("*.cok"))
                    if p.name.startswith(("Blob", "VT", "MidMips"))]
        entries = {}
        for archive in archives:
            for name in archive.namelist():
                if not name.endswith(".cid"):
                    continue
                identity = archive.read(name).decode("utf-8-sig").strip()
                entries.setdefault(identity, []).append((archive, name[:-4]))

        def read(identity):
            matches = entries.get(identity, [])
            if not matches:
                raise ValueError("Missing native asset " + identity)
            values = []
            for archive, name in matches:
                if archive.read(name + ".cid").decode("utf-8-sig").strip() != identity:
                    raise ValueError("CID sidecar mismatch")
                values.append(archive.read(name))
            if any(v != values[0] for v in values[1:]):
                raise ValueError("Conflicting native CID " + identity)
            return values[0], [{"archive": str(a.filename), "entry": n} for a, n in matches]

        def payload(identity):
            if identity not in payloads:
                data, locations = read(identity)
                payloads[identity] = {"cid": identity, "sha256": digest(data), "bytes": len(data), "locations": locations}
            return read(identity)[0]

        for identity in sorted({u["sourceCid"] for u in units}):
            data, locations = read(identity)
            if any(u["sourceDigest"] != digest(data) for u in units if u["sourceCid"] == identity):
                raise ValueError("Source digest differs from measured recipe")
            template, header, vt, bindings = descriptor(data)
            source = {"cid": identity, "sha256": digest(data), "template": template, "locations": locations, "channels": []}
            vt_data = payload(vt) if vt else None
            for channel in descriptions[identity]["channels"]:
                s, layer = channel["stack"], channel["layer"]
                record = dict(channel)
                active = False
                if vt_data:
                    stacks = struct.unpack_from("<i", vt_data, 490)[0]
                    li = 106 + s * 192 + layer * 48
                    active = s < stacks and vt_data[li] != 0
                    if active:
                        width, height = struct.unpack_from("<2i", vt_data, 26 + s * 40)
                        record.update(payloadCid=vt, representation="VTSurfaceLayer", width=width, height=height,
                                      graphicsFormat=struct.unpack_from("<i", vt_data, li + 4)[0])
                if not active and header:
                    backing = cid(header[76 + s * 136 + layer * 16:92 + s * 136 + layer * 16])
                    if backing != "0" * 32:
                        raw = payload(backing)
                        record.update(payloadCid=backing, representation="VTTexture",
                                      width=struct.unpack_from("<H", raw, 23)[0], height=struct.unpack_from("<H", raw, 25)[0],
                                      mipCount=raw[29], graphicsFormat=raw[30], originalTextureCid=cid(raw[3:19]))
                        active = True
                if not active:
                    record.update(representation="NativeTemplateDefault")
                source["channels"].append(record)
            for name, identity2 in bindings.items():
                if name not in {c["name"] for c in descriptions[identity]["channels"]}:
                    payload(identity2)
                    source["channels"].append({"name": name, "payloadCid": identity2, "representation": "Texture"})
            sources.append(source)
    for unit in units:
        path = style_root / unit["family"] / unit["key"] / (unit["family"] + ".Surface")
        _, _, _, bindings = descriptor(path.read_bytes())
        unit["existingPreparedBindings"] = bindings
    files = [p for p in style_root.rglob("*") if p.is_file()]
    texture_files = [p for p in files if p.suffix == ".Texture"]
    available = {}
    for path in texture_files:
        identity = path.with_suffix(path.suffix + ".cid").read_text(encoding="utf-8-sig").strip()
        available.setdefault(identity, []).append(path)
    referenced = {identity for unit in units for identity in unit["existingPreparedBindings"].values()}
    if referenced - available.keys():
        raise ValueError("Prepared sampler dependencies missing from this style: " + str(referenced - available.keys()))
    channel_images = {}
    for source in sources:
        for channel in source["channels"]:
            if "payloadCid" not in channel:
                continue
            # A layer and its channel meaning are distinct from descriptor/sampler identity.
            identity = (channel["payloadCid"], channel.get("stack"), channel.get("layer"), channel["name"])
            if identity not in channel_images:
                channel_images[identity] = dict(channel, sourceSurfaceCids=[], samplerKeys=[])
            channel_images[identity]["sourceSurfaceCids"].append(source["cid"])
            channel_images[identity]["samplerKeys"].extend(u["key"] for u in units if u["sourceCid"] == source["cid"])
    report = {"style": STYLE, "status": "identity-mapping-complete-native-sampling-integration-pending",
              "authoritativeInventorySHA256": INVENTORY_SHA, "repeatInventorySHA256": digest(repeat_path.read_bytes()),
              "sources": sources, "sourcePayloads": list(payloads.values()), "sourceChannelImages": list(channel_images.values()), "samplers": units,
              "summary": {"structureSamplers": len(expected), "repeatSamplers": len(repeat["units"]),
                          "triangleSamplers": sum(u.get("triangle", False) for u in units), "sourceSurfaces": len(sources),
                          "nativePayloadFiles": len(payloads), "nativePayloadBytes": sum(p["bytes"] for p in payloads.values()),
                          "sourceChannelImages": len(channel_images), "resolvedPreparedTextureCids": len(referenced),
                          "styleFiles": len(files), "styleBytes": sum(p.stat().st_size for p in files),
                          "textureFiles": len(texture_files), "textureBytes": sum(p.stat().st_size for p in texture_files),
                          "uniqueTextureHashes": len({digest(p.read_bytes()) for p in texture_files})},
              "limits": ["Native container hashes identify conservative source groups; distinct containers are not claimed texel-equivalent.",
                         "VTSurface layers share their container but remain distinct channels. Native mip payload bytes are retained, not re-encoded.",
                         "No native subrectangle repeat or triangle-fold renderer has been integrated; existing prepared assets remain unchanged.",
                         "Style filesystem bytes are a diagnostic subtotal, not official SDK ZIP or generated bridge size."]}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["summary"], indent=2))


if __name__ == "__main__":
    main()
