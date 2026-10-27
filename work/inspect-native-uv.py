import sys,json,hashlib,struct
from pathlib import Path
r=Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research')
sys.path[:0]=[str(r/'pydeps'),'C:/Users/admin/Documents/Codex/2026-10-04/qi/work/unity-reader']
import UnityPy
from UnityPy.helpers import CompressionHelper
out=Path('artifacts/prepared-materials/shared-nontiling/native-semantics');out.mkdir(parents=True,exist_ok=True)
env=UnityPy.load('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/resources.assets')
objects={o.path_id:o for o in env.objects}
templates=[]
for row in json.loads((r/'material-library-resolved.json').read_text()):
 if row['hash'] not in [0,1,2,17,33]:continue
 d=json.loads(json.dumps(objects[row['ref'][1]].read_typetree()))
 cached=json.loads((r/f"template-{row['hash']}.json").read_text())
 assert d==cached, f"Template {row['hash']} differs from current resources"
 (out/f"template-{row['hash']}.json").write_text(json.dumps(d,indent=2))
 templates.append({'hash':row['hash'],'nativePathId':row['ref'][1],'sourceSha256':hashlib.sha256(json.dumps(d,sort_keys=True).encode()).hexdigest(),'verifiedAgainstCurrentResources':True})
(out/'templates-current.json').write_text(json.dumps(templates,indent=2))
report=[]
for pid in [550,571,581]:
 d=objects[pid].read_typetree();p=d['m_ParsedForm']
 (out/f'shader-{pid}-parsed.json').write_text(json.dumps(p,indent=2))
 report.append({'shader':pid,'name':p['m_Name'],'properties':[x for x in p['m_PropInfo']['m_Props'] if 'UV' in x['m_Name'] or 'Worldspace' in x['m_Name']],'keywords':p['m_KeywordNames']})
 if pid!=550:continue
 blob=bytes(d['compressedBlob'])
 for segment,(off,cl,dl) in enumerate(zip(d['offsets'][0],d['compressedLengths'][0],d['decompressedLengths'][0])):
  raw=CompressionHelper.decompress_lz4(blob[off:off+cl],dl)
  if segment==0:records=[struct.unpack_from('<iii',raw,4+i*12) for i in range(struct.unpack_from('<i',raw,0)[0])]
  for i,(start,size,seg) in enumerate(records):
   if seg!=segment:continue
   entry=raw[start:start+size];pos=entry.find(b'DXBC')
   if pos<0:continue
   size=struct.unpack_from('<I',entry,pos+24)[0]
   (out/f'{pid}-{i}.dxbc').write_bytes(entry[pos:pos+size])
(out/'summary.json').write_text(json.dumps(report,indent=2))
print(json.dumps([{'shader':x['shader'],'name':x['name'],'uvProperties':[p['m_Name'] for p in x['properties']]} for x in report]))
