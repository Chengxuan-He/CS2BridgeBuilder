"""Retain source-only seam witnesses from the native GPU checker, without rebaking."""
import json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift05/road-continuous-candidate'
def main():
 source=ROOT/'artifacts/material-remap/environments/Lift05/outputs/road-continuous-v3'
 manifest=json.loads((OUT/'manifest.json').read_text());evidence=json.loads((OUT/'mapping-evidence.json').read_text());reports=[]
 for unit in manifest['units']:
  name=unit['id']+'-native-check.json';path=source/name
  if path.exists():shutil.copyfile(path,OUT/'results'/name)
  report=json.loads((OUT/'results'/name).read_text());witnesses=[]
  for row in report['levels']:
   if row['mip']!=0:continue
   q=(row['resetX']+.5)/row['width'];u=unit['min'][0]+q*(unit['max'][0]-unit['min'][0])
   edge=next(b for l in evidence['levels'] for b in l['removedPeriodBoundaries'] if l['patches'][b['patches'][0]].get('unit')==unit['id'] and abs(b['integerPhaseDifference'][1])==1)
   witnesses.append(dict(channel=row['channel'],sourceLod=row['sourceLod'],uvLow=[u,unit['min'][1]],uvHigh=[u,unit['max'][1]],sourceLow=row['resetLow'],sourceHigh=row['resetHigh'],sourceResetJump=row['sourceResetJump'],minimumUniformChannelError=row['sourceResetJump']/2,actualNeighboringFacePair=edge['sourceFaces'],physicalSharedEdge=edge['physicalEdge']))
  reports.append(dict(id=unit['id'],sourceCid=unit['sourceCid'],sourceDigest=unit['sourceDigest'],sourceChannels=unit['channels'],witnesses=witnesses))
 result=dict(units=reports,interpretation='Measured native bilinear source endpoint samples at a real neighboring 4m V reset. A single continuous periodic sample must have one seam value; its maximum one-sided channel error is at least half the original source jump. No candidate bake is involved in this lower bound. It disproves exact sampling equivalence, without establishing a relaxed visual tolerance.')
 (OUT/'results/source-reset-counterexamples.json').write_text(json.dumps(result,indent=2)+'\n')
 print('Retained 12 source-only mip-zero channel witnesses across four units; no new pixels.')
if __name__=='__main__':main()
