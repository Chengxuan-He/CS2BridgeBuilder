"""Generate the exact Lift02 sampler catalogue and a reviewable shared-entry patch."""
import difflib
import json
from pathlib import Path
from IntegrateStructures import ROOT, sha

tool = Path(__file__).resolve().parent
out = tool / 'native-binding'
mapping = json.loads((ROOT / 'artifacts/prepared-materials/Lift02/source-sharing/shared-sampling-map.json').read_bytes())
inventory = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
assert sha(inventory.read_bytes()) == mapping['inventorySha256']
units = mapping['samplers']
assert len({u['key'] for u in units}) == len(units)
lines = ['// Generated from immutable Lift02 production sampler/source identities.',
         '// Inventory SHA256: ' + mapping['inventorySha256'],
         'using System;', 'using System.Collections.Generic;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Lift02NativeStructureCatalogue', '{',
         '    internal static readonly HashSet<string> SourceDigests = new(StringComparer.Ordinal)', '    {']
lines += ['        "' + value + '",' for value in sorted({u['sourceDigest'] for u in units})]
lines += ['    };', '    internal static readonly Dictionary<string,string> SamplerSources = new(StringComparer.Ordinal)', '    {']
lines += ['        ["' + u['key'] + '"] = "' + u['sourceCid'] + '",' for u in sorted(units, key=lambda u:u['key'])]
lines += ['    };', '}', '']
(out / 'Lift02NativeStructureCatalogue.g.cs').write_text('\n'.join(lines), encoding='utf-8')
path = 'src/BridgeBuilder/Bridges/Common/Movable/MovableStructureTiles.cs'
before = (ROOT / path).read_text(encoding='utf-8-sig')
anchor = '            var key=MovableStructureKey.For(digest,unit.Triangle,unit.Bounds,unit.A,unit.B,unit.C);'
assert before.count(anchor) == 1
insert = '''            if(_style=="Lift02")
            {
                var native=Lift02NativeStructureBinding.Bind(source,digest,unit);
                slots.Add(unit.Id,new Slot{Surface=native,Original=unit});
                originals++;
                continue;
            }
'''
after = before.replace(anchor, insert + anchor)
patch = ''.join(difflib.unified_diff(before.splitlines(keepends=True), after.splitlines(keepends=True), fromfile='a/'+path, tofile='b/'+path))
(out / 'shared-entry.patch').write_bytes(patch.encode('utf-8'))
report = dict(samplerCount=len(units), sourceCids=len({u['sourceCid'] for u in units}), sourceDigests=len({u['sourceDigest'] for u in units}),
              sharedSourceSha256=sha((ROOT / path).read_bytes()), patchApplied=False,
              supported='Native original Surface with actual receiver inside source region and preserved Float32 UV gradients',
              unsupported='Source-region expansion or precision loss without a validated native representation',
              texturesCreated=0, runtimeBaking=False)
(ROOT / 'artifacts/prepared-materials/Lift02/source-sharing/native-binding-build.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
