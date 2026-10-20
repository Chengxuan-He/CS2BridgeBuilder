"""Offline separation of the measured fixed-span base and its two end frames."""
import json
import numpy as np


def partition(directory, lod):
    data = np.load(directory / f'lift01-approach-{lod}.npz')
    vertices, faces = data['positions'], data['indices']
    groups = json.loads((directory / f'lift01-approach-{lod}-parts.json').read_text())
    main = np.load(directory / f'lift01-main-{lod}.npz')['positions']
    main_groups = json.loads((directory / f'lift01-crossing-{lod}.json').read_text())
    # The longitudinal base uses the same authored section as the moving base.
    # Normalize floating-point centre residues for comparison only, never output.
    def profile(point):
        return (0.0 if abs(point[0]) < 1e-8 else float(point[0]), float(point[1]))
    section = {profile(main[i]) for i in main_groups[487]['vertices']}
    members = np.isin(faces, groups[4]['vertices']).all(1)
    profile_member = np.array([profile(p) in section for p in vertices])
    longitudinal = members & profile_member[faces].all(1)
    p = vertices[faces]
    end_caps = members & ((p[:, :, 2] == 32).all(1) | (p[:, :, 2] == -32).all(1))
    end_caps &= (p[:, :, 1].min(1) >= 9.61572265625) & (p[:, :, 1].max(1) <= 10.10009765625)
    end_caps &= abs(p[:, :, 0]).max(1) <= 3.39190673828125
    base = longitudinal | end_caps
    frame = members & ~base
    base_ids = np.unique(faces[base])
    frame_ids = np.unique(faces[frame])
    assert longitudinal.sum() == 832 and base.sum() == 864 and frame.sum() == 312
    assert len(base_ids) == 1060 and len(frame_ids) == (636, 460)[lod]
    assert not set(base_ids).intersection(frame_ids)
    assert set(base_ids).union(frame_ids) == set(groups[4]['vertices'])
    contacts = set(map(tuple, vertices[base_ids])).intersection(map(tuple, vertices[frame_ids]))
    assert contacts == {(x, 9.61572265625, z) for x in (-3.39190673828125, 0, 3.39190673828125) for z in (-32, 32)}
    for point in contacts:
        assert np.sign(point[0]) == np.clip(point[0] / 3.39190673828125, -1, 1)
    return base_ids.tolist(), frame_ids.tolist()
