"""Integrate verified Draw03 Clean pixels with the actual opaque/double-sided callers."""
import argparse
import json
from pathlib import Path
import shutil
import struct

from PrepareNativeRebuild import digest, surface, payload_metadata

ROOT = Path(__file__).resolve().parents[3]
PROOF = ROOT / 'artifacts/prepared-materials/Draw03/repeat-units-v2'
STAGE = PROOF / 'clean-integration'
LIBRARY = Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/material-library-resolved.json')


def string(value):
    data = value.encode('utf-8')
    length, prefix = len(data), bytearray()
    while length >= 128:
        prefix.append((length & 127) | 128)
        length >>= 7
    return bytes(prefix) + bytes([length]) + data


def identity(data):
    return ''.join(pair[::-1] for pair in (digest(data)[i:i+2] for i in range(0, 32, 2)))


def copy_exact(source, target):
    if target.exists():
        assert source.read_bytes() == target.read_bytes(), f'Conflicting file: {target}'
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--integrate', action='store_true')
    args = parser.parse_args()
    library = json.loads(LIBRARY.read_bytes())
    trial = PROOF / 'private-clean-trial'
    unit = json.loads((trial / 'report.json').read_bytes())['units'][0]
    check = json.loads((trial / 'native-check.json').read_bytes())
    assert check['nativeHeaders'] and check['completeGraph'] and check['mips'] == 30
    assert all(x['maxLinearError'] <= .005 for x in check['levels'])
    candidate = trial / 'output/Draw03'
    original_path = candidate / 'RepeatUnits' / unit['key'] / 'Repeat.Surface'
    original = original_path.read_bytes()
    assert identity(original) == unit['surfaceCid']
    descriptor = surface(original, library)
    assert descriptor['template'] == 1 and not descriptor['virtualTextures']
    assert descriptor['keywords'] == ['_TANGENTSPACE_OCTO']
    assert descriptor['properties']['floats']['_Snow'] == '00000000'
    output = STAGE / 'output/Draw03'
    for path in (candidate / 'Shared').iterdir():
        if path.is_file():
            copy_exact(path, output / 'Shared' / path.name)
    for channel in unit['channels']:
        path = output / 'Shared' / (channel['cid'] + '.Texture')
        assert path.stat().st_size == channel['bytes'] and identity(path.read_bytes()) == channel['cid']
        metadata = payload_metadata(path.read_bytes(), '.Texture')
        assert (metadata['width'], metadata['height'], metadata['mipCount']) == (968, 164, 10)
    evidence = json.loads((PROOF / 'candidate-manifest.json').read_bytes())
    requests = [r for r in evidence['roadRequests'] if r['candidateId'] == 'Draw03/CleanAsphalt']
    aliases = []
    old_keywords = b'\xff' + struct.pack('<i', 1) + string('_TANGENTSPACE_OCTO')
    assert original.endswith(old_keywords)
    for request in requests:
        cutout = request['cutout']
        template = 14 if cutout else 0
        keywords = ['_ALPHATEST_ON', '_DISABLE_SSR_TRANSPARENT', '_DOUBLESIDED_ON', '_TANGENTSPACE_OCTO'] if cutout else ['_TANGENTSPACE_OCTO']
        data = original[:2] + struct.pack('<i', template) + original[6:-len(old_keywords)]
        data += b'\xff' + struct.pack('<i', len(keywords)) + b''.join(string(k) for k in keywords)
        decoded = surface(data, library)
        assert decoded['textureProperties'] == descriptor['textureProperties']
        assert decoded['properties'] == descriptor['properties'] and decoded['keywords'] == keywords
        cid = identity(data)
        key = digest(string('BridgeBuilder/RepeatUnits/2') + string(cid))
        path = output / 'RepeatUnits' / key / 'Repeat.Surface'
        path.parent.mkdir(parents=True, exist_ok=True)
        if path.exists():
            assert path.read_bytes() == data
        path.write_bytes(data)
        path.with_suffix('.Surface.cid').write_text(cid, encoding='utf-8')
        aliases.append(dict(samplerKey=request['legacyKey'], unitKey=key, template=template, surfaceCid=cid))
    formal = ROOT / 'assets/Materials/Draw03'
    existing = {p.relative_to(formal).as_posix(): p for p in formal.rglob('*') if p.is_file()}
    combined = dict(existing)
    for p in output.rglob('*'):
        if p.is_file():
            relative = p.relative_to(output).as_posix()
            assert relative not in combined or combined[relative].read_bytes() == p.read_bytes(), relative
            combined[relative] = p
    local, referenced = {}, set()
    for relative, p in combined.items():
        if relative.endswith('.cid'):
            cid = p.read_text().strip()
            assert cid not in local and relative[:-4] in combined, relative
            local[cid] = combined[relative[:-4]]
        else:
            assert relative+'.cid' in combined, relative
    builtin_proof = ROOT / 'artifacts/prepared-materials/Draw03/rebuild-native-v1'
    native_report = json.loads((builtin_proof/'producer-report.json').read_bytes())
    # Reuse the already verified built-in boundary only while every baseline file is unchanged.
    for row in native_report['files']:
        assert digest(combined[row['path']].read_bytes()) == row['sha256'], row['path']
    builtin = set(json.loads((builtin_proof/'graph-verification.json').read_bytes())['originalBuiltinReferences'])
    dependencies = {}
    for cid, p in local.items():
        if p.suffix == '.Surface':
            deps = surface(p.read_bytes(), library)['dependencies']
            assert all(d in local or d in builtin for d in deps), cid
            referenced.update(d for d in deps if d in local)
            referenced.add(cid)
            dependencies[cid] = deps
    assert referenced == set(local), 'Unreferenced local payload'
    files = [dict(path=k, bytes=p.stat().st_size, sha256=digest(p.read_bytes())) for k,p in sorted(combined.items())]
    if args.integrate:
        assert 'PASS native source descriptors: 2 descriptors' in (STAGE/'native-reader.log').read_text()
        for p in output.rglob('*'):
            if p.is_file():
                copy_exact(p, formal / p.relative_to(output))
        code = ['namespace BridgeBuilder.Bridges;', '', 'internal static partial class MovableRepeatUnits', '{',
                '    // Native opaque and double-sided descriptors share one Clean texture set.',
                '    static partial void Draw03(string samplerKey, ref string? unitKey)', '    {', '        switch (samplerKey)', '        {']
        for alias in aliases:
            code += [f'            case "{alias["samplerKey"]}":', f'                unitKey = "{alias["unitKey"]}";', '                break;']
        code += ['        }', '    }', '}', '']
        path = ROOT / 'src/BridgeBuilder/Bridges/Types/Draw03/Draw03RepeatUnits.cs'
        content = '\n'.join(code)
        assert not path.exists() or path.read_text() == content, 'Do not replace another code edit'
        path.write_text(content, encoding='utf-8')
        assert all(digest((formal/r['path']).read_bytes()) == r['sha256'] for r in files)
        assert not any(p.is_dir() and not any(p.iterdir()) for p in formal.rglob('*'))
    report = dict(style='Draw03', integrated=args.integrate, completeStyle=False, aliases=aliases,
                  fileCount=len(files), totalBytes=sum(f['bytes'] for f in files), files=files,
                  newFileCount=len(combined)-len(existing), localSurfaceCount=len(dependencies),
                  localTextureCount=sum(p.suffix=='.Texture' for p in local.values()), dependencies=dependencies,
                  graphComplete=True, duplicatePayloadCids=0, newBakedPixels=0,
                  originalAtlasBoundaryEquivalent=False, boundaryDisposition='Accepted existing-atlas pattern differences; human observation pending',
                  remaining=['Grating bounded fractional representation', 'LaneWear and BetweenLaneWear unit delivery', 'Unresolved non-source structural mappings', 'Game preview/export/cold-load acceptance'],
                  localization='No UI strings changed; technical identities remain unlocalized')
    (STAGE/'integration.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k:report[k] for k in ('integrated','aliases','fileCount','totalBytes','newFileCount','graphComplete','newBakedPixels')},indent=2))


if __name__ == '__main__':
    main()
