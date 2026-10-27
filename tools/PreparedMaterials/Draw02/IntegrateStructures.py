"""Integrate the exact validated Draw02 production graph without baking or overwrites."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import sys
from AnalyzeUnimodular import native_binding_sizes


def sha(data):
    return hashlib.sha256(data).hexdigest()


def binary_string(text):
    data = text.encode('utf-8')
    n = len(data)
    prefix = bytearray()
    while n >= 128:
        prefix.append((n & 127) | 128)
        n >>= 7
    prefix.append(n)
    return bytes(prefix) + data


def main(repo):
    inventory_path = repo / 'artifacts/uv-density/structure-tessellated-sources.json'
    raw_inventory = inventory_path.read_bytes()
    inventory = json.loads(raw_inventory)
    units = [u for u in inventory['units'] if u['style'] == 'Draw02']
    source = repo / 'artifacts/prepared-current-structures-canonical/Draw02'
    destination = repo / 'assets/Materials/Draw02'
    output = repo / 'artifacts/prepared-materials/Draw02/structure-integration.json'
    planned = set()
    references = set()
    keys = set()
    for unit in units:
        numbers = sum((unit[n] for n in ('a', 'b', 'c')), []) if unit['triangle'] else unit['bounds']
        encoded = binary_string('BridgeBuilder/StructuralRepeat/1') + binary_string(unit['sourceDigest'])
        encoded += bytes([unit['triangle']]) + struct.pack('<' + 'f' * len(numbers), *numbers)
        assert sha(encoded) == unit['key'], 'Production key mismatch: ' + unit['key']
        assert unit['key'] not in keys, 'Duplicate production key'
        keys.add(unit['key'])
        surface = Path('Structure') / unit['key'] / 'Structure.Surface'
        planned.update((surface, Path(str(surface) + '.cid')))
        for binding in native_binding_sizes(source, unit['key']):
            cid = binding['cid']
            references.add(cid)
            texture = Path('Shared') / (cid + '.Texture')
            assert (source / (str(texture) + '.cid')).read_text().strip() == cid
            planned.update((texture, Path(str(texture) + '.cid')))
    assert {p.parent.name for p in (source / 'Structure').glob('*/Structure.Surface')} == keys, 'Candidate inventory differs'
    existing = {}
    for sidecar in destination.rglob('*.cid'):
        payload = Path(str(sidecar)[:-4])
        assert payload.is_file(), 'Orphan CID: ' + str(sidecar)
        cid = sidecar.read_text().strip()
        digest = sha(payload.read_bytes())
        assert cid not in existing or existing[cid] == digest, 'Existing CID collision: ' + cid
        existing[cid] = digest
    incoming = {}
    for relative in planned:
        path = source / relative
        assert path.is_file(), 'Missing dependency: ' + str(path)
        if path.suffix != '.cid':
            cid = Path(str(path) + '.cid').read_text().strip()
            assert re.fullmatch('[0-9a-f]{32}', cid), 'Invalid CID'
            digest = sha(path.read_bytes())
            assert cid not in existing or existing[cid] == digest, 'CID conflicts with packaged resource: ' + cid
            assert cid not in incoming or incoming[cid] == digest, 'Candidate CID collision: ' + cid
            incoming[cid] = digest
        target = destination / relative
        assert not target.exists() or path.read_bytes() == target.read_bytes(), 'Path conflict: ' + str(target)
    # All graph, identity and path conflicts are checked before the first copy.
    copied = retained = 0
    files = []
    for relative in sorted(planned):
        source_path, target = source / relative, destination / relative
        if not target.exists():
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source_path, target)
            copied += 1
        else:
            retained += 1
        source_data, target_data = source_path.read_bytes(), target.read_bytes()
        assert source_data == target_data, 'Copy verification failed'
        files.append(dict(relative=relative.as_posix(), bytes=len(target_data), sha256=sha(target_data)))
    assert {p.parent.name for p in (destination / 'Structure').glob('*/Structure.Surface')} == keys, 'Packaged inventory differs'
    result = dict(status='PASS', inventorySha256=sha(raw_inventory), productionKeys=len(keys),
                  uniqueTextureReferences=len(references), selectedFiles=len(files), copiedFiles=copied,
                  retainedIdenticalFiles=retained, selectedBytes=sum(x['bytes'] for x in files),
                  cidConflicts=0, changedExistingFiles=0, bakedTextures=0, files=files)
    output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k != 'files'}, indent=2))


if __name__ == '__main__':
    main(Path(sys.argv[1]).resolve())
