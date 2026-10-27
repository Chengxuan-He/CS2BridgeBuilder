"""Measure real fixed-road units and record native full-mip carrier constraints."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile
import numpy as np
from ExportRecipes import patches
from ExportSharedSampling import descriptor, cid


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    root = args.repository.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    measured, code_evidence = patches(root)
    source_cid = '15c63410efe103a46b109b0dd6aa246d'
    # Durable offline native inputs, never distributed material products.
    source_inputs = output/'native-sources'
    source_inputs.mkdir(exist_ok=True)
    native_path = root/'artifacts/uv-density/native/PedestrianDrawBridge01 Mesh.npz'
    native = np.load(native_path)
    positions, uvs, faces = (native['mesh0_'+name] for name in ('positions', 'uv0', 'faces'))
    provenance = root/'artifacts/prepared-materials/PedestrianDraw01/shared-sampling'
    bindings = json.loads((provenance/'native-bindings.json').read_text())
    original = source_inputs/(source_cid+'.Surface')
    blob_archives = sorted((args.game_data/'Content').rglob('Blob*.cok'))
    original_payload = None
    for path in blob_archives:
        with zipfile.ZipFile(path) as archive:
            for entry in archive.namelist():
                if entry.endswith('.Surface.cid') and archive.read(entry).decode().strip() == source_cid:
                    payload = archive.read(entry[:-4])
                    assert original_payload is None or original_payload == payload
                    original_payload = payload
    assert original_payload is not None
    assert hashlib.sha256(original_payload).hexdigest() == bindings['nativeDependencyGraph'][source_cid]['sha256']
    original.write_bytes(original_payload)
    original.with_name(original.name+'.cid').write_text(source_cid, encoding='ascii')
    template, header, combined, authored, keywords = descriptor(original_payload)
    assert template == 1 and header and combined is None
    names = ['_BaseColorMap', '_NormalMap', '_MaskMap']
    source_ids = [cid(header[76+16*i:92+16*i]) for i in range(3)]
    authoring_matches = {value: [] for value in authored.values()}
    for path in blob_archives:
        with zipfile.ZipFile(path) as archive:
            for entry in archive.namelist():
                if entry.endswith('.Texture.cid'):
                    identity = archive.read(entry).decode().strip()
                    if identity in authoring_matches:
                        authoring_matches[identity].append(dict(archive=str(path), entry=entry[:-4]))
    channels, layouts = [], []
    for name, identity in zip(names, source_ids):
        carrier = bindings['nativeDependencyGraph'][identity]
        path = source_inputs/(identity+'.VTTexture')
        with zipfile.ZipFile(carrier['archive']) as archive:
            payload = archive.read(carrier['entry'])
        assert hashlib.sha256(payload).hexdigest() == carrier['sha256']
        path.write_bytes(payload)
        path.with_name(path.name+'.cid').write_text(identity, encoding='ascii')
        assert digest(path) == carrier['sha256']
        assert payload[:3] == b'\x01\x00\xff' and cid(payload[3:19]) == authored[name]
        tail_bytes = struct.unpack_from('<i', payload, 34)[0]
        table = 38+tail_bytes
        records = struct.unpack_from('<i', payload, table)[0]
        ends = struct.unpack_from('<'+'i'*records, payload, table+4)
        body = table+4+records*4
        assert tail_bytes >= 0 and all(b > a for a, b in zip((0,)+ends, ends))
        assert body+ends[-1] == len(payload)
        def segment(start, end):
            return dict(offset=start, bytes=end-start,
                sha256=hashlib.sha256(payload[start:end]).hexdigest())
        layouts.append(dict(cid=identity, name=name, path=str(path), sha256=digest(path),
            fullBytes=len(payload), header=segment(0, 38),
            nativeTailBlock=segment(38, table),
            tileTable=segment(table, body),
            compressedTileRecords=[segment(body+a, body+b) for a, b in zip((0,)+ends, ends)],
            nativeHeaderMipCount=payload[29], nativeHeaderMidMipCount=struct.unpack_from('<H', payload, 27)[0],
            allSerializedBytesCovered=True, allLevelsDecoded=False,
            note='Native boundaries only. Do not infer a full-mip decoder from retained serialized bytes.'))
        channels.append(dict(name=name, cid=identity, sha256=carrier['sha256'], path=str(path),
            nativeKind='VTTexture', kind='VTTexture', originalAuthoringCid=authored[name],
            originalTextureFound=bool(authoring_matches[authored[name]]), **{
                key: carrier[key] for key in ('width', 'height', 'mipCount', 'midMipCount',
                    'nativeTailBytes', 'nativeTileRecords', 'format', 'filter', 'wrap', 'anisotropy',
                    'archive', 'entry')}))
    old = json.loads((root/'artifacts/prepared-repeat/PedestrianDraw01/recipes.json').read_text())
    units = []
    for region, face_ids in enumerate(([1035, 1036], [13243, 13244])):
        unit = next(x for x in measured if x['regions'] == [region])
        triangle_positions = positions[faces[face_ids]].astype(np.float64)
        triangle_uvs = uvs[faces[face_ids], :2].astype(np.float64)
        low, high = triangle_uvs.reshape(-1, 2).min(axis=0), triangle_uvs.reshape(-1, 2).max(axis=0)
        assert np.array_equal(low, unit['min']) and np.array_equal(high, unit['max'])
        bounds = triangle_positions.reshape(-1, 3)
        physical = np.ptp(bounds[:, [0, 2]], axis=0)
        assert list(physical) == [4.0, 4.0]
        affine = np.linalg.lstsq(np.column_stack((bounds[:, 0], bounds[:, 2], np.ones(6))),
                                triangle_uvs.reshape(-1, 2), rcond=None)[0]
        pixels = (high-low)*4096
        evidence = dict(style='PedestrianDraw01', kind='authored-fixed-road-unit',
            render='PedestrianDrawBridge01 Mesh', mesh=0, faces=face_ids,
            positions=triangle_positions.tolist(), uv=triangle_uvs.tolist(),
            uvBoundary=[[low[0], low[1]], [high[0], low[1]], [high[0], high[1]], [low[0], high[1]]],
            physicalSize=physical.tolist(), sourcePixels=pixels.tolist(), density=(pixels/physical).tolist(),
            exactSourcePixelBounds=[*(low*4096), *(high*4096)],
            uvAffineXZ=affine.tolist(), nativeMeshPath=str(native_path), nativeMeshSHA256=digest(native_path),
            sourceCid=source_cid, sourceDigest=digest(original), sourceTemplate=template,
            channels=channels, sourceCode=code_evidence,
            boundaryPolicy='Preserve fractional source texel phase; rounding or changing the measured unit is forbidden.',
            seamlessNativeRendererVerified=False)
        ep=output/f'fixed-region-{region}-evidence.json'
        ep.write_text(json.dumps(evidence, indent=2)+'\n')
        alias=next(x['key'] for x in old['units'] if x['regions'] == [region])
        units.append(dict(id=f'fixed-road-region-{region}', sourceCid=source_cid, sourceDigest=digest(original),
            min=low.tolist(), max=high.tolist(), physicalSize=physical.tolist(), density=(pixels/physical).tolist(),
            cutout=True, evidence=dict(path=str(ep), sha256=digest(ep)),
            descriptor=dict(path=str(original), sha256=digest(original)), channels=channels,
            aliases=[alias], purpose='fixed abutment road surface; not the moving selected-road surface',
            compatibleWithCurrentBaker=False))
    manifest = dict(recipe='BridgeBuilder/RepeatUnits/2', style='PedestrianDraw01', units=units,
        requirements=['Read complete native VTTexture chain including mid mips and tiled mip payloads.',
            'Preserve measured half/quarter-texel bounds with exact periods and phase, without rounding or density reduction.',
            'Prepare compatible cutout descriptor from native VT template 1 without using deleted old products.'],
        excluded=[dict(regions=[2, 3], reason='Moving surface uses selected-road material; no prototype unit generation.')])
    (output/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    (output/'native-vt-layout.json').write_text(json.dumps(dict(style='PedestrianDraw01',
        carrierKind='VTTexture', combinedVTSurface=None, channels=layouts,
        additionalNativeDependencyFiles='Resolve authored middle levels through the native MidMips material/stack/layer index.',
        nativeInputBytes=sum(x['fullBytes'] for x in layouts), distributedCandidateBytes=0), indent=2)+'\n')
    sampling=json.loads((provenance/'source-map.json').read_text())
    charts=json.loads((provenance/'native-charts.json').read_text())
    mapped={x['key']:x['chartKeys'] for x in charts['samplerChartMappings']}
    fixed={a:u['id'] for u in units for a in u['aliases']}
    uses=[dict(oldKey=s['key'], sourceCid=s['sourceCid'], family=s['family'],
        repeatUnit=fixed.get(s['key']), nativeChartKeys=mapped[s['key']],
        policy='measured fixed road repeat' if s['key'] in fixed else
            'original Surface if generated footprint is contained; otherwise common mapping remains required')
        for s in sampling['samplers']]
    (output/'legacy-uses.json').write_text(json.dumps(dict(style='PedestrianDraw01', uses=uses), separators=(',', ':'))+'\n')
    report=dict(units=len(units), fixedFaces=4, oldUseRecords=len(uses), vtChannels=len(channels),
        originalStandaloneTextureMatches=authoring_matches, sourceMipCount=13, nativeMidMipCount=3,
        bakeRun=False, generatedPayloads=0, readyForCurrentBaker=False,
        scannedBuiltinBlobArchives=len(blob_archives),
        reason='Current reader accepts ordinary Texture; measured input is full native VT. Fractional period support does not supply a VT mip reader.')
    (output/'evidence-report.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
