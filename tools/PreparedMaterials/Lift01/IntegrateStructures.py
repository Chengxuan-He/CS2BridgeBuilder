"""Integrate validated Lift01 structure payloads without baking or rewriting assets."""
import hashlib
import io
import json
from pathlib import Path
import shutil
import struct


def digest(data):
    return hashlib.sha256(data).hexdigest()


def string_bytes(value):
    data = value.encode()
    size = len(data)
    prefix = bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    return bytes(prefix + bytes([size])) + data


def read_text(reader):
    size = shift = 0
    while True:
        value = reader.read(1)[0]
        size |= (value & 127) << shift
        if value < 128:
            return reader.read(size).decode()
        shift += 7


def count(reader):
    marker = reader.read(1)[0]
    if marker == 0:
        return 0
    assert marker == 255
    return struct.unpack('<i', reader.read(4))[0]


def surface_bindings(data):
    reader = io.BytesIO(data)
    version, template, vt = struct.unpack('<Hi?', reader.read(7))
    assert version == 1 and not vt
    for size in (4, 4, 16, 16):
        for _ in range(count(reader)):
            read_text(reader)
            assert len(reader.read(size)) == size
    result = {}
    for _ in range(count(reader)):
        name = read_text(reader)
        assert reader.read(1) == b'\xff'
        raw = reader.read(16)
        assert len(raw) == 16
        result[name] = ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)
    for _ in range(count(reader)):
        read_text(reader)
    assert reader.tell() == len(data)
    return result


def main():
    repo = Path(__file__).resolve().parents[3]
    inventory_path = repo/'artifacts/uv-density/structure-tessellated-sources.json'
    inventory_bytes = inventory_path.read_bytes()
    inventory = json.loads(inventory_bytes)
    units = [u for u in inventory['units'] if u['style'] == 'Lift01']
    source = repo/'artifacts/prepared-current-structures-canonical/Lift01'
    target = repo/'assets/Materials/Lift01'
    required = {u['key'] for u in units}
    assert len(required) == len(units)
    assert required == {p.name for p in (source/'Structure').iterdir() if p.is_dir()}
    for u in units:
        numeric = u['a'] + u['b'] + u['c'] if u['triangle'] else u['bounds']
        key = digest(string_bytes(inventory['recipe']) + string_bytes(u['sourceDigest'])
                     + struct.pack('<?', u['triangle']) + struct.pack('<'+'f'*len(numeric), *numeric))
        assert key == u['key'], u['key']
    records = []
    identities = {}
    for root in (target, source):
        for sidecar in root.rglob('*.cid'):
            payload = sidecar.with_suffix('')
            identity = sidecar.read_text().strip()
            assert len(identity) == 32 and all(c in '0123456789abcdef' for c in identity)
            value = digest(payload.read_bytes())
            if identity in identities:
                assert identities[identity] == value, 'Conflicting CID: '+identity
            identities[identity] = value
    dependencies = set()
    for key in sorted(required):
        surface = source/'Structure'/key/'Structure.Surface'
        bindings = surface_bindings(surface.read_bytes())
        for identity in bindings.values():
            texture = source/'Shared'/(identity+'.Texture')
            assert texture.is_file() and Path(str(texture)+'.cid').read_text().strip() == identity
            dependencies.add(identity)
    source_shared = {p.stem for p in (source/'Shared').glob('*.Texture')}
    assert dependencies == source_shared, 'Missing or unreferenced canonical structure textures'
    before = {str(p.relative_to(target)): digest(p.read_bytes()) for p in target.rglob('*') if p.is_file()}
    files = sorted(p for p in source.rglob('*') if p.is_file())
    # Preflight every collision before the first copy.
    for src in files:
        relative = src.relative_to(source)
        assert relative.parts[0] in ('Structure', 'Shared')
        assert src.suffix in ('.Surface', '.Texture', '.cid')
        dst = target/relative
        expected = digest(src.read_bytes())
        assert not dst.exists() or digest(dst.read_bytes()) == expected, 'Conflicting path: '+str(dst)
        records.append({'path': str(relative), 'bytes': src.stat().st_size,
                        'sha256': expected, 'alreadyPresent': dst.exists()})
    for src, record in zip(files, records):
        dst = target/src.relative_to(source)
        if not dst.exists():
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(src, dst)
        assert digest(dst.read_bytes()) == record['sha256']
    after = {str(p.relative_to(target)): digest(p.read_bytes()) for p in target.rglob('*') if p.is_file()}
    assert all(after[path] == value for path, value in before.items())
    report = {'status': 'PASS', 'style': 'Lift01', 'inventorySha256': digest(inventory_bytes),
              'productionKeys': len(required), 'resolvedStructureTextures': len(dependencies),
              'sourceFiles': len(records), 'newFiles': sum(not r['alreadyPresent'] for r in records),
              'reusedFiles': sum(r['alreadyPresent'] for r in records),
              'canonicalBytes': sum(r['bytes'] for r in records),
              'styleFilesAfter': len(after), 'styleBytesAfter': sum(p.stat().st_size for p in target.rglob('*') if p.is_file()),
              'previousFilesUnchanged': True, 'conflictingCids': 0, 'missingKeys': [], 'missingDependencies': [],
              'rebakes': 0, 'files': records,
              'nativeEvidence': 'artifacts/uv-density/current-structure-canonical-package.json: runtimePreflightPassed=true; bytesAndCidsUnchanged=true',
              'localization': 'No user-facing text changed; existing localization preserved.',
              'remainingAcceptance': ['Main-agent build and package validation', 'In-game generated bridge appearance and cold loading']}
    report_path = repo/'artifacts/prepared-materials/Lift01/structure-integration.json'
    report_path.write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'files'}, indent=2))


if __name__ == '__main__':
    main()
