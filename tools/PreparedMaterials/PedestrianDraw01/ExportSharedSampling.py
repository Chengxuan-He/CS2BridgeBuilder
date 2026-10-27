"""Separate PedestrianDraw01 native texture provenance from sampler identity.

This exports authoring data only. It neither aliases unequal texture CIDs nor
changes the installed/prepared resources. The shared native sampler must consume
the recorded mappings before any per-sampler texture can be retired.
"""
import argparse
import hashlib
import io
import json
import re
import struct
import zipfile
from collections import Counter
from pathlib import Path

STYLE = 'PedestrianDraw01'
CHANNELS = ('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask', '_EmissiveColorMap')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    assert len(data) == 16
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


def string(stream):
    size, shift = 0, 0
    while True:
        b = stream.read(1)[0]
        size |= (b & 127) << shift
        if b < 128:
            return stream.read(size).decode('utf-8')
        shift += 7


def present(stream):
    value = stream.read(1)[0]
    assert value in (0, 255)
    return value == 255


def count(stream):
    return struct.unpack('<i', stream.read(4))[0] if present(stream) else 0


def descriptor(data):
    stream = io.BytesIO(data)
    version, template, vt = struct.unpack('<Hi?', stream.read(7))
    assert version == 1
    header = stream.read(276) if vt else None
    surface = cid(stream.read(16)) if vt and present(stream) else None
    for size in (4, 4, 16, 16):
        for _ in range(count(stream)):
            string(stream)
            assert len(stream.read(size)) == size
    bindings = {}
    for _ in range(count(stream)):
        name = string(stream)
        if present(stream):
            bindings[name] = cid(stream.read(16))
    keywords = [string(stream) for _ in range(count(stream))]
    assert stream.tell() == len(data)
    return template, header, surface, bindings, keywords


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    inventory_path = args.repository / 'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_text())
    units = [u for u in inventory['units'] if u['style'] == STYLE]
    repeats_path = args.repository / f'artifacts/prepared-repeat/{STYLE}/recipes.json'
    repeats = json.loads(repeats_path.read_text())
    source_ids = {u['sourceCid'] for u in units + repeats['units']}
    render_path = args.repository / f'src/BridgeBuilder/Bridges/Types/{STYLE}/{STYLE}SourceRenders.cs'
    native_render_bindings = [dict(render=name, geometryCid=geometry,
        surfaceCids=re.findall(r'"([a-f0-9]{32})"', surfaces))
        for name, geometry, surfaces in re.findall(r'Create\("([^"]+)", "([a-f0-9]{32})", new\[\] \{ ([^}]+) \}', render_path.read_text())]
    assert native_render_bindings, 'Native render binding extraction failed'
    archives, lookup = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in (args.game_data / 'Content' / family).glob('*.cok'):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                archive = zipfile.ZipFile(path)
                archives.append(archive)
                for entry in archive.namelist():
                    if entry.endswith('.cid'):
                        identity = archive.read(entry).decode().strip()
                        lookup.setdefault(identity, []).append((archive, entry[:-4]))

        def read(identity):
            found = lookup[identity]
            data = found[0][0].read(found[0][1])
            assert all(a.read(e) == data for a, e in found[1:]), identity
            return data

        native_graph = {}
        for identity in sorted({c for row in native_render_bindings for c in row['surfaceCids']} | source_ids):
            data = read(identity)
            template, header, combined, bindings, keywords = descriptor(data)
            refs = set()
            if header:
                for stack in range(struct.unpack_from('<i', header)[0]):
                    for layer in range(4):
                        value = cid(header[76 + stack * 136 + layer * 16:92 + stack * 136 + layer * 16])
                        if value != '0' * 32:
                            refs.add(value)
                if combined:
                    refs.add(combined)
            refs.update(value for name, value in bindings.items() if not header or name not in CHANNELS)
            native_graph[identity] = dict(kind='Surface', sha256=digest(data), bytes=len(data),
                template=template, nativeVT=header is not None, references=sorted(refs))
            for ref in sorted(refs):
                payload = read(ref)
                archive, entry = lookup[ref][0]
                node = dict(kind=Path(entry).suffix[1:], sha256=digest(payload), bytes=len(payload),
                    archive=archive.filename, entry=entry, retainedAsBuiltinReference=True)
                if entry.endswith('.VTTexture'):
                    assert payload[:3] == b'\x01\x00\xff'
                    tile, width, height, mid = struct.unpack_from('<4H', payload, 21)
                    mips = payload[29]
                    tail = struct.unpack_from('<i', payload, 34)[0]
                    table = 38 + tail
                    number = struct.unpack_from('<i', payload, table)[0]
                    offsets = struct.unpack_from('<' + 'i' * number, payload, table + 4)
                    assert len(offsets) and offsets[0] > 0 and all(a < b for a, b in zip(offsets, offsets[1:]))
                    assert table + 4 + 4 * number + offsets[-1] == len(payload)
                    node.update(width=width, height=height, mipCount=mips, midMipCount=mid,
                        tileSize=tile, format=payload[30], filter=payload[31], wrap=payload[32],
                        anisotropy=payload[33], nativeTailBytes=tail, nativeTileRecords=number,
                        fullSerializedMipPayloadPreserved=True)
                elif entry.endswith('.Texture'):
                    version, width, height, depth = struct.unpack_from('<4H', payload)
                    mips, format_id, dimension, filtering, wrapping, aniso = payload[8:14]
                    assert version in (4, 5) and depth == 1 and dimension == 2
                    assert format_id in (4, 108, 109), 'Review native format before calculating mip bytes'
                    raw_size = sum(max(1, width >> level) * max(1, height >> level) * 4
                        if format_id == 4 else ((max(1, width >> level) + 3) // 4) * ((max(1, height >> level) + 3) // 4) * 16
                        for level in range(mips))
                    if version == 4:
                        assert len(payload) == 14 + raw_size
                    else:
                        compressed = struct.unpack_from('<i', payload, 14)[0]
                        assert len(payload) == 18 + (raw_size if compressed == -1 else compressed)
                    node.update(width=width, height=height, mipCount=mips, format=format_id,
                        filter=filtering, wrap=wrapping, anisotropy=aniso, nativeMipBytes=raw_size,
                        fullSerializedMipPayloadPreserved=True)
                if ref in native_graph:
                    assert native_graph[ref]['sha256'] == node['sha256']
                else:
                    native_graph[ref] = node

        textures, materials = {}, []
        for identity in sorted(source_ids):
            data = read(identity)
            source_digest = digest(data)
            assert all(u['sourceDigest'] == source_digest for u in units + repeats['units'] if u['sourceCid'] == identity)
            template, header, combined, bindings, keywords = descriptor(data)
            layers = {}
            if combined:
                packed = read(combined)
                assert struct.unpack_from('<H', packed)[0] == 1
                stacks = struct.unpack_from('<i', packed, 490)[0]
                for stack in range(stacks):
                    si = 26 + stack * 40
                    width, height = struct.unpack_from('<ii', packed, si)
                    number = struct.unpack_from('<i', packed, si + 24)[0]
                    for layer in range(number):
                        li = 106 + stack * 192 + layer * 48
                        if packed[li]:
                            layers[stack, layer] = dict(backingKind='VTSurface', backingCid=combined,
                                backingSHA256=digest(packed), width=width, height=height,
                                format=struct.unpack_from('<i', packed, li + 4)[0], stack=stack, layer=layer)
            for channel in next(s for s in inventory['sources'] if s['cid'] == identity)['channels']:
                name, stack, layer = channel['name'], channel['stack'], channel['layer']
                provenance = layers.get((stack, layer))
                if provenance is None and header:
                    backing = cid(header[76 + stack * 136 + layer * 16:92 + stack * 136 + layer * 16])
                    if backing != '0' * 32:
                        packed = read(backing)
                        assert struct.unpack_from('<H', packed)[0] == 1 and packed[2] == 255
                        width, height = struct.unpack_from('<HH', packed, 23)
                        provenance = dict(backingKind='VTTexture', backingCid=backing,
                            backingSHA256=digest(packed), width=width, height=height,
                            format=packed[30], stack=stack, layer=layer)
                if provenance is None:
                    if header or name not in bindings:
                        continue  # Native default, not an authored texture.
                    packed = read(bindings[name])
                    width, height = struct.unpack_from('<HH', packed, 2)
                    provenance = dict(backingKind='Texture', backingCid=bindings[name],
                        backingSHA256=digest(packed), width=width, height=height, format=packed[9])
                semantic = 'srgb-color' if name in ('_BaseColorMap', '_EmissiveColorMap') else 'native-normal' if name == '_NormalMap' else 'linear-data'
                # Source identity intentionally excludes material and sampling coordinates.
                texture_key = digest(json.dumps(dict(provenance=provenance, semantic=semantic), sort_keys=True, separators=(',', ':')).encode())
                textures.setdefault(texture_key, dict(key=texture_key, semantic=semantic, provenance=provenance, uses=[]))['uses'].append(dict(surfaceCid=identity, property=name))
            materials.append(dict(cid=identity, sha256=source_digest, template=template,
                nativeVT=header is not None, keywords=keywords,
                textures={use['property']: t['key'] for t in textures.values() for use in t['uses'] if use['surfaceCid'] == identity}))
        samplers = [dict(key=u['key'], family='Structure', sourceCid=u['sourceCid'],
                        method='triangle-reflection-repeat' if u['triangle'] else 'rectangle-repeat',
                        bounds=u['bounds'], a=u['a'], b=u['b'], c=u['c']) for u in units]
        samplers += [dict(key=u['key'], family='Repeat', sourceCid=u['sourceCid'], method='rectangle-repeat',
                         minimum=u['min'], maximum=u['max'], regions=u['regions'], cutout=u['cutout']) for u in repeats['units']]
        root = args.repository / f'assets/Materials/{STYLE}'
        files = [p for p in root.rglob('*') if p.is_file()]
        texture_files = [p for p in files if p.suffix == '.Texture']
        counts = Counter(s['method'] for s in samplers)
        result = dict(schema='BridgeBuilder/SharedTextureSamplers/1', style=STYLE,
            inventorySHA256=digest(inventory_path.read_bytes()), repeatsSHA256=digest(repeats_path.read_bytes()),
            sourceMaterials=materials, sourceTextures=list(textures.values()), samplers=samplers,
            measured=dict(structuralSamplers=len(units), repeatSamplers=len(repeats['units']), sourceSurfaces=len(materials),
                authoredChannelSources=len(textures), samplerMethods=dict(counts), installedSourceFiles=len(files),
                installedSourceBytes=sum(p.stat().st_size for p in files), textureFiles=len(texture_files),
                textureBytes=sum(p.stat().st_size for p in texture_files)),
            status='Source mapping complete; native regional sampling implementation pending',
            limits='Provenance identity is not decoded-byte equivalence. Distinct native channel payloads remain distinct until complete mip comparison. No prepared resource has been replaced.')
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
        binding_output = args.output.with_name('native-bindings.json')
        binding_output.write_text(json.dumps(dict(style=STYLE, renderSourceSHA256=digest(render_path.read_bytes()),
            nativeRenderBindings=native_render_bindings, nativeDependencyGraph=native_graph,
            sourceTextureBindings={m['cid']:m['textures'] for m in materials},
            newTextureFiles=0, newSurfaceFiles=0,
            limitations='Original render source bindings and complete native payload identities only. Existing generation retile still requires the coordinator native sampling implementation. Serialized mip retention is not filtered rendered equivalence.'), indent=2) + '\n', encoding='utf-8')
        print(json.dumps(result['measured']))
    finally:
        for archive in archives:
            archive.close()


if __name__ == '__main__':
    main()
