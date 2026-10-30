"""Stage the complete road candidates and encode continuous affine consumer data."""
import hashlib,json,shutil,struct,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Lift05/road-continuous-candidate'
def main():
 source=ROOT/'artifacts/material-remap/environments/Lift05/outputs/road-continuous-v3';retained=OUT/'results';retained.mkdir(parents=True,exist_ok=True)
 shutil.copytree(source,retained,dirs_exist_ok=True)
 data=json.loads((OUT/'consumer-plan.json').read_text());mapping=json.loads((OUT/'mapping-evidence.json').read_text());old=json.loads((OUT.parent/'road-projection-plan.json').read_text());bake=json.loads((retained/'bake.json').read_text());units={u['id']:i for i,u in enumerate(bake['units'])};integers=[2,3];doubles=[]
 for level,evidence,prior in zip(data['levels'],mapping['levels'],old['levels']):
  groups=evidence['continuousComponents']+[[p['patch']] for p in evidence['patches'] if p['mode']=='original-finite'];integers += [len(level['patches']),len(level['nodes']),*[b['root'] for b in level['bands']]]
  for p,group in zip(level['patches'],groups):
   original=prior['patches'][group[0]];center=(original['sourceMin'][0]+original['sourceMax'][0])/2;unit=units.get(p['unit'],-1);integers += [0 if center<-3 else 2 if center>3 else 1,unit];c=np.array(p['coefficients']);lo,hi=p['sourceBounds']
   if unit>=0:
    channels=bake['units'][unit]['channels'];lo,hi=channels[0]['contentMin'],channels[0]['contentMax'];assert all(x['contentMin']==lo and x['contentMax']==hi for x in channels);c*=np.array(hi)-lo;c[2]+=lo
   doubles+=c.ravel().tolist()+lo+hi
  for n in level['nodes']:integers +=[n['negative'],n['positive']];doubles+=n['lineXZ1']
 raw=b'BBN1'+struct.pack('<7i',1,len(integers),0,0,0,0,len(doubles))+struct.pack('<%di'%len(integers),*integers)+struct.pack('<%dd'%len(doubles),*doubles);raw+=hashlib.sha256(raw).digest()
 target=ROOT/'assets/HardcodedNumbers/Lift05/Types_Lift05_RoadProjection.bin';backup=OUT.parent/'backups/continuous-road';backup.mkdir(parents=True,exist_ok=True)
 if not (backup/target.name).exists():shutil.copyfile(target,backup/target.name)
 if '--check' in sys.argv:assert target.read_bytes()==raw,'Continuous road BBN differs.'
 else:target.write_bytes(raw)
 files=[]
 for p in (retained/'output/Lift05').rglob('*'):
  if not p.is_file():continue
  relative=p.relative_to(retained/'output/Lift05');dest=ROOT/'assets/Materials/Lift05'/relative
  if dest.exists():assert dest.read_bytes()==p.read_bytes()
  else:dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
  files.append(dict(path=str(relative),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 (OUT/'binding.json').write_text(json.dumps(dict(units=[dict(id=u['id'],key=u['key'],surfaceCid=u['surfaceCid'],contentMin=u['channels'][0]['contentMin'],contentMax=u['channels'][0]['contentMax']) for u in bake['units']],files=files,closureBytes=sum(f['bytes'] for f in files),qualityAccepted=False),indent=2)+'\n')
 if '--check' not in sys.argv:shutil.copyfile(Path(__file__).with_name('Lift05NativeRoadAppearance.template.cs'),ROOT/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05NativeRoadAppearance.cs')
 print('Candidate graph',sum(f['bytes'] for f in files),'bytes; consumer BBN',len(raw),'bytes')
if __name__=='__main__':main()
