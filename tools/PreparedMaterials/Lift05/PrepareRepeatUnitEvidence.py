"""Export genuine source evidence; never promote unverified crops to repeat units."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import numpy as np
from BuildUvCharts import inputs
from ExportRecipes import numbers, recipes, reachable_regions


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('repo', type=Path)
    p.add_argument('game', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    repo, out = a.repo.resolve(), a.output.resolve()
    source_inputs = json.loads((repo / 'artifacts/prepared-materials/Lift05/rebuild-native-v1/inputs/native-inputs.json').read_text(encoding='utf-8-sig'))
    old = json.loads((repo / 'artifacts/prepared-materials/Lift05/shared-sampling/source-sampling.json').read_text())
    payload_index = {}
    archives = []
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in sorted((a.game / 'Content' / family).glob('*.cok')):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                z = zipfile.ZipFile(path)
                archives.append(z)
                names = set(z.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        payload_index.setdefault(z.read(name).decode().strip(), (z, name[:-4]))
        channels = []
        for source in source_inputs['sources']:
            native = source['native']
            if native['isVT']:
                for stack in native['stacks']:
                    for layer, cid in enumerate(stack['vtTextureCids']):
                        if cid == '0' * 32:
                            continue
                        z, name = payload_index[cid]
                        raw = z.read(name)
                        assert name.endswith('.VTTexture') and raw[:3] == b'\x01\x00\xff'
                        preprocessing, tile, width, height, mid = struct.unpack_from('<5H', raw, 19)
                        mips, fmt, filtering, wrap, aniso = raw[29:34]
                        tail_bytes = struct.unpack_from('<i', raw, 34)[0]
                        count = struct.unpack_from('<i', raw, 38 + tail_bytes)[0]
                        offsets = struct.unpack_from('<' + 'i' * count, raw, 42 + tail_bytes)
                        start = 42 + tail_bytes + 4 * count
                        assert preprocessing == 0 and mips == max(width, height).bit_length()
                        assert all(b > x for x, b in zip((0,) + offsets[:-1], offsets))
                        assert start + offsets[-1] == len(raw)
                        original = stack['originalTextureCids'][layer]
                        channels.append(dict(sourceCid=source['cid'], stack=stack['index'], layer=layer,
                            originalTextureCid=original, originalTextureExists=original in payload_index,
                            payloadCid=cid, kind='VTTexture', archive=z.filename, entry=name,
                            bytes=len(raw), sha256=sha(raw), width=width, height=height, mipCount=mips,
                            graphicsFormat=fmt, filterMode=filtering, wrapMode=wrap, aniso=aniso,
                            tileSize=tile, midMipCount=mid, nativeTailBytes=tail_bytes,
                            tileRecords=count, consumedBytes=len(raw), vtSurfaceCid=native['vtSurfaceCid'],
                            externalMidMipsLookupRequired=mid > 0,
                            midMipsLookup=dict(sourceSurfaceCid=source['cid'],stack=stack['index'],layer=layer),
                            mipEvidence='Original tail and every tile record present; intermediate levels require global MidMips lookup. Source bytes untouched.'))
            else:
                for name, cid in native['textures'].items():
                    if not cid:
                        continue
                    z, entry = payload_index[cid]
                    raw = z.read(entry)
                    version, width, height, depth = struct.unpack_from('<4H', raw)
                    assert version in (4, 5) and entry.endswith('.Texture')
                    mips = raw[8]
                    assert mips == max(width, height).bit_length()
                    channels.append(dict(sourceCid=source['cid'], channel=name, payloadCid=cid,
                        kind='Texture', archive=z.filename, entry=entry, bytes=len(raw), sha256=sha(raw),
                        width=width, height=height, depth=depth, mipCount=mips, graphicsFormat=raw[9],
                        mipEvidence='Native complete mip count declared; original full payload retained, no conversion.'))
        patches, appearance_hashes = recipes(repo)
        reachable, reachability = reachable_regions(repo)
        mesh = next(x for x in inputs() if x[0] == 'LiftBridge05 Mesh' and x[1] == 0)
        name, index, positions, uv, faces = mesh
        source = next(x for x in source_inputs['sources'] if x['cid'] == '78fda4223742d922d34b5cc2331eb65f')
        f = numbers(repo / 'assets/HardcodedNumbers/Lift05/Types_Lift05_Lift05RoadAppearance_cs.bin')['F32']
        measured = []
        for ids in ((557, 558), (1366, 1367)):
            record = []
            for face in ids:
                pp, qq = positions[faces[face]].astype(float), uv[faces[face]].astype(float)
                dp = np.stack((pp[1] - pp[0], pp[2] - pp[0]), axis=0)
                du = np.stack((qq[1] - qq[0], qq[2] - qq[0]), axis=0)
                # Inverse measured UV affine, not an inferred rectangle/bounding box.
                inverse = np.linalg.solve(du, dp)
                record.append(dict(face=face, vertexIndices=faces[face].tolist(), positions=pp.tolist(), uv=qq.tolist(),
                    metresPerUvAxis=np.linalg.norm(inverse, axis=1).tolist(),
                    texelsPerMetre=(4096 / np.linalg.norm(inverse, axis=1)).tolist()))
            measured.append(record)
        region = patches[0]
        span = np.array(region['max']) - region['min']
        physical = span * np.array(measured[0][0]['metresPerUvAxis'])
        boundary = dict(status='NOT_PROVEN_SEAMLESS',
            reason='The original recipe selects a clean strip inside faces 557/558 to exclude authored rails; repeat:true is not boundary continuity evidence.',
            required='Decode the original full mip chain and verify opposite boundaries and normal semantics before authorizing a repeat unit.',
            originalIslandFaces=measured[0],
            declaredAtlasPixelRect=[region['min'][0]*4096, region['min'][1]*4096, region['max'][0]*4096, region['max'][1]*4096])
        out.mkdir(parents=True, exist_ok=True)
        evidence = dict(style='Lift05', sourceCid=source['cid'], sourceDigest=source['sha256'],
            prototypeRender=name, meshIndex=index, fixedFaces=measured[0], movingFaces=measured[1],
            boundary=boundary, channelPayloads=channels, appearance=appearance_hashes,
            reachability=reachability, layoutFixtureSha256=sha((repo / 'artifacts/uv-density/layout-plans.bin').read_bytes()))
        evidence_path = out / 'source-evidence.json'
        evidence_path.write_text(json.dumps(evidence, indent=2) + '\n')
        candidate = dict(id='Lift05.FixedRoad.NativeCleanStrip', sourceCid=source['cid'], sourceDigest=source['sha256'],
            min=region['min'], max=region['max'], physicalSize=physical.tolist(),
            measuredTexelsPerMetre=measured[0][0]['texelsPerMetre'],
            runtimeTexelsPerMetre=[4096*(f[14]-f[12])/f[16],4096*span[1]/f[22]],
            originalFaces=[557,558], cutout=region['cutout'], regions=sorted(reachable),
            aliases=['3b59afd3aa0ad2d963d6c6c7ba95cb1d0e33ccd1eec3f4b19e1a324a77551222'],
            evidence=dict(path='source-evidence.json',sha256=sha(evidence_path.read_bytes())),
            status='CANDIDATE_ONLY_BOUNDARY_AND_FULL_VT_DECODE_REQUIRED')
        manifest = dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift05',units=[],candidates=[candidate],
            note='Empty approved units deliberately prevent the baker from treating an unverified strip as a genuine repeat unit.')
        (out / 'repeat-unit-candidates.json').write_text(json.dumps(manifest, indent=2)+'\n')
        mapping = dict(style='Lift05',repeat=[dict(oldKey=candidate['aliases'][0],candidate=candidate['id'],
            regions=sorted(reachable),status=candidate['status'])],
            structure=[dict(oldKey=s['key'],sourceCid=s['sourceCid'],sourceSampling=s['originalSampling'],
                newUnit=None,action='Reuse original Surface only when complete physical-part UV chart contains receiver; otherwise needs proven semantic source unit.')
                for s in old['structureSamplers']],
            oldPayloadsRead=0,preparedPixelPayloadsCreated=0)
        (out / 'call-mapping.json').write_text(json.dumps(mapping,indent=2)+'\n')
        print(json.dumps(dict(approvedUnits=0,candidates=1,structureMappings=len(mapping['structure']),
                             nativeChannels=len(channels),physicalSize=physical.tolist())))
    finally:
        for z in archives:
            z.close()


if __name__ == '__main__':
    main()
