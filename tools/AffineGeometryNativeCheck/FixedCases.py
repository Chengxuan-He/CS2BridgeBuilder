"""Create fixed-family test rows from the native export receipt, without guessing CIDs."""
from pathlib import Path
import json
import re
import sys

root = Path(sys.argv[1])
rows = []
for record in json.loads((root / 'fixtures.json').read_text()):
    name = re.sub(r'_[0-9a-f]{32}\.Geometry$', '', record['Entry'])
    if record['Mesh'] != 0:
        continue
    cases = []
    if name.startswith('SuspensionBridge03PillarTop'):
        cases = [('GoldenOrnamentGeometry', 'TryApply')]
    elif name.startswith('SuspensionBridge03Support'):
        cases = [('GoldenSupportGeometry', 'TryWiden')]
    elif name.startswith('PedestrianBridgeCoveredWood01Cover'):
        cases = [('CoveredWoodGeometry', 'TryApply')]
    elif name.startswith('ExtradosedBridge02PillarCablesNet'):
        cases = [('APylonCableGeometry', 'TryApply')]
    elif name.startswith('TrussArchBridge02Net'):
        cases = [('TrussArch02Geometry', 'TryWidenTowerPart')]
    elif re.fullmatch(r'TrussArchBridge03Net(?:_LOD[12])?', name):
        cases = [('TrussArch03Geometry', 'TryWidenSection'), ('TrussArch03BaseGeometry', 'TryApply')]
    for owner, method in cases:
        lod = re.search(r'_LOD([12])$', name)
        rows.append('\t'.join([owner, method + '@' + name + ' Mesh', lod[1] if lod else '0', record['File']]))
(root / 'cases.tsv').write_text('\n'.join(rows) + '\n')
print(f'Wrote {len(rows)} native fixed-family entry points.')
