"""Summarize the private production-plan observer without changing resources."""
import hashlib
import json
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[3]
a=root/'artifacts/prepared-materials/Lift03'
r=json.loads((a/'rigid-receiver-plans.json.rigid.json').read_text())
c=json.loads((a/'rigid-receiver-plans.json.classifications.json').read_text())
i=json.loads((root/'artifacts/uv-density/structure-tessellated-sources.json').read_text())
report=json.loads((a/'rigid-receiver-plans.json').read_text())
def signature(u):
    keys=('a','b','c') if u['triangle'] else ('bounds',)
    return u['triangle'],b''.join(struct.pack('<f',v) for k in keys for v in u[k])
needed={signature(x['Recipe']) for x in r}
units=[u for u in i['units'] if u['style']=='Lift03']
unseen=[u['key'] for u in units if signature(u) not in needed]
result=dict(classification='Exact equality of actual XYZ vertex increments; no epsilon or topology inference',
    coverage=dict(scenarios=len(c),inventoryUnits=len(units),inventorySamplerSignaturesObserved=len(units)-len(unseen),unseenKeys=unseen,
                  limit='Sampler-signature coverage across recorded scenarios; not a proof over all real-valued widths.'),
    counts={k:sum(x[k] for x in c) for k in ('allFaces','allExactRigid','unchangedExactRigid','changedExactRigid','originalSourceChecks','retileChecks')},
    retile={k:sum(x[k] for x in r) for k in ('Faces','ExactRigid','NonRigid','OriginalRawEligible','RawAndReprojectedEqual','RawButReprojectedDiffer')},
    validation={k:report[k] for k in ('failures','packingFailures','footprintFailures')},
    removableUnitsProven=0,
    assembly=json.loads((a/'rigid-probe-assembly.json').read_text(encoding='utf-8-sig')),
    files={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [
        root/'artifacts/uv-density/layout-plans.bin',
        root/'src/BridgeBuilder/Bridges/Common/Movable/MovableUvFrame.cs',
        root/'src/BridgeBuilder/Bridges/Common/Movable/MovableStructurePacking.cs',
        root/'tools/PreparedMaterials/Lift03/ExportRigidProbe.py']})
assert not unseen
assert not any(result['validation'].values())
(a/'rigid-receiver-summary.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
