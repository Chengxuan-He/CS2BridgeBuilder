"""Read-only verification of this style's complete Surface reference graph."""
from pathlib import Path
import hashlib,io,json,struct,zipfile
ROOT=Path(__file__).resolve().parents[3]
STYLE=ROOT/'assets/Materials/PedestrianDraw01'
OUT=ROOT/'artifacts/prepared-materials/PedestrianDraw01/repeat-control-graph.json'
def sha(b):return hashlib.sha256(b).hexdigest()
def cid(b):return ''.join(f'{x&15:x}{x>>4:x}' for x in b)
def string(r):
 n=0;shift=0
 while True:
  v=r.read(1)[0];n|=(v&127)<<shift
  if v<128:break
  shift+=7
 return r.read(n).decode()
def count(r):
 p=r.read(1)[0]
 if p==0:return 0
 assert p==255
 return struct.unpack('<i',r.read(4))[0]
def refs(p):
 r=io.BytesIO(p.read_bytes());version,template,vt=struct.unpack('<Hi?',r.read(7));assert version==1
 found=[]
 if vt:
  header=r.read(276)
  stacks=struct.unpack_from('<i',header)[0];assert 0<=stacks<=2
  for stack in range(stacks):
   for layer in range(4):
    value=cid(header[76+136*stack+16*layer:92+136*stack+16*layer]);assert len(value)==32
    if value!='0'*32:found.append((f'VT{stack}/{layer}',value))
  present=r.read(1)[0];assert present in (0,255)
  if present:found.append(('VTSurface',cid(r.read(16))))
 for size in (4,4,16,16):
  for _ in range(count(r)):string(r);assert len(r.read(size))==size
 for _ in range(count(r)):
  name=string(r);p=r.read(1)[0];assert p in (0,255)
  if p:
   value=cid(r.read(16))
   # Same measured 0/1/35 VT stack slots as PreparedMaterialCheck: original
   # texture identities are bypassed; native VT payload references were read above.
   if vt:
    assert template in (0,1,35)
    if name in ('_BaseColorMap','_NormalMap','_MaskMap','_ControlMask','_EmissiveColorMap'):continue
   found.append((name,value))
 for _ in range(count(r)):string(r)
 assert r.tell()==len(r.getvalue())
 return found
local={}
for sidecar in STYLE.rglob('*.cid'):
 p=Path(str(sidecar)[:-4]);assert p.is_file();identity=sidecar.read_text().strip();assert len(identity)==32
 value=sha(p.read_bytes())
 if identity in local:assert local[identity][1]==value
 else:local[identity]=(str(p.relative_to(STYLE)),value)
rows=[];missing=set()
for p in STYLE.rglob('*.Surface'):
 found=refs(p)
 assert all(value!='e998b666087a605187c9285ab1816a23' for _,value in found)
 rows.append(dict(path=str(p.relative_to(STYLE)),sha256=sha(p.read_bytes()),references=[dict(property=name,cid=value,local=value in local) for name,value in found]))
 missing.update(value for _,value in found if value not in local)
resolved={}
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')
for family in ('Game','BridgesAndPorts'):
 for p in (game/family).glob('*.cok'):
  if not p.name.startswith(('Blob','VT')):continue
  with zipfile.ZipFile(p) as z:
   names=set(z.namelist())
   for e in z.infolist():
    if e.filename.endswith('.cid'):
     value=z.read(e).decode().strip()
     if value in missing:
      assert e.filename[:-4] in names
      resolved[value]=dict(archive=str(p),entry=e.filename[:-4])
assert missing==set(resolved),f'Unresolved refs {missing-set(resolved)}'
integration=json.loads((OUT.parent/'repeat-control-integration.json').read_text(encoding='utf-8-sig'))
for c in integration['candidateChanges']:
 path=STYLE/'Repeat'/c['key']/'Repeat.Surface';assert sha(path.read_bytes())==c['newSha256']
 for before in c['before']:
  if before['property']!='_ControlMask':assert local[before['cid']][1]==before['sha256']
shared=STYLE/'Shared/729893d593df5d52ba911bc1e7eab77b.Texture';road=STYLE/'Road/ZeroControl.Texture'
assert shared.read_bytes()==road.read_bytes() and Path(str(shared)+'.cid').read_bytes()==Path(str(road)+'.cid').read_bytes()
assert not (STYLE/'Shared/e998b666087a605187c9285ab1816a23.Texture').exists()
assert not (STYLE/'Shared/e998b666087a605187c9285ab1816a23.Texture.cid').exists()
empty=[str(p) for p in STYLE.rglob('*') if p.is_dir() and not any(p.iterdir())];assert not empty
result=dict(status='PASS',surfaceCount=len(rows),localCidCount=len(local),builtInReferenceCount=len(resolved),unresolved=[],oldControlReferences=0,sharedZeroMatchesRoad=True,otherChannelsUnchanged=True,surfaces=rows,builtInReferences=resolved)
OUT.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:result[k] for k in ('status','surfaceCount','localCidCount','builtInReferenceCount','unresolved','oldControlReferences','sharedZeroMatchesRoad','otherChannelsUnchanged')}))
