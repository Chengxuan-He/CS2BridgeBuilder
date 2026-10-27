"""Integrate evidenced structural unit descriptors; never rebake or duplicate pixels."""
import collections
import json
import struct
from IntegrateCleanUnit import ROOT, PROOF, LIBRARY, identity, copy_exact
from PrepareNativeRebuild import digest, surface


def run():
    out=PROOF/'structure-groups'
    manifest=json.loads((out/'manifest.json').read_bytes())
    audit=json.loads((out/'audit.json').read_bytes())
    assert audit['originalDomains']==224342 and audit['genuineUnits']==1
    unit=manifest['units'][0]; assert unit['id']=='Grating'
    candidate=PROOF/unit['stage']/'output/Draw03'
    formal=ROOT/'assets/Materials/Draw03'
    lib=json.loads(LIBRARY.read_bytes())
    assert 'PASS native source descriptors: 1 descriptors' in (out/'native-descriptor.log').read_text()
    bbn=(ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03RoadAppearance_cs.bin').read_bytes()
    ni,nl,nu,nul,nf,nd=struct.unpack_from('<6i',bbn,8)
    floats=struct.unpack_from('<'+str(nf)+'f',bbn,32+4*ni+8*nl+4*nu+8*nul)
    assert list(floats[11:15])==unit['bounds']
    p=candidate/'RepeatUnits'/unit['bindingKey']/'Repeat.Surface'
    data=p.read_bytes(); cid=p.with_suffix('.Surface.cid').read_text().strip()
    assert identity(data)==cid
    desc=surface(data,lib); assert desc['template']==1 and desc['keywords']==['_TANGENTSPACE_OCTO']
    dependencies=desc['dependencies']
    # Every pixel payload must already be present in the formal style, byte-identical.
    before={f.relative_to(formal).as_posix():digest(f.read_bytes()) for f in formal.rglob('*') if f.is_file()}
    for ref in dependencies:
        source=candidate/'Shared'/(ref+'.Texture')
        installed=formal/'Shared'/(ref+'.Texture')
        assert source.read_bytes()==installed.read_bytes()
        assert installed.with_suffix('.Texture.cid').read_text().strip()==ref
    for source in (p,p.with_suffix('.Surface.cid')):
        copy_exact(source, formal/source.relative_to(candidate))
    assert all(digest((formal/name).read_bytes())==sha for name,sha in before.items())
    local={}
    files=[]
    for f in formal.rglob('*'):
        if not f.is_file():continue
        files.append(dict(path=f.relative_to(formal).as_posix(),bytes=f.stat().st_size,sha256=digest(f.read_bytes())))
        if f.suffix=='.cid':
            key=f.read_text().strip(); assert key not in local
            local[key]=f.with_suffix(''); assert local[key].is_file()
        else: assert f.with_suffix(f.suffix+'.cid').is_file()
    builtins=set(json.loads((PROOF.parent/'rebuild-native-v1/graph-verification.json').read_bytes())['originalBuiltinReferences'])
    graph={key:surface(p.read_bytes(),lib)['dependencies'] for key,p in local.items() if p.suffix=='.Surface'}
    referenced=set(graph)
    for refs in graph.values():
        assert all(ref in local or ref in builtins for ref in refs)
        referenced.update(ref for ref in refs if ref in local)
    assert referenced==set(local)
    assert not any(p.is_dir() and not any(p.iterdir()) for p in formal.rglob('*'))
    totals=collections.Counter()
    for row in audit['groups']: totals[row['kind']]+=row['domains']
    report=dict(style='Draw03',integrated=True,completeStyle=False,newTexturePayloads=0,newPixelBakes=0,
        structuralDescriptor=dict(key=unit['bindingKey'],cid=cid,template=desc['template'],dependencies=dependencies),
        totalFiles=len(files),totalBytes=sum(f['bytes'] for f in files),localSurfaces=len(graph),
        localTextures=sum(p.suffix=='.Texture' for p in local.values()),dependencyGraphComplete=True,
        originalDomains=audit['originalDomains'],domainDisposition=dict(totals),
        changedReceiverCoverageGain=0, completeGeneratedOwnerBytes=None,
        obsoleteZeroReferencePayloads=[],allPreviousFilesUnchanged=True,files=files,
        localization='No UI strings or runtime errors added; technical material identities remain unlocalized.')
    assert report['totalBytes']<=10_000_000
    (out/'integration.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:report[k] for k in ('totalFiles','totalBytes','localSurfaces','localTextures','newPixelBakes','changedReceiverCoverageGain')}))


if __name__=='__main__':run()
