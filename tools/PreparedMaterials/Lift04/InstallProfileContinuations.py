"""Bind measured native profile continuations to the unchanged prepared unit."""
import hashlib
import json
from pathlib import Path
import struct

ROOT=Path(__file__).resolve().parents[3]
proof=json.loads((Path(__file__).parent/'profile-continuations.json').read_text())
path=ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_StructuralProfileRegions.bin'
raw=path.read_bytes();assert hashlib.sha256(raw[:-32]).digest()==raw[-32:]
counts=struct.unpack_from('<6i',raw,8);assert counts[:5]==(0,0,0,0,0) and counts[5] in (24,40)
values=list(struct.unpack_from('<24d',raw,32))
for bounds in proof['repeatCoveragePixels']:
    values.extend(v/4096 for v in proof['unitPixels'])
    values.extend(v/4096 for v in bounds)
data=b'BBN1'+struct.pack('<7i',1,0,0,0,0,0,len(values))+struct.pack('<'+str(len(values))+'d',*values)
data+=hashlib.sha256(data).digest();path.write_bytes(data)
source=ROOT/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RepeatUnits.cs'
code=source.read_text();start='        // Native physical continuation regions.';end='        // End single-axis profiles.'
if start in code:code=code[:code.index(start)]+code[code.index(end):]
extra=start+'\n'+ '\n'.join('        regions.Add(new Region("'+proof['unitKey']+'",\n            '+', '.join('profiles['+str(j)+']' for j in range(i,i+8))+', repeatU: false, repeatV: true));' for i in (24,32))+'\n'
assert code.count(end)==1;code=code.replace(end,extra+end);source.write_text(code)
print(json.dumps(dict(bytes=len(data),sha256=hashlib.sha256(data).hexdigest(),newTextures=0,unitKey=proof['unitKey'])))
