"""Map exact Lift03 sampler records to real immutable source texture identities."""
from pathlib import Path
import json
import hashlib
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Lift03/shared-source-map'
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')

def digest(b): return hashlib.sha256(b).hexdigest()
def cid(b): return ''.join(f'{v & 15:x}{v >> 4:x}' for v in b)
def bits(values): return [struct.pack('<f', v).hex() for v in values]

def main():
    inv_path = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
    inv = json.loads(inv_path.read_text())
    decoded = json.loads((ROOT / 'artifacts/prepared-materials/Lift03/structure-channel-audit.json').read_text())
    assert decoded['inventoryDigest'] == digest(inv_path.read_bytes())
    units = [u for u in inv['units'] if u['style'] == 'Lift03']
    archives, entries = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for p in (GAME/family).glob('*.cok'):
                if not p.name.startswith(('Blob','VT')): continue
                z=zipfile.ZipFile(p);archives.append(z);names=set(z.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:
                        entries[z.read(name).decode().strip()] = (z,name[:-4],str(p))
        sources, textures = {}, {}
        for source in decoded['sources']:
            z,name,archive=entries[source['cid']];data=z.read(name)
            assert digest(data)==source['digest'] and data[6]==1 and data[283] in (0,255)
            combined=cid(data[284:300]) if data[283]==255 else None
            channels={}
            schema=next(s for s in inv['sources'] if s['cid']==source['cid'])
            for ch in source['decoded']:
                pos=next(c for c in schema['channels'] if c['name']==ch['name'])
                offset=7+76+136*pos['stack']+16*pos['layer']
                texture_cid=combined or cid(data[offset:offset+16]);tz,tn,ta=entries[texture_cid];payload=tz.read(tn)
                assert tn.endswith('.VTSurface') if combined else tn.endswith('.VTTexture')
                identity=ch['key']
                record={'decodedIdentity':identity,'channel':ch['name'],'format':ch['format'],
                        'width':ch['width'],'height':ch['height'],'decodedBaseMipBytes':ch['rawBytes'],
                        'nativeCid':texture_cid,'nativeArchive':ta,'nativeEntry':tn,
                        'nativePayloadBytes':len(payload),'nativePayloadSha256':digest(payload),'builtin':True,
                        'nativeStack':pos['stack'],'nativeLayer':pos['layer'],'combinedVtSurface':bool(combined)}
                if identity in textures: assert textures[identity]==record
                else: textures[identity]=record
                channels[ch['name']]=identity
            sources[source['cid']]={'sourceDigest':source['digest'],'template':schema['template'],
                'nativeArchive':archive,'nativeEntry':name,'channels':channels,
                'absentChannels':source['absent'],'unitCount':source['units']}
        records=[]
        for u in units:
            record={'productionUnitKey':u['key'],'sourceCid':u['sourceCid'],
                'channels':sources[u['sourceCid']]['channels'],'triangle':u['triangle'],
                'boundsBits':bits(u['bounds']),'aBits':bits(u['a']),'bBits':bits(u['b']),'cBits':bits(u['c']),
                'legacyPreparedMapping':'triangle-fold-after-local-frac' if u['triangle'] else 'rectangle-after-local-frac',
                'legacyPreparedNormalRule':'reverse tangent XY on reflected triangle half' if u['triangle'] else 'preserve source tangent encoding'}
            records.append(record)
        OUT.mkdir(parents=True,exist_ok=True)
        result={'style':'Lift03','inventorySha256':digest(inv_path.read_bytes()),'sources':sources,
                'textures':textures,'samplers':records,'implementationState':'mapping-only; native region-repeat capability unresolved',
                'unitCount':len(records),'triangles':sum(u['triangle'] for u in units),
                'uniqueDecodedSourceTextures':len(textures),'uniqueNativeTextureCids':len({v['nativeCid'] for v in textures.values()}),
                'nativeTexturePayloadBytes':sum({v['nativeCid']:v['nativePayloadBytes'] for v in textures.values()}.values()),
                'packagedSourceTextureBytesWhenBuiltinReferencesAreSupported':0,'candidateTextureFilesCreated':0}
        encoded=json.dumps(result,separators=(',',':')).encode();(OUT/'mapping.json').write_bytes(encoded)
        # Proposed data-only interchange, not a runtime resource or agreed loader ABI.
        ordered_sources=sorted(sources)
        packed=bytearray(b'BBS1'+struct.pack('<II',len(ordered_sources),len(records)))
        for source_cid in ordered_sources: packed.extend(bytes.fromhex(source_cid))
        for record in records:
            packed.extend(bytes.fromhex(record['productionUnitKey']))
            packed.extend(struct.pack('<HB',ordered_sources.index(record['sourceCid']),int(record['triangle'])))
            for field in (('aBits','bBits','cBits') if record['triangle'] else ('boundsBits',)):
                for value in record[field]: packed.extend(bytes.fromhex(value))
        (OUT/'samplers-proposed.bin').write_bytes(packed)
        print(json.dumps({k:v for k,v in result.items() if k not in ('sources','textures','samplers')},indent=2))
        print('Mapping JSON bytes:',len(encoded))
    finally:
        for z in archives:z.close()

if __name__=='__main__':main()
