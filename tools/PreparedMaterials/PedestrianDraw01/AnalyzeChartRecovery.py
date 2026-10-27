"""Test exact native chart boundaries after removing zero-area UV triangles.

No production data is written. Degenerate triangles have no filled UV area;
their dangling edges must not invalidate otherwise closed authored boundaries.
The existing strict loop/area checks still reject overlapping or ambiguous data.
"""
import hashlib
import json
import struct
from pathlib import Path
from BuildUvCharts import loops_for
from RecoverNativeBoundaries import recover

ROOT = Path(__file__).resolve().parents[3]
PATH = ROOT/'artifacts/prepared-materials/PedestrianDraw01/shared-sampling/native-charts.json'
OUT = ROOT/'artifacts/prepared-materials/PedestrianDraw01/chart-recovery-v1'

def main():
    source = json.loads(PATH.read_text())
    charts = {c['key']: c for c in source['charts']}
    original = {key: loops_for(c) for key, c in charts.items()}
    recovered, candidates = recover(ROOT, source, original, loops_for)
    before = after = 0
    recovered_domains = []
    for mesh in source['meshes']:
        for domain in mesh['domainCharts']:
            keys = domain['chartKeys']
            old = all(original[k] for k in keys)
            new = all(original[k] or k in recovered for k in keys)
            before += bool(old)
            after += bool(new)
            if new and not old:
                recovered_domains.append(dict(render=mesh['render'], mesh=mesh['mesh'], domain=domain['domain']))
    report = dict(sourceSHA256=hashlib.sha256(PATH.read_bytes()).hexdigest(),
        originalReliableDomains=before, candidateReliableDomains=after, recoveredDomains=recovered_domains,
        recoveredCharts=recovered, unresolvedCharts=len(candidates)-len(recovered),
        productionIntegrated=False, limits='Exact boundary reconstruction only; no periodicity or changed-receiver acceptance established.')
    OUT.mkdir(parents=True, exist_ok=True)
    domains = [[d['chartKeys'] if all(original[k] or k in recovered for k in d['chartKeys']) else []
        for d in mesh['domainCharts']] for mesh in source['meshes']]
    used = sorted({key for mesh in domains for domain in mesh for key in domain})
    index = {key: i for i, key in enumerate(used)}
    ints, doubles = [len(domains), len(used)]+[0]*len(used), []
    for mesh in domains:
        ints.append(len(mesh))
        for domain in mesh:
            ints.extend([len(domain)]+[index[k] for k in domain])
    for key in used:
        ints[2+index[key]] = len(ints)
        loops = original[key] or recovered[key]['loops']
        ints.append(len(loops))
        for loop in loops:
            ints.extend((len(doubles), len(loop)))
            doubles.extend(x for point in loop for x in point)
    raw = b'BBN1'+struct.pack('<7i', 1, len(ints), 0, 0, 0, 0, len(doubles))
    raw += struct.pack('<'+'i'*len(ints), *ints)+struct.pack('<'+'d'*len(doubles), *doubles)
    candidate_path = OUT/'UvCharts_PedestrianDraw01.bin'
    candidate_path.write_bytes(raw+hashlib.sha256(raw).digest())
    report['candidateBinarySHA256'] = hashlib.sha256(candidate_path.read_bytes()).hexdigest()
    report['candidateBinaryBytes'] = candidate_path.stat().st_size
    (OUT/'analysis.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(dict(originalReliableDomains=before, candidateReliableDomains=after,
        recoveredCharts=len(recovered), unresolvedCharts=report['unresolvedCharts'])))

if __name__ == '__main__':
    main()
