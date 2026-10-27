"""Extract Lift03's two measured deck units without changing runtime geometry."""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path

RECIPE = 'BridgeBuilder/RoadRepeat/1'


def sampler_key(digest, minimum, maximum, cutout):
    def text(value):
        encoded = value.encode('utf-8')
        count = len(encoded)
        prefix = bytearray()
        while count >= 128:
            prefix.append((count & 127) | 128)
            count >>= 7
        prefix.append(count)
        return prefix + encoded
    payload = text(RECIPE) + text(digest.lower())
    payload += struct.pack('<4f??i', *minimum, *maximum, cutout, False, -1)
    return hashlib.sha256(payload).hexdigest()


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def numbers(path):
    data = path.read_bytes()
    if data[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError('Invalid numerical resource: ' + str(path))
    counts = struct.unpack_from('<6i', data, 8)
    offset, result = 32, {}
    for name, kind, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), 'iIqQfd', counts):
        if count < 0:
            raise ValueError('Negative numerical array count')
        result[name] = struct.unpack_from('<' + kind * count, data, offset)
        offset += struct.calcsize(kind) * count
    if offset != len(data) - 32:
        raise ValueError('Numerical resource length differs')
    return result


def extract(repository):
    code_path = repository / 'src/BridgeBuilder/Bridges/Types/Lift03/Lift03RoadAppearance.cs'
    code = code_path.read_text(encoding='utf-8-sig')
    path = repository / 'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03RoadAppearance_cs.bin'
    values = numbers(path)['F32']
    units = []
    for deck in ('Main', 'Auxiliary'):
        block = code.split('case Lift03RoadSampling.Deck.' + deck + ':', 1)[1].split('break;', 1)[0]
        rectangles = []
        for bound in ('min', 'max'):
            expression = re.search(bound + r' = new Vector2\((.*)\);', block)[1]
            indices = [int(v) for v in re.findall(r'Values.F32\[(\d+)\]', expression)]
            if len(indices) != 4 or expression.count('/') != 2:
                raise ValueError('Lift03 patch expression changed')
            rectangles.append([f32(values[indices[0]] / values[indices[1]]), f32(values[indices[2]] / values[indices[3]])])
        if not all(a < b for a, b in zip(*rectangles)):
            raise ValueError('Invalid measured patch bounds')
        # All ordinary patches have Repeat=true. Pure asphalt promotion is only
        # enabled on the Main deck and uses opaque Default; lower selected-track
        # material stays original and must never be replaced by upper asphalt.
        for cutout in ((True, False) if deck == 'Main' else (True,)):
            units.append(dict(style='Lift03', min=rectangles[0], max=rectangles[1], cutout=cutout,
                              rows=False, bands=None, key='', deck=deck))
    if 'repeat: true' not in code:
        raise ValueError('Lift03 repeat routing changed')
    return units, dict(numericalResourceSHA256=hashlib.sha256(path.read_bytes()).hexdigest(),
                      runtimeSourceSHA256=hashlib.sha256(code_path.read_bytes()).hexdigest())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('repository', 'game_data', 'native_audit', 'structural_inventory', 'output'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    units, evidence = extract(args.repository)
    audit = json.loads(args.native_audit.read_text())
    renders = [x['data'] for x in audit['selected'].values() if x['data'].get('m_Name') == 'LiftBridge03 Mesh']
    if len(renders) != 1:
        raise ValueError('Native Lift03 main render absent or ambiguous')
    guid = renders[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)
    native = None
    for family in ('Game', 'BridgesAndPorts'):
        for archive_path in (args.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(archive_path) as archive:
                for entry in archive.namelist():
                    if entry.endswith('.Surface.cid') and archive.read(entry).decode().strip() == cid:
                        candidate = archive.read(entry[:-4])
                        if native is not None and native != candidate:
                            raise ValueError('Native source CID has conflicting content')
                        native = candidate
    if native is None:
        raise ValueError('Native source payload absent: ' + cid)
    sources = [s for s in json.loads(args.structural_inventory.read_text())['sources'] if s['cid'] == cid]
    if len(sources) != 1 or sources[0]['template'] != struct.unpack_from('<i', native, 2)[0]:
        raise ValueError('Native template evidence differs')
    for unit in units:
        digest = hashlib.sha256(native).hexdigest()
        unit.update(sourceCid=cid, sourceDigest=digest,
                    key=sampler_key(digest, unit['min'], unit['max'], unit['cutout']))
    evidence['nativeAuditSHA256'] = hashlib.sha256(args.native_audit.read_bytes()).hexdigest()
    output = dict(recipe=RECIPE, sources=sources, units=units, evidence=evidence)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift03: {len(units)} descriptors, 2 source rectangles; Main and Auxiliary remain distinct; source={cid}')


if __name__ == '__main__':
    main()
