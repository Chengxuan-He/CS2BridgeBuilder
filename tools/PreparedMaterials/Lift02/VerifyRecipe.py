"""Compare Lift02's extracted road repeat with the two real source top faces."""
import argparse
import json
import math
import struct
from pathlib import Path

import numpy as np


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_mesh', type=Path)
    parser.add_argument('inventory', type=Path)
    parser.add_argument('report', type=Path)
    parser.add_argument('--texture-width', type=int)
    parser.add_argument('--texture-height', type=int)
    args = parser.parse_args()
    data = np.load(args.native_mesh)
    faces = data['mesh0_faces'][83248:83250]
    p = data['mesh0_positions'][faces].astype(np.float64)
    uv = data['mesh0_uv0'][faces].astype(np.float64)
    inventory = json.loads(args.inventory.read_text())
    if len(inventory['units']) != 1:
        raise ValueError('Unexpected number of Lift02 source recipes')
    unit, evidence = inventory['units'][0], inventory['evidence']
    channels = [c for c in evidence['nativeChannels'] if c['name'] in
                ('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask')]
    width, height = max(c['width'] for c in channels), max(c['height'] for c in channels)
    if args.texture_width not in (None, width) or args.texture_height not in (None, height):
        raise ValueError('Requested source dimensions differ from the native VT header')
    low, high = uv.min((0, 1)), uv.max((0, 1))
    if any(struct.pack('<f', x) != struct.pack('<f', y) for x, y in zip([*low, *high], unit['min'] + unit['max'])):
        raise ValueError('Recipe rectangle differs from the real prototype top')
    source_span = p.max((0, 1)) - p.min((0, 1))
    if source_span[0] != evidence['transverseRepeatMetres'] or source_span[2] != evidence['longitudinalRepeatMetres']:
        raise ValueError('Recorded physical repeat dimensions differ from the prototype')
    size = [4 * math.ceil((high[0] - low[0]) * width / 4),
            4 * math.ceil((high[1] - low[1]) * height / 4)]
    source_pixels = (high - low) * np.array([width, height])
    axes = []
    for i, (positions, coords) in enumerate(zip(p, uv)):
        # A unit physical X or Z displacement is compared in each triangle's
        # actual affine map, independently of the authoring projection code.
        basis = np.stack([positions[1] - positions[0], positions[2] - positions[0]])[:, [0, 2]]
        gradient = np.linalg.solve(basis, np.stack([coords[1] - coords[0], coords[2] - coords[0]]))
        native = np.linalg.norm(gradient * [width, height], axis=1)
        prepared = np.linalg.norm(gradient / (high - low) * size, axis=1)
        if np.any(prepared < native):
            raise ValueError('Prepared rectangle reduces native texel density')
        axes.append(dict(face=83248+i, originalPixelsPerMetre=native.tolist(),
                         preparedPixelsPerMetre=prepared.tolist(),
                         physicalAxes=['X', 'Z']))
    report = dict(sourceFaces=[83248, 83249], sourceDimensions=[width, height],
                  nativeVTSurfaceCid=evidence['nativeVTSurfaceCid'], originalRectanglePixels=source_pixels.tolist(),
                  preparedPixels=size, blockAlignment='rounded upward to BC7 blocks',
                  periodMetresXZ=source_span[[0, 2]].tolist(), exactFloat32Bounds=True,
                  unchangedPhysicalPatternScale=True, noDensityReduction=True, axes=axes,
                  limits='Source recipe verification only; native baked payload and in-game appearance require separate checks.')
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift02 native source repeat: two real source triangles; bounds/period exact; {size[0]}x{size[1]}; no density loss.')


if __name__ == '__main__':
    main()
