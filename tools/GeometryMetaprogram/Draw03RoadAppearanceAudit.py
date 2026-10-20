"""Measure Draw03 top-face UV gradients directly from native geometry.

Usage: python Draw03RoadAppearanceAudit.py RESEARCH_DIRECTORY PIPELINE_PLUGIN_DLL
The resulting JSON records the source faces used by Draw03RoadAppearance.
"""
from pathlib import Path
import ctypes as c
import hashlib
import json
import struct
import sys
import numpy as np

r, plugin = map(Path, sys.argv[1:])
raw = (r / 'movable-skin/DrawBridge03 Mesh.Geometry').read_bytes()
digest = hashlib.sha256(raw).hexdigest()
assert digest == 'f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f'
dll = c.CDLL(str(plugin))
for name, args, ret in (
    ('decompress_calc_bound', [c.c_void_p, c.c_int64, c.c_int], c.c_int64),
    ('decompress_data', [c.c_void_p, c.c_int64, c.c_void_p, c.c_int64, c.c_int], c.c_int64),
    ('decompress_meshopt_vertex_attribute', [c.c_void_p, c.c_int64, c.c_int, c.c_int, c.c_void_p], c.c_int),
    ('decompress_meshopt_index_buffer', [c.c_void_p, c.c_int64, c.c_int, c.c_int, c.c_void_p], c.c_int)):
    fn = getattr(dll, name); fn.argtypes = args; fn.restype = ret
version, count, flags = struct.unpack_from('<HII', raw)
assert version == 1 and count == 2
headers = [struct.unpack_from('<QI14i5I', raw, 10 + i*88) for i in range(count)]
offset = 10 + count*88 + sum(h[-1]*40 for h in headers)

def block(size, count, stride, index=False):
    global offset
    data = raw[offset:offset+size]; offset += size
    if flags & 2:
        capacity = dll.decompress_calc_bound(data, len(data), 0)
        buffer = c.create_string_buffer(capacity)
        assert dll.decompress_data(data, len(data), buffer, capacity, 0) == capacity
        data = buffer.raw
    if flags & 1:
        buffer = c.create_string_buffer(count*stride)
        decoder = dll.decompress_meshopt_index_buffer if index else dll.decompress_meshopt_vertex_attribute
        assert decoder(data, len(data), count, stride, buffer) == 0
        data = buffer.raw
    assert len(data) == count*stride
    return data

for mesh, h in enumerate(headers):
    stride = 4 if h[17] & 1 else 2
    data = block(h[16], h[19], stride, True)
    if mesh == 0:
        faces = np.frombuffer(data, dtype='<u4' if stride == 4 else '<u2').reshape(-1, 3)
channels = {}
for attribute in range(14):
    for mesh, h in enumerate(headers):
        size = h[2+attribute]
        if not size: continue
        if mesh != 0 or attribute not in (0, 4, 13):
            offset += size
            continue
        fmt = (h[0] >> (attribute*4)) & 15
        dim = ((h[1] >> (attribute*2)) & 3) + 1
        expected = {0: (0, 3), 4: (1, 2), 13: (10, 1)}[attribute]
        assert (fmt, dim) == expected
        dtype = {0: '<f4', 4: '<f2', 13: '<u4'}[attribute]
        channels[attribute] = np.frombuffer(block(size, h[18], np.dtype(dtype).itemsize*dim), dtype=dtype).reshape(-1, dim)
v, uv, bones = channels[0].astype(float), channels[4].astype(float), channels[13][:, 0]
assert np.all(bones[faces] == bones[faces[:, :1]])
p = v[faces]
normal = np.cross(p[:, 1]-p[:, 0], p[:, 2]-p[:, 0])
selected = np.flatnonzero(np.all(abs(p[:, :, 0]) <= 12.5, axis=1) & (normal[:, 1] > 0) &
                         (np.all(p[:, :, 1] == 10, axis=1) | np.all(p[:, :, 1] == 10.199951171875, axis=1)))
records = []
maximum_phase_error = 0.
for ordinal in selected:
    q = p[ordinal]
    walk = q[0, 1] != 10
    half = 1 if q[:, 2].mean() > 0 else -1
    side = 1 if q[:, 0].mean() > 0 else -1
    zone = 0 if bones[faces[ordinal, 0]] != 0 else 2 if abs(q[:, 2].mean()) > 88.041015625 else 1
    end = 96 if zone == 2 else 88.041015625 if zone == 1 else 80.015625 if walk else 72.015625
    length = 96-88.041015625 if zone == 2 else 8
    u = (-half*q[:, 2]+end)/length
    original = uv[faces[ordinal]]
    native_u = (original[:, 0]-.7529296875)/(.9892578125-.7529296875)
    phase_error = u-native_u
    phase_error -= np.rint(phase_error)
    maximum_phase_error = max(maximum_phase_error, float(max(abs(phase_error))))
    if walk:
        atlas_v = (.01087188720703125 + (.1024169921875-.01087188720703125) *
                   (-side*q[:, 0]+12.5)/3.10009765625)
    else:
        gradient = -side*(.385986328125-.10833740234375)/(9.39990234375-side*half*1.9073486328125e-6)
        atlas_v = .385986328125 + gradient*(q[:, 0]-half*1.9073486328125e-6)
    assert max(abs(atlas_v-original[:, 1])) < 1e-7
    records.append(dict(face=int(ordinal), zone=zone, half=half, side=side, sidewalk=bool(walk),
                        vertices=faces[ordinal].tolist(), positions=q.tolist(), uv=original.tolist()))
assert len(records) == 186
assert maximum_phase_error < .001
# Native float/half quantization causes less than one source texel phase error.
# The 7.958984375 m outer fixed unit remains distinct from the 8 m inner unit.
report = dict(sha256=digest, source_texture_size=[4096, 4096],
              clean_asphalt_pixel_bounds=[3084, 1312, 4052, 1476],
              maximum_longitudinal_phase_error=maximum_phase_error, faces=records)
(r / 'draw03-road-appearance-audit.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(f'{len(records)} native top faces: transverse UV agreement <1e-7; maximum period phase error {maximum_phase_error:.9g}')
