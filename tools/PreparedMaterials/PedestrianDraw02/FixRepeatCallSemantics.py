"""Prepare the road-only cutout descriptor; retain source-template structure data."""
import io
import json
from pathlib import Path
import struct

from ExportRecipes import string
from ExportSourceSampling import cid, descriptor, digest


root = Path(__file__).resolve().parents[3]
style = root / "assets/Materials/PedestrianDraw02"
old_key = "a0322215b6a9765e1697060da0b9a465ac9207840c4fdcec5fb54fb48bd6cc89"
old = style / "RepeatUnits" / old_key / "Repeat.Surface"
raw = old.read_bytes()
r = io.BytesIO(raw)
assert struct.unpack("<Hi?", r.read(7)) == (1, 0, False)


def count():
    marker = r.read(1)
    assert marker in (b"\0", b"\xff")
    return 0 if marker == b"\0" else struct.unpack("<i", r.read(4))[0]


def text():
    n = 0
    shift = 0
    while True:
        b = r.read(1)[0]
        n |= (b & 127) << shift
        if b < 128:
            break
        shift += 7
    return r.read(n).decode()


maps = []
for size in (4, 4, 16, 16):
    values = {}
    for _ in range(count()):
        name = text()
        values[name] = r.read(size)
    maps.append(values)
maps[0]["_Snow"] = struct.pack("<f", 0)
begin = r.tell()
for _ in range(count()):
    text()
    assert r.read(1) == b"\xff"
    r.read(16)
texture_bytes = raw[begin:r.tell()]
keywords = [text() for _ in range(count())]
assert r.tell() == len(raw)
for value in ("_ALPHATEST_ON", "_DISABLE_SSR_TRANSPARENT", "_DOUBLESIDED_ON", "_TANGENTSPACE_OCTO"):
    if value not in keywords:
        keywords.append(value)


def dictionary(values):
    return b"\xff" + struct.pack("<i", len(values)) + b"".join(string(k)+v for k, v in values.items())


converted = struct.pack("<Hi?", 1, 14, False) + b"".join(dictionary(m) for m in maps) + texture_bytes
converted += b"\xff" + struct.pack("<i", len(keywords)) + b"".join(string(k) for k in keywords)
assert descriptor(raw)[3] == descriptor(converted)[3]
identity = cid(bytes.fromhex(digest(converted))[:16])
key = digest(string("BridgeBuilder/RepeatUnits/2") + string(identity))
base = root / "artifacts/prepared-materials/PedestrianDraw02/repeat-call-semantics"
path = base / "output/PedestrianDraw02/RepeatUnits" / key / "Repeat.Surface"
path.parent.mkdir(parents=True, exist_ok=True)
if path.exists():
    assert path.read_bytes() == converted
path.write_bytes(converted)
path.with_suffix(".Surface.cid").write_text(identity)
report = dict(structuralBindingKey=old_key, structuralTemplate=0, roadBindingKey=key,
              roadTemplate=14, snow=0, keywords=keywords, sourceDescriptorSHA256=digest(raw),
              roadDescriptorSHA256=digest(converted), unchangedTextureReferences=descriptor(converted)[3],
              unchangedNativeMapNames=[list(m) for m in maps], pixelPayloadsChanged=0,
              oldRepeatKey="6b68a0d1dcd3be3978c4452f10b446aa9a71047b508956d5097175b8f88a07b2")
(base / "report.json").write_text(json.dumps(report, indent=2)+"\n")
print(json.dumps(report, indent=2))
