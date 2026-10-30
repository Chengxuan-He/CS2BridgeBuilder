"""Independent native-sampler comparison at off-grid points in unchanged units.

The reference samples the original full atlas mip, not a second extracted unit.
Reports filtering errors and alpha decisions; it does not choose a tolerance.
"""
from pathlib import Path
import argparse
import json
import math
import struct
import zipfile
import numpy as np
from ComposeRoadPeriods import Writer, linear, NAMES, CIDS, PROOF
from InspectWearSources import bc7


def native_sample(pixels, u, v, color, repeat):
    h, w = pixels.shape[:2]
    fx, fy = u * w - .5, v * h - .5
    x, y = np.floor(fx).astype(int), np.floor(fy).astype(int)
    dx, dy = fx - x, fy - y
    address = (lambda i, n: i % n) if repeat else (lambda i, n: np.clip(i, 0, n - 1))
    a, b = address(x, w), address(x + 1, w)
    c, d = address(y, h), address(y + 1, h)
    return ((linear(pixels[c[:, None], a], color) * (1 - dx)[None, :, None]
             + linear(pixels[c[:, None], b], color) * dx[None, :, None]) * (1 - dy)[:, None, None]
            + (linear(pixels[d[:, None], a], color) * (1 - dx)[None, :, None]
               + linear(pixels[d[:, None], b], color) * dx[None, :, None]) * dy[:, None, None])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--composition', type=Path, required=True)
    parser.add_argument('--game-data', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.composition.read_text())
    scan = json.loads((PROOF / 'authoring-texture-scan.json').read_text())
    writer = Writer(args.game_data, args.output.parent)
    units = {r['name']: r for r in manifest['units']}
    points_u = np.linspace(.00013, .99983, 257)
    points_v = np.linspace(0, 1, 129)
    reports = []
    for channel, prop in enumerate(NAMES):
        entry = next(row for row in scan['found'] if row['cid'] == CIDS[channel])
        with zipfile.ZipFile(entry['path']) as archive:
            source = [pixels for _, pixels in bc7(archive.read(entry['entry']))]
        for name in ('FullPeriodCleanAsphalt', 'FullPeriodGrating'):
            unit = units[name]
            path = args.composition.parent / 'Materials/Draw03/Shared' / (unit['bindings'][prop] + '.Texture')
            payload = path.read_bytes()
            version, width, height, depth, mips, fmt = struct.unpack_from('<4H2B', payload)
            size = struct.unpack_from('<i', payload, 14)[0]
            expected = sum(max(1, width >> m) * max(1, height >> m) * 4 for m in range(mips))
            import ctypes
            raw = ctypes.create_string_buffer(expected)
            assert writer.decode(payload[18:], size, raw, expected, 0) == expected
            low, high = unit['authoredActiveRows']
            offset_v = unit['finiteUvOffset'] + (low - unit['sourceBounds'][1]) / height
            scale_v = (high - low) / height
            cursor = 0
            for mip in range(mips):
                w, h = max(1, width >> mip), max(1, height >> mip)
                product = np.frombuffer(raw.raw, dtype=np.uint8, count=w * h * 4, offset=cursor).reshape(h, w, 4)
                cursor += w * h * 4
                actual = native_sample(product, points_u, offset_v + points_v * scale_v, channel == 0, True)
                reference = native_sample(source[mip], (3084 + points_u * 968) / 4096,
                                          (low + points_v * (high - low)) / 4096, channel == 0, False)
                error = np.abs(actual - reference)
                interior = (points_u >= 1 / w) & (points_u <= 1 - 1 / w)
                alpha = int(np.sum((actual[:, :, 3] >= .5) != (reference[:, :, 3] >= .5))) if channel == 0 else None
                maximum_angle = None
                if channel == 1:
                    def unpack(value):
                        xy = np.stack((value[:, :, 0] * value[:, :, 3], value[:, :, 1]), axis=-1) * 2 - 1
                        normal = np.concatenate((xy, np.sqrt(np.maximum(0, 1 - np.sum(xy * xy, axis=-1)))[..., None]), axis=-1)
                        return normal / np.maximum(np.linalg.norm(normal, axis=-1)[..., None], 1e-12)
                    angles = np.degrees(np.arccos(np.clip(np.sum(unpack(actual) * unpack(reference), axis=-1), -1, 1)))
                    maximum_angle = float(angles.max())
                reports.append(dict(unit=name, channel=prop, mip=mip, outputSize=[w, h],
                                    points=257 * 129, maximumLinearError=float(error.max()),
                                    interiorMaximumLinearError=float(error[:, interior].max()) if interior.any() else None,
                                    interiorP99=float(np.percentile(error[:, interior], 99)) if interior.any() else None,
                                    baseColorAlphaThresholdDecisionDifferences=alpha,
                                    maximumTangentNormalAngleDegrees=maximum_angle))
    args.output.write_text(json.dumps(dict(reference='Original full native mip sampled at independent off-grid points',
                                         toleranceSelected=False, levels=reports), indent=2) + '\n')
    print(json.dumps({name: max(r['maximumLinearError'] for r in reports if r['unit'] == name) for name in units if name in ('FullPeriodCleanAsphalt', 'FullPeriodGrating')}))


if __name__ == '__main__':
    main()
