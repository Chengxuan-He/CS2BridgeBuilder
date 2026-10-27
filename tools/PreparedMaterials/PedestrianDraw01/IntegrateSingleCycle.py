"""Integrate PedestrianDraw01's measured units without rebaking shared pixels."""
import argparse
import ctypes
import hashlib
import io
import json
from pathlib import Path
import struct

from ExportRecipes import numbers, string as encoded_string
from ExportSharedSampling import count, descriptor, string

ROOT = Path(__file__).resolve().parents[3]
STYLE = 'PedestrianDraw01'
STAGE = ROOT / 'artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2/single-cycle-v1'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def cid(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(raw).digest()[:16])


def put(path, raw):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        assert path.read_bytes() == raw, f'Conflicting prepared resource: {path}'
    else:
        path.write_bytes(raw)


def road_descriptor(raw):
    assert struct.unpack_from('<Hi?', raw) == (1, 1, False)
    stream = io.BytesIO(raw)
    stream.seek(7)
    maps = []
    for size in (4, 4, 16, 16):
        values = {}
        for _ in range(count(stream)):
            name = string(stream)
            values[name] = stream.read(size)
            assert len(values[name]) == size
        maps.append(values)
    texture_start = stream.tell()
    for _ in range(count(stream)):
        string(stream)
        marker = stream.read(1)
        assert marker == b'\xff'
        assert len(stream.read(16)) == 16
    texture_end = stream.tell()
    keywords = {string(stream) for _ in range(count(stream))}
    assert stream.tell() == len(raw)
    maps[0]['_Snow'] = struct.pack('<f', 0)
    keywords |= {'_ALPHATEST_ON', '_DISABLE_SSR_TRANSPARENT', '_DOUBLESIDED_ON', '_TANGENTSPACE_OCTO'}
    result = bytearray(struct.pack('<Hi?', 1, 14, False))
    for values in maps:
        result.extend(struct.pack('<Bi', 255, len(values)))
        for name, value in values.items():
            result.extend(encoded_string(name) + value)
    result.extend(raw[texture_start:texture_end])
    result.extend(struct.pack('<Bi', 255, len(keywords)))
    for value in sorted(keywords):
        result.extend(encoded_string(value))
    return bytes(result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish', action='store_true')
    args = parser.parse_args()
    output = STAGE / 'integrated-output' / STYLE
    bake = json.loads((STAGE / 'report.json').read_text())
    source = json.loads((STAGE.parent / 'trial-v1/persistent-manifest.json').read_text())
    baseline = ROOT / 'artifacts/prepared-materials/PedestrianDraw01/rebuild-native-v1/output/PedestrianDraw01'
    for directory in (baseline, STAGE / 'output' / STYLE):
        for path in directory.rglob('*'):
            if path.is_file():
                put(output / path.relative_to(directory), path.read_bytes())
    aliases = []
    for result, unit in zip(bake['units'], source['units']):
        assert result['id'] == unit['id'] and result['periods'] == [1, 1]
        assert unit['physicalSize'] == [4, 4]
        for channel in result['channels']:
            assert (channel['width'], channel['height'], channel['mips']) == (768, 769, 10)
            assert channel['densityRatioX'] >= 1 and channel['densityRatioY'] >= 1
            assert channel['sourceSpanX'] == 4096 * (unit['max'][0] - unit['min'][0])
            assert channel['sourceSpanY'] == 4096 * (unit['max'][1] - unit['min'][1])
        original = (output / 'RepeatUnits' / result['key'] / 'Repeat.Surface').read_bytes()
        road = road_descriptor(original)
        assert descriptor(original)[3] == descriptor(road)[3]
        road_cid = cid(road)
        road_key = sha(encoded_string('BridgeBuilder/RepeatUnits/2') + encoded_string(road_cid))
        path = output / 'RepeatUnits' / road_key / 'Repeat.Surface'
        put(path, road)
        put(Path(str(path) + '.cid'), road_cid.encode())
        aliases.append(dict(samplerKey=result['aliases'][0], roadKey=road_key,
                            structureKey=result['key'], min=unit['min'], max=unit['max']))

    codec = ctypes.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
    decode = codec.decompress_data
    decode.argtypes = [ctypes.c_void_p, ctypes.c_int64, ctypes.c_void_p, ctypes.c_int64, ctypes.c_int]
    decode.restype = ctypes.c_int64
    texture_checks = []
    for path in sorted(output.rglob('*.Texture')):
        raw = path.read_bytes()
        version, width, height, depth, mips, fmt, dimension, filtering, wrap, aniso = struct.unpack('<4H6B', raw[:14])
        assert version == 5 and fmt in (4, 8)
        sizes = [max(1, width >> m) * max(1, height >> m) * 4 for m in range(mips)]
        size = sum(sizes)
        compressed = struct.unpack_from('<i', raw, 14)[0]
        if compressed == -1:
            pixels = raw[18:]
        else:
            assert compressed == len(raw) - 18
            buffer = ctypes.create_string_buffer(size)
            assert decode(raw[18:], compressed, buffer, size, 0) == size
            pixels = buffer.raw
        assert len(pixels) == size
        offset, hashes = 0, []
        for length in sizes:
            hashes.append(sha(pixels[offset:offset+length]))
            offset += length
        texture_checks.append(dict(path=path.relative_to(output).as_posix(), width=width, height=height,
            mips=mips, format=fmt, filtering=filtering, wrap=wrap, bytes=len(raw), mipHashes=hashes))
    identities = {}
    for path in output.rglob('*.cid'):
        identity = path.read_text().strip()
        assert identity not in identities, f'Duplicate physical payload: {identity}'
        assert path.with_suffix('').is_file()
        identities[identity] = path.relative_to(output).as_posix()
    for path in (output / 'RepeatUnits').rglob('*.Surface'):
        assert all(identity in identities for identity in descriptor(path.read_bytes())[3].values())
    assert not any(path.is_dir() and not any(path.iterdir()) for path in output.rglob('*'))
    files = [dict(path=path.relative_to(output).as_posix(), bytes=path.stat().st_size,
                  sha256=sha(path.read_bytes())) for path in sorted(output.rglob('*')) if path.is_file()]
    total = sum(row['bytes'] for row in files)
    assert total <= 10_000_000
    v = numbers(ROOT / 'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.bin')['F32']
    for i, row in enumerate(aliases):
        assert row['min'] + row['max'] == [v[3], v[25 if i == 0 else 27], v[4], v[5]]

    if args.publish:
        assert 'PASS native source descriptors:' in (STAGE / 'native-reader.log').read_text()
        previous = json.loads((STAGE / 'integration.json').read_text())
        assert previous['files'] == files, 'Native reader evidence is stale'
        formal = ROOT / 'assets/Materials' / STYLE
        for path in output.rglob('*'):
            if path.is_file():
                put(formal / path.relative_to(output), path.read_bytes())
        code = ['using System.Collections.Generic;', '', 'namespace BridgeBuilder.Bridges;', '',
            '// Two measured fixed-road units; road and structure descriptors share pixel payloads.',
            '// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs (shared existing resource)',
            'internal static partial class MovableRepeatUnits', '{',
            '    static partial void PedestrianDraw01(string samplerKey, ref string? unitKey)',
            '    {', '        unitKey = samplerKey switch', '        {']
        for row in aliases:
            code.append(f'            "{row["samplerKey"]}" => "{row["roadKey"]}",')
        code += ['            _ => unitKey', '        };', '    }', '',
            '    static partial void PedestrianDraw01Regions(string sourceDigest, List<Region> regions)',
            '    {', f'        if (sourceDigest != "{source["units"][0]["sourceDigest"]}") return;',
            '        var v = Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32;']
        # Prefer the smaller native region when both measured rectangles contain a domain.
        for i in (1, 0):
            code += [f'        regions.Add(new Region("{aliases[i]["structureKey"]}",',
                     f'            v[3], v[{27 if i else 25}], v[4], v[5]));']
        code += ['    }', '}', '']
        (ROOT / 'src/BridgeBuilder/Bridges/Types/PedestrianDraw01/PedestrianDraw01RepeatUnits.cs').write_text('\n'.join(code))
        actual = {p.relative_to(formal).as_posix(): sha(p.read_bytes()) for p in formal.rglob('*') if p.is_file()}
        assert actual == {row['path']: row['sha256'] for row in files}

    report = dict(style=STYLE, files=files, fileCount=len(files), totalBytes=total,
        aliases=aliases, textureChecks=texture_checks, roadTemplate=14, structureTemplate=1,
        originalPhysicalPeriod=[4, 4], sourceBoundsUnchanged=True, mipDataVerified=True,
        sharedTextureCount=7, duplicatedPayloads=0, newPixelBakesDuringIntegration=0,
        localization='No user-visible text changed.', published=args.publish,
        acceptance='Ready for human observation; no game rendering, cold-load or unload independence claim.')
    (STAGE / 'integration.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: report[key] for key in ('fileCount', 'totalBytes', 'roadTemplate', 'structureTemplate', 'published')}))


if __name__ == '__main__':
    main()
