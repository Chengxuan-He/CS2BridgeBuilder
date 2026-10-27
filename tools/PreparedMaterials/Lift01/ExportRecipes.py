"""Extract Lift01 repeat recipes from unchanged BBN values and native sources."""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path


def numbers(path):
    data = path.read_bytes()
    if data[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError('Invalid numerical resource: ' + str(path))
    counts = struct.unpack_from('<6i', data, 8)
    result, offset = {}, 32
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), ('i', 'I', 'q', 'Q', 'f', 'd'), counts):
        if count < 0:
            raise ValueError('Negative numeric count')
        result[name] = struct.unpack_from('<' + fmt * count, data, offset)
        offset += count * struct.calcsize(fmt)
    if offset != len(data) - 32:
        raise ValueError('Numerical payload length mismatch')
    return result


def source_recipes(repository):
    code_path = repository / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01RoadAppearance.cs'
    resource = repository / 'assets/HardcodedNumbers/Lift01/Types_Lift01_Lift01RoadAppearance_cs.bin'
    code = code_path.read_text(encoding='utf-8-sig')
    values = numbers(resource)
    i, f = values['I32'], values['F32']
    if i[:9] != (8, 0, 2, 0, 2, 0, 2, 4, 2):
        raise ValueError('Lift01 appearance region aliases changed')
    pattern = (r'new Patch\(new Vector2\(Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32\[0\], '
               r'Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32\[1\]\), new Vector2\('
               r'Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32\[2\], '
               r'Numeric_Types_Lift01_Lift01RoadAppearance_cs.Values.F32\[3\]\)')
    if len(re.findall(pattern, code)) != 1 or code.count('repeat: true') != 1 or 'bands:' in code:
        raise ValueError('Lift01 patch sampling changed; review extraction')
    result = [dict(min=list(f[:2]), max=list(f[2:4]), cutout=True, rows=False,
                   bands=None, bandsNull=True, regions=list(range(i[0])))]
    return result, dict(runtimeSourceSHA256=hashlib.sha256(code_path.read_bytes()).hexdigest(),
                        numericalResourceSHA256=hashlib.sha256(resource.read_bytes()).hexdigest())


def recipe_key(digest, unit):
    def text(value):
        raw = value.encode('utf-8')
        count, prefix = len(raw), bytearray()
        while count >= 128:
            prefix.append((count & 127) | 128)
            count >>= 7
        prefix.append(count)
        return prefix + raw
    payload = text('BridgeBuilder/RoadRepeat/1') + text(digest.lower())
    payload += struct.pack('<4f??i', *unit['min'], *unit['max'], unit['cutout'], unit['rows'], -1)
    return hashlib.sha256(payload).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('game_data', type=Path)
    p.add_argument('native_audit', type=Path)
    p.add_argument('structural_inventory', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    recipes, evidence = source_recipes(a.repository)
    audit = json.loads(a.native_audit.read_text())
    renders = [r['data'] for r in audit['selected'].values() if r['data'].get('m_Name') == 'LiftBridge01 Mesh']
    if len(renders) != 1:
        raise ValueError('Native Lift01 source missing or ambiguous')
    guid = renders[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)
    runtime_sources = (a.repository / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01SourceRenders.cs').read_text()
    if not re.search(r'Create\("LiftBridge01 Mesh", "[a-f0-9]+", new\[\] \{ "' + cid + '"', runtime_sources):
        raise ValueError('Runtime Lift01 material slot differs from native source')
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (a.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid') or archive.read(entry).decode().strip() != cid:
                        continue
                    candidate = archive.read(entry[:-4])
                    if payload is not None and payload != candidate:
                        raise ValueError('Native source CID has conflicting bytes')
                    payload = candidate
    if payload is None:
        raise ValueError('Native source bytes unavailable: ' + cid)
    inventory = json.loads(a.structural_inventory.read_text())
    sources = [s for s in inventory['sources'] if s['cid'] == cid]
    if len(sources) != 1 or sources[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Native source template evidence differs')
    digest = hashlib.sha256(payload).hexdigest()
    units = [dict(style='Lift01', sourceCid=cid, sourceDigest=digest, key=recipe_key(digest, r), **r) for r in recipes]
    output = dict(recipe='BridgeBuilder/RoadRepeat/1', sources=sources, units=units,
                  evidence=dict(nativeAuditSHA256=hashlib.sha256(a.native_audit.read_bytes()).hexdigest(), **evidence))
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift01: 8 appearance aliases, {len(units)} unique repeat recipe, source={cid}, key={units[0]["key"]}')


if __name__ == '__main__':
    main()
