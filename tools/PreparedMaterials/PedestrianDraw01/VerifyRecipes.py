"""Compare authored repeat rectangles with the actual prototype UV faces."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

from ExportRecipes import STYLE, key, patches, reachable_regions


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('native_mesh', type=Path)
    parser.add_argument('inventory', type=Path)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    inventory = json.loads(args.inventory.read_text())
    extracted, _ = patches(args.repository)
    reachable, routing = reachable_regions(args.repository)
    assert inventory['evidence']['routing'] == routing
    mesh = np.load(args.native_mesh)
    faces, uv, positions = mesh['mesh0_faces'], mesh['mesh0_uv0'].astype(np.float64), mesh['mesh0_positions'].astype(np.float64)
    # Face IDs are the recorded source evidence in PedestrianDraw01RoadAppearance.
    # Compare actual UV corners, including the distinct half-precision fixed minima.
    source_pairs = ((1035, 1036), (13243, 13244), (1091, 1092), (13299, 13300))
    checked = []
    for region, pair in enumerate(source_pairs):
        unit = next(u for u in inventory['sourceUnits'] if region in u['regions'])
        source = next(u for u in extracted if region in u['regions'])
        assert unit['style'] == STYLE and unit['cutout'] and not unit['rows'] and unit['bands'] is None
        assert unit['min'] == source['min'] and unit['max'] == source['max']
        indices = faces[list(pair)].flatten()
        actual_uv = uv[indices]
        assert np.array_equal(actual_uv.min(axis=0), unit['min'])
        assert np.array_equal(actual_uv.max(axis=0), unit['max'])
        assert unit['key'] == key(unit['sourceDigest'], unit['min'], unit['max'])
        extent = np.ptp(positions[indices], axis=0)[[0, 2]]
        gradients = (actual_uv.max(axis=0) - actual_uv.min(axis=0)) / extent
        checked.append(dict(region=region, faces=pair, key=unit['key'], prepared=region in reachable, physicalUnitMetres=extent.tolist(),
                            atlasUvPerMetre=gradients.tolist()))
    assert len(inventory['sourceUnits']) == 3
    assert len(inventory['units']) == 2
    assert inventory['units'] == [u for u in inventory['sourceUnits'] if reachable.intersection(u['regions'])]
    assert checked[0]['key'] != checked[1]['key']
    assert checked[2]['key'] == checked[3]['key']
    report = dict(style=STYLE, regions=checked, uniqueSourceRecipes=3, preparedRepeatRecipes=2, nativeFacesVerified=8,
                  nativeMeshSHA256=hashlib.sha256(args.native_mesh.read_bytes()).hexdigest(),
                  inventorySHA256=hashlib.sha256(args.inventory.read_bytes()).hexdigest(),
                  limits='Source recipe and native UV evidence only; baked channels and in-game appearance require separate verification.')
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('PASS PedestrianDraw01: 8 native faces; 4 source-density regions; 2 reachable repeats; moving source recipe retained only as evidence.')


if __name__ == '__main__':
    main()
