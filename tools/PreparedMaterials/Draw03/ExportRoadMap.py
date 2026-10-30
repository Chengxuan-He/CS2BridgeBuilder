"""Emit compact immutable Draw03 road affine data from measured source quads."""
from pathlib import Path
import argparse
import hashlib
import json
import struct
import numpy as np


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mapping', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    parser.add_argument('--composition', type=Path, required=True)
    args = parser.parse_args()
    records = json.loads(args.mapping.read_text())['roadTriangles']
    rows, maximum = [], 0.0
    for first, second in zip(records[::2], records[1::2]):
        points = np.asarray(first['position'] + second['position'])
        uv = np.asarray(first['sourceUv'] + second['sourceUv'])
        design = np.column_stack((points[:, 0], points[:, 2], np.ones(len(points))))
        coefficients = np.linalg.lstsq(design, uv, rcond=None)[0].astype(np.float32)
        error = float(np.abs(design @ coefficients - uv).max() * 512)
        assert error < 0.001, (first['face'], error)
        maximum = max(maximum, error)
        rows.append((float(points[:, 2].min()), float(points[:, 2].max()),
                     -1 if points[:, 0].mean() < 0 else 1, coefficients))
    rows.sort(key=lambda r: ((r[0] + r[1]) / 2, r[2]))
    assert len(rows) == 24 and all(rows[i][2] == -1 and rows[i + 1][2] == 1 for i in range(0, 24, 2))
    values = []
    for low, high, side, coefficients in rows:
        values.extend([low, high, *coefficients[:, 0], *coefficients[:, 1]])
    # Unit V guards: original finite source rows stay in the active interior.
    composed = {u['name']: u for u in json.loads(args.composition.read_text())['units']}
    for name in ('FullPeriodLaneWear', 'FullPeriodLaneGap', 'FullPeriodCleanAsphalt', 'FullPeriodGrating'):
        unit = composed[name]
        low, high = unit['authoredActiveRows']
        values.extend([unit['finiteUvOffset'] + (low - unit['sourceBounds'][1]) / unit['textureSize'][1],
                       (high - low) / unit['textureSize'][1]])
    values.extend([296 / 968, 9.39990234375, 12.5])
    payload = struct.pack('<8I', 0x314E4242, 1, 1, 0, 0, 0, len(values), 0)
    payload += struct.pack('<i', len(rows))
    payload += struct.pack('<' + 'f' * len(values), *values)
    payload += hashlib.sha256(payload).digest()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(payload)
    args.report.write_text(json.dumps(dict(quads=24, maximumSourceTexelError=maximum,
                                          sourceMappingSha256=hashlib.sha256(args.mapping.read_bytes()).hexdigest(),
                                          resourceSha256=hashlib.sha256(payload).hexdigest()), indent=2) + '\n')
    print('PASS 24 original finite quad maps; maximum texel residual', maximum)


if __name__ == '__main__':
    main()
