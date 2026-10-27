"""Exercise the production Lift04 extractor against measured arrays and corruption."""
import hashlib
import json
from pathlib import Path
import tempfile
from ExportRecipes import numbers, source_recipes, repeat_key


root = Path(__file__).resolve().parents[3]
units, evidence = source_recipes(root)
recorded = json.loads((Path(__file__).parent / 'recipes.json').read_text())
assert len(units) == 1 and units[0]['regions'] == [0, 1, 2, 3]
assert units[0]['cutout'] and units[0]['bands'] is None and not units[0]['rows']
for name, value in units[0].items():
    assert recorded['units'][0][name] == value, name
for name, value in evidence.items():
    assert recorded['evidence'][name] == value, name
assert repeat_key(recorded['units'][0]['sourceDigest'], units[0]) == recorded['units'][0]['key']
source = root / 'assets/HardcodedNumbers/Lift04/Types_Lift04_Lift04RoadAppearance_cs.bin'
data = source.read_bytes()
with tempfile.TemporaryDirectory(prefix='BridgeBuilder-Lift04-recipes-') as folder:
    target = Path(folder) / 'invalid.bin'
    for invalid in (data[:-1], data[:35] + bytes([data[35] ^ 1]) + data[36:],
                    data[:-32] + hashlib.sha256(b'wrong').digest()):
        target.write_bytes(invalid)
        try:
            numbers(target)
        except ValueError:
            pass
        else:
            raise AssertionError('Damaged BBN resource accepted')
assert hashlib.sha256(source.read_bytes()).hexdigest() == evidence['numericalResourceSHA256']
print('PASS Lift04 repeat extraction: exact Float32 bounds, four aliases, immutable source, damaged BBN rejected.')
