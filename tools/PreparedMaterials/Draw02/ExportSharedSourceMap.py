"""Separate native Draw02 texture identities from exact per-use sampling records."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile


def digest(b): return hashlib.sha256(b).hexdigest()
def bits(v): return [struct.pack('<f', x).hex() for x in v]


def main(repo, game):
    inventory_path = repo/'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_bytes())
    units = [u for u in inventory['units'] if u['style'] == 'Draw02']
    native = json.loads((repo/'artifacts/prepared-materials/Draw02/unimodular-native-inputs.json').read_text())
    archives, entries = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in (game/'Content'/family).glob('*.cok'):
                if not path.name.startswith(('Blob', 'VT')): continue
                archive = zipfile.ZipFile(path); archives.append(archive)
                for name in archive.namelist():
                    if name.endswith('.cid'):
                        cid = archive.read(name).decode().strip()
                        entries.setdefault(cid, (archive, name[:-4]))
        def read(cid):
            archive, name = entries[cid]
            return archive.read(name)
        sources, payloads, channels = [], {}, {}
        for source in native['sources']:
            members = [u for u in units if u['sourceCid'] == source['cid']]
            raw = read(source['cid']); source_hash = digest(raw)
            assert all(u['sourceDigest'] == source_hash for u in members)
            description = next(s for s in inventory['sources'] if s['cid'] == source['cid'])
            source_channels = []
            for backing in source['backing']:
                raw_backing = read(backing['cid'])
                payloads[backing['cid']] = dict(cid=backing['cid'], kind=backing['kind'],
                    sha256=digest(raw_backing), bytes=len(raw_backing), provenance='built-in game archive',
                    packagedCopyRequired=False, path=entries[backing['cid']][1])
                for layer in backing['layers']:
                    if 'property' in layer:
                        name = layer['property']; stack = sublayer = None
                    else:
                        stack, sublayer = layer['stack'], layer['layer']
                        name = next(c['name'] for c in description['channels'] if c['stack'] == stack and c['layer'] == sublayer)
                    # A native container can own several genuinely distinct source channels.
                    # No sampler region, triangle, receiver, or source Surface digest belongs here.
                    identity = dict(backingSha256=digest(raw_backing), backingKind=backing['kind'],
                                    stack=stack, layer=sublayer, channel=name,
                                    colorSpace='sRGB' if name in ('_BaseColorMap','_EmissiveColorMap') else 'linear',
                                    normalEncoding='retain native packed representation' if name == '_NormalMap' else 'not normal',
                                    mipPolicy='retain complete native backing graph; no regenerated mips')
                    key = digest(json.dumps(identity, sort_keys=True, separators=(',', ':')).encode())
                    channels[key] = dict(identity=key, **identity, backingCid=backing['cid'], width=layer['width'], height=layer['height'])
                    source_channels.append(key)
            sources.append(dict(cid=source['cid'], sha256=source_hash, template=source['template'],
                                channelIdentities=source_channels, samplerCount=len(members),
                                nativeReference=True, originalSurfaceBytes=len(raw)))
        records=[]
        for u in units:
            records.append(dict(key=u['key'],sourceCid=u['sourceCid'],sourceDigest=u['sourceDigest'],
                channelIdentities=next(s['channelIdentities'] for s in sources if s['cid']==u['sourceCid']),
                triangle=u['triangle'],bounds=u['bounds'],a=u['a'],b=u['b'],c=u['c'],
                float32Bits={n:bits(u[n]) for n in ('bounds','a','b','c')},
                mapping='frac(q); if q.x+q.y>1 use 1-q; A+q.x*(B-A)+q.y*(C-A)' if u['triangle'] else 'bounds.xy+frac(q)*(bounds.zw-bounds.xy)',
                normalRule='when reflected, invert packed XY as in nativeCropSample' if u['triangle'] else 'retain source frame',
                nativeCapability='source UV reuse requires actual receiver containment and Float32 gradient proof; otherwise unsupported pending shared native sampler'))
        output=repo/'artifacts/prepared-materials/Draw02/shared-source-migration'
        output.mkdir(exist_ok=True)
        data=dict(schema='BridgeBuilder/Draw02SharedSourceAudit/1',inventorySha256=digest(inventory_path.read_bytes()),
                  style='Draw02',sources=sources,sourcePayloads=list(payloads.values()),textureChannels=list(channels.values()),samplers=records,
                  summary=dict(sources=len(sources),sharedNativeBackingPayloads=len(payloads),textureChannelIdentities=len(channels),
                               samplerRecords=len(records),triangleRecords=sum(u['triangle'] for u in units),
                               nativeBackingBytes=sum(p['bytes'] for p in payloads.values()),newTexturePayloads=0,
                               productionAssetsReplaced=0,completeNativeDependencyClosureVerified=False))
        (output/'source-sampler-map.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
        # One real nondegenerate source representative; no synthetic source measurements.
        representative=next(u for u in units if u['key']=='2383b25d3a91b6f24bda62dcbd91b93b687b0e2e4003cf43adcde823db94dcba')
        (output/'representative.json').write_text(json.dumps(representative,indent=2)+'\n')
        print(json.dumps(data['summary'],indent=2))
    finally:
        for archive in archives: archive.close()


if __name__=='__main__': main(Path(sys.argv[1]).resolve(),Path(sys.argv[2]).resolve())
