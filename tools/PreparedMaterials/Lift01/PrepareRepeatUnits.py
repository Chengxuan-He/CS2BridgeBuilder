"""Record real Lift01 repeat-unit provenance; never bake arbitrary structural samplers."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
import math
from pathlib import Path
import struct
import numpy as np

from ExportRecipes import source_recipes, recipe_key
from PrepareNativeObjects import ROOT, GAME
from VerifyDensity import native_bytes


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=True)
    density = json.loads((ROOT / 'artifacts/prepared-materials/Lift01/repeat-density.json').read_text())
    recipes, code_evidence = source_recipes(ROOT)
    assert len(recipes) == 1
    unit = recipes[0]
    source_cid = density['sourceCid']
    surface = native_bytes(GAME, source_cid, 'Blob')
    source_digest = sha(surface)
    old_key = recipe_key(source_digest, unit)
    assert old_key == density['key']
    mesh_path = ROOT / 'artifacts/uv-density/native/LiftBridge01 Mesh.npz'
    assert sha(mesh_path.read_bytes()) == density['nativeMeshEvidence']['sha256']
    mesh = np.load(mesh_path)
    positions, uv, faces = mesh['mesh0_positions'].astype(float), mesh['mesh0_uv0'][:, :2].astype(float), mesh['mesh0_faces']
    low, high = np.array(unit['min']), np.array(unit['max'])
    contained = ((uv >= low) & (uv <= high)).all(axis=1)
    selected = np.where(contained[faces].all(axis=1))[0].tolist()
    assert len(selected) == 192
    assert 11144 in selected and 11145 in selected
    expected_corners = {(x, y) for x in (low[0], high[0]) for y in (low[1], high[1])}
    tiles = []
    edges = defaultdict(list)
    for index in range(0, len(selected), 2):
        pair = selected[index:index + 2]
        assert pair[1] == pair[0] + 1
        ids = faces[pair].reshape(-1)
        p = positions[ids]
        q = uv[ids]
        assert {tuple(x) for x in q} == expected_corners
        assert len({tuple(x) for x in p}) == 4 and np.all(p[:, 1] == p[0, 1])
        minimum, maximum = p.min(axis=0), p.max(axis=0)
        physical = (maximum - minimum)[[0, 2]].tolist()
        assert all(v > 0 for v in physical), physical
        jacobians = []
        for f in pair:
            xz = positions[faces[f]][:, [0, 2]]
            chart = uv[faces[f]]
            jac = np.linalg.solve(xz[1:] - xz[0], chart[1:] - chart[0])
            measured = np.linalg.norm(jac * 4096, axis=1)
            assert np.allclose(measured, np.array([432., 860.]) / physical, rtol=0, atol=1e-10)
            jacobians.append(jac.tolist())
        tile_index = len(tiles)
        boundary = Counter()
        edge_uv = {}
        for f in pair:
            for ia, ib in zip(faces[f], np.roll(faces[f], -1)):
                a, b = tuple(positions[ia]), tuple(positions[ib])
                edge = tuple(sorted((a, b)))
                boundary[edge] += 1
                edge_uv[edge] = {a: tuple(uv[ia]), b: tuple(uv[ib])}
        for edge, count in boundary.items():
            if count == 1:
                edges[edge].append(dict(tile=tile_index, uv=[edge_uv[edge][v] for v in edge]))
        tiles.append(dict(faces=pair, minimum=minimum.tolist(), maximum=maximum.tolist(),
                          physicalSize=physical, originalTexelsPerMeter=(np.array([432., 860.]) / physical).tolist(),
                          matchesReferencePeriod=physical == [2.0, 4.0], originalUvGradients=jacobians))
    seams = [dict(positions=edge, uses=uses) for edge, uses in edges.items() if len(uses) == 2]
    assert seams, 'The real prototype must contain adjacent occurrences, not unrelated atlas aliases.'
    channels = []
    for channel in density['sourceChannels']:
        if channel['width'] == 0:
            channels.append(dict(name=channel['name'], kind='absent', prepared='reuse style Road/ZeroControl.Texture'))
            continue
        cid = channel['backing']
        payload = native_bytes(GAME, cid, 'VT')
        assert payload[:3] == b'\x01\x00\xff'
        tile, width, height, mid_count = struct.unpack_from('<4H', payload, 21)
        mip_count, fmt, filtering, wrapping, aniso = struct.unpack_from('<5B', payload, 29)
        tail_bytes = struct.unpack_from('<i', payload, 34)[0]
        table_at = 38 + tail_bytes
        tile_count = struct.unpack_from('<i', payload, table_at)[0]
        offsets = struct.unpack_from('<' + 'i' * tile_count, payload, table_at + 4)
        assert all(b > a for a, b in zip((0,) + offsets, offsets))
        assert table_at + 4 + tile_count * 4 + offsets[-1] == len(payload)
        assert (width, height) == (4096, 4096) and mip_count == 13
        channels.append(dict(name=channel['name'], kind='VTTexture', cid=cid, sha256=sha(payload),
                             bytes=len(payload), width=width, height=height, mipCount=mip_count,
                             midMipCount=mid_count, tileSize=tile, format=fmt, filter=filtering,
                             wrap=wrapping, anisotropy=aniso, tailBytes=tail_bytes, tileCount=tile_count,
                             completePayloadTableValidated=True,
                             mipDimensions=[[max(1, width >> m), max(1, height >> m)] for m in range(mip_count)],
                             allMipDecodedAndBoundaryPixelsVerified=False))
    inventory_path = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_text())
    structural = [u for u in inventory['units'] if u['style'] == 'Lift01']
    exact = [u['key'] for u in structural if u['sourceCid'] == source_cid and not u['triangle'] and
             all(struct.pack('<f', v) == struct.pack('<f', e) for v, e in zip(u['bounds'], unit['min'] + unit['max']))]
    assert not exact
    missing = [dict(key=u['key'], sourceCid=u['sourceCid'], triangle=u['triangle'],
                    preparedPresent=(ROOT / 'assets/Materials/Lift01/Structure' / u['key'] / 'Structure.Surface').exists(),
                    mappedRepeatUnit=None,
                    status='Native coverage must decide receiver use; no evidenced repeating-unit mapping for this legacy sampling key.') for u in structural]
    evidence = dict(mesh=str(mesh_path.relative_to(ROOT)), meshSha256=sha(mesh_path.read_bytes()),
                    referenceFaces=[11144, 11145], actualPrototypeTileCount=len(tiles),
                    actualPrototypeFaceCount=len(selected), exactSharedBoundaryCount=len(seams),
                    referencePeriodOccurrences=sum(t['matchesReferencePeriod'] for t in tiles),
                    originalPhysicalPeriods=[dict(size=list(k), occurrences=v) for k, v in Counter(tuple(t['physicalSize']) for t in tiles).items()],
                    tiles=tiles, continuousNativeSeams=seams,
                    pixelBoundaryAndFullMipAcceptance=False,
                    boundaryMeaning='Real adjacent prototype rectangles repeat this same authored region; this does not by itself prove converted pixel/mip seam quality.')
    (out / 'prototype-evidence.json').write_text(json.dumps(evidence, indent=2) + '\n')
    manifest = dict(schema='BridgeBuilder/Lift01/GenuineRepeatUnitEvidence/2', style='Lift01',
        units=[dict(id='Lift01.NativeFloor.2mx4m', sourceCid=source_cid, sourceSha256=source_digest,
                    sourceTemplate=1, min=unit['min'], max=unit['max'], physicalSize=[2., 4.],
                    texelsPerMeter=[216., 215.], requiredTopDimensions=[432, 860],
                    requiredOutputMipCount=math.floor(math.log2(860)) + 1,
                    purpose='Fixed and moving prototype floor, independent of selected road/width; all eight phases share one prepared unit.',
                    sourceChannels=channels, legacyRepeatKeys=[old_key], legacyStructureKeys=exact,
                    usages=density['receiverProjections'], geometryEvidence='prototype-evidence.json',
                    geometryEvidenceSha256=sha((out / 'prototype-evidence.json').read_bytes()),
                    fullMipBoundaryValidation='Pending RepeatUnits/2 producer/validator; do not accept mip-zero-only evidence.',
                    newProducerSchemaPending=True)],
        codeEvidence=code_evidence, structureInventorySha256=sha(inventory_path.read_bytes()),
        structureKeyCount=len(structural), structureKeyMapping='missing-keys.json',
        arbitraryTriangleUnitsCreated=0, materialsWritten=0,
        acceptance=dict(originalGeometryPeriodVerified=True, originalDensityVerified=True,
                        sourceMipPayloadTablesVerified=True, decodedFullMipBoundaryVerified=False, readyForHumanAcceptance=False))
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    (out / 'missing-keys.json').write_text(json.dumps(dict(style='Lift01',
        repeat=[dict(key=old_key, unit='Lift01.NativeFloor.2mx4m', preparedPresent=(ROOT / 'assets/Materials/Lift01/Repeat' / old_key / 'Repeat.Surface').exists())],
        structure=missing), indent=2) + '\n')
    print(json.dumps(dict(tiles=len(tiles), faces=len(selected), seams=len(seams), channels=len(channels),
        legacyStructureKeys=len(structural), evidencedStructureUnitMappings=len(exact), manifest=str(out / 'manifest.json'))))


if __name__ == '__main__':
    main()
