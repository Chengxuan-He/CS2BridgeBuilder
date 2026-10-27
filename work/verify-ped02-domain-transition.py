import sys,json,gzip,hashlib,collections
from pathlib import Path
import numpy as np
root=Path.cwd();sys.path.insert(0,str(root/'tools/PreparedMaterials/PedestrianDraw02'))
from ExportRecipes import numbers
from BuildUvSampling import overlaps
from MovableUvDomainFormat import decode_arrays
base=root/'artifacts/prepared-materials/PedestrianDraw02';old=json.loads((root/'work/ped02-domain-fix-originals/classification.json').read_text());new=json.loads((base/'non-tiling/classification.json').read_text());native=np.load(base/'source-consolidation/native-charts.npz');charts=json.loads((base/'source-consolidation/native-charts.json').read_text());changes={};true_count=0;repeat_count=0
for before,after in zip(old['meshes'],new['meshes']):
    assert (before['render'],before['mesh'])==(after['render'],after['mesh'])
    def accepted(r):return {d for reason,ds in r['classifications'].items() if reason in ('native-uv0-finite-authored-mapping','native-window-local-yz') for d in ds}
    prev,now=accepted(before),accepted(after);assert not prev-now
    added=now-prev;assert added<=set(before['classifications'].get('density-existing-road-repeat-overlap',[]))
    values=numbers(root/'assets/HardcodedNumbers/PedestrianDraw02'/('UvDomain_'+after['render'].replace(' ','_')+'_'+str(after['mesh'])+'.bin'));raw,_=decode_arrays(values['I32'],values['F32']);at=2;nr=next(r for r in charts['records'] if (r['name'],r['mesh'])==(after['render'],after['mesh']));prefix=nr['arrayPrefix'];uv=native[prefix+'_uv'];faces=native[prefix+'_faces']
    for d in range(raw[1]):
        count=abs(int(raw[at+3]));at+=4;members=raw[at:at+count];at+=count
        hit=after['sourceDigest']=='5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219' and any(overlaps(t,new['genuineRepeatRegion']) for t in uv[faces[members]].astype(float))
        if hit:assert d not in now;repeat_count+=1
        if d in added:assert not hit
    changes[(after['render'],after['mesh'])]=added;true_count+=len(now)
assert sum(map(len,changes.values()))==475 and true_count==47707 and repeat_count==270
old_i=numbers(root/'work/ped02-domain-fix-originals/PedestrianDraw02UvSampling.bin')['I32'];new_i=numbers(root/'assets/HardcodedNumbers/PedestrianDraw02/PedestrianDraw02UvSampling.bin')['I32'];assert len(old_i)==len(new_i)
assert collections.Counter((a,b) for a,b in zip(old_i,new_i) if a!=b)=={(0,1):475}
rows=[json.loads(s) for s in gzip.open(root/'artifacts/prepared-materials/shared-nontiling/actual-final/styles/PedestrianDraw02.regions.json.classifications.ndjson.gz','rt')];remaining=[r for r in rows if r['status']=='remaining'];admitted=[r for r in remaining if r['originalDomain'] in changes[(r['render'],r['mesh'])]]
assert len(admitted)==96 and len({(r['render'],r['originalDomain']) for r in admitted})==32
report=dict(staticFalseToTrue=475,staticTrueToFalse=0,accepted=47707,genuineRepeatExcluded=270,unresolvedWindows=6,observedRemainingAdmitted=96,observedUniqueDomainsAdmitted=32,observedRemainingConservative=72,transitions=[dict(render=k[0],mesh=k[1],domains=sorted(v)) for k,v in changes.items() if v],resourceSha256=hashlib.sha256((root/'assets/HardcodedNumbers/PedestrianDraw02/PedestrianDraw02UvSampling.bin').read_bytes()).hexdigest(),scope='Same source-only domain predicate for all original domains; no scenario allowlist. All actual source-repeat overlaps remain false. Native replay is separate.')
(base/'non-tiling/domain-exclusion-transition.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='transitions'}))
