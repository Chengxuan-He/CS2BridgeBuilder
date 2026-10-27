"""Prepare full authored Texture inputs for integral Draw01 source-unit trials."""
import hashlib
import json
import struct
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
OUT=ROOT/'artifacts/agent-material-env/Draw01/repeat-units-v2/inputs'
EVIDENCE=ROOT/'artifacts/prepared-materials/Draw01/genuine-unit-requirements/manifest.json'
CID='97291ab7f7938fc35cc670b15f2bca36'
def digest(b):return hashlib.sha256(b).hexdigest()
def cid(b):return ''.join(f'{x&15:x}{x>>4:x}' for x in b)
def write(path,raw):
    path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists():assert path.read_bytes()==raw,'Conflicting authoring input'
    else:path.write_bytes(raw)

def main():
    evidence=json.loads(EVIDENCE.read_text())
    source=next(x for x in evidence['roadCandidates'] if x['sourceCid']==CID)
    index={}
    archives=[]
    try:
        for file in sorted((GAME/'Content').rglob('*.cok')):
            if not file.name.startswith(('Blob','VT')):continue
            archive=zipfile.ZipFile(file);archives.append(archive)
            for entry in archive.namelist():
                if entry.endswith('.cid'):
                    identity=archive.read(entry).decode().strip()
                    if entry[:-4] in archive.NameToInfo:index.setdefault(identity,(archive,entry[:-4],file))
        archive,entry,file=index[CID];raw=archive.read(entry)
        assert digest(raw)==source['sourceDigest'] and struct.unpack_from('<Hi?',raw)==(1,1,True)
        descriptor=OUT/'source'/f'{CID}.Surface';write(descriptor,raw)
        channels=[];checks=[];missing=[]
        for i,name in enumerate(('_BaseColorMap','_NormalMap','_MaskMap','_ControlMask')):
            identity=cid(raw[19+i*16:35+i*16])
            backing=cid(raw[83+i*16:99+i*16])
            backing_record=None
            if backing!='0'*32:
                archive,entry,file=index[backing];vt_bytes=archive.read(entry)
                assert entry.endswith('.VTTexture') and cid(vt_bytes[3:19])==identity
                vt_path=OUT/'source'/f'{backing}.VTTexture';write(vt_path,vt_bytes)
                backing_record=dict(cid=backing,path=str(vt_path),archive=str(file),entry=entry,sha256=digest(vt_bytes),bytes=len(vt_bytes))
            if identity not in index:
                missing.append(dict(name=name,authoringTextureCid=identity,vtBacking=backing_record,
                    required='Full authored VT layer/mip decode, including combined VTSurface control when no standalone VTTexture exists.'))
                continue
            archive,entry,file=index[identity]
            assert entry.endswith('.Texture'),entry
            payload=archive.read(entry);version,w,h=struct.unpack_from('<HHH',payload)
            assert version==5 and payload[8]==13 and w==h==4096
            path=OUT/'source'/f'{identity}.Texture';write(path,payload)
            channels.append(dict(name=name,cid=identity,sha256=digest(payload),path=str(path)))
            checks.append(dict(name=name,cid=identity,archive=str(file),entry=entry,sha256=digest(payload),bytes=len(payload),width=w,height=h,mips=payload[8]))
        units=[];unsupported=[]
        for candidate in evidence['roadCandidates']:
            spans=[(b-a)*4096 for a,b in zip(candidate['region']['min'],candidate['region']['max'])]
            if any(x!=int(x) for x in spans):
                unsupported.append(dict(id=candidate['candidateId'],sourceTexelSpan=spans,reason='Fractional span; preserve exact boundaries, do not round.'))
                continue
            units.append(dict(id=candidate['candidateId'],sourceCid=CID,sourceDigest=digest(raw),
                min=candidate['region']['min'],max=candidate['region']['max'],physicalSize=candidate['proposedPhysicalPeriodMetres'],
                evidence=dict(path=str(EVIDENCE),sha256=digest(EVIDENCE.read_bytes())),
                descriptor=dict(path=str(descriptor),sha256=digest(raw)),channels=channels,aliases=candidate['legacyRequestKeys']))
        assert raw[283]==255,'Expected combined native VTSurface reference'
        combined=cid(raw[284:300])
        archive,entry,file=index[combined];combined_raw=archive.read(entry)
        write(OUT/'source'/f'{combined}.VTSurface',combined_raw)
        report=dict(style='Draw01',sourceChannels=checks,integralSpanCandidates=len(units),readyForBaker=not missing,
            missingAuthoringTextures=missing,scannedArchives=[a.filename for a in archives],indexedNativeCids=len(index),
            combinedSource=dict(cid=combined,archive=str(file),entry=entry,sha256=digest(combined_raw),bytes=len(combined_raw)),unsupported=unsupported,
            boundaryAcceptance=False,scope='Original VT bytes retained; full-mip conversion is required where authoring Texture assets are absent. Original single-sided template 1 is retained.')
        OUT.mkdir(parents=True,exist_ok=True)
        input_name='manifest.json' if not missing else 'manifest-requires-vt-decoder.json'
        (OUT/input_name).write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2',style='Draw01',units=units),indent=2)+'\n')
        # The shared offline adapter can reconstruct complete authored BC7 mip blocks.
        # Keep the original carriers as inputs; these full atlases are never packaged.
        vt_channels=list(channels)
        for index,missing_channel in enumerate(missing):
            name=missing_channel['name'];backing=missing_channel['vtBacking']
            layer=('_BaseColorMap','_NormalMap','_MaskMap','_ControlMask').index(name)
            vt_channels.append(dict(name=name,cid=backing['cid'] if backing else combined,
                sha256=backing['sha256'] if backing else digest(combined_raw),
                path=backing['path'] if backing else str(OUT/'source'/f'{combined}.VTSurface'),
                kind='VTTexture' if backing else 'VTSurface',stack=0,layer=layer))
        for unit in units:unit['channels']=vt_channels
        (OUT/'manifest-vt-sources.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2',style='Draw01',units=units),indent=2)+'\n')
        delivery=ROOT/'artifacts/prepared-materials/Draw01/repeat-units-v2';delivery.mkdir(parents=True,exist_ok=True)
        (delivery/'input-verification.json').write_text(json.dumps(report,indent=2)+'\n')
        print(json.dumps(report,indent=2))
    finally:
        for archive in archives:archive.close()

if __name__=='__main__':main()
