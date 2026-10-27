"""Install native single-axis profiles after complete channel/mip validation."""
import hashlib
import json
from pathlib import Path
import shutil
import struct

ROOT = Path(__file__).resolve().parents[3]
candidate = ROOT/'artifacts/agent-material-env/Lift04/structural-profiles/guarded'
retained = ROOT/'artifacts/prepared-materials/Lift04/structure-inventory/guarded-profiles'
target = ROOT/'assets/Materials/Lift04'
report = json.loads((candidate/'report.json').read_text())
patterns = json.loads((ROOT/'artifacts/prepared-materials/Lift04/structure-inventory/beam-profile-patterns.json').read_text())
profiles = {p['id']: p for p in patterns['profiles']}
keys = {}
for result in report['units']:
    name = result['id'].removeprefix('Lift04-')
    keys[name] = result['key']
    width, mips = (532, 10) if name == 'wide-beam-profile' else (32, 9)
    check = json.loads((candidate/('native-check-'+result['id']+'.json')).read_text())
    assert check['completeGraph'] and check['nativeHeaders'] and check['mips'] == mips*3
    assert all(c['width'] == width and c['height'] == 404 and c['mips'] == mips and c['densityRatioX'] == c['densityRatioY'] == 1 for c in result['channels'])
    assert all(l['maxLinearError'] <= .005 and l['maxRepeatSeamError'] <= .0001 for l in check['levels'])
source = candidate/'output/Lift04'
added = 0
for path in source.rglob('*'):
    if not path.is_file(): continue
    destination = target/path.relative_to(source)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists(): assert destination.read_bytes() == path.read_bytes()
    else:
        shutil.copyfile(path, destination)
        added += path.stat().st_size
values = []
bindings = []
for name in ('wide-beam-profile', 'long-beam-profile', 'outer-flange'):
    unit_name = 'wide-beam-profile' if name == 'long-beam-profile' else name
    x0, y0, x1, _ = profiles[unit_name]['coveragePixels']
    values.extend(v/4096 for v in (x0, y0, x1, y0+404))
    values.extend(v/4096 for v in profiles[name]['coveragePixels'])
    i = len(values)-8
    bindings.append('        regions.Add(new Region("'+keys[unit_name]+'",\n            '+', '.join('profiles['+str(j)+']' for j in range(i,i+8))+', repeatU: false, repeatV: true));')
raw = b'BBN1'+struct.pack('<7i',1,0,0,0,0,0,len(values))+struct.pack('<'+str(len(values))+'d',*values)
(ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_StructuralProfileRegions.bin').write_bytes(raw+hashlib.sha256(raw).digest())
path = ROOT/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RepeatUnits.cs'
code = path.read_text()
start = '        // Single-axis profiles: measured coverage remains independent of unit bounds.'
end = '        // End single-axis profiles.'
block = start+'\n        var profiles = Lift04StructuralProfileData.Values.F64;\n'+'\n'.join(bindings)+'\n'+end
if start in code:
    a = code.index(start); b = code.index(end,a)+len(end)
    code = code[:a]+block+code[b:]
else:
    needle = '            beam[0], beam[1], beam[2], beam[3], beam[4], beam[5], beam[6], beam[7]));'
    assert code.count(needle) == 1
    code = code.replace(needle, needle+'\n'+block)
if 'private static class Lift04StructuralProfileData' not in code:
    code = code.rstrip()[:-1]+'''    // Embedded numerical arrays: Types_Lift04_StructuralProfileRegions
    private static class Lift04StructuralProfileData
    {
        static Lift04StructuralProfileData() { }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_StructuralProfileRegions");
    }
}
'''
path.write_text(code)
shutil.copytree(candidate, retained/'candidate', dirs_exist_ok=True)
shutil.copyfile(candidate.parent/'inputs/manifest.json', retained/'manifest.json')
files = [dict(path=p.relative_to(target).as_posix(), bytes=p.stat().st_size) for p in target.rglob('*') if p.is_file()]
(retained/'integration.json').write_text(json.dumps(dict(status='FINITE_PROFILE_COVERAGE_INTEGRATED', addedBytes=added, preparedBytes=sum(f['bytes'] for f in files), preparedFiles=len(files), keys=keys, files=files, completeOwnerClosureVerified=False, gameVisualAcceptance=False), indent=2)+'\n')
print(json.dumps(dict(addedBytes=added, preparedBytes=sum(f['bytes'] for f in files), keys=keys)))
