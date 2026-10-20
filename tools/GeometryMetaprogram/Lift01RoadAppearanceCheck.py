"""Check Lift01 floor projections against actual native near-LOD UV bytes.

Usage: python Lift01RoadAppearanceCheck.py RESEARCH_DIRECTORY PIPELINE_PLUGIN_DLL
This is an offline source audit, not a visual acceptance test.
"""
from pathlib import Path
import ctypes as c
import hashlib
import struct
import sys
import numpy as np

r, plugin = map(Path, sys.argv[1:])
raw = (r / 'movable-skin/LiftBridge01 Mesh.Geometry').read_bytes()
assert hashlib.sha256(raw).hexdigest() == 'cc34bfcb478f5621513c458120bacfb395e21548e3eee2e753dca97e138b5714'
dll = c.CDLL(str(plugin))
for name, args, result in (
    ('decompress_calc_bound', [c.c_void_p, c.c_int64, c.c_int], c.c_int64),
    ('decompress_data', [c.c_void_p, c.c_int64, c.c_void_p, c.c_int64, c.c_int], c.c_int64),
    ('decompress_meshopt_vertex_attribute', [c.c_void_p, c.c_int64, c.c_int, c.c_int, c.c_void_p], c.c_int)):
    fn = getattr(dll, name); fn.argtypes = args; fn.restype = result
version, count, flags = struct.unpack_from('<HII', raw)
assert version == 1 and count == 2
headers = [struct.unpack_from('<QI14i5I', raw, 10 + i * 88) for i in range(count)]
offset = 10 + count * 88 + sum(h[-1] * 40 + h[16] for h in headers)
uv = None
for attribute in range(14):
    for mesh, h in enumerate(headers):
        size = h[2 + attribute]; block = raw[offset:offset + size]; offset += size
        if attribute != 4 or mesh != 0:
            continue
        fmt = (h[0] >> (attribute * 4)) & 15
        dim = ((h[1] >> (attribute * 2)) & 3) + 1
        assert fmt == 1 and dim == 2
        if flags & 2:
            capacity = dll.decompress_calc_bound(block, len(block), 0)
            assert capacity > 0
            buffer = c.create_string_buffer(capacity)
            assert dll.decompress_data(block, len(block), buffer, capacity, 0) == capacity
            block = buffer.raw
        if flags & 1:
            buffer = c.create_string_buffer(h[18] * 4)
            assert dll.decompress_meshopt_vertex_attribute(block, len(block), h[18], 4, buffer) == 0
            block = buffer.raw
        assert len(block) == h[18] * 4
        uv = np.frombuffer(block, dtype='<f2').reshape(-1, 2).astype(float)
assert uv is not None and len(uv) == 472209
d = np.load(r / 'lift01-main-0.npz')
v, faces = d['positions'], d['indices']
# Stable audit face ordinals, in the same patch order as the runtime recipe:
# fixed/moving, positive/negative Z, negative/positive X.
samples = [11144, 13500, 15858, 18220, 259030, 259630, 371276, 371876]
minimum = np.array([.57421875, .260009765625])
span = np.array([.6796875, .469970703125]) - minimum
for patch, face in enumerate(samples):
    moving, half, side = patch // 4, patch % 4 // 2, patch % 2
    p = v[faces[face]].astype(float)
    assert np.all(p[:, 1] == 9.800048828125)
    assert np.ptp(p[:, 0]) == 2 and np.ptp(p[:, 2]) == 4
    u = p[:, 0] * (-.5 if side == 0 else .5)
    w = p[:, 2] * (-.25 if half == 0 else .25) + (19 if moving else 91.779296875 / 4)
    # At the unit endpoints 1 denotes the upper edge, before texture wrap.
    predicted = minimum + np.column_stack((u, w)) * span
    assert np.array_equal(predicted, uv[faces[face]]), (patch, face, predicted, uv[faces[face]])
    assert abs((-.5 if side == 0 else .5) * (-.25 if half == 0 else .25)) == .125
    # Widening adds repeated periods; it never changes the native UV gradient.
    gradient = span / [2, 4]
    assert np.array_equal(gradient * 4096, [216, 215])
print('PASS Lift01: 8 native floor triangles match all patch projections exactly; '
      '2x4 m unit, 216x215 texels/m at 4096 resolution; no singular horizontal projection.')
