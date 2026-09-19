"""Explicit offline rUUID -> bUUID migration; not a runtime compatibility path.

Requires Python 3 and zstandard. Dry-run by default. The game must be stopped.
Only the named registered bridge's assets, two records, and affected local saves
are changed. Backups and a SHA-256 manifest are mandatory before --apply writes.
No mod-cache traversal or mutation. ZIP headers and unrelated compressed buffers
are preserved, including raw entry names, flags, timestamps and asset IDs.
"""
import argparse
import hashlib
import json
import pathlib
import shutil
import struct
import subprocess
import uuid
import zipfile
import zlib

import zstandard


def digest(data):
    return hashlib.sha256(data).hexdigest()


def long_path(path):
    value = str(path.resolve())
    return pathlib.Path(value if value.startswith('\\\\?\\') else '\\\\?\\' + value)


def copy_file(source, target):
    # Generated dependency names may exceed Win32's legacy MAX_PATH once the
    # backup prefix is added. CopyFile2 needs explicit extended-length paths.
    shutil.copy2(long_path(source), long_path(target))


def game_stopped():
    tasks = subprocess.check_output(['tasklist', '/FI', 'IMAGENAME eq Cities2.exe', '/FO', 'CSV'])
    if b'cities2.exe' in tasks.lower():
        raise RuntimeError('Close Cities2.exe before migration')


def buffers(data):
    """Game.Serialization.ReadSystem: raw metadata, then size/compressedSize pairs."""
    raw_size, = struct.unpack_from('<i', data)
    if not 0 < raw_size <= len(data) - 4:
        raise ValueError('Invalid raw metadata header')
    offset = 4 + raw_size
    yield 0, offset, data[:offset], False
    while offset < len(data):
        size, compressed = struct.unpack_from('<ii', data, offset)
        end = offset + 8 + compressed
        if min(size, compressed) < 0 or end > len(data):
            raise ValueError('Invalid compressed buffer header')
        payload = data[offset + 8:end]
        raw = b'' if size == compressed == 0 else zstandard.ZstdDecompressor().decompress(
            payload, max_output_size=max(size, 1))
        if len(raw) != size:
            raise ValueError('Buffer size mismatch')
        yield offset, end, raw, True
        offset = end


def migrate_data(data, old, new):
    parts, count = [], 0
    for start, end, raw, compressed in buffers(data):
        if old not in raw:
            parts.append(data[start:end])
            continue
        if not compressed:
            raise ValueError('Unexpected bridge reference in metadata')
        # Identity is stored by PrefabID as a length-prefixed name. Do not
        # rewrite arbitrary byte matches in simulation or third-party data.
        index = raw.find(old)
        while index >= 0:
            if index < 4 or struct.unpack_from('<i', raw, index - 4)[0] != len(old):
                raise ValueError('Old UUID is not an exact serialized prefab name')
            index = raw.find(old, index + len(old))
        updated = raw.replace(old, new)
        payload = zstandard.ZstdCompressor(level=3).compress(updated)
        if zstandard.ZstdDecompressor().decompress(payload) != updated:
            raise ValueError('ZStd round-trip mismatch')
        count += raw.count(old)
        parts.append(struct.pack('<ii', len(updated), len(payload)) + payload)
    result = b''.join(parts)
    before, after = list(buffers(data)), list(buffers(result))
    if len(before) != len(after):
        raise ValueError('Buffer count changed')
    for left, right in zip(before, after):
        if right[2] != left[2].replace(old, new):
            raise ValueError('Unexpected decompressed save change')
    return result, count


def replace_zip_entry(original, archive, info, payload):
    """Surgical ZIP32/STORED rewrite. Never recode entry filenames or metadata."""
    if info.compress_type != zipfile.ZIP_STORED or info.flag_bits & 9:
        raise ValueError('Unsupported ZIP compression/encryption/data descriptor')
    start = info.header_offset
    name_size, extra_size = struct.unpack_from('<HH', original, start + 26)
    data_start = start + 30 + name_size + extra_size
    old_end = data_start + info.compress_size
    delta = len(payload) - info.compress_size
    result = bytearray(original[:data_start] + payload + original[old_end:])
    crc = zlib.crc32(payload)
    struct.pack_into('<III', result, start + 14, crc, len(payload), len(payload))
    central = archive.start_dir + delta
    for _ in archive.infolist():
        if result[central:central + 4] != b'PK\x01\x02':
            raise ValueError('Invalid ZIP central directory')
        offset, = struct.unpack_from('<I', result, central + 42)
        if offset == start:
            struct.pack_into('<III', result, central + 16, crc, len(payload), len(payload))
        elif offset >= old_end:
            struct.pack_into('<I', result, central + 42, offset + delta)
        sizes = struct.unpack_from('<HHH', result, central + 28)
        central += 46 + sum(sizes)
    if result[central:central + 4] != b'PK\x05\x06':
        raise ValueError('Unsupported ZIP footer')
    struct.pack_into('<I', result, central + 16, archive.start_dir + delta)
    return bytes(result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=pathlib.Path, required=True)
    parser.add_argument('--backup', type=pathlib.Path, required=True)
    parser.add_argument('--old', required=True)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if args.old != 'r' + str(uuid.UUID(args.old[1:])):
        raise ValueError('An explicit canonical rUUID is required')
    root, backup = args.root.resolve(), args.backup.resolve()
    if backup.is_relative_to(root) or root.is_relative_to(backup):
        raise ValueError('Backup must be outside game data')
    root, backup = long_path(root), long_path(backup)
    old, new = args.old.encode('ascii'), ('b' + args.old[1:]).encode('ascii')
    game_stopped()
    registry = root / 'ModsData/BridgeBuilder/bridge-registry.tsv'
    lines = registry.read_bytes().splitlines()
    rows = [line.split(b'\t') for line in lines[1:]]
    if sum(row[0] == old for row in rows) != 1 or any(row[0] == new for row in rows):
        raise ValueError('Bridge is missing, duplicated, or destination already registered')
    changes, save_reports = [], []

    def add(path, updated, target=None):
        target = path if target is None else target
        if not path.resolve().is_relative_to(root) or not target.resolve().is_relative_to(root):
            raise ValueError('Path outside game data')
        if path != target and target.exists():
            raise ValueError(f'Destination collision: {target}')
        changes.append((path, target, path.read_bytes(), updated))

    for folder in ('ImportedData', 'BridgeBuilder'):
        for path in sorted((root / folder).rglob('*')):
            if not path.is_file():
                continue
            relative = path.relative_to(root)
            if args.old in str(relative):
                if path.suffix not in ('.Prefab', '.Geometry', '.cid'):
                    raise ValueError(f'Unexpected owned file: {path}')
                original = path.read_bytes()
                if path.suffix != '.Prefab' and old in original:
                    raise ValueError('Unexpected UUID inside binary geometry or CID')
                target = root / str(relative).replace(args.old, new.decode())
                add(path, original.replace(old, new), target)
            elif path.suffix == '.Prefab' and old in path.read_bytes():
                raise ValueError(f'Reference outside bridge ownership: {path}')
    if not any(path.name == args.old + '.Prefab' for path, *_ in changes):
        raise ValueError('Root bridge asset missing')
    for name in ('bridge-registry.tsv', 'export-state.tsv'):
        path = root / 'ModsData/BridgeBuilder' / name
        original = path.read_bytes()
        if original.count(old) != 1:
            raise ValueError(f'Unexpected identity count in {name}')
        add(path, original.replace(old, new))

    created = next(row[5].decode() for row in rows if row[0] == old)
    import datetime
    created_timestamp = datetime.datetime.fromisoformat(created.rstrip('Z') + '+00:00').timestamp()
    import io
    for path in sorted((root / 'Saves').rglob('*.cok')):
        original = path.read_bytes()
        with zipfile.ZipFile(io.BytesIO(original)) as archive:
            entries = [entry for entry in archive.infolist() if entry.filename.endswith('.SaveGameData')]
            if len(entries) != 1:
                raise ValueError(f'Unexpected save data entry count: {path}')
            entry = entries[0]
            try:
                data = archive.read(entry)
            except zipfile.BadZipFile as error:
                if path.stat().st_mtime >= created_timestamp:
                    raise ValueError(f'A potentially affected save is corrupt: {path}') from error
                save_reports.append({'path': str(path.relative_to(root)), 'status': 'preexisting CRC error; predates bridge; untouched'})
                continue
            updated, count = migrate_data(data, old, new)
            save_reports.append({'path': str(path.relative_to(root)), 'references': count})
            if count:
                rewritten = replace_zip_entry(original, archive, entry, updated)
                with zipfile.ZipFile(io.BytesIO(rewritten)) as check:
                    if check.testzip() is not None or check.namelist() != archive.namelist():
                        raise ValueError('ZIP verification failed')
                    for item in archive.infolist():
                        expected = updated if item.filename == entry.filename else archive.read(item)
                        if check.read(item.filename) != expected:
                            raise ValueError('Unrelated ZIP entry changed')
                add(path, rewritten)
                cid = path.with_suffix('.cok.cid')
                if cid.exists():
                    add(cid, cid.read_bytes())

    report = {'old': old.decode(), 'new': new.decode(), 'saves': save_reports, 'files': [
        {'source': str(path.relative_to(root)), 'target': str(target.relative_to(root)),
         'before': digest(original), 'after': digest(updated)}
        for path, target, original, updated in changes]}
    print(json.dumps(report, ensure_ascii=False, indent=2), flush=True)
    if not args.apply:
        return
    if backup.exists():
        raise ValueError('Use a fresh backup directory')
    backup.mkdir(parents=True)
    for path, target, original, updated in changes:
        saved = backup / 'original' / path.relative_to(root)
        saved.parent.mkdir(parents=True, exist_ok=True)
        copy_file(path, saved)
        if saved.read_bytes() != original:
            raise ValueError('Backup mismatch')
        staged = backup / 'staged' / target.relative_to(root)
        staged.parent.mkdir(parents=True, exist_ok=True)
        staged.write_bytes(updated)
    (backup / 'manifest.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    game_stopped()
    for path, _, original, _ in changes:
        if path.read_bytes() != original:
            raise ValueError('Source changed after planning')
    # Copy all destinations before removing old names; originals remain recoverable
    # in the manifest-backed backup. No recursive deletion or broad directory move.
    for path, target, original, updated in changes:
        target.parent.mkdir(parents=True, exist_ok=True)
        copy_file(backup / 'staged' / target.relative_to(root), target)
        if target.read_bytes() != updated:
            raise ValueError('Installed data mismatch; restore from manifest')
    for path, target, original, _ in changes:
        if path != target:
            if path.read_bytes() != original:
                raise ValueError('Old source changed; refusing removal')
            path.unlink()
    for directory in sorted({path.parent for path, target, *_ in changes if path != target},
                            key=lambda value: len(value.parts), reverse=True):
        if not directory.is_relative_to(root / 'ImportedData') and not directory.is_relative_to(root / 'BridgeBuilder'):
            raise ValueError('Refusing directory cleanup outside owned asset roots')
        if args.old in directory.name and not any(directory.iterdir()):
            directory.rmdir()
    for _, target, _, updated in changes:
        if target.read_bytes() != updated:
            raise ValueError('Final verification failed')
    (backup / 'COMPLETE').write_text('All destination bytes verified against manifest.\n', encoding='utf-8')
    print(f'MIGRATED {len(changes)} files; backup: {backup}')


if __name__ == '__main__':
    import sys
    sys.stdout.reconfigure(encoding='utf-8')
    main()
