"""Verify every retained cell of trimmed original BBN resources is an exact ordered bit subsequence."""
from pathlib import Path
import hashlib
import json
import struct
import sys

root, originals, output = map(Path, sys.argv[1:])
def arrays(path):
    raw = path.read_bytes()
    assert hashlib.sha256(raw[:-32]).digest() == raw[-32:]
    counts = struct.unpack_from('<6i', raw, 8)
    result = []
    offset = 32
    for count, size in zip(counts, [4, 4, 8, 8, 4, 8]):
        result.append([raw[offset+i*size:offset+(i+1)*size] for i in range(count)])
        offset += count*size
    return result
records = []
for original in (originals/'assets/HardcodedNumbers').rglob('*.bin'):
    current = root/original.relative_to(originals)
    if not current.exists() or current.read_bytes() == original.read_bytes():
        continue
    if not current.name.endswith('_cs.bin'):
        continue
    old, new = arrays(original), arrays(current)
    for before, after in zip(old, new):
        iterator = iter(before)
        for cell in after:
            assert any(value == cell for value in iterator), str(current)
    records.append({'resource': str(current.relative_to(root)), 'retainedCells': sum(map(len,new)),
                    'beforeSha256': hashlib.sha256(original.read_bytes()).hexdigest(),
                    'afterSha256': hashlib.sha256(current.read_bytes()).hexdigest()})
output.write_text(json.dumps(records, indent=2)+'\n')
print(f'PASS {len(records)} trimmed original resources; {sum(r["retainedCells"] for r in records)} retained cells preserve raw bits and order.')
