"""Prepare the measured TrainGravel corresponding native object descriptor.
Only three PBR identities change from the validated native road object template.
No texture is generated; original builtin texture signals and mips stay native.
"""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[3]
def token(cid):return bytes(int(cid[i+1]+cid[i],16) for i in range(0,32,2))
def identity(raw):return ''.join(f'{b&15:x}{b>>4:x}' for b in hashlib.sha256(raw).digest()[:16])
pairs={
 'b00176216b0ce36579bce6fc675cfb77':'18a5b9c996ce7c551bc422cd96a324ad',
 '9f446790360787756b0e5909db12e200':'346c601cafab351569d7d0a4fcac777c',
 '375b871b66aaf675dacef141eb2a9810':'646901d94c691865ba8a1a647f57f7c3'}
report=[]
for style in ('Draw01','Draw02','Draw03','Lift01','Lift02','Lift03','Lift04','Lift05','PedestrianDraw01','PedestrianDraw02'):
 source=ROOT/'assets/Materials'/style/'Road/NativeAsphalt.Surface';raw=source.read_bytes();assert source.with_suffix('.Surface.cid').read_text().strip()=='88df662f6bc8058d3ecf8f7a5d7f2239'
 assert raw[:7]==bytes([1,0,0,0,0,0,0])
 result=raw;offsets=set()
 for old,new in pairs.items():
  t=token(old);assert result.count(t)==1;at=result.index(t);offsets.update(range(at,at+16));result=result.replace(t,token(new))
 assert all(x==y or i in offsets for i,(x,y) in enumerate(zip(raw,result)))
 cid=identity(result);folder=ROOT/'assets/Materials'/style/'NativeSources'/cid;folder.mkdir(parents=True,exist_ok=True);(folder/'Object.Surface').write_bytes(result);(folder/'Object.Surface.cid').write_text(cid)
 report.append(dict(style=style,cid=cid,bytes=len(result)+32,sha256=hashlib.sha256(result).hexdigest(),sourceSha256=hashlib.sha256(raw).hexdigest(),changedPbr=pairs,newTextureBytes=0,otherDescriptorBytesIdentical=True))
out=ROOT/'artifacts/material-remap/shared/ballast-original44';out.mkdir(parents=True,exist_ok=True);(out/'descriptor-receipt.json').write_text(json.dumps(report,indent=2));print(cid,len(result))
