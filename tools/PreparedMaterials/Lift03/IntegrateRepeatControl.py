"""Integrate only the independently verified Lift03 descriptor candidates."""
from pathlib import Path
import hashlib
import io
import json
import shutil
import struct
import zipfile


ROOT = Path(__file__).resolve().parents[3]
STYLE = ROOT / 'assets/Materials/Lift03'
CANDIDATE = ROOT / 'artifacts/prepared-materials/Lift03/repeat-control-validated'
BACKUP = ROOT / 'artifacts/backups/Lift03/repeat-zero-control-integration'
REPORT = ROOT / 'artifacts/prepared-materials/Lift03/repeat-control-integration.json'
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')
BUILTINS = {}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def require(value, message):
    if not value:
        raise ValueError(message)


def bindings(path):
    data = path.read_bytes()
    r = io.BytesIO(data)

    def take(n):
        b = r.read(n)
        require(len(b) == n, f'Truncated Surface: {path}')
        return b

    def present():
        b = take(1)[0]
        require(b in (0, 255), f'Invalid native marker: {path}')
        return b == 255

    def count():
        return struct.unpack('<i', take(4))[0] if present() else 0

    def string():
        n = shift = 0
        while True:
            b = take(1)[0]
            n |= (b & 127) << shift
            if b < 128:
                return take(n).decode('utf-8')
            shift += 7
            require(shift <= 28, 'Invalid string length')

    require(struct.unpack('<H', take(2))[0] == 1, 'Surface version differs')
    take(4)
    vt = take(1)
    require(vt in (b'\0', b'\1'), 'Invalid VT flag')
    result = {}
    if vt == b'\1':
        header = take(276)
        for stack in range(2):
            for layer in range(3 if stack else 4):
                raw = header[76 + stack*136 + layer*16:92 + stack*136 + layer*16]
                if any(raw):
                    result[f'VTHeader/{stack}/{layer}'] = ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)
        if present():
            result['VTSurface'] = ''.join(f'{b & 15:x}{b >> 4:x}' for b in take(16))
    for size in (4, 4, 16, 16):
        for _ in range(count()):
            string()
            take(size)
    for _ in range(count()):
        name = string()
        require(present(), f'Null texture binding: {path}')
        result[name] = ''.join(f'{b & 15:x}{b >> 4:x}' for b in take(16))
    for _ in range(count()):
        string()
    require(r.tell() == len(data), f'Unconsumed Surface: {path}')
    return result


def snapshot():
    return {str(p.relative_to(STYLE)).replace('\\', '/'): digest(p.read_bytes())
            for p in STYLE.rglob('*') if p.is_file()}


def graph():
    textures = {}
    for p in STYLE.rglob('*.Texture'):
        cid = Path(str(p) + '.cid').read_text().strip()
        if cid in textures:
            require(p.read_bytes() == textures[cid].read_bytes(), 'Conflicting texture CID')
        textures[cid] = p
    refs = {str(p.relative_to(STYLE)).replace('\\', '/'): bindings(p)
            for p in STYLE.rglob('*.Surface')}
    missing = [(p, name, cid) for p, values in refs.items()
               for name, cid in values.items() if cid not in textures and cid not in BUILTINS
               and not inactive_vt_binding(p, name)]
    return refs, missing


def inactive_vt_binding(relative, name):
    # VT stack channels are sampled through their VT header/surface. Preserve
    # ordinary authored bindings byte-for-byte but do not mistake them for the
    # active VT lookup. All bindings still participate in deletion checks.
    return (STYLE / relative).read_bytes()[6] == 1 and name in {
        '_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask', '_EmissiveColorMap'}


def main():
    require(not BACKUP.exists() and not REPORT.exists(), 'Integration already has retained evidence')
    audit = json.loads((CANDIDATE / 'audit.json').read_text())
    require(audit['status'] == 'PASS' and len(audit['changes']) == 3, 'Wrong audit')
    require(audit['encodedTextures'] == audit['producedTextures'] == 0, 'Unexpected audit bake')
    require(all(not s['authoredControl'] for s in audit['sources']), 'Authored source control')
    require(all(all(n == 0 for n in c['mismatches']) for c in audit['constants']), 'Constant verification failed')
    # Existing native dependencies are permitted; establish them from real game archives.
    for family in ('Game', 'BridgesAndPorts'):
        for archive in (GAME / family).glob('*.cok'):
            if not archive.name.startswith(('Blob', 'VT')):
                continue
            with zipfile.ZipFile(archive) as z:
                names = set(z.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        BUILTINS[z.read(name).decode().strip()] = str(archive) + '/' + name[:-4]
    before = snapshot()
    before_refs, before_missing = graph()
    require(not before_missing, f'Incomplete Lift03 dependencies: {before_missing}')
    inactive = [(p, name, cid) for p, values in before_refs.items() for name, cid in values.items()
                if cid not in BUILTINS and inactive_vt_binding(p, name)]
    replacement = audit['constants'][0]
    cid = replacement['cid']
    road = STYLE / 'Road/ZeroControl.Texture'
    require(digest(road.read_bytes()) == replacement['sha256'], 'Road zero changed')
    require(Path(str(road) + '.cid').read_text().strip() == cid, 'Road zero identity changed')
    updates = {}
    old_controls = set()
    for change in audit['changes']:
        rel = f"Repeat/{change['key']}/Repeat.Surface"
        target, candidate = STYLE / rel, CANDIDATE / rel
        old, new = target.read_bytes(), candidate.read_bytes()
        require(digest(old) == change['oldSha256'], 'Installed original changed')
        require(Path(str(target) + '.cid').read_text().strip() == change['oldSurfaceCid'], 'Old CID changed')
        require(digest(new) == change['newSha256'], 'Candidate changed')
        new_cid = digest((audit['recipe'] + '/' + digest(new)).encode())[:32]
        require(new_cid == change['newSurfaceCid'] == Path(str(candidate) + '.cid').read_text().strip(), 'Candidate identity differs')
        offset = change['offset']
        require(len(old) == len(new) and old[:offset] == new[:offset] and old[offset+16:] == new[offset+16:], 'Non-control bytes changed')
        old_bind, new_bind = bindings(target), bindings(candidate)
        old_controls.add(old_bind['_ControlMask'])
        require(new_bind['_ControlMask'] == cid, 'Wrong candidate control')
        require({k:v for k,v in old_bind.items() if k != '_ControlMask'} == {k:v for k,v in new_bind.items() if k != '_ControlMask'}, 'Other channels changed')
        updates[rel] = candidate
        updates[rel + '.cid'] = Path(str(candidate) + '.cid')
    obsolete = []
    for old_cid in sorted(old_controls):
        proof = next(c for c in audit['constants'] if c['cid'] == old_cid)
        old = STYLE / f'Shared/{old_cid}.Texture'
        require(digest(old.read_bytes()) == proof['sha256'], 'Old control changed')
        require(Path(str(old) + '.cid').read_text().strip() == old_cid, 'Old control CID changed')
        for rel, refs in before_refs.items():
            if old_cid in refs.values():
                require(rel in updates, f'Old control has another consumer: {rel}')
        obsolete.extend([old, Path(str(old) + '.cid')])
    added = {f'Shared/{cid}.Texture': road,
             f'Shared/{cid}.Texture.cid': Path(str(road) + '.cid')}
    for rel, source in added.items():
        require(not (STYLE / rel).exists(), f'Shared zero already exists: {rel}')
    # Backups contain only byte-identical original files, with original relative paths.
    originals = [STYLE / rel for rel in updates] + obsolete
    for source in originals:
        target = BACKUP / source.relative_to(STYLE)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        require(source.read_bytes() == target.read_bytes(), 'Backup mismatch')
    for rel, source in added.items():
        shutil.copyfile(source, STYLE / rel)
    for rel, source in updates.items():
        shutil.copyfile(source, STYLE / rel)
    installed_refs, missing = graph()
    require(not missing, f'Incomplete dependency graph: {missing}')
    require(all(not old_controls.intersection(refs.values()) for refs in installed_refs.values()), 'Old controls still referenced')
    for target in obsolete:
        resolved = target.resolve()
        require(resolved.parent == (STYLE / 'Shared').resolve(), 'Deletion outside Lift03 Shared')
        target.unlink()
    final_refs, missing = graph()
    require(not missing, 'Dependency graph incomplete after removal')
    after = snapshot()
    expected_changes = set(updates) | set(added) | {str(p.relative_to(STYLE)).replace('\\', '/') for p in obsolete}
    changed = {p for p in set(before) | set(after) if before.get(p) != after.get(p)}
    require(changed == expected_changes, 'Unexpected changed files')
    result = {'status': 'PASS', 'style': 'Lift03', 'auditSha256': digest((CANDIDATE/'audit.json').read_bytes()),
              'backup': str(BACKUP), 'backupFiles': len(originals),
              'surfaceCount': len(final_refs), 'missingDependencies': missing,
              'preservedInactiveVtNativeBindings': inactive,
              'verifiedNativeDependencies': {cid: BUILTINS[cid] for values in final_refs.values() for cid in values.values() if cid in BUILTINS},
              'sourceTextureCid': cid, 'sourceTextureSha256': replacement['sha256'],
              'updatedFiles': sorted(updates), 'addedFiles': sorted(added),
              'removedFiles': sorted(expected_changes - set(updates) - set(added)),
              'before': before, 'after': after, 'surfaceBindingsAfter': final_refs,
              'nonControlBytesUnchanged': True, 'allOtherFilesUnchanged': True,
              'bakedTextures': 0, 'localization': 'No runtime or UI text changed.'}
    REPORT.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k:v for k,v in result.items() if k not in ('before','after','surfaceBindingsAfter')}, indent=2))


if __name__ == '__main__':
    main()
