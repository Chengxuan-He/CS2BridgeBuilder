"""Measure Lift01 repeat density from real native headers without Unity or baking."""
import argparse
import hashlib
import json
import math
import struct
import zipfile
from pathlib import Path
from ExportRecipes import numbers, recipe_key


def hash_text(data):
    return ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in data)


def native_bytes(game, cid, prefix):
    found = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (game / 'Content' / family).glob(prefix + '*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if entry.endswith('.cid') and archive.read(entry).decode().strip() == cid:
                        value = archive.read(entry[:-4])
                        if found is not None and value != found:
                            raise ValueError('Conflicting native identity: ' + cid)
                        found = value
    if found is None:
        raise ValueError('Native identity absent: ' + cid)
    return found


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('game_data', type=Path)
    p.add_argument('inventory', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--native-mesh', type=Path)
    a = p.parse_args()
    inventory = json.loads(a.inventory.read_text())
    if len(inventory['units']) != 1:
        raise ValueError('Lift01 repeat inventory scope changed')
    unit = inventory['units'][0]
    source = native_bytes(a.game_data, unit['sourceCid'], 'Blob')
    if hashlib.sha256(source).hexdigest() != unit['sourceDigest']:
        raise ValueError('Source descriptor changed')
    if recipe_key(unit['sourceDigest'], unit) != unit['key']:
        raise ValueError('Runtime lookup key mismatch')
    if source[:2] != b'\x01\x00' or source[6] != 1:
        raise ValueError('Expected native VT descriptor')
    header = source[7:283]
    surface = native_bytes(a.game_data, hash_text(source[284:300]), 'VT') if source[283] == 255 else None
    channels = []
    for channel in inventory['sources'][0]['channels']:
        if channel['name'] not in ('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask'):
            continue
        stack, layer = channel['stack'], channel['layer']
        if surface is not None and surface[106 + stack * 192 + layer * 48] != 0:
            if surface[:2] != b'\x01\x00':
                raise ValueError('Unexpected VTSurface version')
            width, height = struct.unpack_from('<2i', surface, 26 + stack * 40)
            backing = 'VTSurface'
        else:
            offset = 76 + stack * 136 + layer * 16
            cid = hash_text(header[offset:offset + 16])
            if cid == '0' * 32:
                if channel['name'] == '_ControlMask':
                    channels.append(dict(name=channel['name'], width=0, height=0, backing='absent; constant control has no physical density'))
                    continue
                raise ValueError('Missing mandatory native channel')
            payload = native_bytes(a.game_data, cid, 'VT')
            if payload[:3] != b'\x01\x00\xff':
                raise ValueError('Unexpected VTTexture header')
            width, height = struct.unpack_from('<2H', payload, 23)
            backing = cid
        channels.append(dict(name=channel['name'], width=width, height=height, backing=backing))
    if len(channels) != 4:
        raise ValueError('Incomplete source channel measurement')
    nw, nh = max(c['width'] for c in channels), max(c['height'] for c in channels)
    span = [unit['max'][axis] - unit['min'][axis] for axis in (0, 1)]
    size = [4 * math.ceil(span[axis] * dim / 4) for axis, dim in enumerate((nw, nh))]
    f = numbers(a.repository / 'assets/HardcodedNumbers/Lift01/Types_Lift01_Lift01RoadAppearance_cs.bin')['F32']
    gradients = [abs(f[4]), abs(f[11])]
    before = [nw * span[0] * gradients[0], nh * span[1] * gradients[1]]
    after = [size[axis] * gradients[axis] for axis in (0, 1)]
    if any(after[axis] < before[axis] for axis in (0, 1)):
        raise ValueError('Prepared recipe reduces density')
    # The four half/side combinations use opposite gradient signs but the same
    # metric. The moving/fixed distinction changes only the phase, not the scale.
    projections = [dict(region=moving * 4 + half * 2 + side,
                        u=(-f[4] if side == 0 else f[5]),
                        v=(-f[11] if half == 0 else f[12]),
                        phase=f[13] / f[14] if moving == 0 else f[15])
                   for moving in range(2) for half in range(2) for side in range(2)]
    result = dict(style='Lift01', key=unit['key'], sourceCid=unit['sourceCid'],
                  sourceChannels=channels, preparedDimensions=size,
                  nativeTexelsPerMeter=before, blockAlignedTexelsPerMeter=after,
                  densityRatios=[after[i] / before[i] for i in (0, 1)],
                  receiverProjections=projections,
                  verification='Native headers and immutable recipe only; no baked output or GPU upload verified.')
    if a.native_mesh is not None:
        import numpy as np
        native = np.load(a.native_mesh)
        measured = []
        # These two exact source faces are identified in Lift01RoadAppearance.cs.
        # They are offline evidence only, not new runtime prototype geometry.
        for face_index in (11144, 11145):
            ids = native['mesh0_faces'][face_index]
            positions = native['mesh0_positions'][ids].astype(np.float64)
            uv = native['mesh0_uv0'][ids, :2].astype(np.float64)
            if not np.all(positions[:, 1] == positions[0, 1]):
                raise ValueError('Measured Lift01 reference face is no longer horizontal')
            xz = positions[:, (0, 2)]
            jacobian = np.linalg.solve(xz[1:] - xz[0], uv[1:] - uv[0])
            density = np.linalg.norm(jacobian * np.array([nw, nh]), axis=1)
            if not np.array_equal(density, np.array(before)):
                raise ValueError('Lift01 immutable projection differs from actual prototype texel density')
            measured.append(dict(face=face_index, nativeTexelsPerMeter=density.tolist(),
                                 uvJacobian=jacobian.tolist()))
        result['nativeMeshEvidence'] = dict(sha256=hashlib.sha256(a.native_mesh.read_bytes()).hexdigest(), faces=measured)
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift01 source density: source={nw}x{nh}, repeat={size[0]}x{size[1]}, native={before}, block-aligned={after}; 8 projection aliases.')


if __name__ == '__main__':
    main()
