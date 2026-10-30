"""Prepare a v3 offline input; never publish or accept historical baked output."""
import copy
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]

def main():
    prior=ROOT/'artifacts/material-resume/Lift05/profile-unit'
    evidence=json.loads((prior/'source-evidence.json').read_text())
    unit=copy.deepcopy(json.loads((prior/'input/manifest.json').read_text())['units'][0])
    assert evidence['fullNativeRectangle'] and evidence['physicalPeriodU']==4
    assert evidence['sourcePixels']==[540,20] and len(evidence['sharedPhysicalSeam'])>=1
    assert unit['min']==[.6162109375,.890625] and unit['max']==[.748046875,.8955078125]
    out=ROOT/'artifacts/material-remap/Lift05/offline-repeat-candidate'
    (out/'source').mkdir(parents=True,exist_ok=True)
    channels=[]
    for channel in unit['channels']:
        path=Path(channel['path']);digest=hashlib.sha256(path.read_bytes()).hexdigest()
        assert digest==channel['sha256'],path
        retained=out/'source'/path.name
        if not retained.exists():shutil.copyfile(path,retained)
        assert hashlib.sha256(retained.read_bytes()).hexdigest()==digest
        channel['path']=str(retained);path=retained
        channels.append(dict(name=channel['name'],path=str(path),sha256=digest,bytes=path.stat().st_size))
    unit.update(repeatAxes='U',finiteSourceGrid=True,finiteGridMip=0,
        normalTransform=[1,0,0,0,1,0,0,0,1],status='CANDIDATE_REQUIRES_COMPLETE_MIP_FILTER_NATIVE_VALIDATION')
    out.mkdir(parents=True,exist_ok=True)
    manifest=dict(recipe='BridgeBuilder/RepeatUnits/3',style='Lift05',units=[unit])
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (out/'scope.json').write_text(json.dumps(dict(sourceEvidence=str(prior/'source-evidence.json'),
        sourceEvidenceSha256=hashlib.sha256((prior/'source-evidence.json').read_bytes()).hexdigest(),
        profiles=129,retileDomainRecords=149,sourceFaces=237,sourcePixels=[540,20],physicalPeriodU=4,
        sourceChannels=channels,repeatAxes=['U'],finiteAxes=['V'],
        geometry='Preserve pre-wrap Plan topology. Continuous component-phase U replaces period clipping. Existing legitimate fragments remain.',
        finiteGridMip=0,finiteGridStatus='Initial source-grid candidate only; no deep-mip alignment or filtering acceptance is claimed.',
        pending=['Shared Retile continuous component UV interface','Offline v3 bake in assigned environment','All-channel complete-mip boundary/phase/filter tests','Native loading and upload','Complete persisted owner graph <=10000000 bytes'],
        localization='No user-visible runtime text changed.'),indent=2)+'\n')
    print('PASS v3 candidate input: verified original 540x20 panel and source channel hashes; no bake or production changes.')

if __name__=='__main__':main()
