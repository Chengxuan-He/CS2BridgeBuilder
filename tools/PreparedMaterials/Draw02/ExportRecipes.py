"""Extract Draw02's exact BBN road repeat recipes and native material provenance."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def f32(v):
    return struct.unpack('<f', struct.pack('<f', v))[0]


def numbers(path):
    raw = path.read_bytes()
    if raw[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(raw[:-32]).digest() != raw[-32:]:
        raise ValueError('Invalid BBN resource')
    counts = struct.unpack_from('<6i', raw, 8)
    offset, result = 32, {}
    for name, fmt, count in zip(('I32','U32','I64','U64','F32','F64'), ('i','I','q','Q','f','d'), counts):
        result[name] = struct.unpack_from('<' + fmt * count, raw, offset)
        offset += count * struct.calcsize(fmt)
    if offset != len(raw) - 32:
        raise ValueError('BBN payload length differs')
    return result


def key(unit):
    data = bytearray()
    def text(s):
        encoded = s.encode('utf-8'); length = len(encoded)
        while length >= 128:
            data.append((length & 127) | 128); length >>= 7
        data.append(length); data.extend(encoded)
    text('BridgeBuilder/RoadRepeat/1'); text(unit['sourceDigest'])
    data.extend(struct.pack('<4f??i', *unit['min'], *unit['max'], unit['cutout'], unit['rows'], -1 if unit['bands'] is None else len(unit['bands'])))
    for b in unit['bands'] or []:
        data.extend(struct.pack('<7f', b['start'], b['end'], b['repeatY'], *b['min'], *b['max']))
    return hashlib.sha256(data).hexdigest()


def extract(repo, game, audit_path, sources_path):
    numeric = repo / 'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02RoadAppearance_cs.bin'
    values = numbers(numeric); f = values['F32']
    if len(f) != 56 or len(values['I32']) != 41:
        raise ValueError('Draw02 source recipe layout changed')
    audit = json.loads(audit_path.read_text())
    meshes = [v['data'] for v in audit['selected'].values() if v['data'].get('m_Name') == 'DrawBridge02 Mesh']
    if len(meshes) != 1:
        raise ValueError('Missing or ambiguous Draw02 source mesh')
    guid = meshes[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (game / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if entry.endswith('.Surface.cid') and archive.read(entry).decode().strip() == cid:
                        candidate = archive.read(entry[:-4])
                        if payload is not None and candidate != payload:
                            raise ValueError('Conflicting source CID')
                        payload = candidate
    if payload is None:
        raise ValueError('Missing native material')
    source = [s for s in json.loads(sources_path.read_text())['sources'] if s['cid'] == cid]
    if len(source) != 1 or source[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Source template metadata differs')
    bands = [dict(start=f[0], end=f32(f[1]/f[2]), min=list(f[3:5]), max=list(f[5:7]), repeatY=1),
             dict(start=f32(f[7]/f[8]), end=f32(f[9]/f[10]), min=list(f[11:13]), max=list(f[13:15]), repeatY=1),
             dict(start=f32(f[15]/f[16]), end=f[17], min=list(f[18:20]), max=list(f[20:22]), repeatY=1)]
    units = []
    for label, begin, band in [('moving-carriageway',28,bands), ('moving-sidewalk',34,None), ('fixed-road',38,None), ('fixed-sidewalk',47,None)]:
        # These are prototype sources, not owner.Asset, so every reachable recipe
        # uses the existing runtime source!=owner.Asset cutout branch.
        u = dict(style='Draw02', sourceCid=cid, sourceDigest=hashlib.sha256(payload).hexdigest(),
                 min=list(f[begin:begin+2]), max=list(f[begin+2:begin+4]), cutout=True, rows=False, bands=band, label=label)
        u['key'] = key(u); units.append(u)
    # Homogeneous selected-road asphalt is promoted by the shared composer into
    # an opaque Repeat chart. Its pixels equal the first cutout recipe exactly.
    opaque = dict(units[0], cutout=False, label='moving-carriageway-opaque')
    opaque['key'] = key(opaque); units.append(opaque)
    return dict(recipe='BridgeBuilder/RoadRepeat/1', sources=source, units=units,
                evidence=dict(numericalResourceSHA256=hashlib.sha256(numeric.read_bytes()).hexdigest(),
                              nativeAuditSHA256=hashlib.sha256(audit_path.read_bytes()).hexdigest()))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--legacy-evidence-only', action='store_true', help='Reproduce retired historical artifacts; not production resources.')
    for name in ('repository','game_data','native_audit','structural_inventory','output'):
        p.add_argument(name, type=Path)
    a = p.parse_args()
    if not a.legacy_evidence_only:
        p.error('RoadRepeat recipes are retired; use the source-atlas and fixed-car phase/paint authoring path. Historical reproduction requires --legacy-evidence-only.')
    result = extract(a.repository,a.game_data,a.native_audit,a.structural_inventory)
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print('PASS Draw02 repeat inventory: 4 pixel recipes, 5 descriptors, 16 projection aliases; exact Float32 bands and native source digest.')


if __name__ == '__main__':
    main()
