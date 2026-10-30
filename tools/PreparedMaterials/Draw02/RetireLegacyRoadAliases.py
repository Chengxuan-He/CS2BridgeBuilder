"""Retire unreachable RoadRepeat aliases; preserve every surviving CID reference."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
STYLE = ROOT/'assets/Materials/Draw02'
RETIRED = ROOT/'artifacts/retired/material-remap/Draw02/legacy-road-aliases'

def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--retire', action='store_true'); args=parser.parse_args()
    route=ROOT/'src/BridgeBuilder/Bridges/Types/Draw02/Draw02RoadSampling.cs'
    text=route.read_text()
    assert 'Draw02RoadAppearance.' not in text and 'Draw02AppearanceSupport.ForLod' not in text
    assert 'MovableRoadAppearance.Type.None' in text
    proof=ROOT/'artifacts/material-remap/Draw02/source-face-coverage-A51EE9448EBE8AAEA20314831C1677D929E1DFBFF93A75CED63D3A4BD9463B45.txt'
    assert '38448 measured source-face uses' in proof.read_text() and 'uncovered/legacy findings=0' in proof.read_text()
    plan=next(p for p in json.loads((ROOT/'artifacts/material-remap/shared/outputs/legacy-alias-cleanup-plan.json').read_text()) if p['style']=='Draw02')
    candidates=plan['candidateLegacyRoadAliases']; assert len(candidates)==5
    descriptors={ROOT/c['descriptor'] for c in candidates}
    surfaces=list(STYLE.rglob('*.Surface')); survivors=[s for s in surfaces if s not in descriptors]
    retired=set(descriptors); retained={}; reverse={}
    for entry in candidates:
        source=ROOT/entry['descriptor']; assert source.is_file()
        assert Path(str(source)+'.cid').read_text().strip()==entry['surfaceCid']
        encoded=bytes(int(entry['surfaceCid'][i:i+2][::-1],16) for i in range(0,32,2))
        assert not any(encoded in s.read_bytes() for s in survivors), 'Retained Surface references retired alias'
        for dep in entry['dependencies']:
            payload=ROOT/dep['path']; cid=dep['cid']
            assert Path(str(payload)+'.cid').read_text().strip()==cid
            encoded=bytes(int(cid[i:i+2][::-1],16) for i in range(0,32,2))
            users=[str(p.relative_to(ROOT)) for p in surfaces if encoded in p.read_bytes()]
            assert sorted(users)==sorted(dep['consumers'])
            remaining=[str(p.relative_to(ROOT)) for p in survivors if encoded in p.read_bytes()]
            reverse[cid]=users
            if remaining: retained[cid]=remaining
            else: retired.add(payload)
    # No source method or embedded binding names any alias key/CID explicitly.
    files=list((ROOT/'src/BridgeBuilder').rglob('*.cs'))+list((ROOT/'assets/HardcodedNumbers/Draw02').glob('*.bin'))
    for entry in candidates:
        for path in files:
            assert entry['key'].encode() not in path.read_bytes(), str(path)
    retired|={Path(str(p)+'.cid') for p in list(retired)}
    records=[]
    for path in sorted(retired):
        assert path.resolve().is_relative_to(STYLE.resolve())
        data=path.read_bytes(); relative=path.relative_to(STYLE)
        target=RETIRED/relative
        assert target.resolve().is_relative_to(RETIRED.resolve()) and not target.exists()
        records.append(dict(path=str(path.relative_to(ROOT)),bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
    if args.retire:
        for path in sorted(retired):
            target=RETIRED/path.relative_to(STYLE); target.parent.mkdir(parents=True,exist_ok=True)
            shutil.move(str(path),str(target))
            expected=next(r for r in records if r['path']==str(path.relative_to(ROOT)))
            assert hashlib.sha256(target.read_bytes()).hexdigest()==expected['sha256']
        for directory in sorted((STYLE/'RepeatUnits').rglob('*'),key=lambda p:len(p.parts),reverse=True):
            if directory.is_dir() and not any(directory.iterdir()): directory.rmdir()
    report=dict(retired=args.retire,files=records,bytes=sum(r['bytes'] for r in records),retainedDependencies=retained,
        reverseReferences=reverse,sourceCoverageProof=str(proof),routeSha256=hashlib.sha256(route.read_bytes()).hexdigest(),
        route='Initial fallback has one None domain. All near/mid measured car/walk domains are supplied by explicit SourceUv/Prepared Levels. Far creates its own finite Level. Unsupported types preserve selected input; physical partitions are unchanged.',
        quality='Not accepted',completeOwnerBudget='Not measured')
    (ROOT/'artifacts/material-remap/Draw02/legacy-alias-retirement.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(retired=args.retire,files=len(records),bytes=report['bytes'],retainedTextures=len(retained))))

if __name__=='__main__': main()
