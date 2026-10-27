"""Extract Lift04 repeat material recipes from immutable measured source data."""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path


def repeat_key(digest, unit):
    # BinaryWriter's length-prefixed UTF-8 strings and little-endian Float32.
    data = bytearray()
    for value in ('BridgeBuilder/RoadRepeat/1', digest.lower()):
        encoded = value.encode('utf-8')
        length = len(encoded)
        while length >= 128:
            data.append((length & 127) | 128)
            length >>= 7
        data.append(length)
        data.extend(encoded)
    data.extend(struct.pack('<4f??i', *unit['min'], *unit['max'], unit['cutout'], unit['rows'], -1))
    if unit['bands'] is not None:
        raise ValueError('Lift04 unexpectedly requires banded sampling')
    return hashlib.sha256(data).hexdigest()


def numbers(path):
    data = path.read_bytes()
    if data[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError('Invalid BBN1 numerical resource: ' + str(path))
    counts = struct.unpack_from('<6i', data, 8)
    offset, result = 32, {}
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), ('i', 'I', 'q', 'Q', 'f', 'd'), counts):
        if count < 0:
            raise ValueError('Negative numerical array count')
        result[name] = struct.unpack_from('<' + fmt * count, data, offset)
        offset += count * struct.calcsize(fmt)
    if offset != len(data) - 32:
        raise ValueError('Unconsumed numerical bytes')
    return result


def source_recipes(repository):
    code_path = repository / 'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RoadAppearance.cs'
    code = code_path.read_text(encoding='utf-8-sig')
    numeric_path = repository / 'assets/HardcodedNumbers/Lift04/Types_Lift04_Lift04RoadAppearance_cs.bin'
    values = numbers(numeric_path)
    # All four measured physical domains use the same material rectangle. Their
    # U/V projections differ, but those are receiver coordinates, not baked pixels.
    for variable, indices in (('min', (0, 1)), ('max', (2, 3))):
        expression = re.search(r'var ' + variable + r' = new Vector2\((.*?)\);', code)
        if expression is None or [int(i) for i in re.findall(r'F32\[(\d+)\]', expression[1])] != list(indices):
            raise ValueError('Lift04 source rectangle mapping changed')
    if code.count('Unit(') != 5 or '=> new(min, max,' not in code or 'repeat: true' not in code:
        raise ValueError('Lift04 repeat-domain mapping changed')
    f = values['F32']
    minimum, maximum = list(f[:2]), list(f[2:4])
    if not all(0 <= lo < hi <= 1 for lo, hi in zip(minimum, maximum)):
        raise ValueError('Invalid measured atlas rectangle')
    return [dict(min=minimum, max=maximum, cutout=True, rows=False, bands=None,
                 regions=[0, 1, 2, 3])], dict(
        runtimeSourceSHA256=hashlib.sha256(code_path.read_bytes()).hexdigest(),
        numericalResourceSHA256=hashlib.sha256(numeric_path.read_bytes()).hexdigest(),
        boundsFloat32Bits=[struct.pack('<f', v).hex() for v in f[:4]],
        projectionLengths=[f[13], f[17], struct.unpack('<f', struct.pack('<f', f[5] - f[4]))[0]],
        lateralLength=f[8])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('native_audit', type=Path)
    parser.add_argument('structural_inventory', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    units, evidence = source_recipes(args.repository)
    audit = json.loads(args.native_audit.read_text())
    renders = [v['data'] for v in audit['selected'].values() if v['data'].get('m_Name') == 'LiftBridge04 Mesh']
    if len(renders) != 1:
        raise ValueError('Missing or ambiguous native Lift04 near render')
    guid = renders[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)
    source_code = (args.repository / 'src/BridgeBuilder/Bridges/Types/Lift04/Lift04SourceRenders.cs').read_text(encoding='utf-8-sig')
    if not re.search(r'Create\("LiftBridge04 Mesh", "[a-f0-9]+", new\[\] \{ "' + cid + '"', source_code):
        raise ValueError('Native material differs from recorded runtime source')
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid') or archive.read(entry).decode().strip() != cid:
                        continue
                    candidate = archive.read(entry[:-4])
                    if payload is not None and candidate != payload:
                        raise ValueError('Native material CID has conflicting bytes')
                    payload = candidate
    if payload is None:
        raise ValueError('Native source material is unavailable')
    description = [v for v in json.loads(args.structural_inventory.read_text())['sources'] if v['cid'] == cid]
    if len(description) != 1 or description[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Native source template differs from channel evidence')
    digest = hashlib.sha256(payload).hexdigest()
    output = dict(recipe='BridgeBuilder/RoadRepeat/1', sources=description,
                  units=[dict(style='Lift04', sourceCid=cid, sourceDigest=digest, key=repeat_key(digest, u), **u) for u in units],
                  evidence=dict(nativeAuditSHA256=hashlib.sha256(args.native_audit.read_bytes()).hexdigest(), **evidence))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift04: 4 appearance aliases, 1 unique repeat recipe; source={cid}; SHA256={digest}')


if __name__ == '__main__':
    main()
