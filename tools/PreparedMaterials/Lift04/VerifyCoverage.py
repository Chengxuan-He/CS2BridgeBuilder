"""Check the Lift04 near repeat recipe and measured anchor density without Unity.

This does not certify all near/mid domains or the separate finite far mapping.
"""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path
import numpy as np
from ExportRecipes import numbers, source_recipes, repeat_key


def hash_text(raw):
    return ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)


def read_assets(game, requested):
    found = {}
    for family in ('Game', 'BridgesAndPorts'):
        for path in (game / 'Content' / family).glob('*.cok'):
            if not path.name.startswith(('Blob', 'VT')):
                continue
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.cid'):
                        continue
                    cid = archive.read(entry).decode().strip()
                    if cid not in requested:
                        continue
                    payload = archive.read(entry[:-4])
                    if cid in found and found[cid] != payload:
                        raise ValueError('Conflicting native source CID: ' + cid)
                    found[cid] = payload
    if set(found) != set(requested):
        raise ValueError('Missing native sources: ' + str(set(requested) - set(found)))
    return found


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('native_mesh', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    recipes = json.loads((Path(__file__).parent / 'recipes.json').read_text())
    units, evidence = source_recipes(args.repository)
    unit = recipes['units'][0]
    assert repeat_key(unit['sourceDigest'], units[0]) == unit['key']
    preparation = (args.repository / 'src/BridgeBuilder/Bridges/Common/Movable/MovableRoadPreparation.cs').read_text(encoding='utf-8-sig')
    if 'appearanceForLod?.Invoke(lod)' not in preparation or 'lod < sources.Count' not in preparation:
        raise ValueError('Review changed source material/LOD routing')
    renders = (args.repository / 'src/BridgeBuilder/Bridges/Types/Lift04/Lift04SourceRenders.cs').read_text(encoding='utf-8-sig')
    lods = re.findall(r'Create\("LiftBridge04(?:_LOD\d+)? Mesh",', renders)
    assert len(lods) == 3
    appearance = (args.repository / 'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RoadAppearance.cs').read_text(encoding='utf-8-sig')
    if 'repeat: true' not in appearance or 'Surface =' in appearance or 'bands:' in appearance:
        raise ValueError('Review changed Lift04 material recipe variants')
    if numbers(args.repository / 'assets/HardcodedNumbers/Lift04/Types_Lift04_Lift04RoadAppearance_cs.bin')['I32'] != (0, 0, 2, 1):
        raise ValueError('Review changed fixed/moving appearance indices')
    payload = read_assets(args.game_data, {unit['sourceCid']})[unit['sourceCid']]
    assert hashlib.sha256(payload).hexdigest() == unit['sourceDigest']
    assert payload[:7] == b'\x01\x00\x00\x00\x00\x00\x01' and payload[283] == 0
    # This measured descriptor uses per-channel VTTexture backing, not VTSurface.
    refs = [hash_text(payload[83 + i * 16:99 + i * 16]) for i in range(4)]
    assert refs[3] == '0' * 32
    textures = read_assets(args.game_data, set(refs[:3]))
    channels = []
    for name, cid in zip(('_BaseColorMap', '_NormalMap', '_MaskMap'), refs):
        data = textures[cid]
        assert data[:3] == b'\x01\x00\xff' and struct.unpack_from('<H', data, 19)[0] == 0
        tile, width, height = struct.unpack_from('<3H', data, 21)
        channels.append(dict(name=name, cid=cid, width=width, height=height, tile=tile,
                             mipCount=data[29], graphicsFormat=data[30],
                             payloadSHA256=hashlib.sha256(data).hexdigest()))
    size = np.max([[c['width'], c['height']] for c in channels], axis=0)
    extent = np.array(unit['max']) - unit['min']
    source_pixels = extent * size
    prepared_pixels = np.ceil(source_pixels / 4).astype(int) * 4
    mesh = np.load(args.native_mesh)
    faces, positions, uv = (mesh['mesh0_' + name] for name in ('faces', 'positions', 'uv0'))
    checks = []
    # Measured prototype pairs referenced by Lift04RoadAppearance.cs, in region order.
    for region, pair, length in ((0, (33328, 33329), evidence['projectionLengths'][0]),
                                 (1, (10581, 10582), evidence['projectionLengths'][1]),
                                 (2, (28426, 28427), evidence['projectionLengths'][2]),
                                 (3, (4198, 4199), evidence['projectionLengths'][2])):
        ids = np.unique(faces[list(pair)])
        assert np.array_equal(uv[ids].min(axis=0), unit['min'])
        assert np.array_equal(uv[ids].max(axis=0), unit['max'])
        expected = np.array([source_pixels[1] / evidence['lateralLength'], source_pixels[0] / length])
        for face in pair:
            p = positions[faces[face]][:, (0, 2)]
            gradient = np.linalg.solve(np.column_stack((p, np.ones(3))), uv[faces[face]].astype(np.float64))[:2]
            actual = np.linalg.norm(gradient * size, axis=1)
            relative = np.abs(actual / expected - 1)
            assert relative.max() < 0.00002, (region, face, actual, expected)
            checks.append(dict(region=region, sourceFace=face, nativePixelsPerMetre=actual.tolist(),
                               recordedPixelsPerMetre=expected.tolist(), maxRelativeError=float(relative.max())))
    report = dict(style='Lift04', recipeCount=1, appearanceAliases=4, roadLodCount=len(lods),
                  channels=channels, missingControl='Opaque native blackTexture, supplied by the shared offline recipe',
                  sourceUnitPixels=source_pixels.tolist(), preparedUnitPixels=prepared_pixels.tolist(),
                  alignmentDensityRatio=(prepared_pixels / source_pixels).tolist(), checks=checks,
                  reachability='Historical repeat-recipe anchors only. Active near/mid Ballast mapping uses one hundred complete original-atlas cells; far uses seven finite native charts. This check does not certify active wrap seams.',
                  sourceMeshSHA256=hashlib.sha256(args.native_mesh.read_bytes()).hexdigest(),
                  scope='Native source and recipe verification only; actual prepared GPU payload and game rendering remain unverified.')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('PASS Lift04 near recipe: 3 native channels, 4 anchors / 8 source faces; density preserved before block alignment. Full-domain acceptance remains separate.')


if __name__ == '__main__':
    main()
