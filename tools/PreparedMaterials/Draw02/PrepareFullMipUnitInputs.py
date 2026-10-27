"""Locate original Draw02 full-mip sources without regenerating or encoding pixels."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def sha(data): return hashlib.sha256(data).hexdigest()
def cid(data): return ''.join(f'{v & 15:x}{v >> 4:x}' for v in data)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository',type=Path)
    p.add_argument('game_data',type=Path)
    p.add_argument('output',type=Path)
    a=p.parse_args(); r=a.repository.resolve(); output=a.output.resolve()
    candidates_path=r/'artifacts/prepared-materials/Draw02/repeat-units-v2/units.json'
    candidates=json.loads(candidates_path.read_text())
    surface_path=r/'artifacts/prepared-materials/Draw02/shared-source-migration/source-descriptor-fixtures/8a28ab8d9526e258efbcb81c3bbc936b.Surface'
    surface=surface_path.read_bytes()
    assert sha(surface)==candidates['source']['sha256']
    assert struct.unpack_from('<Hi?',surface)==(1,1,True)
    names=['_BaseColorMap','_NormalMap','_MaskMap','_ControlMask']
    originals=[cid(surface[19+16*i:35+16*i]) for i in range(4)]
    vtids=[cid(surface[83+16*i:99+16*i]) for i in range(4)]
    assert surface[283]==255
    combined=cid(surface[284:300])
    targets=set(originals+vtids+[combined])
    # Include every built-in content family, not just Game and BridgesAndPorts.
    archives=sorted(a.game_data.rglob('*.cok'))
    relevant=[p for p in archives if p.name.startswith(('Blob','VT'))]
    matches={}; search=[]; control='4196445fff21f46eab3a80ed27da7dc6'
    for archive in relevant:
        matched=[]; sidecars=0
        with zipfile.ZipFile(archive) as z:
            for name in z.namelist():
                if not name.endswith('.cid'):continue
                sidecars+=1; identity=z.read(name).decode().strip()
                if identity not in targets and identity!=control:continue
                data=z.read(name[:-4]); entry=dict(archive=str(archive),entry=name[:-4],sha256=sha(data),bytes=len(data))
                matched.append(dict(cid=identity,**entry))
                if identity in matches and matches[identity][0]!=data:raise ValueError('Conflicting builtin CID '+identity)
                matches[identity]=(data,entry)
        search.append(dict(archive=str(archive),sidecars=sidecars,matches=matched))
    output.mkdir(parents=True,exist_ok=True)
    def save(identity):
        data,entry=matches[identity]; path=output/'source'/(identity+Path(entry['entry']).suffix)
        path.parent.mkdir(parents=True,exist_ok=True)
        if path.exists():assert path.read_bytes()==data
        else:path.write_bytes(data)
        return path,data,entry
    channels=[]
    for index,name in enumerate(names):
        original=originals[index]
        if original in matches:
            path,data,entry=save(original)
            channels.append(dict(name=name,cid=original,sha256=sha(data),path=str(path),kind='Texture',provenance=entry))
        elif vtids[index]!='0'*32:
            path,data,entry=save(vtids[index]);assert data[:3]==b'\x01\x00\xff' and cid(data[3:19])==original
            width,height=struct.unpack_from('<HH',data,23)
            channels.append(dict(name=name,cid=vtids[index],sha256=sha(data),path=str(path),kind='VTTexture',
                originalTextureCid=original,width=width,height=height,mips=data[29],provenance=entry))
        else:
            path,data,entry=save(combined);layer=106+index*48
            assert data[layer]!=0 and struct.unpack_from('<i',data,26)[0]==4096
            channels.append(dict(name=name,cid=combined,sha256=sha(data),path=str(path),kind='VTSurface',stack=0,layer=index,
                originalTextureCid=original,width=4096,height=4096,mips=13,provenance=entry,
                mipEvidence='Complete native resource retained; per-level decoder still required, 13 expected levels for 4096 square.'))
    descriptor=output/'source'/'8a28ab8d9526e258efbcb81c3bbc936b.Surface';descriptor.write_bytes(surface)
    units=[]
    for c in candidates['units']:
        if c['bands']:continue
        units.append(dict(id=c['id'],sourceCid=c['sourceCid'],sourceDigest=c['sourceSha256'],
            min=c['region']['min'],max=c['region']['max'],physicalSize=c['physicalSize'],
            evidence=dict(path=str(candidates_path),sha256=sha(candidates_path.read_bytes())),
            descriptor=dict(path=str(descriptor),sha256=sha(surface)),channels=channels,
            aliases=[k['key'] for k in c['legacyKeys']]))
    ready=all(c['kind']=='Texture' for c in channels)
    manifest=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Draw02',units=units)
    (output/('manifest.json' if ready else 'manifest-awaiting-full-vt-reader.json')).write_text(json.dumps(manifest,indent=2)+'\n')
    report=dict(style='Draw02',readyForCurrentProducer=ready,units=len(units),channels=channels,
        missingOriginalTextureCids=[c for c in originals if c not in matches],positiveLookupControl=matches.get(control,[None,None])[1],
        search=search,textureReencoding=False,oldBakeRead=False,
        requiredCapability=[] if ready else ['Full mip VTTexture reader for BaseColor/Normal/Mask','Full mip VTSurface reader for authored stack 0 layer 3 Control; zero replacement prohibited'],
        bandedUnitDeferred='Draw02/moving-carriageway requires three-band composition and is deliberately excluded from this unbanded trial manifest.')
    (output/'input-verification.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(ready=ready,units=len(units),sourceKinds=[c['kind'] for c in channels],missingOriginalTextureCids=report['missingOriginalTextureCids'],searchedArchives=len(search))))


if __name__=='__main__':main()
