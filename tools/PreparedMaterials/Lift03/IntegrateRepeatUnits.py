"""Bind Lift03's two measured units and native template variants without rebaking pixels."""
import argparse
import hashlib
import json
import shutil
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def cid(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(raw).digest()[:16])


def string(raw):
    raw = raw.encode()
    size, prefix = len(raw), bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    return bytes(prefix) + bytes([size]) + raw


def copy_exact(source, destination):
    if destination.exists():
        assert source.read_bytes() == destination.read_bytes(), f'Conflicting existing payload: {destination}'
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, destination)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish', action='store_true')
    args = parser.parse_args()
    candidate = ROOT / 'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2'
    stage = ROOT / 'artifacts/prepared-materials/Lift03/repeat-integration-v2'
    output = stage / 'output/Lift03'
    formal = ROOT / 'assets/Materials/Lift03'
    bake = json.loads((candidate / 'bake.json').read_text())
    validation = json.loads((candidate / 'validation-summary.json').read_text())
    for record in validation['files']:
        path = candidate / 'output' / record['path']
        assert digest(path.read_bytes()) == record['sha256']
    for source_root in (formal, candidate / 'output/Lift03'):
        for source in source_root.rglob('*'):
            if source.is_file():
                copy_exact(source, output / source.relative_to(source_root))
    main = next(x for x in bake['units'] if x['id'] == 'Lift03.MainRoad')
    path = output / 'RepeatUnits' / main['key'] / 'Repeat.Surface'
    original = path.read_bytes()
    assert struct.unpack_from('<i', original, 2)[0] == 1 and original[6] == 0
    variant = original[:2] + struct.pack('<i', 0) + original[6:]
    assert len(original) == len(variant) and original[:2]+original[6:] == variant[:2]+variant[6:]
    variant_cid = cid(variant)
    variant_key = digest(string('BridgeBuilder/RepeatUnits/2') + string(variant_cid))
    path = output / 'RepeatUnits' / variant_key / 'Repeat.Surface'
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        assert path.read_bytes() == variant
    else:
        path.write_bytes(variant)
        path.with_suffix('.Surface.cid').write_text(variant_cid)
    aliases = []
    for unit in bake['units']:
        for index, key in enumerate(unit['aliases']):
            is_default = unit['id'] == 'Lift03.MainRoad' and index == 1
            aliases.append(dict(samplerKey=key, unitKey=variant_key if is_default else unit['key'], template=0 if is_default else 1))
    files = [dict(path=str(p.relative_to(output)).replace('\\','/'), bytes=p.stat().st_size, sha256=digest(p.read_bytes())) for p in sorted(output.rglob('*')) if p.is_file()]
    identities = {}
    for p in output.rglob('*.cid'):
        identity = p.read_text().strip()
        assert identity not in identities, f'Duplicate physical payload for {identity}'
        identities[identity] = str(p.relative_to(output))
    assert not any(p.is_dir() and not any(p.iterdir()) for p in output.rglob('*'))
    report = dict(style='Lift03', aliases=aliases, fileCount=len(files), totalBytes=sum(f['bytes'] for f in files), files=files,
                  duplicatePayloads=0, newBakedPixels=0, variantChangedBytes=[i for i,(a,b) in enumerate(zip(original,variant)) if a!=b],
                  atlasBoundaryPolicy='Existing-atlas pattern differences accepted; retained for human observation',
                  unhandledStructure='No arbitrary structure key is mapped to these road units; incompatible source chart receivers remain common integration work',
                  localization='No user-visible text changed', published=args.publish)
    stage.mkdir(parents=True, exist_ok=True)
    if args.publish:
        native = stage / 'native-reader.log'
        graph = stage / 'graph-validation.json'
        assert native.exists() and 'PASS native source descriptors:' in native.read_text()
        graph_report = json.loads(graph.read_text())
        assert graph_report['status'] == 'PASS'
        verified = {r['path'].replace('\\','/'): r['sha256'] for r in graph_report['files']}
        assert verified == {r['path']: r['sha256'] for r in files}, 'Native graph evidence is stale'
        for p in output.rglob('*'):
            if p.is_file():
                copy_exact(p, formal / p.relative_to(output))
        lines = ['namespace BridgeBuilder.Bridges;', '', 'internal static partial class MovableRepeatUnits', '{',
                 '    // The template variants share all texture bytes; only native descriptor selection differs.',
                 '    static partial void Lift03(string samplerKey, ref string? unitKey)', '    {', '        switch (samplerKey)', '        {']
        for row in aliases:
            lines += [f'            case "{row["samplerKey"]}":', f'                unitKey = "{row["unitKey"]}";', '                break;']
        lines += ['        }','    }','}', '']
        code = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift03/Lift03RepeatUnits.cs'
        assert not code.exists() or code.read_text() == '\n'.join(lines), 'Existing partial differs; review before replacing'
        code.write_text('\n'.join(lines))
    (stage / 'integration.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ('fileCount','totalBytes','aliases','newBakedPixels','published')},indent=2))


if __name__ == '__main__':
    main()
