"""Decode original Draw03 far VT blocks for inspection; never author assets."""
from pathlib import Path
import argparse
import ctypes
import hashlib
import io
import json
import struct
import zipfile
from PIL import Image


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game-data', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    cid = '435e3c499c2aa98d37491206dba02787'
    payload = None
    location = None
    for path in (args.game_data / 'Content/BridgesAndPorts').glob('VT*.cok'):
        with zipfile.ZipFile(path) as archive:
            for entry in archive.namelist():
                if entry.endswith('.cid') and archive.read(entry).decode('utf-8-sig').strip() == cid:
                    payload = archive.read(entry[:-4])
                    location = [str(path), entry[:-4]]
    assert payload is not None
    integer = lambda offset: struct.unpack_from('<i', payload, offset)[0]
    assert struct.unpack_from('<H', payload)[0] == 1 and integer(490) == 2
    assert all(payload[298 + layer * 48] == 0 for layer in range(4))
    tile, width, height, count = integer(18), integer(26), integer(30), integer(34)
    start, layers, footer = integer(46), integer(50), integer(494)
    assert (width, height, layers) == (512, 512, 4)
    assert payload[106 + 3 * 48] == 0
    layers = 3
    codec = ctypes.CDLL(str(args.game_data / 'Plugins/x86_64/PipelinePlugin.dll'))
    decode = codec.decompress_data
    decode.argtypes = [ctypes.c_void_p, ctypes.c_longlong, ctypes.c_void_p, ctypes.c_longlong, ctypes.c_int]
    decode.restype = ctypes.c_longlong
    formats, strides = [], []
    for layer in range(layers):
        offset = 106 + layer * 48
        assert payload[offset] != 0 and struct.unpack_from('<H', payload, offset + 8)[0] == 0
        formats.append(integer(offset + 4))
        size = integer(offset + 24)
        assert size == ((tile + 16) // 4) ** 2 * 16
        strides.append(size + size // 4)
    raw = [bytearray(width * height) for _ in range(layers)]
    offsets = struct.unpack_from('<' + 'i' * count, payload, footer)
    for index in range((width // tile) * (height // tile)):
        first = 0 if index == 0 else offsets[index - 1]
        compressed = payload[start + first:start + offsets[index]]
        magic, size, packed = struct.unpack_from('<iii', compressed)
        assert magic == 2147483647 and packed == len(compressed) - 12
        target = ctypes.create_string_buffer(size)
        assert decode(compressed[12:], packed, target, size, 0) == size
        decoded, offset = target.raw, 0
        for layer in range(layers):
            for row in range(tile // 4):
                source = offset + (row + 2) * ((tile + 16) // 4 * 16) + 32
                dest = ((index // (width // tile)) * (tile // 4) + row) * (width // 4 * 16)
                dest += (index % (width // tile)) * (tile // 4 * 16)
                raw[layer][dest:dest + tile // 4 * 16] = decoded[source:source + tile // 4 * 16]
            offset += strides[layer]
        assert offset == len(decoded)
    assert hashlib.sha256(raw[0]).hexdigest() == '6adfa5795bb96affa36ed66d8d5f84647b0a18cf3f1a43c17c2a5724bed62c69'
    # DDS is only an inspection container for unchanged BC7 blocks. DXGI 99 is
    # BC7_UNORM_SRGB; no source mip generation or texture resize takes place.
    header = [124, 0x81007, height, width, len(raw[0]), 0, 1] + [0] * 11
    header += [32, 4, int.from_bytes(b'DX10', 'little'), 0, 0, 0, 0, 0]
    header += [0x1000, 0, 0, 0, 0]
    dds = b'DDS ' + struct.pack('<31I', *header) + struct.pack('<5I', 99, 3, 0, 1, 0) + raw[0]
    args.output.mkdir(parents=True, exist_ok=True)
    Image.open(io.BytesIO(dds)).save(args.output / 'far-original-base.png')
    report = dict(sourceCid=cid, source=location,
                  sourceSha256=hashlib.sha256(payload).hexdigest(),
                  width=width, height=height, nativeFormats=formats,
                  rawSha256=[hashlib.sha256(r).hexdigest() for r in raw],
                  scope='Original mip-zero decoding only; no production asset authored.')
    (args.output / 'far-original-atlas.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
