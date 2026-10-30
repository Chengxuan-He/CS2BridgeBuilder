"""Conservative Draw02 alias audit; unresolved routes are never retired."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
plan = next(r for r in json.loads((ROOT/'artifacts/material-remap/shared/outputs/legacy-alias-cleanup-plan.json').read_text()) if r['style'] == 'Draw02')
aliases = {r['unitKey']: r['unit'] for r in json.loads((ROOT/'artifacts/prepared-materials/Draw02/repeat-units-v2/composite/all-aliases.json').read_text())['aliases']}
surfaces = list((ROOT/'assets/Materials/Draw02').rglob('*.Surface'))
rows = []
for row in plan['candidateLegacyRoadAliases']:
    descriptor = ROOT/row['descriptor']
    assert Path(str(descriptor)+'.cid').read_text().strip() == row['surfaceCid']
    dependencies = []
    for dep in row['dependencies']:
        payload = ROOT/dep['path']; cid = dep['cid']
        assert Path(str(payload)+'.cid').read_text().strip() == cid
        encoded = bytes(int(cid[i:i+2][::-1], 16) for i in range(0, 32, 2))
        users = sorted(str(s.relative_to(ROOT)) for s in surfaces if encoded in s.read_bytes())
        assert sorted(dep['consumers']) == users
        dependencies.append(dict(cid=cid, consumers=users, bytes=payload.stat().st_size,
            sha256=hashlib.sha256(payload.read_bytes()).hexdigest()))
    role = aliases[row['key']]
    if role.endswith('fixed-road'):
        reason = 'Legacy fixed short/repeat road still uses RoadAppearance slots 4/5/12/13. Retain.'
    elif role.endswith('moving-carriageway'):
        reason = 'Moving native silhouette ends at 78.69921875; legacy fixed threshold is 78.7421875. Fallback in this gap can name old moving slots. Receiver reachability is not yet disproved. Retain.'
    else:
        reason = 'New walk wrappers replace normal measured domains; final per-input receiver reachability audit is pending. Retain conservatively until that proof.'
    rows.append(dict(key=row['key'], role=role, descriptorSha256=hashlib.sha256(descriptor.read_bytes()).hexdigest(),
        action='retain', reason=reason, dependencies=dependencies))
report = dict(rows=rows, retiredFiles=0, removedBytes=0,
    completeOwnerBytesMeasured=False,
    note='Reverse scan checked every currently packaged Draw02 Surface. No source or candidate resource was moved.')
out = ROOT/'artifacts/material-remap/Draw02/legacy-alias-audit.json'
out.write_text(json.dumps(report, indent=2)+'\n')
print('PASS reverse descriptor dependency scan; all five aliases retained pending route completion.')
