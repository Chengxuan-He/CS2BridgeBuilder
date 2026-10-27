"""Measure genuine Lift02 repeat provenance without baking or declaring untested seams valid."""
from collections import Counter
from pathlib import Path
import hashlib
import json
import math
import struct
import zipfile

import numpy as np

from PrepareRecipes import recipes, repeat_key

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Lift02/repeat-units-v2'
SOURCE = ROOT / 'artifacts/prepared-materials/Lift02/source-rebuild/native-source-inventory.json'
STRUCTURE = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(name, value):
    path = OUT / name
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    return dict(path=str(path), sha256=digest(path))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    inventory = json.loads(SOURCE.read_bytes())
    patches, numerical = recipes(ROOT)
    assert len(patches) == 1 and not inventory['prototypeRoad']
    patch = patches[0]
    source = next(s for s in inventory['surfaces'] if s['cid'] == 'ea923855f34e05d7a517992fe064ddb7')
    backing = next(s for s in inventory['sourcePayloads'] if s['cid'] == 'a5d80f9ec1223ac65124d799555c5d6c')
    with zipfile.ZipFile(source['archive']) as z:
        raw = z.read(source['entry'])
    assert hashlib.sha256(raw).hexdigest() == source['sha256']
    with zipfile.ZipFile(backing['archive']) as z:
        raw_backing = z.read(backing['entry'])
    assert hashlib.sha256(raw_backing).hexdigest() == backing['sha256']
    width, height = struct.unpack_from('<2i', raw_backing, 26)
    assert (width, height) == (4096, 4096)
    channels = []
    for layer, name in enumerate(('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask')):
        if raw_backing[106 + layer * 48]:
            channels.append(dict(name=name, sourcePayloadCid=backing['cid'], stack=0, layer=layer,
                                 dimensions=[width, height], colorSpace='sRGB' if layer == 0 else 'linear',
                                 fullMipExtraction='pending shared VT decoder'))
    assert [c['name'] for c in channels] == ['_BaseColorMap', '_NormalMap', '_MaskMap']
    low, high = np.array(patch['min']), np.array(patch['max'])
    evidence = []
    for render in ('LiftBridge02 Mesh', 'LiftBridge02_LOD1 Mesh'):
        path = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        native = np.load(path)
        faces = native['mesh0_faces']
        positions = native['mesh0_positions'][faces].astype(np.float64)
        uv = native['mesh0_uv0'][faces].astype(np.float64)
        matching = np.flatnonzero(np.all(uv.min(1) == low, axis=1) & np.all(uv.max(1) == high, axis=1))
        normals = np.cross(positions[matching, 1] - positions[matching, 0], positions[matching, 2] - positions[matching, 0])
        top = matching[normals[:, 1] > 0]
        assert len(matching) == 424 and len(top) == 212
        rows = []
        for side in (-1, 1):
            selected = top[np.all(positions[top, :, 0] * side > 0, axis=1)]
            intervals = sorted(set(tuple(v) for v in np.stack((positions[selected, :, 2].min(1), positions[selected, :, 2].max(1)), axis=1)))
            assert len(intervals) == 53 and all(b - a == 4 for a, b in intervals)
            assert all(intervals[i][1] == intervals[i + 1][0] for i in range(52))
            rows.append(dict(side=side, faceIndices=selected.tolist(), intervals=intervals,
                             repeatCount=53, periodMetres=4, contiguous=True))
        selected = positions[top[:2]]
        selected_uv = uv[top[:2]]
        gradients = []
        for p, t in zip(selected, selected_uv):
            gradient = np.linalg.solve((p[1:] - p[0])[:, [0, 2]], t[1:] - t[0])
            gradients.append(dict(physicalAxes=['X', 'Z'], uvJacobian=gradient.tolist(),
                                  pixelsPerMetre=np.linalg.norm(gradient * [width, height], axis=1).tolist()))
        evidence.append(dict(render=render, mesh=0, nativeMesh=dict(path=str(path), sha256=digest(path)),
                             sourceFaces=top[:2].tolist(), positions=selected.tolist(), uv=selected_uv.tolist(),
                             matchingFaces=matching.tolist(), topFaceCount=len(top), rows=rows, gradients=gradients))
    structures = json.loads(STRUCTURE.read_bytes())
    old = [u for u in structures['units'] if u['style'] == 'Lift02']
    within = []
    for unit in old:
        if unit['sourceCid'] != source['cid']:
            continue
        points = np.array([unit['a'], unit['b'], unit['c']] if unit['triangle'] else unit['bounds']).reshape(-1, 2)
        if np.all(points >= low - 1e-7) and np.all(points <= high + 1e-7):
            within.append(unit['key'])
    assert not within, 'Review new structural overlap; never alias by bounds alone.'
    old_repeat = repeat_key(dict(patch, sourceDigest=source['sha256']))
    footprint = (high - low) * [width, height]
    physical = [numerical['longitudinalRepeatMetres'], numerical['transverseRepeatMetres']]
    unit_id = hashlib.sha256(json.dumps(dict(sourceCid=source['cid'], sourceDigest=source['sha256'],
                                             min=patch['min'], max=patch['max'],
                                             backingSha256=backing['sha256'], channels=[c['name'] for c in channels]),
                                       sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    measured = dict(schema='BridgeBuilder/Lift02RepeatEvidence/2', style='Lift02',
                    source=source, backing=backing, measurements=evidence, numerical=numerical,
                    min=patch['min'], max=patch['max'], physicalSize=physical,
                    physicalAxes=['negative Z', 'mirrored X'], sourcePixels=footprint.tolist(),
                    sourcePixelsPerMetre=(footprint / physical).tolist(),
                    boundaryTexels=dict(min=(low * [width, height]).tolist(), max=(high * [width, height]).tolist()),
                    channels=channels, authoredControl=False,
                    genuineUnitEvidence=dict(longitudinal='53 contiguous native repetitions per side at two LODs',
                                             transverse='not proven by original strip geometry',
                                             filteringAndAllMips='pending; no mip-zero-only evidence accepted'),
                    originalAtlasMipLevels=1 + int(math.log2(max(width, height))),
                    verifiedDecodedMipLevels=0, extractionAuthorized=False,
                    limitations=['Native geometric repetition proves longitudinal use, not filtered equality of opposite boundaries.',
                                 'The source rectangle has fractional texel V bounds; integer snapping changes its authored pattern.',
                                 'Original source is VTSurface; original Texture CIDs are provenance, not available ordinary payloads.'])
    evidence_ref = write('evidence.json', measured)
    # Keep the proposed producer shape separate from bake authorization. Missing
    # VT channel export/descriptor inputs are explicit and must never be guessed.
    write('manifest.pending.json', dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift02', units=[dict(
        id=unit_id, sourceCid=source['cid'], sourceDigest=source['sha256'], min=patch['min'], max=patch['max'],
        physicalSize=physical, cutout=True, evidence=evidence_ref, descriptor=None, channels=[],
        aliases=[old_repeat])], readyToBake=False,
        pending=['Complete native VT source mip decoding', 'Verified fractional-boundary extraction from the complete VT input',
                 'Filtered boundaries and normal semantics across all mips', 'Verified non-VT cutout descriptor']))
    uses = dict(style='Lift02', unitId=unit_id, prototypeRoad=False, oldRepeatKeys=[old_repeat],
                roadAliases=[dict(region=0, transverseOrientation=1), dict(region=1, transverseOrientation=-1)],
                physicalDomains=dict(moving=dict(z=[-82, 82], bone=1, connection=100, appearance='selected original road'),
                                     fixed=[dict(z=[-106, -82], bone=0), dict(z=[82, 106], bone=0)]),
                sourceLods=['LiftBridge02 Mesh', 'LiftBridge02_LOD1 Mesh'],
                generationLods=['near', 'mid', 'far'], sourceStructuralKeyCount=len(old),
                structuralInventory=dict(path=str(STRUCTURE), sha256=digest(STRUCTURE)),
                structurallyContainedKeys=within, structuralAliases=[],
                unresolvedStructureBySource=dict(Counter(u['sourceCid'] for u in old)),
                unresolvedStructuralKeys=[dict(key=u['key'], sourceCid=u['sourceCid'], triangle=u['triangle']) for u in old],
                rule='The genuine fixed-end candidate does not replace unrelated structural samplers.')
    write('uses-and-missing-keys.json', uses)
    write('summary.json', dict(style='Lift02', genuineUnitCandidates=1, bakeReadyUnits=0,
                              actualSourceTopFaces=424, lods=2, repeatAliases=2, oldRepeatKeys=1,
                              unresolvedStructuralKeys=len(old), producedTextures=0,
                              originalSourceHashesVerified=True, mipLevelsVerified=0,
                              localization='No user-visible text changed. Existing localization remains unchanged.'))
    print(json.dumps(dict(output=str(OUT), candidates=1, ready=0, repeatAliases=2, structureUnmapped=len(old))))


if __name__ == '__main__':
    main()
