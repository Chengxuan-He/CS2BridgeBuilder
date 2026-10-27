"""Validate Lift04 chart coverage, exact binary data and rectangle rejection."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/GeometryMetaprogram'))
from Lift04NativeUvCharts import extract


def main():
    path = ROOT / 'artifacts/prepared-materials/Lift04/shared-sampling/native-charts.json'
    report = json.loads(path.read_text())
    binary = path.with_suffix('.bin').read_bytes()
    assert binary[:8] == b'BBN1\x01\0\0\0'
    assert hashlib.sha256(binary[:-32]).digest() == binary[-32:]
    counts = struct.unpack_from('<6i', binary, 8)
    assert counts[1:4] == (0, 0, 0) and counts[5] == 0
    ints = np.frombuffer(binary, '<i4', counts[0], 32)
    floats = np.frombuffer(binary, '<f4', counts[4], 32 + counts[0] * 4)
    assert len(binary) == 64 + 4 * (counts[0] + counts[4])
    assert tuple(ints[:2]) == (-41, len(report['meshes']))
    at, ft = 2, 0
    for mesh in report['meshes']:
        points, faces, charts = map(int, ints[at:at + 3]); at += 3
        assert (points, faces, charts) == (len(mesh['uvPoints']), mesh['sourceFaces'], len(mesh['charts']))
        assert ints[at:at + faces].tolist() == mesh['faceChart']; at += faces
        expected = np.asarray(mesh['uvPoints'], dtype='<f4').reshape(-1)
        assert np.array_equal(floats[ft:ft + points * 2].view('<u4'), expected.view('<u4')); ft += points * 2
        covered = []
        for chart in mesh['charts']:
            rectangle, loop, members, triangles, edges = map(int, ints[at:at + 5]); at += 5
            assert (rectangle, loop) == (chart['exactRectangle'], chart['singleBoundaryLoop'])
            assert ints[at:at + members].tolist() == chart['faces']; at += members
            assert ints[at:at + triangles * 3].reshape(-1, 3).tolist() == chart['sourceTriangles']; at += triangles * 3
            assert ints[at:at + edges * 2].reshape(-1, 2).tolist() == chart['boundaryEdges']; at += edges * 2
            covered.extend(chart['faces'])
        assert sorted(covered + mesh['degenerateFaces']) == list(range(faces))
        assert hashlib.sha256((ROOT / 'artifacts/uv-density/native' / (mesh['render'] + '.npz')).read_bytes()).hexdigest() == mesh['sourceNPZSHA256']
    assert at == len(ints) and ft == len(floats)
    assert len(report['sampling']) == 525 and all(s['candidates'] for s in report['sampling'])
    # An incomplete atlas triangle must not become its full rectangular bounds.
    uv = np.asarray([[0, 0], [1, 0], [1, 1], [0, 1]], dtype=np.float32)
    full = np.asarray([[0, 1, 2], [0, 2, 3]])
    assert extract(uv, full)[1][0]['exactRectangle']
    assert not extract(uv, full[:1])[1][0]['exactRectangle']
    # Duplicate uses of the same source triangle add aliases, not duplicate area.
    charts = extract(uv, np.vstack((full, full)))[1]
    assert len(charts) == 1 and charts[0]['exactRectangle'] and len(charts[0]['faces']) == 4
    print('PASS Lift04 original UV charts: 11 meshes, 63303 faces, 525 mapped samplers; exact BBN1 roundtrip, incomplete-region rejection, no texture outputs.')


if __name__ == '__main__':
    main()
