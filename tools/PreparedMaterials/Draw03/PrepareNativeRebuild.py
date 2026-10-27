"""Prepare Draw03 native-source rebuild inputs without reading old baked products.

The manifest describes original built-in payloads, not shipped copies. Descriptor
snapshots are authoring inputs only; they retain original CIDs and unmodified bytes.
No per-sampler texture extraction, encoding or material publication occurs here.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

from PrepareRecipes import SOURCE_CID, units


ZERO = '0' * 32


def digest(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    if len(data) != 16:
        raise ValueError('Invalid native CID length')
    return ''.join(f'{value:02x}'[::-1] for value in data)


class Reader:
    def __init__(self, data):
        self.data, self.at = data, 0

    def take(self, count):
        result = self.data[self.at:self.at + count]
        if len(result) != count:
            raise ValueError('Truncated native descriptor')
        self.at += count
        return result

    def number(self, fmt):
        return struct.unpack('<' + fmt, self.take(struct.calcsize('<' + fmt)))[0]

    def present(self):
        value = self.number('B')
        if value not in (0, 255):
            raise ValueError('Unexpected native null marker')
        return value == 255

    def string(self):
        length, shift = 0, 0
        while True:
            value = self.number('B')
            length |= (value & 127) << shift
            if value < 128:
                return self.take(length).decode('utf-8')
            shift += 7
            if shift > 28:
                raise ValueError('Invalid native string length')

    def count(self):
        if not self.present():
            return 0
        count = self.number('i')
        if not 0 <= count <= len(self.data):
            raise ValueError('Invalid collection length')
        return count


def surface(data, library):
    r = Reader(data)
    version, template, vt = r.number('H'), r.number('i'), r.number('B')
    if version != 1 or vt not in (0, 1):
        raise ValueError('Unmeasured native Surface layout')
    definition = next(item for item in library if item['hash'] == template)
    stacks, dependencies = [], set()
    if vt:
        count = r.number('i')
        if not 0 <= count <= 2:
            raise ValueError('Invalid stack count')
        for index in range(2):
            width, height = r.number('i'), r.number('i')
            originals = [cid(r.take(16)) for _ in range(4)]
            payloads = [cid(r.take(16)) for _ in range(4)]
            if index < count:
                stacks.append(dict(width=width, height=height, originals=originals,
                                   payloads=payloads))
                dependencies.update(payloads)
        vt_surface = cid(r.take(16)) if r.present() else ZERO
        dependencies.add(vt_surface)
    else:
        vt_surface = ZERO
    properties = {}
    for kind, width in (('floats', 4), ('ints', 4), ('vectors', 16), ('colors', 16)):
        properties[kind] = {r.string(): r.take(width).hex() for _ in range(r.count())}
    textures = {}
    for _ in range(r.count()):
        name = r.string()
        textures[name] = cid(r.take(16)) if r.present() else ZERO
    keywords = [r.string() for _ in range(r.count())]
    if r.at != len(data):
        raise ValueError('Trailing native Surface bytes')
    stack_names = {name for stack in definition['stacks'] for name in stack}
    dependencies.update(value for name, value in textures.items() if not vt or name not in stack_names)
    dependencies.discard(ZERO)
    return dict(version=version, template=template, virtualTextures=bool(vt),
                stacks=stacks, vtSurfaceCid=vt_surface, textureProperties=textures,
                properties=properties, keywords=keywords, dependencies=sorted(dependencies))


def payload_metadata(data, extension):
    if extension in ('.Texture', '.Texture2D', '.Texture2DArray'):
        r = Reader(data)
        version, width, height = r.number('H'), r.number('H'), r.number('H')
        if not 1 <= version <= 5:
            raise ValueError('Unmeasured Texture layout')
        depth = r.number('H') if version >= 4 else 1
        mips = r.number('B')
        fmt = r.number('B') if version >= 3 else r.string()
        dimension = r.number('B') if version >= 4 else 2
        filtering, wrapping, aniso = (r.number('B'), r.number('B'), r.number('B')) if version >= 2 else (2, 0, 16)
        return dict(version=version, width=width, height=height, depth=depth, mipCount=mips,
                    format=fmt, dimension=dimension, filterMode=filtering,
                    wrapMode=wrapping, anisotropy=aniso)
    if extension == '.VTTexture':
        if struct.unpack_from('<H', data)[0] != 1 or data[2] != 255:
            raise ValueError('Unmeasured VTTexture layout')
        values = struct.unpack_from('<5H5B', data, 19)
        return dict(originalTextureCid=cid(data[3:19]), **dict(zip(
            ('preprocessVersion', 'tileSize', 'width', 'height', 'midMipCount',
             'mipCount', 'format', 'filterMode', 'wrapMode', 'anisotropy'), values)))
    if extension == '.VTSurface':
        version = struct.unpack_from('<H', data)[0]
        count = struct.unpack_from('<i', data, 490)[0]
        if version != 1 or not 0 <= count <= 2:
            raise ValueError('Unmeasured VTSurface layout')
        stacks = []
        for index in range(count):
            at = 26 + 40 * index
            width, height = struct.unpack_from('<2i', data, at)
            layers = struct.unpack_from('<i', data, at + 24)[0]
            if not 0 <= layers <= 4:
                raise ValueError('Invalid VTSurface layer count')
            records = []
            for layer in range(layers):
                at = 106 + 192 * index + 48 * layer
                records.append(dict(preprocessed=bool(data[at]),
                    format=struct.unpack_from('<i', data, at + 4)[0],
                    preprocessVersion=struct.unpack_from('<H', data, at + 8)[0],
                    highMipBytes=struct.unpack_from('<i', data, at + 16)[0],
                    midMipCount=struct.unpack_from('<i', data, at + 20)[0],
                    originalTextureCid=cid(data[at + 32:at + 48])))
            stacks.append(dict(width=width, height=height, layers=records))
        return dict(version=version, materialCid=cid(data[2:18]), stacks=stacks,
                    tileSize=struct.unpack_from('<i', data, 18)[0],
                    mipBias=struct.unpack_from('<i', data, 22)[0])
    return dict(nativeHeaderInspection='Not decoded; full native bytes remain authoritative')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('material_library', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    library = json.loads(args.material_library.read_text(encoding='utf-8-sig'))
    style = args.repository / 'src/BridgeBuilder/Bridges/Types/Draw03'
    pattern = re.compile(r'Create(?:<[^>]+>)?\("(DrawBridge03[^"\n]*)", "([a-f0-9]{32})", new\[\] \{ ([^}]+) \}')
    uses, inputs = [], []
    for path in sorted(style.glob('*.cs')):
        text = path.read_text(encoding='utf-8-sig')
        matches = list(pattern.finditer(text))
        if not matches:
            continue
        inputs.append(dict(path=str(path.relative_to(args.repository)), sha256=digest(path.read_bytes())))
        for match in matches:
            for index, source in enumerate(re.findall(r'"([a-f0-9]{32})"', match[3])):
                uses.append(dict(render=match[1], geometryCid=match[2], mesh=index, sourceCid=source))
    if not uses:
        raise ValueError('No Draw03 native source uses were identified')
    archives, entries = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in sorted((args.game_data / 'Content' / family).glob('*.cok')):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                archive = zipfile.ZipFile(path)
                archives.append(archive)
                for name in archive.namelist():
                    if name.endswith('.cid'):
                        identity = archive.read(name).decode('utf-8-sig').strip()
                        entries.setdefault(identity, []).append((archive, name[:-4]))
        def read(identity):
            found = entries[identity]
            data = found[0][0].read(found[0][1])
            for archive, name in found[1:]:
                if archive.read(name) != data:
                    raise ValueError('Conflicting built-in CID: ' + identity)
            return data, found[0]
        sources, dependencies = [], set()
        args.output.mkdir(parents=True, exist_ok=True)
        descriptor_dir = args.output / 'authoring-descriptors'
        descriptor_dir.mkdir(exist_ok=True)
        for identity in sorted({use['sourceCid'] for use in uses} | {SOURCE_CID}):
            data, (archive, name) = read(identity)
            parsed = surface(data, library)
            dependencies.update(parsed['dependencies'])
            (descriptor_dir / (identity + '.Surface')).write_bytes(data)
            sources.append(dict(cid=identity, sha256=digest(data), bytes=len(data),
                                archive=archive.filename, entry=name, builtIn=True,
                                packageAction='direct-reference', descriptor=parsed))
        payloads = []
        for identity in sorted(dependencies):
            data, (archive, name) = read(identity)
            extension = Path(name).suffix
            payloads.append(dict(cid=identity, type=extension, sha256=digest(data), bytes=len(data),
                archive=archive.filename, entry=name, builtIn=True, packageAction='direct-reference',
                metadata=payload_metadata(data, extension)))
        road = units(args.repository, next(s['sha256'] for s in sources if s['cid'] == SOURCE_CID))
        manifest = dict(schema='Draw03NativeRebuildInputs/1', sourceInputs=inputs,
            sourceSurfaces=sources, nativePayloads=payloads, structureUses=uses,
            roadSamplingUses=road,
            samplingImplementation='Original source charts and density frames; public native remapping integration pending',
            mipEvidence='Complete native payload hashes and native header metadata; no decoded mip equivalence claim',
            midMipCacheResolution='Native TextureStreamingSystem/AtlasMaterialsDatabase resolve built-in MidMipCacheAsset by tile size and mip count; this input does not copy or rebuild the global cache',
            provenance='Original game/DLC COK only; no prior BridgeBuilder material product is an input')
        (args.output / 'native-rebuild.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
        summary = dict(sourceSurfaces=len(sources), distinctSurfaceContents=len({s['sha256'] for s in sources}),
            sourceTemplates={str(t): sum(s['descriptor']['template'] == t for s in sources)
                             for t in sorted({s['descriptor']['template'] for s in sources})},
            structureUses=len(uses), nativePayloadCids=len(payloads),
            payloadTypes={kind: sum(p['type'] == kind for p in payloads) for kind in sorted({p['type'] for p in payloads})},
            nativePayloadBytes=sum(p['bytes'] for p in payloads),
            distinctNativePayloadContents=len({(p['type'], p['sha256']) for p in payloads}),
            roadSamplingUses=len(road), roadSourceSurfaces=1,
            copiedTextureFiles=0, bakedTextureFiles=0, publishedMaterialFiles=0,
            authoringDescriptorBytes=sum(s['bytes'] for s in sources),
            nativeSamplingComplete=False, localization='No UI text or runtime localization changes')
        (args.output / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
        print(json.dumps(summary, indent=2))
    finally:
        for archive in archives:
            archive.close()


if __name__ == '__main__':
    main()
