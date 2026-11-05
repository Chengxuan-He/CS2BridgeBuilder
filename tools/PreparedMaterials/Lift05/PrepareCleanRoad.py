"""Author bounded, rail-free Lift05 road periods from measured native donors.

The three physical appearances remain distinct. This is not reconstruction of
occluded pixels, an original-period claim, or a receiver-sized road bake.
"""
import hashlib
import json
import shutil
import struct
import sys
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Lift05/rail-contamination'
sys.path.insert(0, str(ROOT / 'tools/PreparedMaterials/Lift04'))
from InspectStructureCounterexample import bc7_image
sys.path.insert(0, str(ROOT / 'tools/PreparedMaterials/Draw03'))
import ComposeRoadPeriods as native
sys.path.insert(0, str(Path(__file__).parent))
from ExportRecipes import numbers

NAMES = ('_BaseColorMap', '_NormalMap', '_MaskMap')
RECIPE = 'BridgeBuilder/Lift05/PureRoadPeriods/3'


def digest(b):
    return hashlib.sha256(b).hexdigest()


def levels(pixels, channel, srgb):
    value = pixels.astype(np.float32) / 255
    if channel == 0 and srgb:
        rgb = value[..., :3]
        value[..., :3] = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
    elif channel == 1:
        xy = np.stack((value[..., 0] * value[..., 3], value[..., 1]), -1) * 2 - 1
        value[..., :3] = np.concatenate((xy, np.sqrt(np.maximum(0, 1 - (xy * xy).sum(-1)))[..., None]), -1)
    # Author a narrow periodic boundary in decoded channel space. All channels
    # use identical paired donor coordinates/weights. Grain is not rescaled.
    for axis in (0, 1):
        size = value.shape[axis]
        border = min(8, size // 2)
        original = value.copy()
        for i in range(border):
            a, b = [slice(None)] * 3, [slice(None)] * 3
            a[axis], b[axis] = i, size - 1 - i
            weight = .5 * (1 - i / border)
            value[tuple(a)] = original[tuple(a)] * (1 - weight) + original[tuple(b)] * weight
            value[tuple(b)] = original[tuple(b)] * (1 - weight) + original[tuple(a)] * weight
    result = []
    while True:
        encoded = value.copy()
        if channel == 0 and srgb:
            rgb = encoded[..., :3]
            encoded[..., :3] = np.where(rgb <= .0031308, rgb * 12.92, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
        elif channel == 1:
            n = encoded[..., :3]
            n /= np.maximum(np.linalg.norm(n, axis=-1)[..., None], 1e-12)
            x, y = (n[..., 0] * .5 + .5).copy(), (n[..., 1] * .5 + .5).copy()
            encoded[..., 0], encoded[..., 1], encoded[..., 2], encoded[..., 3] = 1, y, y, x
        result.append(np.clip(np.rint(encoded * 255), 0, 255).astype(np.uint8))
        h, w = value.shape[:2]
        if h == w == 1:
            break
        size = (max(1, w // 2), max(1, h // 2))
        value = np.stack([np.asarray(Image.fromarray(value[..., c], mode='F').resize(size, Image.Resampling.BOX)) for c in range(4)], -1)
    return result


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((OUT.parent / 'road-continuous-candidate/manifest.json').read_text())
    near = []
    source_hashes = []
    for c in manifest['units'][0]['channels']:
        raw = (OUT.parent / 'offline-repeat-candidate/source' / (c['sha256'] + '.Texture')).read_bytes()
        assert digest(raw) == c['sha256']
        near.append(np.asarray(bc7_image(raw)))
        source_hashes.append(digest(raw))
    sys.path.insert(0, str(ROOT/'tools/PrepareRepeatUnits'))
    from ExtractVtSources import Decoder
    decoder = Decoder(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'))
    evidence = json.loads((ROOT/'artifacts/prepared-materials/Lift05/repeat-units-v2-evidence/source-evidence.json').read_text())
    far_provenance = []
    for channel in evidence['channelPayloads']:
        if channel['sourceCid'] != 'd68e4bd81e9e8a61ded03d2ac017680f':
            continue
        with zipfile.ZipFile(channel['archive']) as archive:
            raw = archive.read(channel['entry'])
        assert digest(raw) == channel['sha256']
        name = NAMES[channel['layer']]
        texture, proof = decoder.recover(dict(name=name, kind=channel['kind'], layer=channel['layer']), raw, channel['sourceCid'])
        (OUT/('far'+name+'.Texture')).write_bytes(texture)
        far_provenance.append(dict(name=name, source=channel, recoveredSha256=digest(texture), proof=proof))
    (OUT/'far-source-extraction.json').write_text(json.dumps(far_provenance, indent=2)+'\n')
    far = [np.asarray(bc7_image((OUT / ('far' + n + '.Texture')).read_bytes())) for n in NAMES]
    native.RECIPE = RECIPE
    writer = native.Writer(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'), OUT / 'staged-v3')
    zero, _ = writer.texture([np.zeros((1, 1, 4), np.uint8)], False)
    records = []
    for index, (name, source, rect) in enumerate((
        ('Fixed', near, (1664, 1728, 128, 256)),
        ('Moving', near, (64, 2600, 64, 256)),
        ('FarMoving', far, (277, 60, 3, 64)),
    )):
        x, y, w, h = rect
        assert not source[2][y:y+h, x:x+w, :3].any(), 'Metal/foreign mask in pure donor'
        key = digest((RECIPE + '/' + name).encode())
        bindings, channels = {'_ControlMask': zero}, []
        for c, full in enumerate(source):
            donor = full[y:y+h, x:x+w].copy()
            # Names are verified against the installed Unity enum: BC7 SRGB=108,
            # BC7 UNorm=109. Preserve the actual carrier's declared color space.
            source_raw = ((OUT.parent / 'offline-repeat-candidate/source' / (source_hashes[c]+'.Texture')).read_bytes()
                          if source is near else (OUT / ('far'+NAMES[c]+'.Texture')).read_bytes())
            srgb = source_raw[9] == 108
            assert source_raw[9] in (108, 109)
            chain = levels(donor, c, srgb)
            if c == 2:
                assert all(not mip[..., :3].any() for mip in chain)
            cid, size = writer.texture(chain, srgb)
            bindings[NAMES[c]] = cid
            Image.fromarray(chain[0]).save(OUT / (name + NAMES[c] + '.png'))
            channels.append(dict(name=NAMES[c], cid=cid, bytes=size, sourceFormat=source_raw[9], srgb=srgb,
                                 mips=[list(m.shape[1::-1]) for m in chain],
                                 decodedMipHashes=[digest(m.tobytes()) for m in chain],
                                 baseSeamMax=int(max(np.abs(chain[0][0].astype(int)-chain[0][-1]).max(), np.abs(chain[0][:,0].astype(int)-chain[0][:,-1]).max()))))
        cid, _ = writer.surface(bindings, key)
        records.append(dict(part=name, unit=4+index, key=key, surfaceCid=cid,
                            donorRectangle=list(rect), sourceSize=source[0].shape[0], channels=channels))
    # Preserve the measured physical partition tree and every noncentral record.
    original = ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_RoadProjection.bin'
    backup = OUT / 'before'
    backup.mkdir(exist_ok=True)
    if not (backup / original.name).exists():
        shutil.copyfile(original, backup / original.name)
    data = numbers(backup / original.name)
    integers, doubles = list(data['I32']), list(data['F64'])
    evidence = json.loads((OUT.parent / 'road-continuous-candidate/mapping-evidence.json').read_text())
    prior = json.loads((OUT.parent / 'road-projection-plan.json').read_text())
    i, j, mappings = 2, 0, []
    for lod, (ev, old) in enumerate(zip(evidence['levels'], prior['levels'])):
        count, nodes = integers[i:i+2]
        i += 5
        groups = ev['continuousComponents'] + [[p['patch']] for p in ev['patches'] if p['mode'] == 'original-finite']
        assert count == len(groups)
        for p, group in enumerate(groups):
            band = integers[i]
            if band == 1:
                src = old['patches'][group[0]]
                record = records[2 if lod == 2 else (0 if src['bone'] == 0 else 1)]
                assert src['bone'] in (0, 2)
                coefficients = np.array(src['coefficients'], dtype=float)
                span = np.array(record['donorRectangle'][2:]) / record['sourceSize']
                coefficients /= span
                coefficients[2] = 0  # One declared object-origin phase per physical appearance.
                integers[i+1] = record['unit']
                doubles[j:j+10] = coefficients.ravel().tolist() + [0., 0., 1., 1.]
                mappings.append(dict(lod=lod, patch=p, unit=record['unit'],
                                     faces=[f for k in group for f in old['patches'][k]['faces']],
                                     coefficients=coefficients.tolist(), bone=src['bone']))
            i += 2
            j += 10
        i += nodes * 2
        j += nodes * 3
    assert i == len(integers) and j == len(doubles)
    raw = b'BBN1' + struct.pack('<7i', 1, len(integers), 0, 0, 0, 0, len(doubles))
    raw += struct.pack('<%di' % len(integers), *integers) + struct.pack('<%dd' % len(doubles), *doubles)
    (OUT / original.name).write_bytes(raw + hashlib.sha256(raw).digest())
    report = dict(recipe=RECIPE, sourceNearHashes=source_hashes,
                  farSourceHashes=[digest((OUT / ('far'+n+'.Texture')).read_bytes()) for n in NAMES],
                  authoredPeriods=True, runtimeBake=False, geometryChanges=0,
                  sourceAssetsChanged=False, sourceCoordinatesShared=True,
                  parts=records, mappings=mappings,
                  scope='Central fixed and moving road domains, including finite terminals and far central road. Original side domains and all geometry unchanged.',
                  quality='Native validation and human visual acceptance pending; new periodic edges are authored, not original evidence.')
    (OUT / 'authoring.json').write_text(json.dumps(report, indent=2)+'\n')
    print('Staged', len(records), 'parts;', len(mappings), 'central patches; bytes', sum(p.stat().st_size for p in (OUT/'staged-v3').rglob('*') if p.is_file()))


def install(check=False):
    pairs = [(OUT/'Types_Lift05_RoadProjection.bin', ROOT/'assets/HardcodedNumbers/Lift05/Types_Lift05_RoadProjection.bin')]
    pairs += [(p, ROOT/'assets/Materials/Lift05'/p.relative_to(OUT/'staged-v3'))
              for p in (OUT/'staged-v3').rglob('*') if p.is_file()]
    for source, target in pairs:
        if check:
            assert target.read_bytes() == source.read_bytes(), target
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists() and target.suffix != '.bin':
                assert target.read_bytes() == source.read_bytes(), 'Changed immutable CID payload'
            shutil.copyfile(source, target)
    if not check:
        shutil.copyfile(Path(__file__).with_name('Lift05NativeRoadAppearance.template.cs'),
                        ROOT/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05NativeRoadAppearance.cs')


if __name__ == '__main__':
    main()
    if '--install' in sys.argv or '--check' in sys.argv:
        install('--check' in sys.argv)
