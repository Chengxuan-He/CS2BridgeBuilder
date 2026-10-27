"""Recover original BC7 mip blocks from native VT carriers and MidMips.

Offline input adapter for RepeatUnits/2. This does not resample, recompress BC7,
generate mipmaps, or publish source atlases as prepared materials.
"""
import argparse
import ctypes
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import zipfile


def sha(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


def exact(stream, size):
    data = stream.read(size)
    if len(data) != size:
        raise ValueError('Incomplete native source')
    return data


def read(stream, fmt):
    return struct.unpack('<' + fmt, exact(stream, struct.calcsize('<' + fmt)))


def blocks(w, h):
    return ((w + 3) // 4) * ((h + 3) // 4) * 16


class MidCache:
    def __init__(self, game):
        self.archives = []
        self.entries = {}
        for path in sorted((game / 'Content').glob('*/MidMips*.cok')):
            archive = zipfile.ZipFile(path)
            self.archives.append(archive)
            for name in archive.namelist():
                if not name.endswith('.MidMips'):
                    continue
                with archive.open(name) as s:
                    tile, levels, group, stacks, groups, entries = read(s, '6i')
                    categories = []
                    for stack in range(stacks):
                        for _ in range(read(s, 'i')[0]):
                            w, h, count, first, tiles, disabled, bias = read(s, '5i?i')
                            categories.append((stack, w, h, count, disabled))
                    pending = []
                    for stack, w, h, count, disabled in categories:
                        if disabled:
                            continue
                        for _ in range(count):
                            material = cid(exact(s, 16))
                            mask, index, duplicates = read(s, 'ihi')
                            identities = [material] + [cid(exact(s, 16)) for _ in range(duplicates)]
                            indices = [read(s, 'ii') for _ in range(levels)]
                            pending.append((identities, stack, mask, index, indices, w, h))
                    tile_count = read(s, 'i')[0]
                    position = s.tell()
                    if read(s, 'q')[0] != position:
                        raise ValueError('Invalid MidMips header alignment')
                    start = s.tell()
                    for identities, stack, mask, index, indices, w, h in pending:
                        for identity in identities:
                            self.entries.setdefault((identity, stack), []).append(
                                (archive, name, tile, levels, mask, index, indices, start, tile_count, w, h))

    def recover(self, material, stack, layer, tile, levels, width, height):
        candidates = [e for e in self.entries.get((material, stack), [])
                      if e[2] == tile and e[3] == levels and e[4] & (1 << layer)
                      and (e[9], e[10]) == (width, height)]
        if not candidates:
            raise ValueError(f'Missing authored MidMips entry: {material}, stack {stack}, layer {layer}')
        results = []
        provenance = []
        for archive, name, _, _, mask, index, indices, start, count, _, _ in candidates:
            mips = []
            with archive.open(name) as stream:
                for level, (first, grouped_mask) in enumerate(indices):
                    if not grouped_mask & (1 << layer):
                        raise ValueError('MidMip layer mask excludes requested channel')
                    tile_index = first + (grouped_mask & ((1 << layer) - 1)).bit_count()
                    if not 0 <= tile_index < count:
                        raise ValueError('MidMip tile index outside carrier')
                    side = tile >> (level + 1)
                    # ZOrderer coordinates: consecutive bits alternate X, Y.
                    ordinal = index % (4 ** (level + 1))
                    x = sum(((ordinal >> (2 * bit)) & 1) << bit for bit in range(level + 1))
                    y = sum(((ordinal >> (2 * bit + 1)) & 1) << bit for bit in range(level + 1))
                    pitch = (tile + 16) // 4 * 16
                    tile_bytes = blocks(tile + 16, tile + 16)
                    offset = start + tile_index * (tile_bytes + tile_bytes // 4)
                    raw = bytearray()
                    for row in range(side // 4):
                        stream.seek(offset + (2 + y * side // 4 + row) * pitch + (2 + x * side // 4) * 16)
                        raw.extend(exact(stream, side // 4 * 16))
                    mips.append(bytes(raw))
            results.append(mips)
            provenance.append({'archive': str(archive.filename), 'entry': name,
                               'sha256PerMip': [sha(m) for m in mips]})
        if any(m != results[0] for m in results[1:]):
            raise ValueError('Conflicting authored MidMips candidates')
        return results[0], provenance


class Decoder:
    def __init__(self, game):
        self.library = ctypes.CDLL(str(game / 'Plugins/x86_64/PipelinePlugin.dll'))
        self.decode = self.library.decompress_data
        self.decode.argtypes = [ctypes.c_void_p, ctypes.c_int64, ctypes.c_void_p, ctypes.c_int64, ctypes.c_int]
        self.decode.restype = ctypes.c_int64
        self.mid = None
        self.game = game

    def decompress(self, data):
        marker, size, compressed = struct.unpack_from('<iii', data)
        if marker != 2147483647 or compressed != len(data) - 12 or size <= 0:
            raise ValueError('Invalid native compressed tile')
        output = ctypes.create_string_buffer(size)
        if self.decode(data[12:], compressed, output, size, 0) != size:
            raise ValueError('Native tile decompression failed')
        return output.raw

    def recover(self, channel, source, material):
        kind = channel['kind']
        stack = channel.get('stack', 0)
        layer = channel.get('layer', {'_BaseColorMap': 0, '_NormalMap': 1, '_MaskMap': 2, '_ControlMask': 3}[channel['name']])
        s = io.BytesIO(source)
        if kind == 'VTTexture':
            if read(s, 'HB')[0:2] != (1, 255):
                raise ValueError('Unsupported VTTexture header')
            exact(s, 16)
            mode, tile, width, height, mid, mips, fmt, filt, wrap, aniso, tail_size = read(s, '5H5Bi')
            if mode != 0:
                raise ValueError('Unsupported VT preprocessing')
            tail = exact(s, tail_size)
            count = read(s, 'i')[0]
            offsets = read(s, 'i' * count)
            start = s.tell()
            layer_offset = 0
            stride = blocks(tile + 16, tile + 16) * 5 // 4
        elif kind == 'VTSurface':
            if struct.unpack_from('<H', source)[0] != 1:
                raise ValueError('Unsupported VTSurface header')
            material = cid(source[2:18])
            tile, bias = struct.unpack_from('<ii', source, 18)
            if bias != 0:
                raise ValueError('Source VTSurface has a baked density reduction')
            width, height, count, _, high, start, layers = struct.unpack_from('<7i', source, 26 + stack * 40)
            if not 0 <= layer < layers:
                raise ValueError('Invalid VTSurface layer')
            layer_offset = 0
            stride = 0
            # Native ReadHeader rebases HighMipOffset relative to the end of
            # the packed 498-byte header; TilesDataOffset is already absolute.
            tail_offset = 498 + high
            for i in range(layers):
                base = 106 + stack * 192 + i * 48
                active = source[base] != 0
                form, mode, _, tail_size_i, mid_i, tile_bytes = struct.unpack_from('<iH2x4i', source, base + 4)
                if not active:
                    continue
                if mode != 0 or form not in (108, 109):
                    raise ValueError('Unsupported combined VT layer encoding')
                if i == layer:
                    fmt, mid, tail = form, mid_i, source[tail_offset:tail_offset + tail_size_i]
                    layer_offset = stride
                stride += tile_bytes * 5 // 4
                tail_offset += tail_size_i
            footer = struct.unpack_from('<i', source, 494)[0]
            for previous in range(stack):
                footer += struct.unpack_from('<i', source, 26 + previous * 40 + 8)[0] * 4
            offsets = struct.unpack_from('<' + 'i' * count, source, footer)
            mips = 1 + int(math.log2(max(width, height)))
            filt, wrap, aniso = 2, 1, 16
        else:
            raise ValueError('Unknown source kind: ' + kind)
        if fmt not in (108, 109) or width != height or width % tile or tile % 4:
            raise ValueError('This adapter requires original square BC7 VT data')
        complete = 1 + int(math.log2(width))
        if mips != complete:
            raise ValueError('Incomplete VT mip pyramid')
        result = []
        index = 0
        w = width
        cached = None
        while w >= tile:
            raw = bytearray(blocks(w, w))
            for y in range(w // tile):
                for x in range(w // tile):
                    first = offsets[index - 1] if index else 0
                    last = offsets[index]
                    data = self.decompress(source[start + first:start + last])
                    if len(data) != stride:
                        raise ValueError('Native tile layout mismatch')
                    pitch = (tile + 16) // 4 * 16
                    for row in range(tile // 4):
                        a = layer_offset + (row + 2) * pitch + 32
                        b = ((y * tile // 4 + row) * (w // 4) + x * tile // 4) * 16
                        raw[b:b + tile * 4] = data[a:a + tile * 4]
                    if w == tile:
                        # Independent duplicate of first mid mip for byte comparison.
                        cached = b''.join(data[layer_offset + blocks(tile + 16, tile + 16) +
                                             (row + 1) * (pitch // 2) + 16:
                                             layer_offset + blocks(tile + 16, tile + 16) +
                                             (row + 1) * (pitch // 2) + 16 + tile * 2]
                                          for row in range(tile // 8))
                    index += 1
            result.append(bytes(raw))
            w //= 2
        if index != count:
            raise ValueError('Unconsumed native tiles')
        provenance = []
        if mid:
            if self.mid is None:
                self.mid = MidCache(self.game)
            recovered, provenance = self.mid.recover(material, stack, layer, tile, mid, width, height)
            if recovered[0] != cached:
                raise ValueError('MidMips differs from independently stored trilinear tile')
            result.extend(recovered)
        offset = 0
        while len(result) < mips:
            side = max(1, width >> len(result))
            size = blocks(side, side)
            result.append(tail[offset:offset + size])
            if len(result[-1]) != size:
                raise ValueError('Incomplete authored VT tail')
            offset += size
        if offset != len(tail):
            raise ValueError('Unexpected authored VT tail bytes')
        raw = b''.join(result)
        header = struct.pack('<4H6Bi', 5, width, height, 1, mips, fmt, 2, filt, wrap, aniso, -1)
        return header + raw, {'mips': mips, 'format': fmt, 'mipSha256': [sha(m) for m in result],
                              'midSources': provenance, 'cachedMidMipByteEqual': bool(mid)}


def main():
    p = argparse.ArgumentParser()
    p.add_argument('manifest', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('game_data', type=Path)
    a = p.parse_args()
    manifest = json.loads(a.manifest.read_text(encoding='utf-8-sig'))
    decoder = Decoder(a.game_data)
    a.output.mkdir(parents=True, exist_ok=True)
    report, converted = [], {}
    for unit in manifest['units']:
        for ch in unit['channels']:
            path = Path(ch['path'])
            if not path.is_absolute():
                path = a.manifest.parent / path
            ch['path'] = str(path.resolve())
            if ch.get('kind', 'Texture') == 'Texture':
                continue
            data = path.read_bytes()
            if sha(data) != ch['sha256']:
                raise ValueError('Changed VT source: ' + str(path))
            key = (ch['sha256'], ch.get('stack', 0), ch.get('layer', ch['name']), unit['sourceCid'])
            if key not in converted:
                native, evidence = decoder.recover(ch, data, unit['sourceCid'])
                name = sha(native)
                target = a.output / (name + '.Texture')
                if target.exists() and target.read_bytes() != native:
                    raise ValueError('Conflicting extracted source')
                target.write_bytes(native)
                converted[key] = (target, name)
                report.append({'source': str(path), 'sourceSha256': sha(data), 'output': str(target),
                               'outputSha256': name, 'bytes': len(native), **evidence})
            target, digest = converted[key]
            ch.update(kind='Texture', path=str(target.resolve()), sha256=digest)
        for member in ('descriptor', 'evidence'):
            path = Path(unit[member]['path'])
            if not path.is_absolute():
                unit[member]['path'] = str((a.manifest.parent / path).resolve())
    (a.output / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    (a.output / 'extraction.json').write_text(json.dumps({'status': 'PASS', 'sources': report}, indent=2), encoding='utf-8')
    print(f'PASS: {len(report)} native VT sources; authored mip blocks preserved, no pixel conversion')


if __name__ == '__main__':
    main()
