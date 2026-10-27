"""Restore existing Draw02 cutout caller semantics without reading/encoding pixels."""
import hashlib
import json
from pathlib import Path
import struct
import sys

repo = Path(sys.argv[1]).resolve()
retained = repo / 'artifacts/prepared-materials/Draw02/repeat-units-v2'
source = retained / 'trial-result-corrected'
out = retained / 'descriptor-semantics'
out.mkdir(parents=True, exist_ok=True)

def digest(b): return hashlib.sha256(b).hexdigest()
def string(s):
    b=s.encode(); n=len(b); p=bytearray()
    while n>=128: p.append((n&127)|128); n>>=7
    return bytes(p)+bytes([n])+b
def cid(b): return ''.join(f'{v&15:x}{v>>4:x}' for v in hashlib.sha256(b).digest()[:16])
def hash_bytes(s): return bytes(int(s[i+1]+s[i],16) for i in range(0,32,2))

inventory=json.loads((repo/'artifacts/prepared-materials/Draw02/repeat-inventory.json').read_text())
old=json.loads((source/'producer-report.json').read_text())
aliases=[]; records=[]
names=['_BaseColorMap','_NormalMap','_MaskMap','_ControlMask']
keywords=['_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO']
for u in old['units']:
    assert u['periods']==[1,1], 'Review multi-period descriptor UV scaling'
    for key in u['aliases']:
        assert next(x for x in inventory['units'] if x['key']==key)['cutout'] is True
    bindings={c['name']:c['cid'] for c in u['channels']}
    assert set(bindings)==set(names)
    # Byte-for-byte native schema of the old PrepareRepeat.Surface(cutout:true).
    b=struct.pack('<Hi?',1,14,False)+b'\xff'+struct.pack('<i',1)+string('_Snow')+struct.pack('<f',0)
    b+=(b'\xff'+struct.pack('<i',0))*3
    b+=b'\xff'+struct.pack('<i',4)
    for n in names: b+=string(n)+b'\xff'+hash_bytes(bindings[n])
    b+=b'\xff'+struct.pack('<i',len(keywords))+b''.join(map(string,keywords))
    identity=cid(b)
    key=digest(string('BridgeBuilder/RepeatUnits/2')+string(identity))
    dest=out/'output/Draw02/RepeatUnits'/key/'Repeat.Surface'
    dest.parent.mkdir(parents=True,exist_ok=True)
    if dest.exists(): assert dest.read_bytes()==b
    else: dest.write_bytes(b)
    Path(str(dest)+'.cid').write_text(identity)
    records.append(dict(unit=u['id'],oldKey=u['key'],key=key,oldCid=u['surfaceCid'],surfaceCid=identity,
                        template=14,snow=0,keywords=keywords,channels=u['channels'],bytes=len(b)))
    for alias in u['aliases']: aliases.append(dict(alias=alias,unitKey=key,surfaceCid=identity,unit=u['id']))
    for c in u['channels']:
        src=source/'output/Draw02/Shared'/(c['cid']+'.Texture')
        dst=out/'output/Draw02/Shared'/src.name
        dst.parent.mkdir(parents=True,exist_ok=True)
        if dst.exists(): assert dst.read_bytes()==src.read_bytes()
        else: dst.write_bytes(src.read_bytes())
        Path(str(dst)+'.cid').write_bytes(Path(str(src)+'.cid').read_bytes())
(out/'aliases.json').write_text(json.dumps(dict(style='Draw02',aliases=aliases),indent=2)+'\n')
(out/'report.json').write_text(json.dumps(dict(style='Draw02',producedTextures=0,unchangedTextures=12,
    evidence=['repeat-inventory.json cutout:true','PrepareRepeat.Editor.cs Surface(bool cutout,...)'],units=records),indent=2)+'\n')
print(json.dumps(records,indent=2))
