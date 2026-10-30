"""Verify the shared CID closure proposal; retire only reviewed Draw03 old road aliases."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil

ROOT=Path(__file__).resolve().parents[3]

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--retire',action='store_true');args=parser.parse_args()
    plan=next(r for r in json.loads((ROOT/'artifacts/material-remap/shared/outputs/legacy-alias-cleanup-plan.json').read_text()) if r['style']=='Draw03')
    candidates=plan['candidateLegacyRoadAliases']
    descriptors={ROOT/r['descriptor'] for r in candidates}
    survivors=list((ROOT/'assets/Materials/Draw03').rglob('*.Surface'))
    survivors=[p for p in survivors if p not in descriptors]
    retired=set(descriptors);kept={}
    for row in candidates:
        assert Path(str(ROOT/row['descriptor'])+'.cid').read_text().strip()==row['surfaceCid']
        for dep in row['dependencies']:
            payload=ROOT/dep['path'];cid=dep['cid']
            assert payload.stat().st_size+Path(str(payload)+'.cid').stat().st_size==dep['bytes']
            assert Path(str(payload)+'.cid').read_text().strip()==cid
            encoded=bytes(int(cid[i:i+2][::-1],16) for i in range(0,32,2))
            users=[str(p.relative_to(ROOT)) for p in survivors if encoded in p.read_bytes()]
            # Conservative binary membership includes provenance refs too: never
            # discard a payload when any retained descriptor still names it.
            if users: kept[cid]=users
            else: retired.add(payload)
    retired|={Path(str(p)+'.cid') for p in list(retired)}
    target=ROOT/'artifacts/retired/Draw03-legacy-road-aliases'
    records=[]
    for path in sorted(retired):
        resolved=path.resolve();assert resolved.is_relative_to((ROOT/'assets/Materials/Draw03').resolve())
        data=path.read_bytes();relative=path.relative_to(ROOT)
        records.append(dict(path=str(relative),bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
        if args.retire:
            dest=target/relative;assert dest.resolve().is_relative_to(target.resolve())
            assert not dest.exists();dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.move(str(path),str(dest));assert dest.read_bytes()==data
    if args.retire:
        for directory in sorted((ROOT/'assets/Materials/Draw03').rglob('*'),key=lambda p:len(p.parts),reverse=True):
            if directory.is_dir() and not any(directory.iterdir()):directory.rmdir()
    report=dict(retired=args.retire,files=records,bytes=sum(r['bytes'] for r in records),retainedDependencies=kept,
        route='Current Draw03 Near/Far/Band always supply completed Surface + PreparedUnit/SourceUv. Compose assigns Prepared before legacy alias stage. StructuralRepeat keys are distinct from these RoadRepeat keys. Draw03Regions b2b2715 remains retained.',
        ownerBudget='Not measured: package reachability cleanup is not complete generated-owner measurement.')
    (ROOT/'artifacts/material-remap/Draw03/legacy-alias-retirement.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(retired=args.retire,files=len(records),bytes=report['bytes'],retainedDependencyCount=len(kept))))

if __name__=='__main__':main()
