"""Install native-validated Lift05 structural semantic units without replacing existing assets."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
E=ROOT/'artifacts/prepared-materials/Lift05/structure-groups';source=E/'output/Lift05';target=ROOT/'assets/Materials/Lift05'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
old={str(p.relative_to(target)):sha(p) for p in target.rglob('*') if p.is_file()};bake=json.loads((E/'bake.json').read_text());added=[]
for unit in bake['units']:
 check=json.loads((E/(unit['id']+'-native-check.json')).read_text());assert check['nativeHeaders'] and check['completeGraph'] and check['unitKey']==unit['key']
 assert len(check['levels'])==sum(x['mips'] for x in unit['channels'])
 for c in unit['channels']:assert c['densityRatioX']==c['densityRatioY']==1
for p in source.rglob('*'):
 if not p.is_file():continue
 q=target/p.relative_to(source)
 if q.exists():assert sha(p)==sha(q);continue
 q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,q);assert sha(p)==sha(q);added.append(str(q.relative_to(target)))
assert all(sha(target/k)==h for k,h in old.items())
files=[p for p in target.rglob('*') if p.is_file()]
regions=json.loads((E/'receivers.json.regions.json').read_text());metrics={k:sum(x[k] for x in regions) for k in ['receiverUnits','needsPrepared','containedDomains','mappedUnits','additionalPreparedCoverage']}
report=dict(status='INTEGRATED_NATIVE_VALIDATED_FINITE_COVERAGE',files=len(files),bytes=sum(p.stat().st_size for p in files),oldFilesUnchanged=len(old),added=added,metrics=metrics,actualCompleteGeneratedBridgeBytes=None,completeGeneration=False,limitations=['Two genuine semantic units only; nonperiodic and unresolved classes remain in audit.json.','Native saved-owner graph, previews, export, cold load, deletion independence and filtered game appearance remain integration/human acceptance.'],filesSha256={str(p.relative_to(target)):sha(p) for p in files})
(E/'installed.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k] for k in ['files','bytes','oldFilesUnchanged','metrics']}))
