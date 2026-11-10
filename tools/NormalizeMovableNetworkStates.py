"""Normalize regenerated mechanism state appends without changing measured binary data."""
from pathlib import Path
import re
import sys
ROOT = Path(__file__).resolve().parents[1]
pattern = re.compile(r'target\.(m_(?:Edge|Node)States) = \(target\.\1 \?\? Array\.Empty<(Net(?:Edge|Node)StateInfo)>\(\)\)\.Concat\((new\[\] \{[^\n]+)\)\.ToArray\(\);')
changed = []
for path in (ROOT / 'src/BridgeBuilder/Bridges/Types').glob('*/*NetworkData.g.cs'):
    source = path.read_text(encoding='utf-8-sig')
    result, count = pattern.subn(lambda m: f'target.{m[1]} = MovableNetworkStates.Append(target.{m[1]}, {m[3]});', source)
    if count:
        changed.append(str(path.relative_to(ROOT)))
        if '--check' not in sys.argv: path.write_text(result, encoding='utf-8')
if changed and '--check' in sys.argv: raise SystemExit('Unnormalized mechanism appearance state appends: ' + ', '.join(changed))
print('PASS mechanism-state normalization; changed=' + str(len(changed)))
