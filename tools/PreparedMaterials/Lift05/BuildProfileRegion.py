"""Reproduce measured profile metadata and demand-initialized owner runtime declarations."""
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/material-resume/Lift05/profile-unit'
evidence = json.loads((BASE / 'source-evidence.json').read_text())
assert evidence['sourceFaces'] == [9892, 9893]
assert evidence['fullNativeRectangle'] and evidence['physicalPeriodU'] == 4
bounds = evidence['uvMin'] + evidence['uvMax']
assert bounds == [.6162109375, .890625, .748046875, .8955078125]
raw = b'BBN1' + struct.pack('<7i', 1, 0, 0, 0, 0, 0, 4) + struct.pack('<4d', *bounds)
raw += hashlib.sha256(raw).digest()
(ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_ProfileRegion.bin').write_bytes(raw)
template = Path(__file__).with_name('Lift05RepeatRegions.cs.template').read_text()
assert 'd1b116c5e0d5370d5a20b11fd85e5de3b71558b9db2cefa47425b6ba80788d31' in template
assert '{{PROFILE_KEY}}' not in template
(ROOT / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RepeatRegions.cs').write_text(template)
print('PASS Lift05 profile metadata and explicit demand initialization')
