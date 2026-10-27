"""Resolve Lift02 repeat authoring textures and its original native mip graph."""
from pathlib import Path
import hashlib
import json
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
OUT = ROOT / 'artifacts/prepared-materials/Lift02/repeat-units-v2'


def cid(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)


def mid_entries(stream, wanted):
    """Native AtlasMaterialsGrouper.Read/AtlasCategory constructor binary layout."""
    def read(fmt):
        n = struct.calcsize(fmt)
        raw = stream.read(n)
        if len(raw) != n:
            raise EOFError('Incomplete original native mid-mip metadata')
        return struct.unpack(fmt, raw)
    tile, mips, group_size, stacks, groups, entries = read('<6i')
    assert tile == 512 and mips == 3 and 0 < stacks <= 2
    categories = []
    for stack in range(stacks):
        count, = read('<i')
        assert 0 <= count < 100000
        for _ in range(count):
            w, h, count, first, tiles, disabled, bias = read('<5i?i')
            categories.append(dict(stack=stack, width=w, height=h, entries=count, firstTile=first,
                                   tiles=tiles, disabled=disabled, mipBiasOverride=bias))
    matches = []
    for category in categories:
        if category['disabled']:
            continue
        for _ in range(category['entries']):
            start = stream.tell()
            identity = cid(stream.read(16))
            mask, group_index, duplicates_count = read('<ihi')
            assert 0 <= duplicates_count < 100000
            duplicates = [cid(stream.read(16)) for _ in range(duplicates_count)]
            levels = [dict(zip(('firstTileIndex', 'groupedMask'), read('<2i'))) for _ in range(mips)]
            if identity in wanted or wanted.intersection(duplicates):
                matches.append(dict(entryOffset=start, cid=identity, duplicates=duplicates,
                                    mask=mask, indexInGroup=group_index, category=category, levels=levels))
    tiles, = read('<i')
    offset = stream.tell()
    recorded_offset, = read('<q')
    assert recorded_offset == offset, 'Native mid-mip metadata alignment differs'
    return dict(tileSize=tile, midMipLevels=mips, groupSize=group_size, stacks=stacks,
                groups=groups, entries=entries, tiles=tiles, payloadOffset=stream.tell(), matches=matches)


def main():
    evidence = json.loads((OUT / 'evidence.json').read_bytes())
    source, backing = evidence['source'], evidence['backing']
    authoring = source['serializedTextureBindings']
    wanted = set(authoring.values())
    archives, ordinary, sidecars = [], [], 0
    for path in sorted((GAME / 'Content').rglob('*.cok')):
        if not path.name.startswith(('Blob', 'VT')):
            continue
        with zipfile.ZipFile(path) as z:
            names = set(z.namelist())
            count = 0
            for name in sorted(names):
                if not name.endswith('.Texture.cid'):
                    continue
                count += 1
                identity = z.read(name).decode().strip()
                if identity in wanted:
                    raw = z.read(name[:-4])
                    ordinary.append(dict(cid=identity, archive=str(path), entry=name[:-4], bytes=len(raw),
                                         sha256=hashlib.sha256(raw).hexdigest()))
            sidecars += count
            archives.append(dict(path=str(path), ordinaryTextureSidecars=count))
    with zipfile.ZipFile(backing['archive']) as z:
        raw = z.read(backing['entry'])
    assert hashlib.sha256(raw).hexdigest() == backing['sha256']
    assert cid(raw[2:18]) == source['cid']
    integer = lambda offset: struct.unpack_from('<i', raw, offset)[0]
    stacks = []
    for stack in range(integer(490)):
        offset = 26 + stack * 40
        width, height, tiles, compressed_start, tail, tile_data, layers = struct.unpack_from('<7i', raw, offset)
        layer_records = []
        tail_offset = tail + 498
        for layer in range(layers):
            li = 106 + stack * 192 + layer * 48
            if not raw[li]:
                continue
            size = integer(li + 16)
            assert tail_offset + size <= len(raw)
            layer_records.append(dict(layer=layer, format=integer(li + 4),
                                      preprocessing=struct.unpack_from('<H', raw, li + 8)[0],
                                      highMipDataIndex=integer(li + 12), tailBytes=size,
                                      midMipCount=integer(li + 20), tileBytes=integer(li + 24),
                                      originalTextureCid=cid(raw[li + 32:li + 48]),
                                      tailOffset=tail_offset,
                                      tailSha256=hashlib.sha256(raw[tail_offset:tail_offset + size]).hexdigest()))
            tail_offset += size
        stacks.append(dict(stack=stack, dimensions=[width, height], tileCount=tiles,
                           compressedStart=compressed_start + 498, tileDataOffset=tile_data,
                           layers=layer_records))
    mid_scans, matched = [], []
    for path in sorted((GAME / 'Content').rglob('MidMips*.cok')):
        with zipfile.ZipFile(path) as z:
            for entry in z.infolist():
                if not entry.filename.lower().endswith('.midmips'):
                    continue
                with z.open(entry) as stream:
                    metadata = mid_entries(stream, {source['cid']})
                record = dict(archive=str(path), entry=entry.filename, bytes=entry.file_size,
                              crc32=f'{entry.CRC:08x}', **metadata)
                mid_scans.append(dict(archive=str(path), entry=entry.filename, matchCount=len(metadata['matches'])))
                if metadata['matches']:
                    h = hashlib.sha256()
                    with z.open(entry) as stream:
                        while chunk := stream.read(1024 * 1024):
                            h.update(chunk)
                    record['sha256'] = h.hexdigest()
                    sidecar = entry.filename + '.cid'
                    if sidecar in z.namelist():
                        record['cid'] = z.read(sidecar).decode().strip()
                    matched.append(record)
    # An 8-period repeat gives integral transverse texels without ceil rescaling.
    width, height = 322, 271
    raw_bytes = sum(max(1, width >> mip) * max(1, height >> mip) * 4 for mip in range(9))
    report = dict(schema='BridgeBuilder/Lift02RepeatNativeGraph/1', style='Lift02',
                  authoringTextureCids=authoring, archives=archives, scannedOrdinarySidecars=sidecars,
                  ordinaryTextureMatches=ordinary, originalSurface=source, originalVTSurface=backing,
                  vtHeader=dict(tileSize=integer(18), footerOffset=integer(494), stacks=stacks),
                  midMipScans=mid_scans, matchingMidMipCaches=matched,
                  outputBudget=dict(periods=[1, 8], dimensions=[322, 271], mipCount=9,
                                    rgba8BytesPerChannel=raw_bytes, channels=3,
                                    totalUncompressedPixelBytes=raw_bytes * 3,
                                    scope='Calculation only; no payload or compressed package was produced.'),
                  status='requires full VTSurface plus matched MidMips reader' if not ordinary else 'ordinary source inputs available',
                  validationLimit='Header, metadata and source identity checks only. No pixel/mip/boundary equivalence is claimed.')
    (OUT / 'native-mip-graph.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(archives=len(archives), ordinaryTextures=len(ordinary), midMipMatches=len(matched),
                         output=str(OUT / 'native-mip-graph.json'), proposedRawBytes=raw_bytes * 3)))


if __name__ == '__main__':
    main()
