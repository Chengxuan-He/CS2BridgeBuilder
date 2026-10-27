"""Extract Draw03's immutable repeat units from its measured typed arrays.

No generated bridge is an input. Source descriptor bytes come from the installed
native content archives; the output is an offline preparation inventory only.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


RECIPE = "BridgeBuilder/RoadRepeat/1"
SOURCE_CID = "792a855e9d0c5c34d2c41d48b062139d"


def numbers(path):
    data = path.read_bytes()
    if data[:8] != struct.pack("<II", 0x314E4242, 1):
        raise ValueError(f"Unsupported measured resource: {path}")
    if hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError(f"Measured resource checksum differs: {path}")
    counts = struct.unpack_from("<6i", data, 8)
    if min(counts) < 0:
        raise ValueError("Negative typed-array length")
    offset, result = 32, {}
    for name, fmt, count in zip(("I32", "U32", "I64", "U64", "F32", "F64"), "iIqQfd", counts):
        result[name] = struct.unpack_from("<" + fmt * count, data, offset)
        offset += struct.calcsize(fmt) * count
    if offset + 32 != len(data):
        raise ValueError("Measured resource length differs")
    return result


def native_source(game_data, cid):
    found = None
    for family in ("Game", "BridgesAndPorts"):
        for path in (game_data / "Content" / family).glob("Blob*.cok"):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.infolist():
                    if not entry.filename.endswith((".Surface.cid", ".Material.cid")):
                        continue
                    if archive.read(entry).decode("utf-8-sig").strip() != cid:
                        continue
                    data = archive.read(entry.filename[:-4])
                    if found is not None and found != data:
                        raise ValueError("Conflicting native source CID")
                    found = data
    if found is None:
        raise ValueError("Measured Draw03 material is unavailable in native archives")
    return found


def string(value):
    data = value.encode("utf-8")
    count, prefix = len(data), bytearray()
    while count >= 128:
        prefix.append((count & 127) | 128)
        count >>= 7
    prefix.append(count)
    return bytes(prefix) + data


def sampler_key(digest, low, high, cutout):
    # BinaryWriter protocol shared with MovableRepeatKey; bands=null, rows=false.
    payload = string(RECIPE) + string(digest)
    payload += struct.pack("<4f??i", *low, *high, cutout, False, -1)
    return hashlib.sha256(payload).hexdigest()


def units(repository, digest):
    root = repository / "assets/HardcodedNumbers/Draw03"
    appearance = numbers(root / "Types_Draw03_Draw03RoadAppearance_cs.bin")["F32"]
    wear = numbers(root / "Types_Draw03_Draw03RoadWear_cs.bin")["F32"]
    clean = (appearance[18:20], appearance[20:22])
    grating = (appearance[11:13], appearance[13:15])
    lane = ((wear[1] / wear[2], wear[3] / wear[4]),
            (wear[5] / wear[6], wear[7] / wear[8]))
    crop = wear[9] / wear[10]
    bottom = lane[0][1] - crop * wear[11]
    gap = ((lane[0][0], bottom), (lane[1][0], bottom + crop))
    result = []
    for name, (low, high), variants in (
        ("CleanAsphalt", clean, (False, True)),
        ("Grating", grating, (True,)),
        ("LaneWear", lane, (False, True)),
        ("BetweenLaneWear", gap, (False, True)),
    ):
        if not all(0 <= a < b <= 1 for a, b in zip(low, high)):
            raise ValueError("Measured repeat rectangle is invalid: " + name)
        for cutout in variants:
            result.append(dict(style="Draw03", sourceCid=SOURCE_CID, sourceDigest=digest,
                               key=sampler_key(digest, low, high, cutout), min=low, max=high,
                               cutout=cutout, rows=False, bands=None, purpose=name))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("game_data", type=Path)
    parser.add_argument("material_library", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = native_source(args.game_data, SOURCE_CID)
    if struct.unpack_from("<H", source)[0] != 1:
        raise ValueError("Unmeasured native Surface version")
    template = struct.unpack_from("<i", source, 2)[0]
    library = json.loads(args.material_library.read_text(encoding="utf-8-sig"))
    description = next(item for item in library if item["hash"] == template)
    channels = [dict(stack=stack, layer=layer, name=name)
                for stack, names in enumerate(description["stacks"])
                for layer, name in enumerate(names)]
    inventory = dict(recipe=RECIPE, sources=[dict(cid=SOURCE_CID, template=template, channels=channels)],
                     units=units(args.repository, hashlib.sha256(source).hexdigest()))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
    print(f"PASS Draw03 repeat inventory: {len(inventory['units'])} native-source recipes; "
          "measured BBN arrays unchanged; no receiver-sized images")


if __name__ == "__main__":
    main()
