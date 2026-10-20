"""Export audited Lift02 mesh-0 caches for the C# width composition check.

Usage: python ExportInputs.py RESEARCH_DIRECTORY
The caches are produced by native decoding and checked by the style metaprograms.
This exporter does not establish their provenance or replace those checks.
"""
from pathlib import Path
import sys
import numpy as np

root = Path(sys.argv[1])
for lod, (vertices, triangles) in enumerate(((199372, 134080), (159895, 80439), (4152, 2600))):
    data = np.load(root / f'lift02-main-{lod}-0.npz')
    assert data['positions'].shape == (vertices, 3)
    assert data['indices'].size == triangles * 3
    with (root / f'lift02-widthcheck-{lod}.bin').open('wb') as output:
        np.array([vertices, triangles * 3], dtype='<i4').tofile(output)
        data['positions'].astype('<f4').tofile(output)
        data['indices'].astype('<i4').tofile(output)
