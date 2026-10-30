"""Compose paint-free Draw03 far road islands in their unchanged finite atlas."""
from pathlib import Path
from collections import deque
import argparse
import json
import struct
import sys
import zipfile
import numpy as np
from PIL import Image
from ComposeRoadPeriods import Writer, donors, digest, NAMES, ROOT

sys.path.insert(0, str(ROOT / 'tools/PrepareRepeatUnits'))
from ExtractVtSources import Decoder


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-data', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--mapping', type=Path, required=True)
    args = parser.parse_args()
    source_cid = '9212c7ed45a80abe105853ffe71bc8c4'
    with zipfile.ZipFile(args.game_data / 'Content/BridgesAndPorts/VT.cok') as archive:
        raw = archive.read('DrawBridge03_LOD2_dee8e815ea6fb17c0cb1a6c7d5af7f55.VTSurface')
    assert digest(raw) == '73a279521e3cd6fa993c0f266ae99650ff14c6675447efc6084b09a12fe1b68d'
    decoder, sources, evidence = Decoder(args.game_data), [], []
    for layer, name in enumerate(NAMES):
        texture, proof = decoder.recover(dict(kind='VTSurface', stack=0, layer=layer, name=name), raw, source_cid)
        version, width, height, depth, mips, fmt = struct.unpack_from('<4H2B', texture)
        assert (version, width, height, depth, mips) == (5, 512, 512, 1, 10)
        assert struct.unpack_from('<i', texture, 14)[0] == -1
        offset, levels = 18, []
        for mip in range(mips):
            side = max(1, 512 >> mip)
            size = ((side + 3) // 4) ** 2 * 16
            levels.append(np.asarray(Image.frombytes('RGBA', (side, side), texture[offset:offset + size], 'bcn', (7, 'BC7'))))
            offset += size
        assert offset == len(texture)
        sources.append(levels)
        evidence.append(dict(channel=name, extractedSha256=digest(texture), **proof))
    # Offline original-face atlas coverage, never runtime geometry operations.
    coverage = np.zeros((512, 512), dtype=bool)
    mapping = json.loads(args.mapping.read_text())
    assert mapping['sourceSurface'] == source_cid
    for face in mapping['roadTriangles']:
        uv = np.asarray(face['sourceUv']) * 512
        low = np.maximum(0, np.floor(uv.min(0)).astype(int))
        high = np.minimum(512, np.ceil(uv.max(0)).astype(int))
        for y in range(low[1], high[1]):
            for x in range(low[0], high[0]):
                point = np.array([x + .5, y + .5])
                a, b = np.linalg.solve(np.column_stack((uv[1] - uv[0], uv[2] - uv[0])), point - uv[0])
                if a >= 0 and b >= 0 and a + b <= 1:
                    coverage[y, x] = True
    # Native far paint is already reduced into the finite 512 atlas (maximum
    # 151/255 here); the near source's 180/255 seed is not transferable.
    seeds = coverage & np.all(sources[0][0][:, :, :3] > 130, axis=2)
    assert seeds.any(), (int(coverage.sum()), sources[0][0][coverage, :3].max(0).tolist(),
                         np.percentile(sources[0][0][coverage, :3].min(1), [50, 90, 99, 100]).tolist())
    mask0 = seeds.copy()
    for y, x in zip(*np.where(seeds)):
        mask0[max(0, y - 1):min(512, y + 2), max(0, x - 1):min(512, x + 2)] = True
    # The coverage does not expand into unrelated structural atlas islands.
    mask0 &= coverage
    donor_y, donor_x, _ = donors(mask0 | ~coverage)
    visited = np.zeros_like(mask0)
    for sy, sx in zip(*np.where(mask0)):
        if visited[sy, sx]:
            continue
        queue, points = deque([(sy, sx)]), []
        visited[sy, sx] = True
        while queue:
            y, x = queue.popleft()
            points.append((y, x))
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                a, b = y + dy, x + dx
                if 0 <= a < 512 and 0 <= b < 512 and mask0[a, b] and not visited[a, b]:
                    visited[a, b] = True
                    queue.append((a, b))
        points = np.asarray(points)
        ys, xs = points[:, 0], points[:, 1]
        # Shift along a dash's long direction to nearby unpainted road in the
        # same lane. This retains its transverse wear instead of sampling the
        # darker neighbouring wheel-track stripe.
        axes = ((0, 1), (1, 0)) if np.ptp(xs) >= np.ptp(ys) else ((1, 0), (0, 1))
        found = False
        for ay, ax in axes:
            for distance in range(1, 65):
                for sign in (-1, 1):
                    yy, xx = ys + ay * distance * sign, xs + ax * distance * sign
                    if yy.min() < 0 or yy.max() >= 512 or xx.min() < 0 or xx.max() >= 512:
                        continue
                    if np.all(coverage[yy, xx] & ~mask0[yy, xx]):
                        donor_y[ys, xs], donor_x[ys, xs] = yy, xx
                        found = True
                        break
                if found:
                    break
            if found:
                break
    outputs = [[], [], []]
    mip_records = []
    for mip in range(10):
        side, scale = max(1, 512 >> mip), 1 << mip
        mask = mask0.reshape(side, scale, side, scale).any((1, 3))
        yy, xx = np.indices((side, side))
        yy *= scale
        xx *= scale
        for y, x in zip(*np.where(mask)):
            local = mask0[y * scale:(y + 1) * scale, x * scale:(x + 1) * scale]
            a, b = np.argwhere(local)[0]
            yy[y, x] = donor_y[y * scale + a, x * scale + b]
            xx[y, x] = donor_x[y * scale + a, x * scale + b]
        yy >>= mip
        xx >>= mip
        complete = not mask.all()
        for channel in range(3):
            original = sources[channel][mip]
            value = original.copy()
            if complete:
                value[mask] = original[yy[mask], xx[mask]]
            else:
                previous = outputs[channel][-1]
                value[:] = np.rint(previous.reshape(side, 2, side, 2, 4).mean((1, 3))).astype(np.uint8)
            assert np.array_equal(value[~mask], original[~mask])
            outputs[channel].append(value)
        mip_records.append(dict(mip=mip, width=side, maskPixels=int(mask.sum()), outsideMaskMaximumError=0,
                                originalHashes=[digest(s[mip].tobytes()) for s in sources],
                                outputHashes=[digest(s[-1].tobytes()) for s in outputs]))
    assert not np.any(coverage & np.all(outputs[0][0][:, :, :3] > 130, axis=2))
    writer = Writer(args.game_data, args.output / 'Materials/Draw03')
    bindings, total = {}, 0
    for channel, name in enumerate(NAMES):
        cid, size = writer.texture(outputs[channel], channel == 0, repeat=False)
        bindings[name] = cid
        total += size
    bindings['_ControlMask'], _ = writer.texture([np.zeros((1, 1, 4), dtype=np.uint8)], False)
    surface, size = writer.surface(bindings, 'FarRoadAtlas', template=1)
    total += size
    for name, pixels in (('before', sources[0][0]), ('after', outputs[0][0]), ('mask', mask0.astype(np.uint8) * 255)):
        Image.fromarray(pixels).save(args.output / ('far-' + name + '.png'))
    report = dict(sourceSurface=source_cid, surface=surface, bindings=bindings, nativeMaterialBytes=total,
                  sourceExtraction=evidence, mips=mip_records, seedPixels=int(seeds.sum()),
                  scope='New road-only composition in unchanged finite atlas; original structural Surface remains unchanged.')
    (args.output / 'far-composition.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(nativeMaterialBytes=total, seedPixels=int(seeds.sum()), mips=10)))


if __name__ == '__main__':
    main()
