"""Prepare Lift03 real-source repeat-unit evidence for the full-mip producer."""
import hashlib,json,math,struct,sys,zipfile
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools/PreparedMaterials/Lift03'))
from ExportRecipes import extract,numbers,sampler_key
from Lift03NativeUvCharts import meshes
OUT=ROOT/'artifacts/prepared-materials/Lift03/repeat-units-v2'
def sha(b):return hashlib.sha256(b).hexdigest()
def main():
 OUT.mkdir(parents=True,exist_ok=True)
 old=json.loads((ROOT/'artifacts/prepared-materials/Lift03/shared-source-map/mapping.json').read_text())
 source='d5708265307c0c422270f918fb8ca8e8';raw=old['sources'][source]
 with zipfile.ZipFile(raw['nativeArchive']) as z:surface=z.read(raw['nativeEntry'])
 assert sha(surface)==raw['sourceDigest']
 channels=[]
 for name,key in raw['channels'].items():
  t=old['textures'][key]
  with zipfile.ZipFile(t['nativeArchive']) as z:payload=z.read(t['nativeEntry'])
  assert sha(payload)==t['nativePayloadSha256'] and struct.unpack_from('<H',payload)[0]==1
  tile,w,h,mid,mips,fmt,filter_mode,wrap,aniso,tail=struct.unpack_from('<HHHHBBBBBi',payload,21)
  table_at=38+tail;count=struct.unpack_from('<i',payload,table_at)[0];offsets=struct.unpack_from('<'+'i'*count,payload,table_at+4)
  start=table_at+4+4*count
  assert 0<offsets[0] and all(a<b for a,b in zip(offsets,offsets[1:])) and start+offsets[-1]==len(payload)
  channels.append(dict(name=name,cid=t['nativeCid'],sha256=sha(payload),nativeArchive=t['nativeArchive'],nativeEntry=t['nativeEntry'],width=w,height=h,completeMipCount=mips,midMipLevel=mid,format=fmt,filter=filter_mode,wrap=wrap,aniso=aniso,tileSize=tile,mipTailBytes=tail,tileCount=count,tailSha256=sha(payload[38:38+tail]),allTileRangesInFile=True,allMipPixelsDecoded=False))
 fixture=(ROOT/'artifacts/uv-density/layout-plans.bin').read_bytes()
 near=next(item for item in meshes(fixture) if item[0]=='LiftBridge03 Mesh' and item[2]==0)
 _,_,_,uv,faces,xyz=near
 recipes,evidence=extract(ROOT)
 values=numbers(ROOT/'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03RoadAppearance_cs.bin')['F32']
 units=[]
 for deck,face,coeff in [('Main',23857,(8,9)),('Auxiliary',27296,(18,19))]:
  recipe=next(r for r in recipes if r['deck']==deck);ids=faces[face];p=xyz[ids].astype(float);u=uv[ids].astype(float)
  e,f=p[1]-p[0],p[2]-p[0];det=e[0]*f[2]-f[0]*e[2]
  gx=((u[1]-u[0])*f[2]-(u[2]-u[0])*e[2])/det
  gz=(e[0]*(u[2]-u[0])-f[0]*(u[1]-u[0]))/det
  size=np.array(channels[0]['width']);density=[float(np.linalg.norm(gx*size)),float(np.linalg.norm(gz*size))]
  span=[b-a for a,b in zip(recipe['min'],recipe['max'])];pixelSize=[int(round(v*4096)) for v in span]
  physical=[span[i]/abs(values[c]) for i,c in enumerate(coeff)]
  assert all(math.isclose(pixelSize[i]/physical[i],density[i],rel_tol=1e-6) for i in (0,1))
  variants=[]
  for r in recipes:
   if r['deck']==deck:variants.append(dict(cutout=r['cutout'],targetTemplate=1 if r['cutout'] else 0,legacyRepeatKey=sampler_key(raw['sourceDigest'],r['min'],r['max'],r['cutout'])))
  units.append(dict(id='Lift03.MainRoad' if deck=='Main' else 'Lift03.AuxiliaryBallast',sourceCid=source,sourceSha256=sha(surface),min=recipe['min'],max=recipe['max'],physicalSize=physical,density=density,pixelSize=pixelSize,
   purpose='Prototype Main asphalt surface' if deck=='Main' else 'Prototype auxiliary ballast patch; do not replace arbitrary selected lower-road or track surfaces',
   prototype=dict(render='LiftBridge03 Mesh',mesh=0,geometryCid='44fbc3680c666332d10037f10025bfa7',face=face,indices=ids.tolist(),positions=p.tolist(),uv=u.tolist(),gradientX=gx.tolist(),gradientZ=gz.tolist()),
   variants=variants,projectionAliases=4,authoritativeEvidence='Recorded real prototype road-face mapping and measured source unit region',
   boundaryEvidence=dict(status='pending-full-mip-check',regionIsMeasured=True,sourceImagePeriodicityProven=False,reason='The previous evidence verifies source coordinates and density only; it does not prove tileable filtering boundaries across the full mip chain.'),
   authoringEligible=False))
 result=dict(schema='Lift03/RepeatUnitEvidence/2',style='Lift03',sourceCid=source,sourceSha256=sha(surface),sourceTemplate=raw['template'],units=units,channels=channels,
    absentChannels=raw['absentChannels'],defaultControl='transparent native zero; no authored control channel',evidence=dict(fixtureSha256=sha(fixture),**evidence),
    productionGate='Adapt to shared RepeatUnits/2 schema and require boundary/full-mip verification before accepting any unit; no arbitrary chart or triangle units',
    structurePolicy='5954 legacy structural samplers map to original source CIDs, not to invented repeating units')
 (OUT/'unit-evidence.json').write_text(json.dumps(result,indent=2)+'\n')
 mapping=dict(style='Lift03',repeat=[dict(unit=u['id'],key=v['legacyRepeatKey'],cutout=v['cutout']) for u in units for v in u['variants']],
  structure=[dict(key=s['productionUnitKey'],sourceCid=s['sourceCid'],unit=None,resolution='native-source-where-receiver-contained; common-source-sampling-required-otherwise') for s in old['samplers']])
 (OUT/'legacy-key-map.json').write_text(json.dumps(mapping,indent=2)+'\n')
 report=dict(status='PASS source inputs; boundary/all-mip gate pending',units=len(units),repeatKeys=len(mapping['repeat']),structureKeys=len(mapping['structure']),sourceChannels=len(channels),sourceMipCounts=[c['completeMipCount'] for c in channels],sourcePayloadHashAndRangesVerified=True,outputTexturesCreated=0,authoringAcceptedUnits=0)
 (OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
