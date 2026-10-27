"""Finite, lossless integration of the exact current Lift03 structural inventory."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import struct
import zipfile
import IntegrateRepeatControl as graph_tools


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'artifacts/prepared-current-structures-canonical/Lift03'
TARGET = ROOT / 'assets/Materials/Lift03'
INVENTORY = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
REPORT = ROOT / 'artifacts/prepared-materials/Lift03/structure-integration.json'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def need(value, reason):
    if not value:
        raise ValueError(reason)


def net_string(value):
    raw = value.encode('utf-8')
    size, prefix = len(raw), bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    prefix.append(size)
    return bytes(prefix) + raw


def key(unit, recipe):
    values = unit['a'] + unit['b'] + unit['c'] if unit['triangle'] else unit['bounds']
    return sha(net_string(recipe) + net_string(unit['sourceDigest']) + bytes([unit['triangle']])
               + struct.pack('<' + 'f' * len(values), *values))


def main():
    inventory = json.loads(INVENTORY.read_text())
    recipe = inventory['recipe']
    production = ROOT / 'src/BridgeBuilder/Bridges/Common/Movable/MovableStructureKey.cs'
    code = production.read_text()
    need(recipe == re.search(r'Recipe = "([^"]+)"', code).group(1), 'Production recipe changed')
    units = [u for u in inventory['units'] if u['style'] == 'Lift03']
    need(len(units) == 5954, 'Current inventory count requires review')
    keys = set()
    for unit in units:
        need(key(unit, recipe) == unit['key'], 'Production sampler key differs: ' + unit['key'])
        need(unit['key'] not in keys, 'Duplicate inventory unit')
        keys.add(unit['key'])
    need({p.name for p in (SOURCE / 'Structure').iterdir() if p.is_dir()} == keys, 'Candidate coverage differs')
    source_files = {}
    by_cid = {}
    for path in SOURCE.rglob('*'):
        if not path.is_file():
            continue
        rel = path.relative_to(SOURCE).as_posix()
        need(path.suffix in ('.Surface', '.Texture', '.cid'), 'Unexpected candidate file: ' + rel)
        data = path.read_bytes()
        source_files[rel] = {'sha256': sha(data), 'bytes': len(data)}
        if path.suffix == '.cid':
            need(Path(str(path)[:-4]).is_file(), 'Orphan CID')
            continue
        cid = Path(str(path) + '.cid').read_text().strip()
        need(re.fullmatch('[0-9a-f]{32}', cid), 'Invalid CID')
        need(cid == sha((recipe + '/' + sha(data)).encode())[:32], 'Prepared resource identity differs')
        if cid in by_cid:
            need(by_cid[cid]['sha256'] == sha(data), 'Conflicting candidate CID: ' + cid)
        by_cid[cid] = {'sha256': sha(data), 'extension': path.suffix}
        if path.suffix == '.Surface':
            need(path.name == 'Structure.Surface' and path.parent.name in keys, 'Unexpected structure descriptor')
        else:
            need(rel == f'Shared/{cid}.Texture', 'Texture not in canonical Shared layout')
            need(len(data) >= 18 and struct.unpack('<H', data[:2])[0] == 5, 'Invalid texture header')
            width, height, depth = struct.unpack('<HHH', data[2:8])
            need(width > 0 and height > 0 and depth == 1 and data[10] == 2, 'Invalid texture extent')
            need(data[8] == max(width, height).bit_length(), 'Incomplete native mip chain')
            stored = struct.unpack('<i', data[14:18])[0]
            need(stored >= -1 and (stored == -1 or stored == len(data) - 18), 'Stored texture length differs')
    refs = {}
    used = set()
    for unit in units:
        path = SOURCE / 'Structure' / unit['key'] / 'Structure.Surface'
        need(set(p.name for p in path.parent.iterdir()) == {'Structure.Surface', 'Structure.Surface.cid'}, 'Unexpected files inside unit')
        channels = graph_tools.bindings(path)
        need(set(channels) == {'_BaseColorMap', '_NormalMap', '_MaskMap'}, 'Unexpected structural channels')
        for cid in channels.values():
            need(cid in by_cid and by_cid[cid]['extension'] == '.Texture', 'Missing structural texture: ' + cid)
            used.add(cid)
        refs[unit['key']] = channels
    need(used == {cid for cid, item in by_cid.items() if item['extension'] == '.Texture'}, 'Unreferenced candidate texture')
    before = graph_tools.snapshot()
    # Preflight every existing identity and path before the first write.
    for path in TARGET.rglob('*.cid'):
        payload = Path(str(path)[:-4])
        cid = path.read_text().strip()
        if cid in by_cid:
            need(sha(payload.read_bytes()) == by_cid[cid]['sha256'], 'Existing CID collision: ' + str(payload))
    existing_structure = TARGET / 'Structure'
    if existing_structure.exists():
        need({p.name for p in existing_structure.iterdir() if p.is_dir()} <= keys, 'Existing obsolete structure units need explicit review')
    added = []
    retained = []
    for rel, record in source_files.items():
        destination = TARGET / rel
        if destination.exists():
            need(sha(destination.read_bytes()) == record['sha256'], 'Existing path conflict: ' + rel)
            retained.append(rel)
        else:
            added.append(rel)
    for rel in added:
        destination = TARGET / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(SOURCE / rel, destination)
        need(sha(destination.read_bytes()) == source_files[rel]['sha256'], 'Copied bytes differ: ' + rel)
    # One complete final graph check, including preserved built-in VT dependencies.
    for family in ('Game', 'BridgesAndPorts'):
        for archive in (graph_tools.GAME / family).glob('*.cok'):
            if not archive.name.startswith(('Blob', 'VT')):
                continue
            with zipfile.ZipFile(archive) as z:
                names = set(z.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        graph_tools.BUILTINS[z.read(name).decode().strip()] = str(archive) + '/' + name[:-4]
    final_refs, missing = graph_tools.graph()
    need(not missing, 'Unresolved active sampled dependencies: ' + str(missing))
    after = graph_tools.snapshot()
    need(all(after.get(rel) == value for rel, value in before.items()), 'Existing files changed')
    need(set(after) - set(before) == set(added), 'Unexpected additions')
    need({p.name for p in (TARGET / 'Structure').iterdir()} == keys, 'Integrated inventory coverage differs')
    need(all(any(p.iterdir()) for p in TARGET.rglob('*') if p.is_dir()), 'Empty directory')
    result = {'status': 'PASS', 'style': 'Lift03', 'inventoryPath': str(INVENTORY),
              'inventorySha256': sha(INVENTORY.read_bytes()), 'productionKeySourceSha256': sha(production.read_bytes()),
              'recipe': recipe, 'units': len(units), 'uniqueStructuralTextures': len(used),
              'candidateFiles': len(source_files), 'candidateFileBytes': sum(x['bytes'] for x in source_files.values()),
              'addedFiles': len(added), 'retainedIdenticalFiles': len(retained), 'overwrittenFiles': 0,
              'completeStyleFiles': len(after), 'completeStyleBytes': sum(p.stat().st_size for p in TARGET.rglob('*') if p.is_file()),
              'completeStyleSurfaces': len(final_refs), 'missingActiveDependencies': missing,
              'sameCidConflicts': 0, 'existingFilesUnchanged': True, 'bakedTextures': 0,
              'candidateFilesManifest': source_files, 'structureReferences': refs,
              'localization': 'No runtime or user-visible text changed.',
              'remaining': ['Coordinator build/package verification', 'Game visual and cold-load human acceptance']}
    REPORT.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k:v for k,v in result.items() if k not in ('candidateFilesManifest','structureReferences')}, indent=2))


if __name__ == '__main__':
    main()
