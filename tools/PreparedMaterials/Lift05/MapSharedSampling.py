"""Read-only Lift05 source-texture/sampling inventory; never bakes or publishes."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import struct
import zipfile


def sha(data):
    return hashlib.sha256(data).hexdigest()


def cid(data):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


class Reader:
    def __init__(self, data): self.f = io.BytesIO(data)
    def take(self, count):
        value = self.f.read(count)
        assert len(value) == count, 'Truncated native descriptor'
        return value
    def number(self, fmt): return struct.unpack('<' + fmt, self.take(struct.calcsize('<' + fmt)))[0]
    def present(self):
        value = self.number('B')
        assert value in (0, 255)
        return value == 255
    def text(self):
        size = shift = 0
        while True:
            b = self.number('B'); size |= (b & 127) << shift
            if b < 128: break
            shift += 7
        return self.take(size).decode('utf-8')


def surface(data):
    r = Reader(data)
    version, template, vt = r.number('H'), r.number('i'), r.number('B')
    assert version == 1 and vt in (0, 1)
    stacks = []
    if vt:
        count = r.number('i')
        for stack in range(2):
            dimensions = [r.number('i'), r.number('i')]
            originals = [cid(r.take(16)) for _ in range(4)]
            layers = [cid(r.take(16)) for _ in range(4)]
            if stack < count: stacks.append(dict(index=stack, dimensions=dimensions, originalTextureCids=originals, vtTextureCids=layers))
        vt_surface = cid(r.take(16)) if r.present() else None
    else: vt_surface = None
    properties = {}
    for name, size in [('floats', 4), ('ints', 4), ('vectors', 16), ('colors', 16)]:
        values = {}
        if r.present():
            for _ in range(r.number('i')):
                key = r.text(); values[key] = r.take(size).hex()
        properties[name] = values
    textures = {}
    if r.present():
        for _ in range(r.number('i')):
            key = r.text(); textures[key] = cid(r.take(16)) if r.present() else None
    keywords = [r.text() for _ in range(r.number('i'))] if r.present() else []
    assert r.f.tell() == len(data), 'Trailing native descriptor data'
    return dict(template=template, isVT=bool(vt), stacks=stacks, vtSurfaceCid=vt_surface, properties=properties, textures=textures, keywords=keywords)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('repo', type=Path)
    parser.add_argument('game', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    repo, output = args.repo.resolve(), args.output.resolve()
    assert not output.exists(), 'Use a new evidence directory'
    style = repo / 'assets/Materials/Lift05'
    inventory_path = repo / 'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_text())
    units = {u['key']: u for u in inventory['units'] if u['style'] == 'Lift05'}
    packaged = {p.parent.name: p for p in (style / 'Structure').glob('*/Structure.Surface')}
    assert packaged and packaged.keys() <= units.keys(), 'Missing measured sampler'
    repeat_path = repo / 'artifacts/prepared-lift05/repeat-recipes.json'
    repeat = json.loads(repeat_path.read_text())
    selected = [units[k] for k in sorted(packaged)]
    source_ids = {u['sourceCid'] for u in selected + repeat['units']}
    raw_sources = {}
    for family in ('Game', 'BridgesAndPorts'):
        for path in (args.game / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid'): continue
                    source_id = archive.read(entry).decode().strip()
                    if source_id not in source_ids: continue
                    raw = archive.read(entry[:-4])
                    assert source_id not in raw_sources or raw_sources[source_id] == raw
                    raw_sources[source_id] = raw
    assert raw_sources.keys() == source_ids
    sources = {key: dict(cid=key, sha256=sha(raw), native=surface(raw)) for key, raw in raw_sources.items()}
    texture_files = {}
    for path in style.rglob('*.Texture'):
        key = Path(str(path) + '.cid').read_text().strip()
        data = path.read_bytes()
        if key in texture_files: assert texture_files[key]['sha256'] == sha(data)
        texture_files[key] = dict(path=path.relative_to(style).as_posix(), bytes=len(data), sha256=sha(data))
    records = []
    for unit in selected:
        source = sources[unit['sourceCid']]
        assert source['sha256'] == unit['sourceDigest']
        if unit['triangle']:
            origin = unit['a']; x = [b-a for a,b in zip(origin,unit['b'])]; y = [c-a for a,c in zip(origin,unit['c'])]
        else:
            a,b,c,d = unit['bounds']; origin=[a,b]; x=[c-a,0]; y=[0,d-b]
        native = surface(packaged[unit['key']].read_bytes())
        bindings = native['textures']
        assert all(t in texture_files for t in bindings.values())
        records.append(dict(key=unit['key'], sourceCid=unit['sourceCid'], originalSampling=unit,
            sourceUvAffine=dict(origin=origin, columnX=x, columnY=y),
            wrapOrder='frac local coordinates before affine mapping',
            triangleReflection=unit['triangle'], normalHalfTurn=unit['triangle'],
            nativeMaterial=native, packagedTextures={name:dict(cid=t,**texture_files[t]) for name,t in bindings.items()}))
    totals=[]
    for key, source in sources.items():
        group=[r for r in records if r['sourceCid']==key]
        textures={v['cid']:v for r in group for v in r['packagedTextures'].values()}
        totals.append(dict(sourceCid=key,units=len(group),triangularUnits=sum(r['triangleReflection'] for r in group),
            uniqueTextureFiles=len(textures),textureBytes=sum(v['bytes'] for v in textures.values())))
    all_files=[p for p in style.rglob('*') if p.is_file()]
    report=dict(status='MAPPED_NATIVE_SAMPLING_NOT_YET_PROVEN',style='Lift05',
        inventorySha256=sha(inventory_path.read_bytes()),repeatInventorySha256=sha(repeat_path.read_bytes()),
        sources=list(sources.values()),structureSamplers=records,repeatSamplers=repeat['units'],groups=totals,
        baseline=dict(files=len(all_files),bytes=sum(p.stat().st_size for p in all_files),
                      structureSurfaces=len(records),texturePayloadFiles=len(list(style.rglob('*.Texture')))),
        requirements=dict(regionLocalRepeat=True,triangleFold=True,halfTurnNormalCorrection=True,
                          regionFilteredMips=True,float32GradientPreservation=True),
        noBaking=True,assetsModified=False)
    output.mkdir(parents=True)
    (output/'source-sampling.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(groups=totals,baseline=report['baseline']),indent=2))


if __name__ == '__main__': main()
