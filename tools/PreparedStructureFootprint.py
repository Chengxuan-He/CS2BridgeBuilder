"""Measure prepared structural inflation against the exact native texture inputs.

This reports a material subtree, not a complete generated-bridge size verdict.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def cid(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--inventory', type=Path, required=True)
    p.add_argument('--prepared', type=Path, required=True)
    p.add_argument('--game-data', type=Path, required=True)
    p.add_argument('--style', required=True)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    inventory = json.loads(args.inventory.read_text())
    units = [u for u in inventory['units'] if u['style'] == args.style]
    sources = {u['sourceCid'] for u in units}
    archives, entries = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in sorted((args.game_data/'Content'/family).glob('*.cok')):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                archive = zipfile.ZipFile(path)
                archives.append(archive)
                for name in archive.namelist():
                    if not name.endswith('.cid'):
                        continue
                    identity = archive.read(name).decode().strip()
                    entries.setdefault(identity, (archive, name[:-4]))

        def read(identity):
            archive, name = entries[identity]
            return archive.read(name)

        source_rows, native = [], {}
        for identity in sorted(sources):
            data = read(identity)
            version, template, vt = struct.unpack_from('<Hi?', data)
            if version != 1:
                raise ValueError('Unsupported structural source: '+identity)
            deps = {}
            active = set()
            at = 7
            if vt and data[283] == 255:
                backing = cid(data[284:300])
                payload = read(backing)
                if struct.unpack_from('<H', payload)[0] != 1:
                    raise ValueError('Unsupported VTSurface version')
                stacks = struct.unpack_from('<i', payload, 490)[0]
                dimensions = []
                for stack in range(stacks):
                    w, h = struct.unpack_from('<ii', payload, 26+stack*40)
                    layers = struct.unpack_from('<i', payload, 50+stack*40)[0]
                    for layer in range(layers):
                        if payload[106+stack*192+layer*48]:
                            active.add((stack, layer))
                            dimensions.append({'stack': stack, 'layer': layer, 'width': w, 'height': h})
                deps[backing] = {'cid': backing, 'kind': 'VTSurface', 'bytes': len(payload), 'layers': dimensions}
                at = 300
            elif vt and data[283] != 0:
                raise ValueError('Invalid native optional reference')
            elif vt:
                at = 284
            description = next(s for s in inventory['sources'] if s['cid'] == identity)
            for channel in description['channels']:
                if not vt:
                    break
                stack, layer = channel['stack'], channel['layer']
                if (stack, layer) in active:
                    continue
                offset = 7+76+stack*136+layer*16
                backing = cid(data[offset:offset+16])
                if backing == '0'*32:
                    continue
                payload = read(backing)
                w, h = struct.unpack_from('<HH', payload, 23)
                deps[backing] = {'cid': backing, 'kind': 'VTTexture', 'bytes': len(payload),
                                 'layers': [{'stack': stack, 'layer': layer, 'width': w, 'height': h}]}
            def count():
                nonlocal at
                marker = data[at]
                at += 1
                if marker == 0:
                    return 0
                if marker != 255:
                    raise ValueError('Invalid property map marker')
                n = struct.unpack_from('<i', data, at)[0]
                at += 4
                return n
            def text():
                nonlocal at
                size, shift = 0, 0
                while True:
                    b = data[at]
                    at += 1
                    size |= (b & 127) << shift
                    if not b & 128:
                        break
                    shift += 7
                value = data[at:at+size].decode()
                at += size
                return value
            for stride in (4, 4, 16, 16):
                for _ in range(count()):
                    text()
                    at += stride
            for _ in range(count()):
                name = text()
                marker = data[at]
                at += 1
                if marker == 0:
                    continue
                if marker != 255:
                    raise ValueError('Invalid texture reference')
                backing = cid(data[at:at+16])
                at += 16
                if vt and any(c['name'] == name for c in description['channels']):
                    continue
                payload = read(backing)
                w, h = struct.unpack_from('<HH', payload, 2)
                deps[backing] = {'cid': backing, 'kind': 'Texture', 'bytes': len(payload),
                                 'layers': [{'property': name, 'width': w, 'height': h}]}
            native.update(deps)
            source_rows.append({'cid': identity, 'name': entries[identity][1], 'template': template,
                                'samplers': sum(u['sourceCid'] == identity for u in units),
                                'backing': list(deps.values())})
        files = [f for f in args.prepared.rglob('*') if f.is_file()]
        textures = [f for f in files if f.suffix == '.Texture']
        unique = {}
        for path in textures:
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            unique[digest] = path.stat().st_size
        report = {'scope': 'Structural texture inputs and prepared outputs only; not complete bridge footprint.',
                  'style': args.style, 'samplers': len(units), 'preparedFiles': len(files),
                  'preparedBytes': sum(f.stat().st_size for f in files), 'textureFiles': len(textures),
                  'uniqueTextureContents': len(unique), 'uniqueTextureBytes': sum(unique.values()),
                  'nativeTextureBackingBytes': sum(r['bytes'] for r in native.values()),
                  'nativeTextureBackingCount': len(native), 'sources': source_rows}
        args.output.write_text(json.dumps(report, indent=2)+'\n')
        print(json.dumps({k: v for k, v in report.items() if k != 'sources'}))
    finally:
        for archive in archives:
            archive.close()


if __name__ == '__main__':
    main()
