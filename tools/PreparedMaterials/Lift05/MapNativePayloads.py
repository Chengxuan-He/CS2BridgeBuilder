"""Resolve measured Lift05 source channels to real native payload identities."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile


def main():
    p=argparse.ArgumentParser();p.add_argument('mapping',type=Path);p.add_argument('game',type=Path);p.add_argument('output',type=Path)
    args=p.parse_args();mapping=json.loads(args.mapping.read_text());channels=[]
    zero='0'*32
    for source in mapping['sources']:
        native=source['native']
        for name,original in native['textures'].items():
            if not original or original==zero: continue
            item=dict(sourceSurfaceCid=source['cid'],channel=name,originalTextureCid=original,
                      srgb=name in ('_BaseColorMap','_EmissiveColorMap','_InteriorAtlas'),
                      normalEncoding='original native octahedral/tangent-space semantics' if name=='_NormalMap' else None)
            slots=[(stack,layer) for stack in native['stacks'] for layer,cid in enumerate(stack['originalTextureCids']) if cid==original]
            if slots:
                assert len(slots)==1
                stack,layer=slots[0];item.update(payloadCid=stack['vtTextureCids'][layer],kind='VTTexture',
                    stack=stack['index'],layer=layer,sourceDimensions=stack['dimensions'])
            else: item.update(payloadCid=original,kind='Texture')
            channels.append(item)
    wanted={c['payloadCid'] for c in channels};found={}
    for family in ('Game','BridgesAndPorts'):
        for path in (args.game/'Content'/family).glob('*.cok'):
            if not path.name.startswith(('Blob','VT')):continue
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.cid'):continue
                    cid=archive.read(entry).decode().strip()
                    if cid not in wanted:continue
                    raw=archive.read(entry[:-4]);digest=hashlib.sha256(raw).hexdigest()
                    if cid in found: assert found[cid]['sha256']==digest
                    else: found[cid]=dict(cid=cid,bytes=len(raw),sha256=digest,archive=str(path),entry=entry[:-4])
    assert found.keys()==wanted, f'Missing source payloads: {wanted-found.keys()}'
    report=dict(status='SOURCE_PAYLOADS_VERIFIED',channels=channels,payloads=list(found.values()),
                uniqueNativePayloads=len(found),nativeSerializedBytes=sum(v['bytes'] for v in found.values()),
                caveat='Native source bytes are not a proposed package measurement: compatible built-in assets remain referenced; a required conversion, sampler support and complete mip dependency graph remain unproven',
                bakedTextures=0,assetsModified=False)
    assert not args.output.exists();args.output.write_text(json.dumps(report,indent=2)+'\n')
    print(f"Lift05 source identities: {len(channels)} channel records, {len(found)} real native payloads, {report['nativeSerializedBytes']} bytes; no texture creation.")


if __name__=='__main__':main()
