"""Offline Draw03 paint removal within its original 968-texel road period.

This authors a replacement for occluded texels; it does not claim to recover
unknown original asphalt. All channels use the same donor coordinates. Native
source mip samples outside the conservative paint mask remain byte-identical.
"""
from collections import deque
from pathlib import Path
import argparse
import ctypes
import hashlib
import json
import math
import struct
import zipfile
import numpy as np
from PIL import Image
from InspectWearSources import bc7
from PrepareRecipes import string

ROOT = Path(__file__).resolve().parents[3]
PROOF = ROOT / 'artifacts/prepared-materials/Draw03/repeat-units-v2'
NAMES = ('_BaseColorMap', '_NormalMap', '_MaskMap')
CIDS = ('c14edc56a02c1d7b56942b5cfcf2873c', 'af4d79635cc9bbaedd6d46ec066774b3',
        '57e96ebbfe702a5d07d2beacec3afe26')
RECIPE = 'BridgeBuilder/Draw03/OriginalPeriodPaintRemoval/1'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def linear(pixels, color):
    value = pixels.astype(np.float32) / 255
    if color:
        rgb = value[..., :3]
        value[..., :3] = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
    return value


def encoded(value, color):
    value = np.clip(value, 0, 1).copy()
    if color:
        rgb = value[..., :3]
        value[..., :3] = np.where(rgb <= .0031308, rgb * 12.92, 1.055 * rgb ** (1 / 2.4) - .055)
    return np.rint(value * 255).astype(np.uint8)


def coordinates(shape, u, v):
    x = u * shape[1] - .5
    y = v * shape[0] - .5
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    return (np.clip(x0, 0, shape[1] - 1), np.clip(x0 + 1, 0, shape[1] - 1),
            np.clip(y0, 0, shape[0] - 1), np.clip(y0 + 1, 0, shape[0] - 1),
            (x - x0)[None, :, None], (y - y0)[:, None, None])


def sample(image, coords, color):
    x0, x1, y0, y1, fx, fy = coords
    a = linear(image[y0[:, None], x0], color) * (1 - fx) + linear(image[y0[:, None], x1], color) * fx
    b = linear(image[y1[:, None], x0], color) * (1 - fx) + linear(image[y1[:, None], x1], color) * fx
    return encoded(a * (1 - fy) + b * fy, color)


def reduce_delta(deltas, mask, x, y):
    before_x, before_y = x % 2, y % 2
    after_x, after_y = (mask.shape[1] + before_x) % 2, (mask.shape[0] + before_y) % 2
    padding = ((before_y, after_y), (before_x, after_x))
    expanded = np.pad(mask, padding)
    h, w = expanded.shape[0] // 2, expanded.shape[1] // 2
    result = [np.pad(d, (*padding, (0, 0))).reshape(h, 2, w, 2, 4).mean((1, 3)) for d in deltas]
    return result, expanded.reshape(h, 2, w, 2).any((1, 3)), x // 2, y // 2


def donors(mask):
    """Nearest non-paint source pixel, with deterministic vertical-first ties."""
    height, width = mask.shape
    yy, xx = np.indices(mask.shape)
    seen = ~mask.copy()
    queue = deque()
    for y, x in zip(*np.where(mask)):
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            a, b = y + dy, x + dx
            if 0 <= a < height and 0 <= b < width and not mask[a, b]:
                yy[y, x], xx[y, x] = a, b
                seen[y, x] = True
                queue.append((y, x))
                break
    while queue:
        y, x = queue.popleft()
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            a, b = y + dy, x + dx
            if 0 <= a < height and 0 <= b < width and not seen[a, b]:
                yy[a, b], xx[a, b] = yy[y, x], xx[y, x]
                seen[a, b] = True
                queue.append((a, b))
    return yy, xx, bool(seen.all())


def patch_donors(mask, full_base, bottom):
    """Copy nearby real asphalt patches, rather than extruding a single row."""
    yy, xx = np.indices(mask.shape)
    yy += bottom
    xx += 3084
    visited = np.zeros_like(mask)
    rows = []
    for sy, sx in zip(*np.where(mask)):
        if visited[sy, sx]:
            continue
        queue, points = deque([(sy, sx)]), []
        visited[sy, sx] = True
        while queue:
            y, x = queue.popleft()
            points.append((y, x))
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                a, b = y + dy, x + dx
                if 0 <= a < len(mask) and 0 <= b < mask.shape[1] and mask[a, b] and not visited[a, b]:
                    visited[a, b] = True
                    queue.append((a, b))
        points = np.asarray(points)
        ys, xs = points[:, 0], points[:, 1]
        low, high = int(ys.min()), int(ys.max())
        found = None
        for distance in range(high - low + 9, 129):
            for direction in (-1, 1):
                shift = distance * direction
                gy = bottom + ys + shift
                # Keep donors inside the measured full asphalt cross-section.
                if gy.min() < 444 or gy.max() >= 1580:
                    continue
                sample = full_base[gy, xs + 3084, :3]
                if np.any(np.all(sample > 175, axis=1)) or np.percentile(sample.min(1), 99) > 145:
                    continue
                found = shift
                break
            if found is not None:
                break
        if found is None:
            raise ValueError('No local unpainted native patch donor')
        yy[ys, xs] += found
        rows.append(dict(maskBounds=[int(xs.min()), low, int(xs.max()) + 1, high + 1],
                         donorTranslation=[0, found]))
    return yy, xx, rows


class Writer:
    def __init__(self, game, output):
        self.output = output
        self.codec = ctypes.CDLL(str(game / 'Plugins/x86_64/PipelinePlugin.dll'))
        self.bound = self.codec.compress_calc_bound
        self.bound.argtypes = [ctypes.c_longlong, ctypes.c_int]
        self.bound.restype = ctypes.c_longlong
        self.encode = self.codec.compress_data
        self.encode.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p,
                               ctypes.c_longlong, ctypes.c_int, ctypes.c_int]
        self.encode.restype = ctypes.c_longlong
        self.decode = self.codec.decompress_data
        self.decode.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p,
                               ctypes.c_longlong, ctypes.c_int]
        self.decode.restype = ctypes.c_longlong

    def save(self, payload, extension, folder=None, filename=None):
        cid = digest((RECIPE + '/' + digest(payload)).encode())[:32]
        target = self.output / (folder or 'Shared') / (filename or (cid + extension))
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            assert target.read_bytes() == payload, target
        else:
            target.write_bytes(payload)
            Path(str(target) + '.cid').write_text(cid, encoding='utf-8')
        return cid, len(payload) + 32

    def texture(self, levels, color, repeat=True):
        raw = b''.join(p.tobytes() for p in levels)
        bound = self.bound(len(raw), 0)
        encoded = ctypes.create_string_buffer(bound)
        size = self.encode(raw, len(raw), encoded, bound, 0, 11)
        assert 0 < size <= bound
        restored = ctypes.create_string_buffer(len(raw))
        assert self.decode(encoded, size, restored, len(raw), 0) == len(raw)
        assert restored.raw == raw
        h, w = levels[0].shape[:2]
        # Native Texture v5, RGBA8 SRGB/UNorm, 2D, trilinear, repeat, aniso 16.
        header = struct.pack('<4H6Bi', 5, w, h, 1, len(levels), 4 if color else 8,
                             2, 2, 0 if repeat else 1, 16, size)
        return self.save(header + encoded.raw[:size], '.Texture')

    def surface(self, bindings, name, template=0):
        payload = struct.pack('<Hi?Bi', 1, template, False, 255, 1) + string('_Snow') + struct.pack('<f', 0)
        payload += (b'\xff' + struct.pack('<i', 0)) * 3
        payload += b'\xff' + struct.pack('<i', len(bindings))
        for prop, cid in bindings.items():
            encoded = bytes(int(cid[i:i + 2][::-1], 16) for i in range(0, 32, 2))
            payload += string(prop) + b'\xff' + encoded
        payload += b'\xff' + struct.pack('<i', 1) + string('_TANGENTSPACE_OCTO')
        return self.save(payload, '.Surface', 'RepeatUnits/' + name, 'Repeat.Surface')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-data', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    scan = json.loads((PROOF / 'authoring-texture-scan.json').read_text())
    sources, source_records = [], []
    for cid in CIDS:
        entry = next(row for row in scan['found'] if row['cid'] == cid)
        with zipfile.ZipFile(entry['path']) as archive:
            raw = archive.read(entry['entry'])
        assert digest(raw) == entry['sha256']
        sources.append([pixels for _, pixels in bc7(raw)])
        source_records.append(dict(cid=cid, sha256=digest(raw)))
    assert all(len(s) == 13 and s[0].shape == (4096, 4096, 4) for s in sources)
    writer = Writer(args.game_data, args.output / 'Materials/Draw03')
    zero, _ = writer.texture([np.zeros((1, 1, 4), dtype=np.uint8)], False)
    reports = []
    for name, bottom, top, remove_paint in (
            ('FullPeriodLaneWear', 818, 1194, True), ('FullPeriodLaneGap', 802, 834, True),
            ('FullPeriodCleanAsphalt', 1312, 1476, False), ('FullPeriodGrating', 44, 420, False)):
        width, height = 968, top - bottom
        # This is an observed white-paint seed restricted to the road source
        # domain. No texture-global threshold can alter structural content.
        base = sources[0][0][bottom:top, 3084:4052]
        roi_bottom, roi_top = bottom - 16, top + 16
        roi = sources[0][0][roi_bottom:roi_top, 3084:4052]
        seeds = np.all(roi[:, :, :3] > 180, axis=2) if remove_paint else np.zeros(roi.shape[:2], dtype=bool)
        assert not remove_paint or (seeds.any() and not seeds[:, :296].any())
        # Include the native BC7 block footprint and one adjacent block for its
        # filtered edge. A seed-only mask leaves grey antialiasing from paint as
        # a replacement donor. This bounded influence mask is reported explicitly.
        mask0 = np.zeros_like(seeds)
        for y, x in zip(*np.where(seeds)):
            by, bx = (roi_bottom + y) // 4 * 4 - roi_bottom, (3084 + x) // 4 * 4 - 3084
            mask0[max(0, by - 4):min(len(roi), by + 8), max(0, bx - 4):min(width, bx + 8)] = True
        donor_y, donor_x, donor_records = patch_donors(mask0, sources[0][0], roi_bottom)
        deltas = []
        for channel in range(3):
            original = sources[channel][0][roi_bottom:roi_top, 3084:4052]
            composed = original.copy()
            composed[mask0] = sources[channel][0][donor_y[mask0], donor_x[mask0]]
            deltas.append(linear(composed, channel == 0) - linear(original, channel == 0))
        outputs = [[], [], []]
        mip_reports = []
        guarded_height = 1 << int(math.ceil(math.log2(height * 2)))
        guard = (guarded_height - height) // 2
        mask, x0, y0 = mask0, 3084, roi_bottom
        for mip in range(1 + int(math.log2(max(width, guarded_height)))):
            w, h = max(1, width >> mip), max(1, guarded_height >> mip)
            u = (3084 + (np.arange(w) + .5) / w * width) / 4096
            # Preserve native filtering context immediately outside the finite
            # domain instead of clamping its boundary halfway between texels.
            # Farther guard rows hold this bounded context; no V repetition.
            context = max(8, 1 << mip) / height
            finite_v = np.clip(((np.arange(h) + .5) / h - guard / guarded_height) / (height / guarded_height),
                               -context, 1 + context)
            v = (bottom + finite_v * height) / 4096
            coords = coordinates(sources[0][mip].shape, u, v)
            native_mask = np.zeros(sources[0][mip].shape[:2], dtype=bool)
            native_mask[y0:y0 + mask.shape[0], x0:x0 + mask.shape[1]] = mask
            ax, bx, ay, by, _, _ = coords
            output_mask = (native_mask[ay[:, None], ax] | native_mask[ay[:, None], bx]
                           | native_mask[by[:, None], ax] | native_mask[by[:, None], bx])
            source_hashes = []
            for channel in range(3):
                original = sources[channel][mip]
                corrected = original.copy()
                region = original[y0:y0 + mask.shape[0], x0:x0 + mask.shape[1]]
                corrected_region = corrected[y0:y0 + mask.shape[0], x0:x0 + mask.shape[1]]
                corrected_region[mask] = encoded(linear(region, channel == 0) + deltas[channel], channel == 0)[mask]
                assert np.array_equal(corrected_region[~mask], region[~mask])
                baseline = sample(original, coords, channel == 0)
                value = sample(corrected, coords, channel == 0)
                assert np.array_equal(value[~output_mask], baseline[~output_mask])
                outputs[channel].append(value)
                source_hashes.append(digest(original.tobytes()))
            mip_reports.append(dict(mip=mip, width=w, height=h, paintMaskPixels=int(mask.sum()),
                                    outsideMaskMaximumError=0, globalSourceOrigin=[x0, y0],
                                    sourceHashes=source_hashes,
                                    outputHashes=[digest(p[-1].tobytes()) for p in outputs]))
            deltas, mask, x0, y0 = reduce_delta(deltas, mask, x0, y0)
        bindings, total = {}, 0
        # Preserve U's exact 968-texel period. Only finite V receives duplicated
        # edge guards; the runtime maps to the unchanged active source rows.
        for channel, prop in enumerate(NAMES):
            cid, size = writer.texture(outputs[channel], channel == 0)
            bindings[prop] = cid
            total += size
        bindings['_ControlMask'] = zero
        surface, size = writer.surface(bindings, name, 1 if name == 'FullPeriodGrating' else 0)
        total += size
        review = args.output / 'review'
        review.mkdir(parents=True, exist_ok=True)
        Image.fromarray(base).save(review / (name + '-before.png'))
        Image.fromarray(outputs[0][0][guard:guard + height]).save(review / (name + '-after.png'))
        Image.fromarray(mask0[16:16 + height].astype(np.uint8) * 255).save(review / (name + '-mask.png'))
        reports.append(dict(name=name, surface=surface, bindings=bindings, periodTexels=width,
                            periodMetres=8, sourceBounds=[3084, bottom, 4052, top],
                            nativeMaterialBytes=total, paintSeedPixels=int(seeds.sum()),
                            donorPatches=donor_records,
                            textureSize=[width, guarded_height], finiteAxis='V',
                            finiteUvOffset=guard / guarded_height,
                            finiteUvScale=height / guarded_height,
                            authoredActiveRows=[44.53125, 419.5] if name == 'FullPeriodGrating' else [bottom, top],
                            maskRule='Native BC7 seed blocks plus one adjacent block for paint edge influence',
                            mips=mip_reports))
    report = dict(recipe=RECIPE, sources=source_records, units=reports,
                  scope='Offline authored paint removal, not recovery of unknown occluded asphalt.',
                  pending=['Native loading, finite-axis filtering guards, shader sampling, package budget and runtime integration.'])
    (args.output / 'composition.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({u['name']: u['nativeMaterialBytes'] for u in reports}))


if __name__ == '__main__':
    main()
