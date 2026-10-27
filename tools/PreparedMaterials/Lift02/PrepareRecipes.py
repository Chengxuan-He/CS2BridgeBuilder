"""Extract Lift02's constant repeat unit from immutable numerical and native data."""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path


def repeat_key(unit):
    def text(value):
        raw = value.encode('utf-8')
        size, prefix = len(raw), bytearray()
        while size >= 128:
            prefix.append((size & 127) | 128)
            size >>= 7
        return bytes(prefix) + bytes([size]) + raw
    if unit['bands'] is not None:
        raise ValueError('Lift02 has no recorded composite bands')
    raw = text('BridgeBuilder/RoadRepeat/1') + text(unit['sourceDigest'].lower())
    raw += struct.pack('<4f??i', *unit['min'], *unit['max'], unit['cutout'], unit['rows'], -1)
    return hashlib.sha256(raw).hexdigest()


def numbers(path):
    raw = path.read_bytes()
    if raw[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(raw[:-32]).digest() != raw[-32:]:
        raise ValueError('Invalid BBN resource: ' + str(path))
    counts = struct.unpack_from('<6i', raw, 8)
    arrays, offset = {}, 32
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), ('i', 'I', 'q', 'Q', 'f', 'd'), counts):
        if count < 0:
            raise ValueError('Invalid array length')
        arrays[name] = struct.unpack_from('<' + fmt * count, raw, offset)
        offset += struct.calcsize(fmt) * count
    if offset != len(raw) - 32:
        raise ValueError('Unconsumed numerical bytes')
    return arrays


def recipes(repository):
    source = repository / 'src/BridgeBuilder/Bridges/Types/Lift02/Lift02RoadAppearance.cs'
    resource = repository / 'assets/HardcodedNumbers/Lift02/Types_Lift02_Lift02RoadAppearance_cs.bin'
    code, values = source.read_text(encoding='utf-8-sig'), numbers(resource)
    # Verify actual array bindings before applying the measured source recipe.
    for variable, indices in [('min', (0, 1)), ('max', (2, 3))]:
        match = re.search(r'var ' + variable + r' = new Vector2\((.*)\);', code)
        if match is None or [int(n) for n in re.findall(r'Values.F32\[(\d+)\]', match[1])] != list(indices):
            raise ValueError('Lift02 patch bounds binding changed')
    if values['I32'] != (2, 0, 2, 0, 0, 1) or len(values['F32']) != 19:
        raise ValueError('Lift02 appearance layout changed')
    if code.count('new Patch(min, max,') != 1 or 'repeat: true' not in code:
        raise ValueError('Lift02 repeat treatment changed')
    minimum, maximum = list(values['F32'][:2]), list(values['F32'][2:4])
    if not all(0 <= lo < hi <= 1 for lo, hi in zip(minimum, maximum)):
        raise ValueError('Invalid measured source rectangle')
    return [dict(min=minimum, max=maximum, cutout=True, rows=False, bands=None, regions=[0, 1])], {
        'runtimeSourceSHA256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'numericalResourceSHA256': hashlib.sha256(resource.read_bytes()).hexdigest(),
        'longitudinalRepeatMetres': 1 / values['F32'][8],
        'transverseRepeatMetres': values['F32'][10],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for argument in ('repository', 'game_data', 'native_audit', 'structural_inventory', 'output'):
        parser.add_argument(argument, type=Path)
    args = parser.parse_args()
    units, evidence = recipes(args.repository)
    audit = json.loads(args.native_audit.read_text())
    renders = [v['data'] for v in audit['selected'].values() if v['data'].get('m_Name') == 'LiftBridge02 Mesh']
    if len(renders) != 1:
        raise ValueError('Native LiftBridge02 near render absent or ambiguous')
    guid = renders[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)
    recorded = (args.repository / 'src/BridgeBuilder/Bridges/Types/Lift02/Lift02SourceRenders.cs').read_text(encoding='utf-8-sig')
    if not re.search(r'Create\("LiftBridge02 Mesh", "[0-9a-f]+", new\[\] \{ "' + cid + '"', recorded):
        raise ValueError('Runtime source render differs from native evidence')
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid') or archive.read(entry).decode().strip() != cid:
                        continue
                    candidate = archive.read(entry[:-4])
                    if payload is not None and payload != candidate:
                        raise ValueError('Conflicting source material bytes')
                    payload = candidate
    if payload is None:
        raise ValueError('Native material absent: ' + cid)
    inventory = json.loads(args.structural_inventory.read_text())
    sources = [s for s in inventory['sources'] if s['cid'] == cid]
    if len(sources) != 1 or sources[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Source template evidence differs')
    digest = hashlib.sha256(payload).hexdigest()
    # Inspect the native VT header without Unity or GPU decoding. This proves the
    # channel dimensions used by the density verifier, rather than assuming the
    # atlas size from a source comment.
    if payload[6] != 1 or payload[283] != 255:
        raise ValueError('Lift02 expected native VTSurface binding is absent')
    vt_cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in payload[284:300])
    header = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('VT*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.VTSurface.cid') or archive.read(entry).decode().strip() != vt_cid:
                        continue
                    with archive.open(entry[:-4]) as stream:
                        candidate = stream.read(498)
                    if header is not None and header != candidate:
                        raise ValueError('Conflicting source VT header')
                    header = candidate
    if header is None or len(header) != 498 or struct.unpack_from('<H', header)[0] != 1:
        raise ValueError('Native Lift02 VTSurface header unavailable')
    channels = []
    for channel in sources[0]['channels']:
        stack, layer = channel['stack'], channel['layer']
        if header[106 + stack * 192 + layer * 48] == 0:
            continue
        width, height = struct.unpack_from('<2i', header, 26 + stack * 40)
        channels.append(dict(name=channel['name'], width=width, height=height))
    required = {'_BaseColorMap', '_NormalMap', '_MaskMap'}
    if not required.issubset({c['name'] for c in channels}):
        raise ValueError('Native repeat source channels are missing')
    evidence['nativeVTSurfaceCid'] = vt_cid
    evidence['nativeVTSurfaceHeaderSHA256'] = hashlib.sha256(header).hexdigest()
    evidence['nativeChannels'] = channels
    evidence['nativeAuditSHA256'] = hashlib.sha256(args.native_audit.read_bytes()).hexdigest()
    units = [dict(style='Lift02', sourceCid=cid, sourceDigest=digest, **u) for u in units]
    for unit in units:
        unit['key'] = repeat_key(unit)
    output = dict(recipe='BridgeBuilder/RoadRepeat/1', sources=sources, units=units, evidence=evidence)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift02: 2 projection aliases, {len(units)} constant pixel recipe; source={cid}; SHA256={digest}')


if __name__ == '__main__':
    main()
