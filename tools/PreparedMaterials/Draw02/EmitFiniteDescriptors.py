"""Emit native descriptors only for the verified LOD0 finite-width cell candidates."""
import hashlib,json,struct
from pathlib import Path
root=Path(__file__).resolve().parents[3];folder=root/'artifacts/material-remap/Draw02/near-cells'
report=json.loads((folder/'producer-report.json').read_text())
def s(value):
    data=value.encode();n=len(data);prefix=bytearray()
    while n>=128:prefix.append((n&127)|128);n>>=7
    return bytes(prefix)+bytes([n])+data
def h(value):return bytes(int(value[i+1]+value[i],16)for i in range(0,32,2))
def cid(data):return ''.join(f'{b&15:x}{b>>4:x}'for b in hashlib.sha256(data).digest()[:16])
units=[]
for name in ('moving-carriageway','moving-sidewalk','fixed-road','fixed-sidewalk'):
    rows=[r for r in report['products']if r['unit']=='lod0-'+name]
    assert len(rows)==4
    data=struct.pack('<Hi?',1,0,False)+b'\xff'+struct.pack('<i',1)+s('_Snow')+struct.pack('<f',0)
    data+=(b'\xff'+struct.pack('<i',0))*3+b'\xff'+struct.pack('<i',4)
    for row in rows:data+=s(row['channel'])+b'\xff'+h(row['cid'])
    data+=b'\xff'+struct.pack('<i',1)+s('_TANGENTSPACE_OCTO')
    identity=cid(data);key=hashlib.sha256(s('BridgeBuilder/RepeatUnits/2')+s(identity)).hexdigest()
    dest=folder/'output/Draw02/RepeatUnits'/key/'Repeat.Surface';dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_bytes(data);Path(str(dest)+'.cid').write_text(identity)
    units.append(dict(name=name,key=key,cid=identity,channels=rows,repeatU=False,repeatV=True,finiteGuard=[.25,.75]))
(folder/'verified-lod0-bindings.json').write_text(json.dumps(dict(units=units,publicationReady=False,
    limitation='Complete source/mip and mask isolation checks exist. Native reader/render, normal-basis, periodic seam checks and owner/package size compliance remain required.'),indent=2)+'\n')
print([(r['name'],r['key'])for r in units])
