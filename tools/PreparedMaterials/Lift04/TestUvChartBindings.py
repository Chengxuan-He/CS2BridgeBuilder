"""Verify the installed Lift04 chart resource against exact original domains."""
import hashlib
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tools/GeometryMetaprogram'))
from Lift04UvChartBindings import read_domains, reliable_loop

raw = (ROOT/'assets/HardcodedNumbers/Lift04/Lift04UvCharts.bin').read_bytes()
assert raw[:8] == b'BBN1\x01\0\0\0' and hashlib.sha256(raw[:-32]).digest() == raw[-32:]
ni, nu, nl, nul, nf, nd = struct.unpack_from('<6i', raw, 8)
assert (nu,nl,nul,nf) == (0,0,0,0) and len(raw) == 64+ni*4+nd*8
ints = struct.unpack_from('<'+'i'*ni, raw, 32)
values = struct.unpack_from('<'+'d'*nd, raw, 32+ni*4)
assert ints[0] == 1
loops = []
for offset in ints[2:2+ints[1]]:
    assert ints[offset] == 1
    start, count = ints[offset+1:offset+3]
    loops.append([[values[start+p*2], values[start+p*2+1]] for p in range(count)])
measured = json.loads((ROOT/'artifacts/prepared-materials/Lift04/shared-sampling/native-charts.json').read_text())
at, accepted, rejected = 2+ints[1], 0, 0
assert ints[at] == len(measured['meshes']); at += 1
references = {}
for mesh_index, mesh in enumerate(measured['meshes']):
    name = 'UvDomain_'+mesh['render'].replace(' ','_')+'_'+str(mesh['mesh'])+'.bin'
    domains = read_domains(ROOT/'assets/HardcodedNumbers/Lift04'/name)
    assert ints[at] == len(domains); at += 1
    for faces in domains:
        count = ints[at]; at += 1
        refs = ints[at:at+count]; at += count
        ids = sorted(set(mesh['faceChart'][f] for f in faces))
        expected = []
        for chart_id in ids:
            loop = None if chart_id < 0 else reliable_loop(mesh, mesh['charts'][chart_id])
            if loop is None: expected = []; break
            expected.append(loop)
        assert [loops[r] for r in refs] == expected
        for chart_id, ref in zip(ids, refs):
            key = mesh_index, chart_id
            assert references.setdefault(key, ref) == ref
        accepted += bool(refs); rejected += not bool(refs)
assert len(set(references.values())) == len(references) # No cross-part shared chart identity.
assert at == ints[2] # Variable domain records end at the first shared chart record.
print(f'PASS Lift04 runtime bindings: {accepted} accepted, {rejected} conservative-empty domains; exact source loops, original indices, shared identities, BBN checksum.')
