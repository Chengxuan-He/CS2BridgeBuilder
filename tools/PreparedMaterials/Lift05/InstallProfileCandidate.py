"""Stage the complete unaccepted candidate graph; never deploy installed files."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'artifacts/material-remap/Lift05/offline-repeat-candidate/results'

def main():
    bake=json.loads((BASE/'bake.json').read_text());unit=bake['units'][0]
    assert len(bake['units'])==1 and unit['id']=='Lift05.UpperCrossbeam.NativeProfile'
    assert all(c['contentMin']==[0,.25] and c['contentMax']==[1,.75] for c in unit['channels'])
    source=BASE/'output/Lift05';dest=ROOT/'assets/Materials/Lift05';files=[]
    for path in sorted(source.rglob('*')):
        if not path.is_file():continue
        target=dest/path.relative_to(source);raw=path.read_bytes()
        if target.exists():assert target.read_bytes()==raw,'Existing CID payload differs: '+str(target)
        else:target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
        files.append(dict(path=str(path.relative_to(source)),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
    code='''using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Complete offline candidate graph. Filtering quality remains unaccepted;
// candidate integration is not deployment or visual acceptance.
internal static class Lift05StructuralMaterial
{
    internal static SurfaceAsset Resolve() => BridgePreparedMaterials.RepeatUnit("Lift05", "KEY");
    internal static Vector2 ContentMin => new(0f, .25f);
    internal static Vector2 ContentMax => new(1f, .75f);
}
'''.replace('KEY',unit['key'])
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05StructuralMaterial.cs').write_text(code)
    report=dict(bindingKey=unit['key'],surfaceCid=unit['surfaceCid'],contentMin=[0,.25],contentMax=[1,.75],
        channels=[dict(name=c['name'],cid=c['cid'],bytes=c['bytes']) for c in unit['channels']],
        controlCid='729893d593df5d52ba911bc1e7eab77b',files=files,closureBytes=sum(f['bytes'] for f in files),
        accepted=False,limitations='Candidate graph only. Boundary filtering fails equivalence validation; no deployment. Full generated-owner closure remains unmeasured.')
    (BASE/'candidate-binding.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
