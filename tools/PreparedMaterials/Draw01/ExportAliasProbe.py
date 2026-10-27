"""Build read-only native GPU filter comparisons for measured Draw01 aliases."""
import json
import struct
from pathlib import Path
import numpy as np
from AuditStructureReuse import bindings

root = Path(__file__).resolve().parents[3]
report = json.loads((root/'artifacts/prepared-materials/Draw01/structure-reuse-audit.json').read_text())
payload = root/'artifacts/prepared-current-structures-canonical/Draw01'
pairs = []
for pair in report['permutationReuse']['candidates']:
    old = bindings(payload/'Structure'/pair['old']/'Structure.Surface')[1]
    keep = bindings(payload/'Structure'/pair['keeper']/'Structure.Surface')[1]
    matrix = np.array(pair['uvAffineRows'])
    channels = []
    for name, cid in old.items():
        a = payload/'Shared'/f'{cid}.Texture'
        b = payload/'Shared'/f'{keep[name]}.Texture'
        w0,h0 = struct.unpack_from('<HH',a.read_bytes(),2)
        w1,h1 = struct.unpack_from('<HH',b.read_bytes(),2)
        metric = np.diag([w1,h1]) @ matrix[:,:2] @ np.diag([1/w0,1/h0])
        sv = np.linalg.svd(metric,compute_uv=False)
        channels.append(dict(name=name,oldPath=str(a),keepPath=str(b),minimumStretch=float(min(sv)),maximumStretch=float(max(sv))))
    pairs.append(dict(oldKey=pair['old'],keepKey=pair['keeper'],matrix=matrix.flatten().tolist(),channels=channels))
path=root/'artifacts/prepared-materials/Draw01/alias-filter-input.json'
path.write_text(json.dumps(dict(pairs=pairs),indent=2)+'\n')
print(path)
