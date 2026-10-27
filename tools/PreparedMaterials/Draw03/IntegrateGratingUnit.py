"""Bind the verified Draw03 single-period Grating unit without changing Clean pixels."""
import argparse
import json
import struct

from IntegrateCleanUnit import ROOT, PROOF, LIBRARY, string, identity, copy_exact
from PrepareNativeRebuild import digest, surface, payload_metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--integrate', action='store_true')
    args = parser.parse_args()
    stage = PROOF / 'grating-single-period'
    candidate = stage / 'output/Draw03'
    output = stage / 'integration-output/Draw03'
    formal = ROOT / 'assets/Materials/Draw03'
    library = json.loads(LIBRARY.read_bytes())
    unit = json.loads((stage/'report.json').read_bytes())['units'][0]
    manifest = json.loads((stage/'inputs/manifest.json').read_bytes())['units'][0]
    check = json.loads((stage/'native-check.json').read_bytes())
    assert unit['periods'] == [1, 1] and unit['id'] == 'Draw03/Grating'
    assert check['nativeHeaders'] and check['completeGraph'] and check['mips'] == 30
    assert all(x['maxLinearError'] <= .005 and x['maxRepeatSeamError'] <= .0001 for x in check['levels'])
    evidence = json.loads((PROOF/'candidate-manifest.json').read_bytes())
    measured = next(x for x in evidence['candidates'] if x['candidateId']==unit['id'])
    assert manifest['min']==measured['region']['min'] and manifest['max']==measured['region']['max']
    assert manifest['physicalSize']==measured['physicalSize'] and measured['boundaryEvidence']['nativeUResets']==38
    for p in (candidate/'Shared').iterdir():
        copy_exact(p, output/'Shared'/p.name)
    for channel in unit['channels']:
        p = output/'Shared'/(channel['cid']+'.Texture')
        assert identity(p.read_bytes())==channel['cid'] and p.stat().st_size==channel['bytes']
        header=payload_metadata(p.read_bytes(),'.Texture')
        assert (header['width'],header['height'],header['mipCount'])==(968,375,10)
        assert channel['sourceSpanX']==968 and channel['sourceSpanY']==374.96875
        assert channel['densityRatioX']>=1 and channel['densityRatioY']>=1
    original=(candidate/'RepeatUnits'/unit['key']/'Repeat.Surface').read_bytes()
    assert identity(original)==unit['surfaceCid']
    desc=surface(original,library)
    assert desc['template']==1 and desc['keywords']==['_TANGENTSPACE_OCTO']
    old=b'\xff'+struct.pack('<i',1)+string('_TANGENTSPACE_OCTO')
    assert original.endswith(old)
    keywords=['_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO']
    data=original[:2]+struct.pack('<i',14)+original[6:-len(old)]
    data+=b'\xff'+struct.pack('<i',len(keywords))+b''.join(string(k) for k in keywords)
    converted=surface(data,library)
    assert converted['textureProperties']==desc['textureProperties'] and converted['properties']==desc['properties']
    cid=identity(data); key=digest(string('BridgeBuilder/RepeatUnits/2')+string(cid))
    path=output/'RepeatUnits'/key/'Repeat.Surface'
    path.parent.mkdir(parents=True,exist_ok=True)
    assert not path.exists() or path.read_bytes()==data
    path.write_bytes(data);path.with_suffix('.Surface.cid').write_text(cid)
    files={p.relative_to(formal).as_posix():p for p in formal.rglob('*') if p.is_file()}
    baseline=json.loads((PROOF/'clean-integration/integration.json').read_bytes())
    for row in baseline['files']:
        assert digest(files[row['path']].read_bytes())==row['sha256'],'Changed Clean/native baseline'
    for p in output.rglob('*'):
        if p.is_file():
            name=p.relative_to(output).as_posix()
            assert name not in files or files[name].read_bytes()==p.read_bytes(),name
            files[name]=p
    local={}
    for name,p in files.items():
        if name.endswith('.cid'):
            asset=p.read_text().strip()
            assert asset not in local and name[:-4] in files
            local[asset]=files[name[:-4]]
        else:
            assert name+'.cid' in files
    builtins=set(json.loads((PROOF.parent/'rebuild-native-v1/graph-verification.json').read_bytes())['originalBuiltinReferences'])
    dependencies={c:surface(p.read_bytes(),library)['dependencies'] for c,p in local.items() if p.suffix=='.Surface'}
    reached=set(dependencies)
    for refs in dependencies.values():
        assert all(c in local or c in builtins for c in refs)
        reached.update(c for c in refs if c in local)
    assert reached==set(local)
    records=[dict(path=n,bytes=p.stat().st_size,sha256=digest(p.read_bytes())) for n,p in sorted(files.items())]
    total=sum(r['bytes'] for r in records)
    assert total<=10_000_000
    alias=dict(samplerKey=unit['aliases'][0],unitKey=key,template=14,surfaceCid=cid)
    if args.integrate:
        assert 'PASS native source descriptors: 1 descriptors' in (stage/'descriptor-check.log').read_text()
        for p in output.rglob('*'):
            if p.is_file():copy_exact(p,formal/p.relative_to(output))
        code=ROOT/'src/BridgeBuilder/Bridges/Types/Draw03/Draw03RepeatUnits.cs'
        content=code.read_text()
        insert=f'            case "{alias["samplerKey"]}":\n                unitKey = "{key}";\n                break;\n'
        if alias['samplerKey'] not in content:
            backup=ROOT/'artifacts/backups/Draw03/grating-single-period/Draw03RepeatUnits.cs'
            copy_exact(code,backup)
            assert content.count('        switch (samplerKey)\n        {\n')==1
            content=content.replace('        switch (samplerKey)\n        {\n','        switch (samplerKey)\n        {\n'+insert)
            code.write_text(content)
        assert insert in code.read_text()
        assert all(digest((formal/r['path']).read_bytes())==r['sha256'] for r in records)
        assert not any(p.is_dir() and not any(p.iterdir()) for p in formal.rglob('*'))
    report=dict(style='Draw03',integrated=args.integrate,completeStyle=False,alias=alias,
        fileCount=len(records),totalBytes=total,localSurfaces=len(dependencies),localTextures=sum(p.suffix=='.Texture' for p in local.values()),
        files=records,dependencies=dependencies,graphComplete=True,baselineCleanUnchanged=True,newPixelBakes=3,
        sourceBoundsPreserved=True,physicalPeriodPreserved=True,periods=unit['periods'],channels=unit['channels'],
        originalAtlasBoundaryEquivalent=check['originalAtlasBoundaryEquivalent'],gameVisualAcceptance=False,
        boundaryDisposition='Accepted existing-atlas pattern differences remain for human observation',
        pending=['LaneWear and BetweenLaneWear incomplete-period evidence', 'Unsupported structural mappings','Game preview/export/cold load'],
        localization='No UI text changed')
    (stage/'integration.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ('integrated','alias','fileCount','totalBytes','graphComplete','baselineCleanUnchanged')},indent=2))


if __name__=='__main__':main()
