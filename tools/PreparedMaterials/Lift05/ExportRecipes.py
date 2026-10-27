"""Extract Lift05 road repeat units from the measured BBN1 arrays and native material."""
import argparse
import hashlib
import json
import re
import struct
import zipfile
from pathlib import Path


def numbers(path):
    payload = path.read_bytes()
    if payload[:8] != b'BBN1\x01\x00\x00\x00' or hashlib.sha256(payload[:-32]).digest() != payload[-32:]:
        raise ValueError('Invalid immutable numerical resource: ' + str(path))
    offset, result = 32, {}
    for name, fmt, count in zip(('I32', 'U32', 'I64', 'U64', 'F32', 'F64'),
                                ('i', 'I', 'q', 'Q', 'f', 'd'), struct.unpack_from('<6i', payload, 8)):
        if count < 0:
            raise ValueError('Negative numeric array length')
        result[name] = struct.unpack_from('<' + fmt * count, payload, offset)
        offset += count * struct.calcsize(fmt)
    if offset != len(payload) - 32:
        raise ValueError('Unconsumed numeric resource data')
    return result


def recipes(repository):
    source = repository / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RoadAppearance.cs'
    binary = repository / 'assets/HardcodedNumbers/Lift05/Types_Lift05_Lift05RoadAppearance_cs.bin'
    code = source.read_text(encoding='utf-8-sig')
    data = numbers(binary)
    integers, floats = data['I32'], data['F32']
    if integers[:11] != (8, 0, 2, 0, 2, 0, 2, 0, 0, 0, 0) or integers[16:18] != (4, 2):
        raise ValueError('Lift05 fixed/moving and half/side region layout changed')
    # Extract the actual branches rather than inventing a new atlas rectangle.
    rectangles = []
    for name in ('min', 'max'):
        expression = re.search(r'var ' + name + r' = moving == .*?\? new Vector2\((.*?)\) : new Vector2\((.*?)\);', code)
        if expression is None:
            raise ValueError('Lift05 rectangle expression changed: ' + name)
        branches = []
        for branch in expression.groups():
            indices = re.findall(r'Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.F32\[(\d+)\]', branch)
            if len(indices) != 2:
                raise ValueError('Lift05 rectangle must use two measured Float32 coordinates')
            branches.append([floats[int(index)] for index in indices])
        rectangles.append(branches)
    if 'new Patch(min, max, u, v, repeat: true)' not in code:
        raise ValueError('Lift05 native repeat/cutout semantics changed')
    return [dict(min=rectangles[0][moving], max=rectangles[1][moving],
                 cutout=True, rows=False, bands=None, regions=list(range(moving * 4, moving * 4 + 4)))
            for moving in range(2)], dict(runtimeSourceSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),
                                          numericalResourceSHA256=hashlib.sha256(binary.read_bytes()).hexdigest())


def reachable_regions(repository):
    """Check the actual fixed/moving call chain before filtering authoring payloads."""
    paths = (
        'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RoadPreparation.cs',
        'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RoadAppearance.cs',
        'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RoadPartitions.cs',
        'src/BridgeBuilder/Bridges/Types/Lift05/Lift05MechanismData.g.cs',
        'src/BridgeBuilder/Bridges/Common/Movable/MovableRoadPreparation.cs',
        'src/BridgeBuilder/Bridges/Common/Movable/MovableSurfaceAtlas.cs',
    )
    texts = [(repository / path).read_text(encoding='utf-8-sig') for path in paths]
    own, appearance, partitions, skeleton, shared, atlas = texts
    conditions = (
        'new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "Lift05")' in own,
        'Lift05RoadPartitions.Carriageway, Lift05RoadPartitions.Carriageway, appearance, appearance' in own,
        'Lift05RoadAppearance.Patches(sample.RoadToObject)' in own,
        '0 => Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[19]' in appearance,
        '2 => Numeric_Types_Lift05_Lift05RoadAppearance_cs.Values.I32[20]' in appearance,
        'return Convert(Lift05RoadPartitions.Carriageway);' in appearance,
        'bones[face.Bone].m_ConnectionID != 0' in shared,
        'receivers.Where(face => !Original(face))' in shared,
        'face.Bone | MovableSurfaceAtlas.MovingSurface' in shared,
        'Native = sources[0].GetSurfaceAsset(0)' in shared,
        'var roadSource = nativeRoad && input.PrototypeRoad ? input.Native : null;' in atlas,
        'var source = nativeRoad ? original : patch.Surface ?? input.Native;' in atlas,
        'bool repeat=!nativeRoad &&' in atlas,
    )
    prototype = re.search(r'PrototypeRoad = layout.Prototype is (.*?),', shared)
    if not all(conditions) or prototype is None or '"Lift05"' in prototype[1]:
        raise ValueError('Lift05 repeat reachability changed; review source selection before authoring')
    folder = repository / 'assets/HardcodedNumbers/Lift05'
    partition_values = numbers(folder / 'Types_Lift05_Lift05RoadPartitions_cs.bin')['I32']
    appearance_values = numbers(folder / 'Types_Lift05_Lift05RoadAppearance_cs.bin')['I32']
    mechanism_values = numbers(folder / 'Types_Lift05_Lift05MechanismData_g_cs.bin')['I32']
    connection_indices = re.findall(r'm_ConnectionID = Numeric_Types_Lift05_Lift05MechanismData_g_cs.Values.I32\[(\d+)\]', skeleton)
    connections = [mechanism_values[int(index)] for index in connection_indices]
    if (partition_values != (0, 2, 0) or appearance_values[19:22] != (0, 4, 2)
            or connections != [0, 0, 100] * 3):
        raise ValueError('Lift05 bone ownership or fixed/moving appearance mapping changed')
    refs = re.findall(r'new Partition\(Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.I32\[(\d+)\]\)', partitions)
    if refs != ['0', '1', '2']:
        raise ValueError('Lift05 motion partition leaves changed')
    return set(range(4)), dict(
        fixedBone=0, fixedConnection=0, movingBone=2, movingConnection=100,
        prototypeRoad=False, reachableRegions=list(range(4)),
        movingAppearance='Original selected surface; prototype repeat recipe is unreachable.',
        callChainSHA256={path: hashlib.sha256((repository / path).read_bytes()).hexdigest() for path in paths})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('repository', 'game_data', 'native_audit', 'structural_inventory', 'output'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    patches, evidence = recipes(args.repository)
    reachable, reachability = reachable_regions(args.repository)
    audit = json.loads(args.native_audit.read_text())
    renders = [item['data'] for item in audit['selected'].values()
               if item['data'].get('m_Name') == 'LiftBridge05 Mesh']
    if len(renders) != 1:
        raise ValueError('Lift05 near source render is absent or ambiguous')
    guid = renders[0]['m_SurfaceAssets'][0]
    raw = struct.pack('<4i', *(guid['m_GuidPart' + str(i)] for i in range(1, 5)))
    cid = ''.join(format(value & 15, 'x') + format(value >> 4, 'x') for value in raw)
    render_source = args.repository / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05SourceRenders.cs'
    material = re.search(r'Create\("LiftBridge05 Mesh", "[0-9a-f]{32}", new\[\] \{ "([0-9a-f]{32})"',
                         render_source.read_text(encoding='utf-8-sig'))
    if material is None or material[1] != cid:
        raise ValueError('Runtime Lift05 native material differs from measured source')
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
        raise ValueError('Native source material is missing: ' + cid)
    sources = [source for source in json.loads(args.structural_inventory.read_text())['sources'] if source['cid'] == cid]
    if len(sources) != 1 or sources[0]['template'] != struct.unpack_from('<i', payload, 2)[0]:
        raise ValueError('Native source template differs from extracted source evidence')
    digest = hashlib.sha256(payload).hexdigest()
    declared = [dict(style='Lift05', sourceCid=cid, sourceDigest=digest, key='', **patch) for patch in patches]
    result = dict(recipe='BridgeBuilder/RoadRepeat/1', sources=sources,
                  units=[unit for unit in declared if set(unit['regions']) <= reachable],
                  evidence=dict(nativeAuditSHA256=hashlib.sha256(args.native_audit.read_bytes()).hexdigest(),
                                renderSourceSHA256=hashlib.sha256(render_source.read_bytes()).hexdigest(),
                                reachability=reachability, declaredUnits=declared, **evidence))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(f'PASS Lift05 repeat recipes: 8 declared regions; 4 reachable fixed regions; 1 prepared unit; moving recipe evidence only; source={cid}; SHA256={digest}')


if __name__ == '__main__':
    main()
