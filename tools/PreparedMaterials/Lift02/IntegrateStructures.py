"""Copy the validated Lift02 production graph, rejecting any identity conflict."""
import hashlib
import io
import json
from pathlib import Path
import re
import shutil
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'artifacts/prepared-current-structures-canonical/Lift02'
DEST = ROOT / 'assets/Materials/Lift02'
OUT = ROOT / 'artifacts/prepared-materials/Lift02/structure-integration.json'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    assert len(data) == 16
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


def read_string(r):
    n = shift = 0
    while True:
        v = r.read(1)[0]
        n |= (v & 127) << shift
        if v < 128:
            break
        shift += 7
    return r.read(n).decode('utf-8')


def count(r):
    p = r.read(1)[0]
    assert p in (0, 255)
    return struct.unpack('<i', r.read(4))[0] if p else 0


def refs(data):
    r = io.BytesIO(data)
    version, template, vt = struct.unpack('<Hi?', r.read(7))
    assert version == 1
    result = []
    if vt:
        header = r.read(276)
        stacks = struct.unpack_from('<i', header)[0]
        assert 0 <= stacks <= 2
        for stack in range(stacks):
            for layer in range(4):
                value = cid(header[76 + stack * 136 + layer * 16:92 + stack * 136 + layer * 16])
                if value != '0' * 32:
                    result.append(value)
        present = r.read(1)[0]
        assert present in (0, 255)
        if present:
            result.append(cid(r.read(16)))
    for size in (4, 4, 16, 16):
        for _ in range(count(r)):
            read_string(r)
            assert len(r.read(size)) == size
    for _ in range(count(r)):
        name = read_string(r)
        present = r.read(1)[0]
        assert present in (0, 255)
        if present:
            value = cid(r.read(16))
            if vt and name in ('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask', '_EmissiveColorMap'):
                assert template in (0, 1, 35)
            else:
                result.append(value)
    for _ in range(count(r)):
        read_string(r)
    assert r.tell() == len(data), 'Unconsumed Surface bytes'
    return result


def string_bytes(s):
    data = s.encode('utf-8')
    assert len(data) < 128
    return bytes([len(data)]) + data


def main():
    inventory_path = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
    inventory_bytes = inventory_path.read_bytes()
    units = [u for u in json.loads(inventory_bytes)['units'] if u['style'] == 'Lift02']
    keys, planned, textures = set(), set(), set()
    for u in units:
        values = sum((u[n] for n in ('a', 'b', 'c')), []) if u['triangle'] else u['bounds']
        packed = string_bytes('BridgeBuilder/StructuralRepeat/1') + string_bytes(u['sourceDigest'])
        packed += bytes([u['triangle']]) + struct.pack('<' + 'f' * len(values), *values)
        assert sha(packed) == u['key'], 'Production key mismatch'
        assert u['key'] not in keys, 'Duplicate production key'
        keys.add(u['key'])
        relative = Path('Structure') / u['key'] / 'Structure.Surface'
        planned.update((relative, Path(str(relative) + '.cid')))
        for value in refs((SOURCE / relative).read_bytes()):
            texture = Path('Shared') / (value + '.Texture')
            assert (SOURCE / (str(texture) + '.cid')).read_text().strip() == value
            planned.update((texture, Path(str(texture) + '.cid')))
            textures.add(value)
    assert {p.parent.name for p in (SOURCE / 'Structure').glob('*/Structure.Surface')} == keys
    existing_hashes = {p.relative_to(DEST).as_posix(): sha(p.read_bytes()) for p in DEST.rglob('*') if p.is_file()}
    identities = {}
    for sidecar in DEST.rglob('*.cid'):
        payload = Path(str(sidecar)[:-4])
        identity = sidecar.read_text().strip()
        assert re.fullmatch('[0-9a-f]{32}', identity)
        digest = sha(payload.read_bytes())
        assert identity not in identities or identities[identity] == digest, 'Existing CID conflict'
        identities[identity] = digest
    for relative in sorted(planned):
        data = (SOURCE / relative).read_bytes()
        target = DEST / relative
        assert not target.exists() or target.read_bytes() == data, 'Destination path conflict: ' + str(relative)
        if relative.suffix != '.cid':
            identity = (SOURCE / (str(relative) + '.cid')).read_text().strip()
            assert re.fullmatch('[0-9a-f]{32}', identity)
            digest = sha(data)
            assert identity not in identities or identities[identity] == digest, 'Incoming CID conflict: ' + identity
            identities[identity] = digest
    # Validate all existing and incoming Surface dependencies before copying anything.
    surfaces = {p.relative_to(DEST): p for p in DEST.rglob('*.Surface')}
    surfaces.update({p: SOURCE / p for p in planned if p.suffix == '.Surface'})
    graph = []
    missing = set()
    for relative, path in sorted(surfaces.items()):
        values = refs(path.read_bytes())
        missing.update(v for v in values if v not in identities)
        graph.append(dict(path=relative.as_posix(), references=values))
    built_in = {}
    content = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')
    for family in ('Game', 'BridgesAndPorts'):
        for archive in (content / family).glob('*.cok'):
            if not missing or not archive.name.startswith(('Blob', 'VT')):
                continue
            with zipfile.ZipFile(archive) as z:
                names = set(z.namelist())
                for name in names:
                    if not name.endswith('.cid'):
                        continue
                    value = z.read(name).decode().strip()
                    if value in missing:
                        assert name[:-4] in names
                        built_in[value] = dict(archive=str(archive), entry=name[:-4])
                        missing.remove(value)
    assert not missing, 'Unresolved graph: ' + ','.join(sorted(missing))
    copied = retained = 0
    files = []
    for relative in sorted(planned):
        source_path, target = SOURCE / relative, DEST / relative
        if target.exists():
            retained += 1
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source_path, target)
            copied += 1
        data = target.read_bytes()
        assert data == source_path.read_bytes(), 'Copy differs'
        files.append(dict(path=relative.as_posix(), bytes=len(data), sha256=sha(data)))
    assert {p.parent.name for p in (DEST / 'Structure').glob('*/Structure.Surface')} == keys
    for relative, digest in existing_hashes.items():
        assert sha((DEST / relative).read_bytes()) == digest, 'Existing resource changed'
    assert not [p for p in DEST.rglob('*') if p.is_dir() and not any(p.iterdir())]
    report = dict(status='PASS', inventorySha256=sha(inventory_bytes), productionKeys=len(keys),
                  uniqueStructureTextures=len(textures), copiedFiles=copied, retainedIdenticalFiles=retained,
                  selectedFiles=len(files), selectedBytes=sum(f['bytes'] for f in files),
                  allStyleSurfaces=len(graph), unresolvedReferences=[], cidConflicts=[],
                  changedExistingFiles=0, bakedTextures=0, exactGaps=[],
                  localizationReview='No UI text or runtime localization changes.',
                  acceptance='Offline integration only; in-game acceptance remains separate.',
                  files=files, graph=graph, builtInReferences=built_in)
    OUT.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in report.items() if k not in ('files', 'graph', 'builtInReferences')}, indent=2))


if __name__ == '__main__':
    main()
