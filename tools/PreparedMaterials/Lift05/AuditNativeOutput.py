"""Verify Lift05 native-descriptor output without modifying any material payload."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile
from MapSharedSampling import surface


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('repo', type=Path)
    p.add_argument('game', type=Path)
    p.add_argument('root', type=Path)
    a = p.parse_args()
    root = a.root.resolve()
    style = root / 'output/Lift05'
    producer = json.loads((root / 'rebuild.json').read_text())
    templates = json.loads((a.repo / 'tools/PrepareLanes/NativeTemplates.json').read_text())
    stacks = {t['hash']: {s for group in t['stacks'] for s in group} for t in templates}
    index = {}
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
                        index.setdefault(z.read(name).decode().strip(), (z, name[:-4]))
        files = {p.relative_to(style).as_posix(): p for p in style.rglob('*') if p.is_file()}
        expected = {f['path']: f for f in producer['files']}
        assert files.keys() == expected.keys(), 'Unexpected output file set'
        for key, path in files.items():
            assert path.stat().st_size == expected[key]['bytes']
            assert digest(path.read_bytes()) == expected[key]['sha256']
        local = {}
        for name, path in files.items():
            if name.endswith('.cid'):
                cid = path.read_text().strip()
                payload = files[name[:-4]]
                assert cid not in local, 'Duplicate local payload identity'
                local[cid] = payload
        references = []
        for name, path in files.items():
            if not name.endswith('.Surface'):
                continue
            raw = path.read_bytes()
            if name == 'Paint/SkinnedPaint.Surface':
                assert digest(raw)[:32] == files[name + '.cid'].read_text().strip()
            value = surface(raw)
            refs = {cid for stack in value['stacks'] for cid in stack['vtTextureCids']}
            if value['vtSurfaceCid']:
                refs.add(value['vtSurfaceCid'])
            for key, cid in value['textures'].items():
                if not value['isVT'] or key not in stacks[value['template']]:
                    refs.add(cid)
            refs.discard(None)
            refs.discard('0' * 32)
            assert all(cid in local or cid in index for cid in refs), (name, refs)
            references.append(dict(path=name, template=value['template'], isVT=value['isVT'],
                                   builtin=sorted(refs - local.keys()), local=sorted(refs & local.keys())))
        all_builtin = {cid for row in references for cid in row['builtin']}
        all_builtin.update(cid for row in producer['roots'] for cid in row['builtinDependencies'])
        payloads = []
        for cid in sorted(all_builtin):
            z, name = index[cid]
            raw = z.read(name)
            payloads.append(dict(cid=cid, archive=z.filename, entry=name, bytes=len(raw), sha256=digest(raw)))
        assert not any(p.is_dir() and not any(p.iterdir()) for p in style.rglob('*'))
        log = (root / 'native-check.log').read_text(errors='replace')
        native_pass = next(line.strip() for line in log.splitlines() if 'PASS native source descriptors:' in line)
        report = dict(style='Lift05', fileCount=len(files), bytes=sum(p.stat().st_size for p in files.values()),
            surfaceCount=len(references), textureCount=sum(p.suffix == '.Texture' for p in files.values()),
            nativePass=native_pass, descriptors=references, builtinPayloads=payloads,
            unresolvedSurfaceReferences=0, duplicateLocalPayloads=0, emptyDirectories=0,
            newTextureReencoding=producer['nativeMipBytesReencoded'],
            limit='Native binary parsing and active Surface dependency resolution; not renderer/mip-filter acceptance.',
            tools={str(p): digest((a.repo / p).read_bytes()) for p in (
                Path('tools/PrepareNativeMaterials/Program.cs'),
                Path('tools/GpuBake/NativeMaterialDescriptorCheck.Editor.cs'),
                Path('tools/PreparedMaterials/Lift05/PrepareNativeInputs.py'))})
        (root / 'audit.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({k: report[k] for k in ('fileCount', 'bytes', 'surfaceCount', 'textureCount', 'unresolvedSurfaceReferences')}))
    finally:
        for z in archives:
            z.close()


if __name__ == '__main__':
    main()
