import json,gzip,hashlib,sys
from pathlib import Path
R=Path(__file__).resolve().parents[1]/'artifacts/prepared-materials'
out=R/('shared-nontiling/'+(sys.argv[1] if len(sys.argv)>1 else 'actual-final'))
allowed={}
for line in (out/'observer.json.source-uv.ndjson').open():
 r=json.loads(line);key=(r['style'],r['name'],r['mesh']);s=set(r['sourceUvDomains'])
 if key in allowed:assert allowed[key]==s
 else:allowed[key]=s
def fingerprint(r):
 return hashlib.sha256(json.dumps({k:v for k,v in r.items() if k not in ('id','charts','canUseSource')},sort_keys=True,separators=(',',':')).encode()).hexdigest()
reports=[]
for p in sorted((R/'shared-polar/receivers').glob('*.units.ndjson.gz')):
 style=p.name.split('.')[0];new=[json.loads(x) for x in gzip.open(out/'styles'/p.name,'rt')]
 newhash={fingerprint(r) for r in new};old=retained=kept=mismatch=0
 for line in gzip.open(p,'rt'):
  r=json.loads(line);old+=1
  if r['originalDomain'] in allowed.get((style,r['name'],r['mesh']),set()):retained+=1
  else:
   kept+=1
   if fingerprint(r) not in newhash:mismatch+=1
 rec=dict(style=style,oldChangedUnits=old,nowRetainedSourceUnits=retained,oldDensityUnitsStillSelected=kept,newDensityUnits=len(new),densityNumericLineageMismatches=mismatch)
 reports.append(rec);print(json.dumps(rec),flush=True)
(out/'changed-unit-transitions.json').write_text(json.dumps(dict(baseline='shared-polar/receivers',scope='Exact source classification join; density fingerprints exclude id, charts and canUseSource because chart catalogs have independently evolved. All numeric mapping/face provenance otherwise compared.',styles=reports),indent=2))
