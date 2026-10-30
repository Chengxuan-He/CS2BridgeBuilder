"""Compare actual successful Compose UV bits with the original rejected corners."""
import json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'artifacts/material-remap/Lift05'
def main():
 witnesses=json.loads((BASE/'road-continuous-04-guard-inputs.guard-inputs.json').read_text())['witnesses'];maps={};count=0
 for witness in witnesses:
  key=(witness['lod'],int(witness['delta']),witness['pose'])
  if key not in maps:
   raw=(BASE/'native-wrap-inputs'/('road-capture-%d-%d-%d.bin'%key)).read_bytes();n=struct.unpack_from('<i',raw)[0];at=4;values={}
   for triangle in range(n):
    domain=struct.unpack_from('<i',raw,at)[0];at+=4
    for corner in range(3):
     bits=struct.unpack_from('<17i',raw,at);at+=68;values[(domain,*bits[:3])]=bits[3:5]
   assert at==len(raw);maps[key]=values
  bits=witness['pointUVBoundsFloatBits'];assert tuple(bits[-2:])==maps[key][(witness['domain'],*bits[:3])];count+=1
 report=json.loads((BASE/'road-continuous-06.json').read_text())
 result=dict(candidate=report['candidateSha256'],matchedOriginalGuardWitnessCorners=count,changedFloat32Uvs=0,scope='Exact Float32 bits from original rejected point/UV witnesses compared with actual successful Compose captures; native upload equality separately asserted in the 27-case harness.')
 (BASE/'road-continuous-candidate/results/guard-fix-uv-preservation.json').write_text(json.dumps(result,indent=2)+'\n');print('PASS exact UV preservation:',count,'original guard witnesses')
if __name__=='__main__':main()
