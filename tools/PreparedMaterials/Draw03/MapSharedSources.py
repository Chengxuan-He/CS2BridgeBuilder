"""Record Draw03 source-texture identity separately from all immutable samplers."""
from pathlib import Path
import base64,hashlib,json,struct,zipfile,collections
repo=Path(__file__).resolve().parents[3]
out=repo/'artifacts/prepared-materials/Draw03/shared-source-migration'
invpath=repo/'artifacts/uv-density/structure-tessellated-sources.json'
inventory=json.loads(invpath.read_bytes());units=[u for u in inventory['units'] if u['style']=='Draw03']
audit=json.loads((out/'native-source-audit.json').read_bytes())
bycid={s['cid']:s for s in audit['sources']};wanted=set(bycid)
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
archives=[];entries={}
for family in ('Game','BridgesAndPorts'):
 for path in (game/'Content'/family).glob('*.cok'):
  if not path.name.startswith(('Blob','VT')):continue
  z=zipfile.ZipFile(path);archives.append(z)
  for n in z.namelist():
   if not n.endswith('.cid'):continue
   cid=z.read(n).decode().strip();entries.setdefault(cid,(z,n[:-4]))
def data(cid):
 z,n=entries[cid];return z.read(n)
def sha(b):return hashlib.sha256(b).hexdigest()
def cid(b):return ''.join(f'{x:02x}'[::-1] for x in b)
def native_bindings(b):
 at=7
 if b[6]:
  at+=276
  present=b[at];at+=1
  if present==255:at+=16
 def string():
  nonlocal at
  size=shift=0
  while True:
   x=b[at];at+=1;size|=(x&127)<<shift
   if x<128:break
   shift+=7
  s=b[at:at+size].decode();at+=size;return s
 def count():
  nonlocal at
  marker=b[at];at+=1
  if marker==0:return 0
  assert marker==255
  n=struct.unpack_from('<i',b,at)[0];at+=4;return n
 for width in (4,4,16,16):
  for _ in range(count()):string();at+=width
 result={}
 for _ in range(count()):
  key=string();present=b[at];at+=1
  if present==255:result[key]=cid(b[at:at+16]);at+=16
 return result
sources=[];contents={};settings=[]
for sourceid,proof in bycid.items():
 b=data(sourceid);assert sha(b)==proof['digest'];desc=next(s for s in inventory['sources'] if s['cid']==sourceid);bindings=native_bindings(b);channelrows=[]
 for ch in proof['channels']:
  row=dict(ch);row['decodedMipScope']='mip-zero-only' if row['mips']==1 else 'decoded-native-mips'
  slot=next((v for v in desc['channels'] if v['name']==ch['property']),None)
  if b[6] and b[283]==255 and slot:
   sc=cid(b[284:300]);sb=data(sc);row.update(nativeVtSurfaceCid=sc,nativeVtSurfacePayloadSha256=sha(sb),nativeVtSurfacePayloadBytes=len(sb),stack=slot['stack'],layer=slot['layer'])
  if b[6] and slot:
   wrapper=cid(b[7+76+slot['stack']*136+slot['layer']*16:7+92+slot['stack']*136+slot['layer']*16]);row['nativeVtChannelCid']=wrapper
   if wrapper!='0'*32:
    wb=data(wrapper);row['nativeVtPayloadSha256']=sha(wb);row['nativeVtPayloadBytes']=len(wb)
    original=cid(wb[3:19]);row['originalTextureCid']=original
    if original in entries:
     ob=data(original);row['originalTexturePayloadSha256']=sha(ob);row['originalTexturePayloadBytes']=len(ob)
  elif ch['property'] in bindings:
   original=bindings[ch['property']];ob=data(original);row.update(originalTextureCid=original,originalTexturePayloadSha256=sha(ob),originalTexturePayloadBytes=len(ob))
  row['builtInSource']=True
  channelrows.append(row)
  contents.setdefault(row['contentKey'],{'contentKey':row['contentKey'],'rawSha256':row['rawSha256'],'width':row['width'],'height':row['height'],'format':row['format'],'decodedMips':row['mips'],'decodedBytes':row['rawBytes'],'uses':[]})['uses'].append({'surface':sourceid,'property':ch['property']})
 sources.append({'sourceCid':sourceid,'sourceDigest':proof['digest'],'template':proof['template'],'nativeSurfaceDescriptorBase64':base64.b64encode(b).decode(),'channels':channelrows})
records=[]
for u in units:
 records.append({'key':u['key'],'sourceCid':u['sourceCid'],'sourceDigest':u['sourceDigest'],'sampling':{k:u[k] for k in ('triangle','bounds','a','b','c')},'requiredOperation':'sourceMap(foldTriangle(frac(localUV)))' if u['triangle'] else 'sourceMap(frac(localUV))','normalRule':'invert native tangent XY in reflected half' if u['triangle'] else 'preserve source encoding','originalSamplerSettings':'source channel filter/wrap/aniso records; descriptor retains native settings','implementationStatus':'requires verified native local-repeat sampling'})
report={'schema':'Draw03SharedSourceAudit/1','inventorySha256':sha(invpath.read_bytes()),'sourceSurfaces':sources,'uniqueDecodedContents':list(contents.values()),'samplingRecords':records,'scope':'Audit mapping only. mip-zero content identity is not proof of full-mip identity; never merge until full original payload/channel semantics agree. No runtime replacement or package-size claim.'}
(out/'source-sampling-map.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
orig={ch['originalTextureCid']:(ch.get('originalTexturePayloadBytes'),ch.get('originalTexturePayloadSha256')) for s in sources for ch in s['channels'] if ch.get('originalTextureCid')}
summary={'sourceSurfaces':len(sources),'samplingRecords':len(records),'triangleRecords':sum(u['triangle'] for u in units),'rectangularRecords':sum(not u['triangle'] for u in units),'nativeChannelBindings':sum(len(s['channels']) for s in sources),'uniqueTopLevelContents':len(contents),'uniqueOriginalTextureCids':len(orig),'availableOriginalPayloads':sum(n is not None for n,h in orig.values()),'availableOriginalPayloadBytes':sum(n or 0 for n,h in orig.values()),'channelsWithVtSurface':sum('nativeVtSurfaceCid' in ch for s in sources for ch in s['channels']),'sampledMipLevels':sorted({t['mip'] for t in audit['samples']}),'affineHardwareMaxDifference':max(t['maxAffineError'] for t in audit['samples']),'localWrapHardwareMaxDifference':max(t['maxLocalWrapDifference'] for t in audit['samples']),'packagedFilesModified':0,'encodedTextures':0,'nativeMigrationComplete':False}
(out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary,indent=2))
for z in archives:z.close()
