"""Create a reviewable shared-layer patch without modifying shared production code."""
import difflib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent/'NativeIntegration'
relative='src/BridgeBuilder/Bridges/Common/Movable/MovableStructureTiles.cs'
before=(root/relative).read_bytes().decode('utf-8-sig')
needle='            var key=MovableStructureKey.For(digest,unit.Triangle,unit.Bounds,unit.A,unit.B,unit.C);'
assert before.count(needle)==1
addition='''            if (_style == "Draw01")
            {
                // The compatible original-Surface path above remains unchanged.
                // Old per-sampler folded images are derived artifacts, not native
                // prototype sampling semantics or an allowed fallback.
                Draw01NativeMaterialBinding.RequireCompatible(digest, unit);
                slots.Add(unit.Id, new Slot { Surface = source, Original = unit });
                originals++;
                continue;
            }
'''
if '\r\n' in before:
    addition=addition.replace('\n','\r\n')
after=before.replace(needle,addition+needle)
(folder/'MovableStructureTiles.patch').write_bytes(''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='a/'+relative,tofile='b/'+relative)).encode())
data=json.loads((root/'artifacts/prepared-materials/Draw01/source-sharing/sampling-map.json').read_text())
pending=[]
for sampler in data['samplers']:
    proof=sampler['receiverEvidence']
    if proof['requiresLocalSampling'] or not proof['observations']:
        pending.append(dict(key=sampler['key'],sourceMaterial=sampler['sourceMaterial'],
            sourceDigest=sampler['sourceDigest'],evidence=proof,
            reason='Authored atlas-domain mapping not proven for every receiver; legacy folded image is not native semantics.'))
(folder/'Unsupported.json').write_text(json.dumps(dict(scope='Draw01 production sampler inventory; prior shape-matched receiver observations',
    unresolved=pending),indent=2)+'\n')
print(f'PASS reviewable common patch generated; {len(pending)} sampler keys retain explicit unresolved coverage.')
