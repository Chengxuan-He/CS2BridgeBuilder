"""Retain and install one native beam unit with independent original coverage."""
import hashlib
import json
from pathlib import Path
import shutil
import struct

ROOT=Path(__file__).resolve().parents[3]
KEY='649d6588b132d3b40bd9e329c2071e6df523b547530119704f5c3699143144a7'


def install(code,target):
    retained=ROOT/'artifacts/prepared-materials/Lift04/beam-repeat'
    candidate=ROOT/'artifacts/agent-material-env/Lift04/beam-repeat/candidate'
    if candidate.exists():
        shutil.copytree(candidate,retained/'candidate',dirs_exist_ok=True)
        shutil.copyfile(candidate.parent/'inputs/manifest.json',retained/'manifest.json')
    candidate=retained/'candidate'
    result=json.loads((candidate/'report.json').read_text())['units'][0]
    check=json.loads((candidate/'native-check.json').read_text())
    assert result['key']==KEY and result['aliases']==[]
    assert check['completeGraph'] and check['nativeHeaders'] and check['mips']==27
    assert all((c['width'],c['height'],c['mips'],c['densityRatioX'],c['densityRatioY'])==(46,404,9,1,1) for c in result['channels'])
    source=candidate/'output/Lift04'
    assert struct.unpack_from('<i',(source/'RepeatUnits'/KEY/'Repeat.Surface').read_bytes(),2)[0]==0
    for path in source.rglob('*'):
        if not path.is_file():continue
        destination=target/path.relative_to(source)
        destination.parent.mkdir(parents=True,exist_ok=True)
        if destination.exists():assert destination.read_bytes()==path.read_bytes()
        else:shutil.copyfile(path,destination)
    proof=json.loads((retained/'source-evidence.json').read_text())
    values=proof['min']+proof['max']+proof['coverageMin']+proof['coverageMax']
    raw=b'BBN1'+struct.pack('<7i',1,0,0,0,0,0,len(values))+struct.pack('<8d',*values)
    (ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_BeamRepeatRegions.bin').write_bytes(raw+hashlib.sha256(raw).digest())
    needle='            values[0], values[1], values[2], values[3]));'
    assert code.count(needle)==1
    code=code.replace(needle,needle+f'''
        var beam = Lift04BeamRepeatData.Values.F64;
        regions.Add(new Region("{KEY}",
            beam[0], beam[1], beam[2], beam[3], beam[4], beam[5], beam[6], beam[7]));''')
    code=code.replace('// One measured 638 x 640 source rectangle.', '// Measured floor rectangle and beam repeat strip with separate coverage bounds.')
    code=code.rstrip()[:-1]+'''
    // Embedded numerical arrays: Types_Lift04_BeamRepeatRegions
    private static class Lift04BeamRepeatData
    {
        static Lift04BeamRepeatData() { }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_BeamRepeatRegions");
    }
}
'''
    files=[dict(path=p.relative_to(target).as_posix(),bytes=p.stat().st_size) for p in sorted(target.rglob('*')) if p.is_file()]
    report=dict(status='INTEGRATED_PENDING_HUMAN_VISUAL_ACCEPTANCE',bindingKey=KEY,template=0,
                unitPixels=[46,404],coveragePixels=[46,1266],densityRatio=[1,1],channelMips=[9,9,9],
                aliases=[],preparedFiles=len(files),preparedBytes=sum(p['bytes'] for p in files),
                files=files,localization='No UI text changed; existing localization preserved.')
    (retained/'integration.json').write_text(json.dumps(report,indent=2)+'\n')
    return code
