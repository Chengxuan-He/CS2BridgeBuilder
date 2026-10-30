from pathlib import Path
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[3]
source=ROOT/'artifacts/agent-material-env/Lift02/source-rebuild/inputs/native-source-inventory.json'
inventory=json.loads(source.read_text())
rows=[]
archives={}
try:
 for row in inventory['surfaces']+inventory['sourcePayloads']:
  if row['archive'] not in archives:archives[row['archive']]=zipfile.ZipFile(row['archive'])
  archive=archives[row['archive']]
  digest=hashlib.sha256()
  with archive.open(row['entry']) as stream:
   while chunk:=stream.read(1024*1024):digest.update(chunk)
  assert digest.hexdigest()==row['sha256'],row['cid']
  assert archive.getinfo(row['entry']).file_size==row['bytes']
  cid=archive.read(row['entry']+'.cid').decode().strip()
  assert cid==row['cid']
  rows.append(dict(cid=cid,bytes=row['bytes'],sha256=digest.hexdigest(),builtIn=True))
finally:
 for archive in archives.values():archive.close()
known={r['cid'] for r in rows}
assert all(set(s['physicalBackingCids'])<=known for s in inventory['surfaces'])
report=dict(schema='BridgeBuilder/Lift02OriginalAtlasClosure/1',inventorySha256=hashlib.sha256(source.read_bytes()).hexdigest(),surfaces=len(inventory['surfaces']),physicalBackings=len(inventory['sourcePayloads']),records=rows,allPhysicalEdgesResolved=True,originalPayloadBytes=sum(r['bytes'] for r in rows),newRoadTextureBytes=0,newRoadMaterialBytes=0,originalMipPayloadsByteIdentical=True,builtInBoundaryPersistedBytes=0,completeGeneratedOwnerBytes=None,limitations=['This verifies original native Surface and physical backing identities, not generated-owner closure or native cold loading.','All original native payload bytes, including original mip data, are retained without transcoding. No new cropped unit is shipped.'])
out=ROOT/'artifacts/material-remap/Lift02/source-mapping/original-atlas-closure.json'
out.write_text(json.dumps(report,indent=2));print(json.dumps({k:v for k,v in report.items() if k!='records'},indent=2))
