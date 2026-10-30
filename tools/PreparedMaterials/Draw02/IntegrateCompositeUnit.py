"""Validate and prepare Draw02's existing full-mip three-band pixels; no baking."""
import hashlib, json, math, struct, sys
from pathlib import Path

if '--legacy-evidence-only' not in sys.argv:
    raise SystemExit('Retired RoadRepeat aliases are historical only; pass --legacy-evidence-only to reproduce artifacts, never to stage production resources.')
sys.argv.remove('--legacy-evidence-only')

r=Path(sys.argv[1]).resolve()
shared=r/'artifacts/shared-material-pipeline/composite-unit-check'
out=r/'artifacts/prepared-materials/Draw02/repeat-units-v2/composite'
out.mkdir(parents=True,exist_ok=True)
manifest=json.loads((shared/'manifest.json').read_text()); unit=manifest['units'][0]
report=json.loads((shared/'report.json').read_text()); product=report['units'][0]
original=json.loads((r/'artifacts/prepared-materials/Draw02/repeat-inventory.json').read_text())
rows=[u for u in original['units'] if u['bands']]
assert len(rows)==2 and rows[0]['bands']==rows[1]['bands']==unit['bands']
real=next(u for u in json.loads((r/'artifacts/prepared-materials/Draw02/repeat-units-v2/units.json').read_text())['units'] if u['id']=='Draw02/moving-carriageway')
previous_physical=unit['physicalSize'];unit['physicalSize']=real['physicalSize']
assert unit['bands'][0]['start']==0 and unit['bands'][-1]['end']==1
for a,b in zip(unit['bands'],unit['bands'][1:]): assert a['end']==b['start']
sx=max((b['max'][0]-b['min'][0])*4096/(b['end']-b['start']) for b in unit['bands'])
sy=max((b['max'][1]-b['min'][1])*4096*b['repeatY'] for b in unit['bands'])
for c in product['channels']:
    assert c['width']>=sx and c['height']>=sy
    assert c['mips']==1+int(math.log2(max(c['width'],c['height'])))==10
    assert abs(c['sourceSpanX']-sx)<1e-9 and c['sourceSpanY']==sy
    assert (c['width'],c['height'])==(311,827)

def s(v):
    b=v.encode();n=len(b);p=bytearray()
    while n>=128:p.append((n&127)|128);n>>=7
    return bytes(p)+bytes([n])+b
def hb(v):return bytes(int(v[i+1]+v[i],16) for i in range(0,32,2))
def cid(v):return ''.join(f'{b&15:x}{b>>4:x}' for b in hashlib.sha256(v).digest()[:16])
def save(path,b):
    path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists():assert path.read_bytes()==b,'Conflicting output '+str(path)
    else:path.write_bytes(b)
names=['_BaseColorMap','_NormalMap','_MaskMap','_ControlMask']
maps={c['name']:c['cid'] for c in product['channels']};assert set(maps)==set(names)
aliases=[]
for row in rows:
    template=14 if row['cutout'] else 0
    keys=['_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO'] if row['cutout'] else ['_TANGENTSPACE_OCTO']
    data=struct.pack('<Hi?',1,template,False)+b'\xff'+struct.pack('<i',1)+s('_Snow')+struct.pack('<f',0)
    data+=(b'\xff'+struct.pack('<i',0))*3+b'\xff'+struct.pack('<i',4)
    for n in names:data+=s(n)+b'\xff'+hb(maps[n])
    data+=b'\xff'+struct.pack('<i',len(keys))+b''.join(map(s,keys))
    identity=cid(data);key=hashlib.sha256(s('BridgeBuilder/RepeatUnits/2')+s(identity)).hexdigest()
    dest=out/'output/Draw02/RepeatUnits'/key/'Repeat.Surface'
    save(dest,data);save(Path(str(dest)+'.cid'),identity.encode())
    aliases.append(dict(alias=row['key'],unitKey=key,surfaceCid=identity,template=template,unit=real['id']))
for c in product['channels']:
    for suffix in ['.Texture','.Texture.cid']:
        relative=Path('Shared')/(c['cid']+suffix)
        save(out/'output/Draw02'/relative,(shared/'output/Draw02'/relative).read_bytes())
(out/'aliases.json').write_text(json.dumps(dict(style='Draw02',aliases=aliases),indent=2)+'\n')
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
(out/'producer-report.json').write_bytes((shared/'report.json').read_bytes())
(out/'verification.json').write_text(json.dumps(dict(style='Draw02',producedTextures=0,reusedTextures=4,
    sourcePhysicalSizeMetadata=previous_physical,physicalSize=unit['physicalSize'],physicalMetadataCorrectionDoesNotChangePixels=True,
    sourceSpan=[sx,sy],outputSize=[311,827],densityRatios=[311/sx,827/sy],mips=10,
    texelsPerMeter=[311/unit['physicalSize'][0],827/unit['physicalSize'][1]],bandsEqualMeasuredRecipe=True,
    aliases=aliases,channels=product['channels']),indent=2)+'\n')
print(json.dumps(aliases,indent=2))
