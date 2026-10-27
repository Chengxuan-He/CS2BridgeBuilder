"""Integrate verified Draw03 canonical structures without baking or overwriting conflicts."""
import hashlib,json,shutil
from pathlib import Path

repo=Path(__file__).resolve().parents[3]
source=repo/'artifacts/prepared-current-structures-canonical/Draw03'
target=repo/'assets/Materials/Draw03'
evidence=repo/'artifacts/prepared-materials/Draw03/structure-delivery'
evidence.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def require(ok,msg):
 if not ok:raise RuntimeError(msg)
inv=repo/'artifacts/uv-density/structure-tessellated-sources.json'
units=[u for u in json.loads(inv.read_bytes())['units'] if u['style']=='Draw03']
keys={u['key'] for u in units}
require(len(keys)==len(units),'Duplicate production keys')
require(keys=={p.name for p in (source/'Structure').iterdir() if p.is_dir()},'Canonical/production key coverage differs')
existing={};before={}
for p in target.rglob('*'):
 if not p.is_file():continue
 relative=p.relative_to(target).as_posix();h=sha(p);before[relative]=h
 if p.suffix=='.cid':
  asset=Path(str(p)[:-4]);require(asset.is_file(),'Orphan existing CID')
  cid=p.read_text().strip();value=sha(asset)
  require(cid not in existing or existing[cid]==value,'Existing CID conflict '+cid)
  existing[cid]=value
files=[];known=dict(existing);conflicts=[]
for i,p in enumerate(source.rglob('*')):
 if not p.is_file():continue
 relative=p.relative_to(source).as_posix()
 require(relative.startswith(('Shared/','Structure/')),'Unexpected canonical folder')
 h=sha(p);dest=target/relative
 if dest.exists() and sha(dest)!=h:conflicts.append(relative)
 if p.suffix=='.cid':
  asset=Path(str(p)[:-4]);require(asset.is_file(),'Orphan source CID')
  cid=p.read_text().strip();value=sha(asset)
  if cid in known and known[cid]!=value:conflicts.append('CID '+cid)
  known[cid]=value
 files.append({'path':relative,'bytes':p.stat().st_size,'sha256':h,'new':not dest.exists()})
 if len(files)%10000==0:print('Preflighted',len(files),flush=True)
require(not conflicts,'Conflicts (no files copied): '+str(conflicts))
for i,row in enumerate(files):
 dest=target/row['path']
 if row['new']:
  dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source/row['path'],dest)
 require(sha(dest)==row['sha256'],'Copied bytes differ: '+row['path'])
 if (i+1)%10000==0:print('Copied/verified',i+1,flush=True)
for relative,h in before.items():require(sha(target/relative)==h,'Existing resource modified: '+relative)
require(all(any(p.iterdir()) for p in target.rglob('*') if p.is_dir()),'Empty packaged directory')
report={'style':'Draw03','source':str(source),'inventorySha256':sha(inv),'productionKeys':len(keys),'sourceFiles':len(files),'sourceBytes':sum(r['bytes'] for r in files),'addedFiles':sum(r['new'] for r in files),'unchangedFiles':sum(not r['new'] for r in files),'addedBytes':sum(r['bytes'] for r in files if r['new']),'priorFilesPreserved':len(before),'sameCidConflicts':conflicts,'rebakedResources':0,'files':files}
(evidence/'integration.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='files'},indent=2),flush=True)
