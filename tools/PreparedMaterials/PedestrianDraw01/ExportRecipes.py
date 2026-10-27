"""Extract PedestrianDraw01 repeat recipes from measured BBN and native sources.

This authoring tool never reads generated bridges or modifies runtime geometry.
The output is consumed by tools/Prepare-Repeats.ps1.
"""
import argparse
import ast
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path


STYLE = 'PedestrianDraw01'
RECIPE = 'BridgeBuilder/RoadRepeat/1'


def numbers(path):
    data = path.read_bytes()
    if data[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(data[:-32]).digest() != data[-32:]:
        raise ValueError('Invalid numerical resource: ' + str(path))
    counts = struct.unpack_from('<6i', data, 8)
    result, offset = {}, 32
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), 'iIqQfd', counts):
        if count < 0:
            raise ValueError('Negative typed-array count')
        result[name] = struct.unpack_from('<' + fmt * count, data, offset)
        offset += count * struct.calcsize(fmt)
    if offset + 32 != len(data):
        raise ValueError('Unconsumed numerical resource bytes')
    return result


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def evaluate(expression, values, names):
    expression = re.sub(r'Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values\.(I32|F32)\[(\d+)\]',
                        lambda m: repr(values[m[1]][int(m[2])]), expression)
    def visit(node):
        if isinstance(node, ast.Constant) and isinstance(node.value, (int, float)):
            return f32(node.value)
        if isinstance(node, ast.Name):
            return names[node.id]
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
            return f32(-visit(node.operand))
        if isinstance(node, ast.BinOp):
            a, b = visit(node.left), visit(node.right)
            if isinstance(node.op, ast.Add): return f32(a + b)
            if isinstance(node.op, ast.Sub): return f32(a - b)
            if isinstance(node.op, ast.Mult): return f32(a * b)
            if isinstance(node.op, ast.Div): return f32(a / b)
        raise ValueError('Unknown material expression: ' + ast.dump(node))
    return visit(ast.parse(expression.strip(), mode='eval').body)


def string(value):
    value = value.encode('utf-8')
    count, prefix = len(value), bytearray()
    while count >= 128:
        prefix.append((count & 127) | 128)
        count >>= 7
    prefix.append(count)
    return bytes(prefix) + value


def key(digest, minimum, maximum):
    return hashlib.sha256(string(RECIPE) + string(digest) +
                          struct.pack('<4f??i', *minimum, *maximum, True, False, -1)).hexdigest()


def patches(repository):
    source = repository / f'src/BridgeBuilder/Bridges/Types/{STYLE}/{STYLE}RoadAppearance.cs'
    binary = repository / f'assets/HardcodedNumbers/{STYLE}/Types_{STYLE}_{STYLE}RoadAppearance_cs.bin'
    code, values = source.read_text(encoding='utf-8-sig'), numbers(binary)
    definitions = {}
    for name in ('Fixed', 'Moving'):
        match = re.search(r'Patch ' + name + r'\([^\n]*?=> new\(new Vector2\(([^()]*)\), new Vector2\(([^()]*)\),[^\n]*?repeat: true\);', code)
        if match is None:
            raise ValueError('Measured repeat constructor changed: ' + name)
        definitions[name] = match.group(1), match.group(2)
    returned = code.split('return new[]', 1)[1].split('};', 1)[0]
    calls = re.findall(r'(Fixed|Moving)\(([^()]*)\)', returned)
    if [name for name, _ in calls] != ['Fixed', 'Fixed', 'Moving', 'Moving']:
        raise ValueError('Appearance-region layout changed')
    units = {}
    for region, (name, args) in enumerate(calls):
        names = {}
        if name == 'Fixed':
            names['minV'] = evaluate(args.split(',')[0], values, {})
        low, high = ([evaluate(v, values, names) for v in expression.split(',')]
                     for expression in definitions[name])
        if not all(0 <= a < b <= 1 for a, b in zip(low, high)):
            raise ValueError('Invalid measured atlas rectangle')
        identity = tuple(low + high)
        if identity not in units:
            units[identity] = dict(min=low, max=high, cutout=True, rows=False, bands=None, regions=[])
        units[identity]['regions'].append(region)
    return list(units.values()), dict(runtimeSourceSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),
                                    numericalResourceSHA256=hashlib.sha256(binary.read_bytes()).hexdigest())


def reachable_regions(repository):
    common = repository / 'src/BridgeBuilder/Bridges/Common/Movable'
    preparation = common / 'MovableRoadPreparation.cs'
    atlas = common / 'MovableSurfaceAtlas.cs'
    code = preparation.read_text(encoding='utf-8-sig')
    assignment = re.search(r'PrototypeRoad\s*=\s*layout\.Prototype is ([^,\n]+)', code)
    if assignment is None or STYLE in re.findall(r'"([^"]+)"', assignment[1]):
        raise ValueError('Prototype moving-surface routing changed; review reachable recipes')
    if ('bones[face.Bone].m_ConnectionID != 0' not in code or
        'face.Geometry, face.Bone | MovableSurfaceAtlas.MovingSurface' not in code):
        raise ValueError('Moving-domain routing changed')
    sampling = atlas.read_text(encoding='utf-8-sig')
    for required in ('nativeRoad && input.PrototypeRoad ? input.Native : null',
                     'nativeRoad ? original : patch.Surface ?? input.Native'):
        if required not in sampling:
            raise ValueError('Native/prototype material selection changed')
    # The current movable route sets MovingSurface for both leaf regions. With
    # PrototypeRoad=false neither can sample the measured prototype road patch.
    # Keep their source recipes as evidence, not prepared assets or new behavior.
    return {0, 1}, dict(prototypeRoad=False, preparedRegions=[0, 1], sourceOnlyRegions=[2, 3],
                      preparationSHA256=hashlib.sha256(preparation.read_bytes()).hexdigest(),
                      atlasSHA256=hashlib.sha256(atlas.read_bytes()).hexdigest())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('native_audit', type=Path)
    parser.add_argument('structural_inventory', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    units, evidence = patches(args.repository)
    reachable, routing = reachable_regions(args.repository)
    audit = json.loads(args.native_audit.read_text(encoding='utf-8-sig'))
    render = [v['data'] for v in audit['selected'].values()
              if v['data'].get('m_Name') == 'PedestrianDrawBridge01 Mesh']
    if len(render) != 1:
        raise ValueError('Native near render missing or ambiguous')
    guid = render[0]['m_SurfaceAssets'][0]
    cid = ''.join(f'{b & 15:x}{b >> 4:x}' for b in struct.pack('<4i',
                  *(guid['m_GuidPart' + str(i)] for i in range(1, 5))))
    source_code = (args.repository / f'src/BridgeBuilder/Bridges/Types/{STYLE}/{STYLE}SourceRenders.cs').read_text()
    if not re.search(r'Create\("PedestrianDrawBridge01 Mesh", "[0-9a-f]+", new\[\] \{ "' + cid + '"', source_code):
        raise ValueError('Runtime native road source differs from audit')
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for name in archive.namelist():
                    if not name.endswith('.Surface.cid') or archive.read(name).decode().strip() != cid:
                        continue
                    candidate = archive.read(name[:-4])
                    if payload is not None and payload != candidate:
                        raise ValueError('Conflicting native source CID')
                    payload = candidate
    if payload is None:
        raise ValueError('Native source descriptor missing: ' + cid)
    inventory = json.loads(args.structural_inventory.read_text())
    sources = [s for s in inventory['sources'] if s['cid'] == cid]
    if len(sources) != 1 or sources[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Measured native template differs')
    digest = hashlib.sha256(payload).hexdigest()
    for unit in units:
        unit.update(style=STYLE, sourceCid=cid, sourceDigest=digest,
                    key=key(digest, unit['min'], unit['max']))
    prepared = [unit for unit in units if reachable.intersection(unit['regions'])]
    output = dict(recipe=RECIPE, sources=sources, units=prepared, sourceUnits=units,
                  evidence=dict(nativeAuditSHA256=hashlib.sha256(args.native_audit.read_bytes()).hexdigest(),
                                routing=routing, **evidence))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS {STYLE}: 4 measured regions, {len(units)} source recipes, {len(prepared)} reachable repeats; source={cid}; SHA256={digest}')


if __name__ == '__main__':
    main()
