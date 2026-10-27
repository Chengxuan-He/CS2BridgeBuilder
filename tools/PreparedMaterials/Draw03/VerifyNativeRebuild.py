"""Verify Draw03's fresh native descriptor output and built-in graph boundary."""
import argparse
import json
from pathlib import Path
import zipfile

from PrepareNativeRebuild import digest, surface


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('output', 'report', 'game_data', 'library', 'verification'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    library = json.loads(args.library.read_text(encoding='utf-8-sig'))
    report = json.loads(args.report.read_text(encoding='utf-8-sig'))
    expected = {item['path']: item for item in report['files']}
    actual = {p.relative_to(args.output).as_posix(): p for p in args.output.rglob('*') if p.is_file()}
    if set(actual) != set(expected):
        raise ValueError('Output file set differs from producer report')
    for relative, path in actual.items():
        if len(path.read_bytes()) != expected[relative]['bytes'] or digest(path.read_bytes()) != expected[relative]['sha256']:
            raise ValueError('Output content differs: ' + relative)
    local = {}
    for relative, path in actual.items():
        if not relative.endswith('.cid'):
            if relative + '.cid' not in actual:
                raise ValueError('Missing CID sidecar: ' + relative)
            continue
        identity = path.read_text(encoding='utf-8-sig').strip()
        if identity in local:
            raise ValueError('Duplicate local payload identity: ' + identity)
        local[identity] = actual[relative[:-4]]
    builtins = set()
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game_data / 'Content' / family).glob('*.cok'):
            if not path.name.startswith(('Blob', 'VT')):
                continue
            with zipfile.ZipFile(path) as archive:
                names = set(archive.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        builtins.add(archive.read(name).decode('utf-8-sig').strip())
    references, reached, originals = {}, set(), set()
    for identity, path in local.items():
        if path.suffix != '.Surface':
            continue
        descriptor = surface(path.read_bytes(), library)
        references[identity] = descriptor['dependencies']
        reached.add(identity)
        for dependency in descriptor['dependencies']:
            if dependency in local:
                reached.add(dependency)
            elif dependency in builtins:
                originals.add(dependency)
            else:
                raise ValueError('Unresolved dependency: ' + dependency)
    if reached != set(local):
        raise ValueError('Unreferenced local resource')
    for root in report['roots']:
        if root['path']:
            if root['cid'] not in local or local[root['cid']].relative_to(args.output).as_posix() != root['path']:
                raise ValueError('Converted root mapping differs')
        elif root['cid'] != root['sourceCid'] or root['cid'] not in builtins:
            raise ValueError('Original native root not retained')
    result = dict(style='Draw03', passed=True, fileCount=len(actual), bytes=sum(p.stat().st_size for p in actual.values()),
        surfaces=len(references), localTexturePayloads=sum(p.suffix == '.Texture' for p in local.values()),
        originalBuiltinReferences=sorted(originals), dependencies=references,
        directBuiltinRoots=sum(r['operation'] == 'builtin-reference' for r in report['roots']),
        convertedSourceRoots=sum(r['operation'] == 'native-descriptor-conversion' for r in report['roots']),
        orphanPayloads=0, duplicatePayloadCids=0, emptyDirectories=sum(p.is_dir() and not any(p.iterdir()) for p in args.output.rglob('*')),
        graphBoundary='All external references terminate at verified game/DLC builtin assets; no other bridge/mod references',
        nativeRenderAcceptance=False)
    args.verification.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k not in ('dependencies', 'originalBuiltinReferences')}, indent=2))


if __name__ == '__main__':
    main()
