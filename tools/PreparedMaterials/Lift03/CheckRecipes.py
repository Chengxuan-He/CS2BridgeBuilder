"""Check Lift03 source-density and reachable repeat variants against native evidence.

This verifies preparation inputs, not rendered/baked output or in-game acceptance.
"""
import argparse
import hashlib
import json
import math
import re
import struct
import zipfile
from pathlib import Path
from ExportRecipes import extract, f32, numbers, sampler_key


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('repository', 'game_data', 'roadbed_audit', 'inventory', 'report'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    units, evidence = extract(args.repository)
    inventory = json.loads(args.inventory.read_text())
    if len(inventory['units']) != len(units):
        raise ValueError('Incomplete Lift03 repeat descriptors')
    for expected, actual in zip(units, inventory['units']):
        for field in ('style', 'deck', 'min', 'max', 'cutout', 'bands', 'rows'):
            if expected[field] != actual[field]:
                raise ValueError('Prepared recipe differs: ' + field)
        if actual['key'] != sampler_key(actual['sourceDigest'], actual['min'], actual['max'], actual['cutout']):
            raise ValueError('Prepared recipe key differs')
    for key, value in evidence.items():
        if inventory['evidence'][key] != value:
            raise ValueError('Stale recipe evidence: ' + key)
    common = args.repository / 'src/BridgeBuilder/Bridges/Common/Movable'
    preparation = (common / 'MovableRoadPreparation.cs').read_text(encoding='utf-8-sig')
    atlas = (common / 'MovableSurfaceAtlas.cs').read_text(encoding='utf-8-sig')
    prototype = re.search(r'PrototypeRoad\s*=\s*([^;]+?),\s*\n', preparation)[1]
    if '"Lift03 Main"' not in prototype or '"Lift03 Auxiliary"' in prototype:
        raise ValueError('Review changed per-deck prototype routing')
    if 'patch.Repeat || source!=owner.Asset' not in atlas:
        raise ValueError('Review changed ordinary repeat cutout routing')
    promotion = atlas.split('tile=new Chart', 1)[1].split('repeating.Add', 1)[0]
    if 'Repeat=true' not in promotion or 'RepeatCutout=' in promotion:
        raise ValueError('Review changed homogeneous asphalt cutout promotion')
    native = json.loads(args.roadbed_audit.read_text())
    if {unit['sourceCid'] for unit in inventory['units']} != {native['surface_cid']}:
        raise ValueError('Roadbed audit source differs')
    payload = None
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('VT*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if entry.endswith('.VTTexture.cid') and archive.read(entry).decode().strip() == native['vt_cid']:
                        candidate = archive.read(entry[:-4])
                        if payload is not None and candidate != payload:
                            raise ValueError('Conflicting source VT payload')
                        payload = candidate
    if payload is None or hashlib.sha256(payload).hexdigest() != native['vt_sha256']:
        raise ValueError('Native VT source missing or changed')
    if struct.unpack_from('<H', payload)[0] != 1 or payload[2] != 255 or struct.unpack_from('<H', payload, 19)[0] != 0:
        raise ValueError('Unsupported native VT header')
    width, height = struct.unpack_from('<HH', payload, 23)
    if [width, height] != native['size']:
        raise ValueError('Native VT dimensions differ from audit')
    values = numbers(args.repository / 'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03RoadAppearance_cs.bin')['F32']
    metrics = []
    for deck, face_id, indices in [('Main', 23857, (8, 9)), ('Auxiliary', 27296, (18, 19))]:
        face = next(x for x in native['roadbed_faces'] if x['face'] == face_id)
        a, b, c = face['xyz']
        uv_a, uv_b, uv_c = face['uv']
        bx, bz, cx, cz = b[0]-a[0], b[2]-a[2], c[0]-a[0], c[2]-a[2]
        determinant = bx*cz-cx*bz
        gradients = [((uv_b[j]-uv_a[j])*cz-(uv_c[j]-uv_a[j])*bz)/determinant for j in (0, 1)]
        longitudinal = [(bx*(uv_c[j]-uv_a[j])-cx*(uv_b[j]-uv_a[j]))/determinant for j in (0, 1)]
        reference = [math.hypot(width*gradients[0], height*gradients[1]),
                     math.hypot(width*longitudinal[0], height*longitudinal[1])]
        unit = next(x for x in units if x['deck'] == deck)
        spans = [f32(b-a) for a, b in zip(unit['min'], unit['max'])]
        pixels = [4*math.ceil(f32(span*size)/4) for span, size in zip(spans, (width, height))]
        generated = [pixels[i]*abs(f32(values[index]/spans[i])) for i, index in enumerate(indices)]
        # Float32 projection quantization is bounded separately from downsampling.
        if any(not math.isclose(a, b, rel_tol=1e-6, abs_tol=1e-6) for a, b in zip(reference, generated)):
            raise ValueError('Measured source density is not retained: ' + deck)
        metrics.append(dict(deck=deck, nativeFace=face_id, sourceTexelsPerMetre=reference,
                            recipeTexelsPerMetre=generated, unitPixels=pixels,
                            physicalPeriodMetres=[spans[i]/abs(values[index]) for i, index in enumerate(indices)],
                            projectionAliases=4, cutoutVariants=[x['cutout'] for x in units if x['deck']==deck]))
    result = dict(status='PASS', scope='Offline recipe inputs; no baked/output appearance claim',
                  sourceVT=native['vt_cid'], sourceVTSHA256=native['vt_sha256'], sourcePixels=[width,height],
                  descriptors=len(units), distinctPixelRecipes=2, metrics=metrics,
                  runtimeInputs={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (common/'MovableRoadPreparation.cs',common/'MovableSurfaceAtlas.cs')})
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print('PASS Lift03: 3 reachable descriptors, 2 native-density pixel recipes, 8 mirror aliases; native VT identity and both deck gradients verified.')


if __name__ == '__main__':
    main()
