"""Stage reviewed BC7/RGBA native products; never rebake texture pixels."""
import hashlib
import json
import shutil
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
REPORTS=ROOT/'artifacts/prepared-materials/Draw01'
OUT=REPORTS/'reviewed-units'


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(raw):return hashlib.sha256(raw).hexdigest()
def cid(raw):return ''.join(f'{b&15:x}{b>>4:x}' for b in hashlib.sha256(raw).digest()[:16])
def cid_bytes(identity):return bytes(int(identity[i+1]+identity[i],16) for i in range(0,32,2))
def string(value):
    raw=value.encode();assert len(raw)<128
    return bytes([len(raw)])+raw


def main():
    inputs=REPORTS/'repeat-units-single-period'
    bc7=read(ROOT/'artifacts/shared-material-pipeline/native-bc7-check/three-report.json')['results']
    reviewed={(r['unit'],r['channel']):r for r in bc7 if r['channel']=='_BaseColorMap' or
              (r['unit']=='stationary-carriageway-cap' and r['channel']=='_NormalMap')}
    assert len(reviewed)==4
    lossless=read(inputs/'lossless-level22/report.json')['channels']
    products={(r['unit'],r['channel']):r for r in lossless}
    source_inputs=read(inputs/'manifest.json')['units'][0]['channels']
    source_aniso={c['name']:Path(c['path']).read_bytes()[13] for c in source_inputs}
    public_root=ROOT/'artifacts/shared-material-pipeline/native-bc7-check'
    public_products={p['name']:p for p in read(public_root/'public-report.json')['units'][0]['channels']}
    units=read(inputs/'report.json')['units']+read(inputs/'grating-descriptor/report.json')['units']
    destination=OUT/'output/Draw01'
    records=[];output_units=[];aliases={}
    for unit in units:
        unit=dict(unit)
        folder='grating-descriptor/Draw01' if unit['id']=='metal-grating' else 'candidate-output/Draw01'
        descriptor=(inputs/folder/'RepeatUnits'/unit['key']/'Repeat.Surface').read_bytes()
        channels=[]
        for product in unit['channels']:
            key=(unit['id'],product['name']);review=reviewed.get(key)
            if review:
                p=Path(review['path']);raw=p.read_bytes();assert sha(raw)==review['outputSha256']
                # Private BC7 probe headers defaulted to aniso=1; production must
                # retain the actual source setting without changing encoded mips.
                corrected=raw[:13]+bytes([source_aniso[product['name']]])+raw[14:]
                if unit['id']=='stationary-carriageway-cap':
                    p=public_root/'public-output/Draw01/Shared'/(public_products[product['name']]['cid']+'.Texture')
                    raw=p.read_bytes();assert raw==corrected
                else:
                    raw=corrected
                encoding='BC7 Best; lossy, explicitly reviewed'
                if product['name']=='_BaseColorMap':
                    assert all(e['alphaThresholdCrossings']==0 for e in review['error'])
            else:
                p=inputs/'lossless-level22'/(products[key]['newCid']+'.Texture');raw=p.read_bytes()
                assert cid(raw)==products[key]['newCid'] and products[key]['bitExact']
                encoding='RGBA32; native ZSTD22; mip bytes lossless'
            identity=cid(raw)
            old=cid_bytes(product['cid']);assert descriptor.count(old)==1
            descriptor=descriptor.replace(old,cid_bytes(identity))
            file=destination/'Shared'/(identity+'.Texture');file.parent.mkdir(parents=True,exist_ok=True)
            if file.exists():assert file.read_bytes()==raw
            else:file.write_bytes(raw);Path(str(file)+'.cid').write_text(identity)
            updated=dict(product,cid=identity,width=struct.unpack_from('<H',raw,2)[0],
                         height=struct.unpack_from('<H',raw,4)[0],mips=raw[8],bytes=len(raw))
            channels.append(updated)
            records.append(dict(unit=unit['id'],channel=product['name'],source=str(p),sha256=sha(raw),
                                cid=identity,bytes=len(raw),encoding=encoding,
                                validation=review if review else products[key]))
        identity=cid(descriptor)
        unitkey=sha(string('BridgeBuilder/RepeatUnits/2')+string(identity))
        file=destination/'RepeatUnits'/unitkey/'Repeat.Surface';file.parent.mkdir(parents=True,exist_ok=True)
        file.write_bytes(descriptor);Path(str(file)+'.cid').write_text(identity)
        unit.update(key=unitkey,surfaceCid=identity,channels=channels);output_units.append(unit)
        for alias in unit['aliases']:aliases[alias]=unitkey
    report=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Draw01',producedTextures=0,
                reusedTextures=16,units=output_units)
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    (OUT/'integration.json').write_text(json.dumps(dict(channels=records,aliases=aliases,
             textureBytes=sum(r['bytes'] for r in records),lossyTextures=4),indent=2)+'\n')
    print(json.dumps(dict(aliases=aliases,textureBytes=sum(r['bytes'] for r in records))))


if __name__=='__main__':main()
