"""Validate final Lift03 domain references and exact prototype boundary coordinates."""
import hashlib,json,sys
from collections import Counter
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).parent))
from ExportRecipes import numbers
folder=ROOT/'artifacts/prepared-materials/Lift03/native-charts'
report=json.loads((folder/'runtime-integration.json').read_text())
resource=ROOT/report['resource'];data=numbers(resource)
actual_sha=hashlib.sha256(resource.read_bytes()).hexdigest()
overrides={}
if actual_sha!=report['resourceSha256']:
    candidate=folder.parent/'exact-union-chart-candidate'
    integration=json.loads((candidate/'integration.json').read_text())
    assert actual_sha==integration['candidateSha256'] and report['resourceSha256']==integration['baselineSha256']
    payload=(candidate/'chart-overrides.json').read_bytes()
    assert hashlib.sha256(payload).hexdigest()==integration['overrideSha256']
    overrides={(r['render'],r['mesh'],r['originalDomain']):r['charts'] for r in json.loads(payload)}
I,F=data['I32'],data['F64'];assert tuple(I[:2])==(1,len(report['streams']))
checked=domains=0
for si,stream in enumerate(report['streams']):
    source=json.loads((folder/(stream['stream']+'.json')).read_text())
    raw=numbers(folder/(stream['stream']+'.bin'));uv=list(zip(raw['F32'][::2],raw['F32'][1::2]))
    table=I[si+2];assert I[table]==len(source['domains'])
    unique={}
    for di,domain in enumerate(source['domains']):
        at=I[table+di+1];refs=I[at+1:at+1+I[at]]
        expected=[c for c in domain['charts'] if str(c) not in stream['rejected']]
        override=overrides.get((stream['render'],stream['mesh'],di))
        if override is not None:
            assert not expected and len(refs)==len(override)
            restored=[]
            for offset in refs:
                rings=[]
                for li in range(I[offset]):
                    fa,n=I[offset+1+li*2:offset+3+li*2]
                    rings.append([list(p) for p in zip(F[fa:fa+n*2:2],F[fa+1:fa+n*2:2])])
                restored.append(rings)
            assert restored==override
        else:
            assert len(refs)==len(expected)
        for c,offset in zip(expected,refs):
            if c in unique:assert unique[c]==offset;continue
            unique[c]=offset
            boundary=Counter()
            for li in range(I[offset]):
                fa,n=I[offset+1+li*2:offset+3+li*2]
                ring=list(zip(F[fa:fa+n*2:2],F[fa+1:fa+n*2:2]))
                assert len(ring)>=3 and ring[0]!=ring[-1]
                boundary.update(zip(ring,ring[1:]+ring[:1]))
            original=Counter((uv[a],uv[b]) for a,b in source['charts'][c]['boundary'])
            assert boundary==original,(stream['stream'],c)
            checked+=1
        domains+=1
assert checked==report['acceptedCharts'] and domains==report['domains']
result=dict(status='PASS',domains=domains,charts=checked,recoveredDomains=len(overrides),resourceSha256=actual_sha,exactOriginalBoundaryEdges=True,domainOrderPreserved=True,noCrossStreamReferences=True)
(folder/'runtime-data-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))

