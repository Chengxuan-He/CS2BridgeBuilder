"""Extract PedestrianDraw02 repeat units from measured arrays and native assets.

The exporter does not change geometry, numeric resources, or installed materials.
Mirrored receiver projections share the same pixel recipe.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile


STYLE = "PedestrianDraw02"
RECIPE = "BridgeBuilder/RoadRepeat/1"


def numbers(path):
    data = path.read_bytes()
    if data[:8] != b"BBN1\x01\x00\x00\x00" or hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError("Invalid measured numerical resource: " + str(path))
    counts = struct.unpack_from("<6i", data, 8)
    if min(counts) < 0:
        raise ValueError("Negative typed-array count")
    result, offset = {}, 32
    for name, fmt, count in zip(("I32", "U32", "I64", "U64", "F32", "F64"), "iIqQfd", counts):
        result[name] = struct.unpack_from("<" + fmt * count, data, offset)
        offset += struct.calcsize(fmt) * count
    if offset + 32 != len(data):
        raise ValueError("Unconsumed numerical resource data")
    return result


def native_source(game_data, cid):
    result = None
    for family in ("Game", "BridgesAndPorts"):
        for archive_path in (game_data / "Content" / family).glob("Blob*.cok"):
            with zipfile.ZipFile(archive_path) as archive:
                for entry in archive.namelist():
                    if entry.endswith(".Surface.cid") and archive.read(entry).decode("utf-8-sig").strip() == cid:
                        candidate = archive.read(entry[:-4])
                        if result is not None and result != candidate:
                            raise ValueError("Conflicting native source CID: " + cid)
                        result = candidate
    if result is None:
        raise ValueError("Native source material is absent: " + cid)
    return result


def string(value):
    data = value.encode("utf-8")
    size, prefix = len(data), bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    prefix.append(size)
    return prefix + data


def key(digest, low, high):
    # BinaryWriter protocol, including null bands rather than an empty array.
    data = string(RECIPE) + string(digest)
    data += struct.pack("<4f??i", *low, *high, True, False, -1)
    return hashlib.sha256(data).hexdigest()


def extract(repository, game_data, native_audit, structural_inventory):
    folder = repository / "src/BridgeBuilder/Bridges/Types" / STYLE
    appearance = folder / (STYLE + "RoadAppearance.cs")
    code = appearance.read_text(encoding="utf-8-sig")
    numeric = repository / "assets/HardcodedNumbers" / STYLE / ("Types_" + STYLE + "_" + STYLE + "RoadAppearance_cs.bin")
    values = numbers(numeric)
    if len(values["F32"]) != 31 or values["I32"] != (0, 1, 0, 1):
        raise ValueError("Measured appearance layout changed; review all patch aliases")
    indices = re.findall(r"new Vector2\(Numeric_\w+\.Values.F32\[(\d+)\], Numeric_\w+\.Values.F32\[(\d+)\]\)", code)
    if indices != [("6", "7"), ("8", "9"), ("17", "18"), ("19", "20")] or code.count("repeat: true") != 2:
        raise ValueError("Runtime patch bounds/repetition changed")
    common_path = repository / "src/BridgeBuilder/Bridges/Common/Movable/MovableRoadPreparation.cs"
    common = common_path.read_text(encoding="utf-8-sig")
    prototype_selection = re.search(r"PrototypeRoad = layout.Prototype is ([^;\n]+)", common)
    if prototype_selection is None or STYLE in prototype_selection[1]:
        raise ValueError("Moving prototype appearance selection changed; review recipe reachability")
    if "bones[face.Bone].m_ConnectionID != 0" not in common or "face.Bone | MovableSurfaceAtlas.MovingSurface" not in common:
        raise ValueError("Moving original-material routing changed")
    mechanism = numbers(repository / "assets/HardcodedNumbers" / STYLE / ("Types_" + STYLE + "_" + STYLE + "MechanismData_g_cs.bin"))
    if tuple(mechanism["I32"][i] for i in (20, 24, 28, 40, 44, 48)) != (0, 101, 100, 0, 101, 100):
        raise ValueError("Fixed/moving native bone connections changed")
    source_code = (folder / (STYLE + "SourceRenders.cs")).read_text(encoding="utf-8-sig")
    cid = re.search(r'Create\("PedestrianDrawBridge02 Mesh", "[0-9a-f]+", new\[\] \{ "([0-9a-f]+)"', source_code)[1]
    audit = json.loads(native_audit.read_text(encoding="utf-8-sig"))
    renders = [item["data"] for item in audit["selected"].values() if item["data"].get("m_Name") == "PedestrianDrawBridge02 Mesh"]
    if len(renders) != 1:
        raise ValueError("Native mechanism evidence missing or ambiguous")
    guid = renders[0]["m_SurfaceAssets"][0]
    raw = struct.pack("<4i", *(guid["m_GuidPart" + str(i)] for i in range(1, 5)))
    measured_cid = "".join(format(b & 15, "x") + format(b >> 4, "x") for b in raw)
    if cid != measured_cid:
        raise ValueError("Runtime material differs from measured native material")
    payload = native_source(game_data, cid)
    descriptions = [s for s in json.loads(structural_inventory.read_text(encoding="utf-8-sig"))["sources"] if s["cid"] == cid]
    if len(descriptions) != 1 or descriptions[0]["template"] != struct.unpack_from("<i", payload, 2)[0]:
        raise ValueError("Native template inventory differs")
    digest = hashlib.sha256(payload).hexdigest()
    units = []
    for name, low_indices, high_indices, regions in (("Fixed", (6, 7), (8, 9), [0, 1]), ("Moving", (17, 18), (19, 20), [2, 3])):
        low, high = ([values["F32"][i] for i in indices] for indices in (low_indices, high_indices))
        if not all(0 <= a < b <= 1 for a, b in zip(low, high)):
            raise ValueError("Invalid measured repeat rectangle")
        units.append(dict(style=STYLE, sourceCid=cid, sourceDigest=digest, key=key(digest, low, high),
                          min=low, max=high, cutout=True, rows=False, bands=None, purpose=name, regions=regions))
    evidence = {"runtimeSourceSHA256": hashlib.sha256(appearance.read_bytes()).hexdigest(),
                "commonPreparationSHA256": hashlib.sha256(common_path.read_bytes()).hexdigest(),
                "numericalResourceSHA256": hashlib.sha256(numeric.read_bytes()).hexdigest(),
                "nativeAuditSHA256": hashlib.sha256(native_audit.read_bytes()).hexdigest(),
                "inactivePatches": [units[1]],
                "inactiveReason": "Moving bones use original selected-surface appearance because PrototypeRoad is false; do not bake this unused prototype patch."}
    return dict(recipe=RECIPE, sources=descriptions, units=[units[0]], evidence=evidence)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("repository", "game_data", "native_audit", "structural_inventory", "output"):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    inventory = extract(args.repository, args.game_data, args.native_audit, args.structural_inventory)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
    print("PASS PedestrianDraw02 repeat inventory: 2 active fixed regions, 1 pixel recipe; moving patch retained as inactive evidence; native source/BBN verified; no generated bridge input.")


if __name__ == "__main__":
    main()
