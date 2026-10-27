"""Extract Draw01's immutable repeat recipes from its actual source and BBN arrays.

This is offline authoring only. It never reads generated bridge assets, modifies
geometry, or writes installed materials. Output is accepted by Prepare-Repeats.ps1.
"""
import argparse
import ast
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path

RECIPE = 'BridgeBuilder/RoadRepeat/1'


def sampler_key(digest, minimum, maximum, cutout):
    # Exact BinaryWriter protocol used by MovableRepeatKey, without bands.
    def string(value):
        encoded, prefix = value.encode('utf-8'), bytearray()
        count = len(encoded)
        while count >= 128:
            prefix.append((count & 127) | 128)
            count >>= 7
        prefix.append(count)
        return bytes(prefix) + encoded
    payload = string(RECIPE) + string(digest)
    payload += struct.pack('<4f??i', *minimum, *maximum, cutout, False, -1)
    return hashlib.sha256(payload).hexdigest()


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def numbers(path):
    payload = path.read_bytes()
    if payload[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(payload[:-32]).digest() != payload[-32:]:
        raise ValueError('Invalid immutable numerical resource: ' + str(path))
    counts = struct.unpack_from('<6i', payload, 8)
    offset = 32
    values = {}
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'), ('i', 'I', 'q', 'Q', 'f', 'd'), counts):
        if count < 0:
            raise ValueError('Negative array length')
        values[name] = struct.unpack_from('<' + fmt * count, payload, offset)
        offset += count * struct.calcsize(fmt)
    if offset != len(payload) - 32:
        raise ValueError('Unconsumed numerical resource bytes')
    return values


def evaluate(expression, values, names):
    expression = re.sub(r'Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values\.(I32|F32)\[(\d+)\]',
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
        raise ValueError('Unrecognized Draw01 material expression: ' + ast.dump(node))
    return visit(ast.parse(expression.strip(), mode='eval').body)


def source_recipes(repository):
    path = repository / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01ClosedRoad.cs'
    code = path.read_text(encoding='utf-8-sig')
    numeric_path = repository / 'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01ClosedRoad_cs.bin'
    values = numbers(numeric_path)
    names = {'asphaltMin': evaluate(re.search(r'float asphaltMin = (.*);', code)[1], values, {})}
    block = code.split('var positive = new[]', 1)[1].split('// Native right carriageway', 1)[0]
    rectangles = re.findall(r'Project\(new Vector2\(([^()]*)\), new Vector2\(([^()]*)\),', block)
    if len(rectangles) != 5:
        raise ValueError('Draw01 positive-leaf material recipe layout changed')
    rects = []
    for low, high in rectangles:
        rects.append(([evaluate(v, values, names) for v in low.split(',')],
                      [evaluate(v, values, names) for v in high.split(',')]))
    # These relationships are the actual runtime assignments. Reject a changed
    # layout instead of silently authoring recipes for a stale interpretation.
    if [values['I32'][i] for i in range(38, 47)] != [14, 0, 3, 5, 6, 0, 7, 7, 7]:
        raise ValueError('Draw01 leaf aliases changed')
    if [values['I32'][i] for i in range(47, 55)] != [0, 5, 7, 12, 1, 4, 8, 11]:
        raise ValueError('Draw01 asphalt/grating assignments changed')
    if 'patch.Min, patch.Max, patch.U, patch.V, roadMaterial.Asset' not in code or 'patch.Min, patch.Max, patch.U, patch.V, repeat: true' not in code:
        raise ValueError('Draw01 material selection changed')
    if values['I32'][64:66] != (0, 0):
        raise ValueError('Draw01 source render/material slot changed')
    # Patch five mirrors asphalt, patch six mirrors the concrete cap, then all
    # seven repeat on the opposite leaf. U/V phase is receiver data, not pixels.
    aliases = [0, 1, 2, 3, 4, 0, 3] * 2
    result = {}
    for region, index in enumerate(aliases):
        minimum, maximum = rects[index]
        cutout = region not in (0, 5, 7, 12)
        key = (tuple(minimum), tuple(maximum), cutout)
        if key not in result:
            result[key] = dict(min=minimum, max=maximum, cutout=cutout, rows=False, bands=None, regions=[])
        result[key]['regions'].append(region)
    if len(result) != 4:
        raise ValueError('Draw01 repeat resource count changed')
    return list(result.values()), {'runtimeSourceSHA256': hashlib.sha256(path.read_bytes()).hexdigest(),
                                  'numericalResourceSHA256': hashlib.sha256(numeric_path.read_bytes()).hexdigest()}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('game_data', type=Path)
    p.add_argument('native_audit', type=Path)
    p.add_argument('structural_inventory', type=Path)
    p.add_argument('output', type=Path)
    args = p.parse_args()
    recipes, evidence = source_recipes(args.repository)
    audit = json.loads(args.native_audit.read_text())
    render = [item['data'] for item in audit['selected'].values() if item['data'].get('m_Name') == 'DrawBridge01 Mesh']
    if len(render) != 1:
        raise ValueError('Native near render is missing or ambiguous')
    guid = render[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid') or archive.read(entry).decode().strip() != cid:
                        continue
                    candidate = archive.read(entry[:-4])
                    if payload is not None and payload != candidate:
                        raise ValueError('Native source CID has conflicting bytes')
                    payload = candidate
    if payload is None:
        raise ValueError('Native material source is absent: ' + cid)
    inventory = json.loads(args.structural_inventory.read_text())
    descriptions = [s for s in inventory['sources'] if s['cid'] == cid]
    if len(descriptions) != 1 or descriptions[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Native material template evidence differs')
    digest = hashlib.sha256(payload).hexdigest()
    units = [dict(style='Draw01', sourceCid=cid, sourceDigest=digest,
                  key=sampler_key(digest, r['min'], r['max'], r['cutout']), **r) for r in recipes]
    output = dict(recipe=RECIPE, sources=descriptions, units=units,
                  evidence=dict(nativeAuditSHA256=hashlib.sha256(args.native_audit.read_bytes()).hexdigest(), **evidence))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Draw01 repeat recipes: 14 appearance regions, {len(units)} unique pixel recipes; source={cid}; SHA256={digest}; unchanged Float32 bounds; no generated bridge inputs.')


if __name__ == '__main__':
    main()
