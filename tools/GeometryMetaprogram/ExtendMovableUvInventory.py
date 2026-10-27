"""Extend offline UV evidence to literal movable render sources omitted by an earlier dump.

Accept only matching original Geometry names with a unique content hash. Ambiguous
or absent sources are reported, never substituted by a generated bridge.
"""
import hashlib
import json
from pathlib import Path
import re
import sys
import numpy as np
from UvDensityAudit import NativeGeometry

root, geometry_list, existing, output, plugin = map(Path, sys.argv[1:])
names = set()
for family in (root/'src/BridgeBuilder/Bridges/Types').iterdir():
    if not family.name.startswith(('Draw', 'Lift', 'PedestrianDraw')):
        continue
    for source in family.glob('*.cs'):
        names.update(re.findall(r'"((?:DrawBridge|LiftBridge|PedestrianDrawBridge|Default_Base)[^"\r\n]* Mesh)"', source.read_text(encoding='utf-8-sig')))
paths = {}
for value in geometry_list.read_text(encoding='utf-8-sig').splitlines():
    path = Path(value)
    if path.stem in names and path.is_file():
        paths.setdefault(path.stem, []).append(path)
output.mkdir(parents=True, exist_ok=True)
reader = NativeGeometry(plugin)
report = {'existing': [], 'added': [], 'missing': [], 'ambiguous': []}
for name in sorted(names):
    if (existing/(name+'.npz')).is_file():
        report['existing'].append(name)
        continue
    candidates = paths.get(name, [])
    if not candidates:
        report['missing'].append(name)
        continue
    hashes = {hashlib.sha256(p.read_bytes()).hexdigest() for p in candidates}
    if len(hashes) != 1:
        report['ambiguous'].append({'name': name, 'candidates': list(map(str, candidates))})
        continue
    source = sorted(candidates, key=lambda p: (len(str(p)), str(p)))[0]
    arrays = {}
    for index, mesh in enumerate(reader.read(source)):
        arrays.update({f'mesh{index}_{key}': value for key, value in mesh.items()})
    np.savez_compressed(output/(name+'.npz'), **arrays)
    report['added'].append({'name': name, 'source': str(source), 'sha256': next(iter(hashes))})
    print('Decoded '+name, flush=True)
(output/'coverage.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print({key: len(value) for key, value in report.items()})
