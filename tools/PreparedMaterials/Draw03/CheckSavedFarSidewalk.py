"""Measure the saved Draw03 far sidewalk's native frames and filtered alpha."""
import argparse
import ctypes
import json
import struct
from pathlib import Path

import numpy as np


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--attributes', type=Path, required=True)
    parser.add_argument('--texture', type=Path, required=True)
    parser.add_argument('--pipeline', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    mesh = json.loads(args.attributes.read_text())['meshes'][2]
    positions = np.asarray(mesh['positions'])
    uv = np.asarray(mesh['uv0'])[:, :2]
    faces = np.asarray(mesh['faces'])
    dll = ctypes.CDLL(str(args.pipeline))
    decode_t = dll.math_array_oct15_to_fp32tangents
    decode_t.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p]
    packed = np.asarray(mesh['tangentBits'], dtype=np.uint32)
    tangents = np.zeros((len(packed), 4), np.float32)
    decode_t(packed.ctypes.data, len(packed), tangents.ctypes.data)
    decode_n = dll.math_array_oct16_to_fp32normals
    decode_n.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p]
    packed_normals = np.asarray(mesh['normalBits'], dtype=np.uint32)
    normals = np.zeros((len(packed_normals), 3), np.float32)
    decode_n(packed_normals.ctypes.data, len(packed_normals), normals.ctypes.data)
    dots, hands = [], []
    for ids in faces:
        edges = positions[ids[1:]] - positions[ids[0]]
        gradient = uv[ids[1:]] - uv[ids[0]]
        du, dv = np.linalg.solve(gradient, edges)
        du /= np.linalg.norm(du)
        normal = np.cross(edges[0], edges[1])
        normal /= np.linalg.norm(normal)
        dots.extend(tangents[ids, :3] @ du)
        for vertex in ids:
            expected = np.sign(np.dot(np.cross(normal, tangents[vertex, :3]), dv))
            hands.append(bool(expected == tangents[vertex, 3]))
    raw = args.texture.read_bytes()
    version, width, height, depth, levels, fmt = struct.unpack_from('<4H2B', raw)
    assert version == 5 and depth == 1 and fmt == 4
    size = sum(max(1, width >> i) * max(1, height >> i) * 4 for i in range(levels))
    output = ctypes.create_string_buffer(size)
    decode = dll.decompress_data
    decode.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p, ctypes.c_longlong, ctypes.c_int]
    decode.restype = ctypes.c_longlong
    assert decode(raw[18:], len(raw) - 18, output, size, 0) == size
    # Includes every vertex and edge; texture rows retain native bottom-left UV ordering.
    weights = np.array([[i / 20, j / 20, 1 - (i + j) / 20]
                        for i in range(21) for j in range(21 - i)])
    points = np.einsum('sv,fvd->fsd', weights, uv[faces]).reshape(-1, 2)
    records, at = [], 0
    for mip in range(levels):
        size = width * height * 4
        pixels = np.frombuffer(output.raw[at:at + size], np.uint8).reshape(height, width, 4)
        at += size
        xy = points * [width, height] - .5
        low = np.floor(xy).astype(int)
        fraction = xy - low
        samples = np.zeros((len(points), 4))
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            weight = (fraction[:, 0] if dx else 1 - fraction[:, 0]) * (fraction[:, 1] if dy else 1 - fraction[:, 1])
            samples += weight[:, None] * pixels[np.clip(low[:, 1] + dy, 0, height - 1),
                                                np.clip(low[:, 0] + dx, 0, width - 1)]
        records.append(dict(mip=mip, samples=len(samples),
                            alphaPercentiles=np.percentile(samples[:, 3], [0, 1, 5, 50, 100]).tolist(),
                            rgbMinimum=samples[:, :3].min(0).tolist(),
                            belowHalfAlpha=int((samples[:, 3] < 127.5).sum())))
        width, height = max(1, width // 2), max(1, height // 2)
    result = dict(attributes=str(args.attributes), texture=str(args.texture), triangles=len(faces),
                  tangentDotMinimum=float(min(dots)), handednessMismatches=hands.count(False),
                  packedNormalsDecoded=np.unique(normals, axis=0).tolist(),
                  mips=records, limitation='Offline bilinear samples, not native rendered or anisotropic visual acceptance.')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(triangles=len(faces), tangentDotMinimum=min(dots), handednessMismatches=hands.count(False))))


if __name__ == '__main__':
    main()
