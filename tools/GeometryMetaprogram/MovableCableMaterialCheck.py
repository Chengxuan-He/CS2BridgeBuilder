"""Verify the converted cable descriptor against native asset and template data.

Usage: python MovableCableMaterialCheck.py RESEARCH_DIRECTORY
This checks measured coefficients, not Unity rendering or native serialization.
"""
from pathlib import Path
import hashlib
import json
import re
import struct
import sys

research = Path(sys.argv[1])
source = (research / "native-cable.Material").read_bytes()
assert hashlib.sha256(source).hexdigest() == "8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219"
marker = b"\x0a_BaseColor"
assert source.count(marker) == 1
rgba = struct.unpack_from("<4f", source, source.index(marker)+len(marker))
templates = json.loads((research / "material-library-resolved.json").read_text())
native = next(item["material"] for item in templates if item["hash"] == 30)
assert native["m_Name"] == "CurvedTransparent"
native_floats = dict(native["m_SavedProperties"]["m_Floats"])
runtime = (Path(__file__).resolve().parents[2] /
           "src/BridgeBuilder/Bridges/Common/Movable/MovableCableSurface.cs").read_text()
color = re.search(r'new Color\(([^)]+)\)', runtime).group(1)
converted = tuple(float(number.strip().removesuffix("f")) for number in color.split(","))
assert struct.pack("<4f", *converted) == struct.pack("<4f", *rgba), (converted, rgba)
for property_name in ("_Metallic", "_Smoothness", "_Snow"):
    match = re.search(r'\["'+property_name+r'"\]\s*=\s*([0-9.]+)f', runtime)
    assert match and float(match.group(1)) == native_floats[property_name], property_name
assert rgba[3] == 1., "Opaque object conversion requires the recorded fully opaque cable."
print("PASS native payload hash; bit-exact RGBA; native metallic/smoothness/snow defaults; alpha=1.")
print("Rendering, descriptor reload and preview lifetime remain native-game checks.")
