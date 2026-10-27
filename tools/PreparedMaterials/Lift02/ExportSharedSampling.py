"""Describe original Lift02 texture identities separately from sampler records."""
from collections import defaultdict
import io
import json
from pathlib import Path
import struct
import zipfile
from IntegrateStructures import ROOT, sha, cid, read_string, count

OUT = ROOT / 'artifacts/prepared-materials/Lift02/source-sharing'
INVENTORY = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')


def descriptor(data):
    r = io.BytesIO(data)
    version, template, vt = struct.unpack('<Hi?', r.read(7))
    assert version == 1
    header = None
    vt_cid = None
    if vt:
        header = r.read(276)
        p = r.read(1)[0]
        assert p in (0, 255)
        if p:
            vt_cid = cid(r.read(16))
    for size in (4, 4, 16, 16):
        for _ in range(count(r)):
            read_string(r)
            r.read(size)
    bindings = {}
    for _ in range(count(r)):
        name = read_string(r)
        p = r.read(1)[0]
        assert p in (0, 255)
        if p:
            bindings[name] = cid(r.read(16))
    keywords = [read_string(r) for _ in range(count(r))]
    assert r.tell() == len(data)
    return template, vt, header, vt_cid, bindings, keywords


def main():
    inventory = json.loads(INVENTORY.read_bytes())
    native = json.loads((OUT / 'native-sources.json').read_bytes())
    assert native['inventorySha256'] == sha(INVENTORY.read_bytes())
    units = [u for u in inventory['units'] if u['style'] == 'Lift02']
    grouped = defaultdict(list)
    for unit in units:
        grouped[unit['sourceCid']].append(unit)
    wanted = set(grouped)
    archives, entries = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in (GAME / family).glob('*.cok'):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                archive = zipfile.ZipFile(path)
                archives.append(archive)
                names = set(archive.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        identity = archive.read(name).decode().strip()
                        entries[identity] = (archive, name[:-4])
        texture_rows = {}
        source_rows = []
        physical_sources = {}
        for source in native['sources']:
            source_id = source['cid']
            archive, name = entries[source_id]
            data = archive.read(name)
            assert sha(data) == source['descriptorSha256']
            template, vt, header, vt_id, bindings, keywords = descriptor(data)
            description = next(s for s in inventory['sources'] if s['cid'] == source_id)
            native_channels = []
            for ch in source['channels']:
                slot = next((s for s in description['channels'] if s['name'] == ch['name']), None)
                if vt and slot is not None:
                    stack, layer = slot['stack'], slot['layer']
                    fallback = cid(header[76 + stack * 136 + layer * 16:92 + stack * 136 + layer * 16])
                    # VTSurface decoded layers win; explicit stack texture fills absent layers.
                    active = False
                    if vt_id:
                        va, vn = entries[vt_id]
                        with va.open(vn) as stream:
                            vh = stream.read(498)
                        active = vh[106 + stack * 192 + layer * 48] != 0
                    payload_id = vt_id if active else fallback
                    provenance = dict(kind='VTSurface' if active else 'VTTexture', cid=payload_id, stack=stack, layer=layer)
                else:
                    payload_id = bindings[ch['name']]
                    provenance = dict(kind='Texture', cid=payload_id)
                pa, pn = entries[payload_id]
                info = pa.getinfo(pn)
                physical_sources[payload_id] = dict(cid=payload_id, path=pn, archive=pa.filename, bytes=info.file_size)
                shared_id = ch['textureIdentity']
                if shared_id not in texture_rows:
                    texture_rows[shared_id] = dict(**ch, sourceUses=[], samplers=[])
                row = texture_rows[shared_id]
                row['sourceUses'].append(dict(surfaceCid=source_id, native=provenance))
                row['samplers'].extend(u['key'] for u in grouped[source_id])
                native_channels.append(dict(name=ch['name'], sharedTextureIdentity=shared_id, native=provenance))
            source_rows.append(dict(cid=source_id, descriptorSha256=sha(data), template=template, vt=vt,
                                    archive=archive.filename, entry=name, bytes=len(data), channels=native_channels, keywords=keywords))
        samplers = []
        for u in units:
            source = next(s for s in source_rows if s['cid'] == u['sourceCid'])
            samplers.append(dict(key=u['key'], sourceCid=u['sourceCid'], sourceDigest=u['sourceDigest'],
                                 triangle=u['triangle'], bounds=u['bounds'], a=u['a'], b=u['b'], c=u['c'],
                                 previousGeneratorMapping='q=frac(localUV); if q.x+q.y>1 then q=1-q; sourceUV=A+q.x*(B-A)+q.y*(C-A)' if u['triangle'] else 'q=frac(localUV); sourceUV=bounds.xy+q*(bounds.zw-bounds.xy)',
                                 previousGeneratorNormalRule='RG/AG decode and XY negation on folded half' if u['triangle'] else 'Preserve native normal encoding',
                                 nativeSourceAuthority='Original Surface, native UV region, texture settings and full native mip graph; previous generator folding is not presumed prototype semantics',
                                 supportedNativeMapping='SourceUv(localUV) only when actual receiver region and Float32 gradients pass CanUseSource',
                                 sharedTextures=[c['sharedTextureIdentity'] for c in source['channels']]))
        differing = [p for p in native['probes'] if max(p['maximumDifferenceByCoordinate']) > .001]
        baseline = json.loads((OUT.parent / 'structure-integration.json').read_bytes())
        result = dict(schema='Lift02/SharedSourceSampling/1', status='Source map complete; native sampler implementation required',
                      inventorySha256=sha(INVENTORY.read_bytes()), textureIdentityBasis='Decoded content, semantic channel, format, dimensions and available mip bytes; excludes sampling region and material descriptor identity',
                      baseline=dict(samplerKeys=baseline['productionKeys'], derivedTextureFiles=baseline['uniqueStructureTextures'], files=baseline['selectedFiles'], bytes=baseline['selectedBytes']),
                      counts=dict(samplers=len(samplers), sourceSurfaces=len(source_rows), decodedChannelUses=sum(len(s['channels']) for s in source_rows), uniqueDecodedTextureIdentities=len(texture_rows), physicalNativeSourcePayloads=len(physical_sources), physicalNativeSourceBytes=sum(p['bytes'] for p in physical_sources.values()), differingProbeRows=len(differing)),
                      productionReplacementReady=False, textureOutputs=0, bakedTextures=0,
                      limitations=['Native VT decoder exposes only base level; original full VT filtered mip semantics are not validated by this probe.', 'No supported native region-frac/triangle-fold material implementation is identified; ordinary affine UV substitution demonstrably differs.', 'Physical original-source bytes are an asset-graph measurement, not a candidate SDK package size; compatible built-in references would add zero packaged source payload bytes.'],
                      sources=source_rows, physicalSources=list(physical_sources.values()), sharedTextures=list(texture_rows.values()), samplers=samplers)
        (OUT / 'shared-sampling-map.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
        print(json.dumps(result['counts'], indent=2))
    finally:
        for archive in archives:
            archive.close()


if __name__ == '__main__':
    main()
