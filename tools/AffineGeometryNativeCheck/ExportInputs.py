"""Copy explicitly selected audited NPZ geometry into standalone comparison fixtures.

Usage: python ExportInputs.py RESEARCH_DIRECTORY CASES_TSV OUTPUT_DIRECTORY
TSV rows: class, method, LOD, original-NPZ filename (tab separated).
Never writes into the research directory. NPZ decoding provenance remains the
responsibility of the original native audit/metaprogram for each selected mesh.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

research, cases, output = map(Path, sys.argv[1:])
output.mkdir(parents=True, exist_ok=True)
rows = []
receipts = {}
for line in cases.read_text(encoding='utf-8-sig').splitlines():
    if not line.strip() or line.startswith('#'):
        continue
    name, method, lod, filename = line.split('\t')
    source = research / filename
    target = output / (source.stem + '.bin')
    if filename not in receipts:
        with np.load(source) as data:
            positions = data['positions'].astype('<f4')
            indices = data['indices'].astype('<i4').reshape(-1)
        assert positions.ndim == 2 and positions.shape[1] == 3
        with target.open('wb') as stream:
            np.array([len(positions), len(indices)], dtype='<i4').tofile(stream)
            positions.tofile(stream)
            indices.tofile(stream)
        receipts[filename] = {'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                              'fixtureSha256': hashlib.sha256(target.read_bytes()).hexdigest(),
                              'vertices': len(positions), 'indices': len(indices)}
    rows.append('\t'.join([name, method, lod, target.name]))
(output / 'cases.tsv').write_text('\n'.join(rows) + '\n', encoding='utf-8')
(output / 'fixtures.json').write_text(json.dumps(receipts, indent=2) + '\n', encoding='utf-8')
print(f'Exported {len(receipts)} original meshes for {len(rows)} production entry points.')
