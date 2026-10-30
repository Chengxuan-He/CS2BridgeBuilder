"""Check Lift01 road-removal membership against source UV classification.

This reads the current embedded production tables and original mesh fixtures.
It does not generate materials, modify geometry or certify native rendering.
"""
import hashlib
import json
from pathlib import Path
import re
import sys

import numpy as np

from BuildUvCharts import ROOT, domains
from ExportRecipes import numbers


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    output = Path(sys.argv[1])
    source = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01RoadTops.g.cs'
    resource = ROOT / 'assets/HardcodedNumbers/Lift01/Types_Lift01_Lift01RoadTops_g_cs.bin'
    values = numbers(resource)['I32']
    expanded = re.sub(r'Numeric_Types_Lift01_Lift01RoadTops_g_cs.Values.I32\[(\d+)\]',
                      lambda m: str(values[int(m[1])]), source.read_text())
    classification = ROOT / 'artifacts/prepared-materials/Lift01/non-tiling/classification.json'
    streams = json.loads(classification.read_text())['streams']
    results = []
    for lod, suffix in enumerate(('', '_LOD1', '_LOD2')):
        name = f'LiftBridge01{suffix} Mesh'
        mesh_path = ROOT / 'artifacts/uv-density/native' / (name + '.npz')
        mesh = np.load(mesh_path)
        faces = mesh['mesh0_faces']
        body = expanded.split(f'{lod} => new (int, int, int, int)[]')[1].split('}', 1)[0]
        entries = [tuple(map(int, row)) for row in re.findall(r'\((\d+), (\d+), (\d+), (\d+)\)', body)]
        removed = set()
        for face, a, b, c in entries:
            assert face not in removed
            assert np.array_equal(faces[face], (a, b, c)), (lod, face)
            removed.add(face)
        assert len(removed) == (384, 192, 8)[lod]
        record = next(s for s in streams if s['render'] == name and s['mesh'] == 0)
        members = domains(name, 0)
        exclusions = record['exclusions'].get('density-repeated-floor-region', [])
        retained = []
        wholly_removed = 0
        for domain in exclusions:
            surviving = sorted(set(members[domain]) - removed)
            if surviving:
                retained.append(dict(domain=domain, faces=surviving))
            else:
                wholly_removed += 1
        results.append(dict(lod=lod, source=name, sourceCid=record['sourceCid'],
                            meshSha256=sha(mesh_path), roadTopFaces=len(removed),
                            repeatExcludedDomains=len(exclusions),
                            whollyRemovedDomains=wholly_removed,
                            retainedRepeatDomains=retained))
    report = dict(scope='Production road-removal table versus original UV-domain membership; not material rendering acceptance',
                  sourceSha256=sha(source), resourceSha256=sha(resource),
                  classificationSha256=sha(classification), levels=results)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + '\n')
    for row in results:
        print(row['source'], 'removed road faces', row['roadTopFaces'],
              'wholly removed repeat domains', row['whollyRemovedDomains'],
              'retained repeat domains', len(row['retainedRepeatDomains']))


if __name__ == '__main__':
    main()
