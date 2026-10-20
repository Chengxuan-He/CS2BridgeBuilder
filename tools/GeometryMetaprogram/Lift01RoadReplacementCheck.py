"""Check composed Lift01 removal membership against native geometry and fragments.
Usage: python Lift01RoadReplacementCheck.py RESEARCH_DIRECTORY
"""
from pathlib import Path
import ast
import json
import re
import sys
import numpy as np

r = Path(sys.argv[1])
src = Path(__file__).resolve().parents[2] / 'src/BridgeBuilder/Bridges/Types/Lift01'
audit = json.loads((r / 'lift-native-road-plane-candidates.json').read_text())
joined = (src / 'Lift01JoinedFrames.g.cs').read_text()
tops = (src / 'Lift01RoadTops.g.cs').read_text()
for lod in (0, 1):
    data = np.load(r / f'lift01-main-{lod}.npz')
    faces = data['indices']
    record = next(x for x in audit if x['style'] == 1 and x['lod'] == lod)
    top = {i for c in record['candidates'] if c['bone'] in (0, 4)
           and c['y'] == 9.800048828125 for i in c['faces']}
    emitted = tops.split(f'{lod} => new (int, int, int, int)[]')[1].split('}', 1)[0]
    tuples = [tuple(map(int, m)) for m in re.findall(r'\((\d+), (\d+), (\d+), (\d+)\)', emitted)]
    assert {x[0] for x in tuples} == top
    for face, a, b, c in tuples:
        assert np.array_equal(faces[face], (a, b, c))
    masks = [top]
    for name in ('SleeperRemoval', 'RailRemoval', 'CatenaryRemoval'):
        text = (src / f'Lift01{name}.g.cs').read_text()
        body = text.split(f'[] Lod{lod} =')[1].split('};', 1)[0]
        ranges = [tuple(map(int, m)) for m in re.findall(r'\((\d+), (\d+)\)', body)]
        ids = [i for a, b in ranges for i in range(a, b + 1)]
        assert len(ids) == len(set(ids)) and min(ids) >= 0 and max(ids) < len(faces)
        assert all(set(ids).isdisjoint(mask) for mask in masks)
        masks.append(set(ids))
    removed = set.union(*masks)
    fragments = joined.split(f'[] Fragments{lod} =')[1].split('};', 1)[0]
    replaced = {int(m) for m in re.findall(r'new\(0, (\d+),', fragments)}
    assert removed.isdisjoint(replaced)
    assert len(replaced) == (1088 if lod == 0 else 752)
    assert len(removed) == (60636 if lod == 0 else 42715)
    groups = json.loads((r / f'lift01-crossing-{lod}.json').read_text())
    sides = json.loads((r / f'lift01-side-{lod}.json').read_text())
    covered = np.zeros(len(data['positions']), dtype=bool)
    for part in sides:
        assert not covered[part['vertices']].any()
        covered[part['vertices']] = True
    selected = set()
    for name in ('Bases', 'TransverseParts', 'Connectors', 'JoinedFrames', 'TowerFrames', 'TowerBodies'):
        tree = ast.parse(Path(__file__).with_name(f'Lift01{name}.py').read_text())
        for node in ast.walk(tree):
            if (isinstance(node, ast.Assign) and isinstance(node.value, ast.List) and
                    any(isinstance(t, ast.Name) and t.id == 'selected' for t in node.targets)):
                ids = ast.literal_eval(node.value)
                assert selected.isdisjoint(ids)
                selected.update(ids)
    assert len(selected) == 377
    for ordinal in selected:
        ids = groups[ordinal]['vertices']
        assert not covered[ids].any()
        covered[ids] = True
    retained = np.ones(len(faces), dtype=bool)
    retained[list(removed)] = False
    assert covered[faces[retained]].all(), 'Surviving structure has unassigned vertices'
    print(f'LOD{lod}: {len(removed)} distinct road/facility faces; '
          f'{len(replaced)} structural fragments remain disjoint; '
          f'{len(faces) - len(removed)} original structural faces retained before subdivision; '
          'every surviving vertex has exactly one measured part assignment.')

# Far road sheets are not horizontal at the detailed datum. Verify the explicitly
# measured fixed/moving sheets, including topology, bones and preservation of the
# native side caps and underside. Do not apply detailed rail-removal ordinals here.
data = np.load(r / 'lift01-main-2.npz')
v, faces = data['positions'], data['indices']
emitted = tops.split('2 => new (int, int, int, int)[]')[1].split('}', 1)[0]
tuples = [tuple(map(int, m)) for m in re.findall(r'\((\d+), (\d+), (\d+), (\d+)\)', emitted)]
removed = {x[0] for x in tuples}
assert removed == {320, 321, 342, 343, 708, 709, 730, 731}
bones, active = [], None
for line in (r / 'movable-skin/LiftBridge01_LOD2 Mesh.mesh-0.obj').read_text().splitlines():
    fields = line.split()
    if not fields:
        continue
    if fields[0] == 'g':
        active = int(fields[1].removeprefix('bone_'))
    elif fields[0] == 'f':
        assert np.array_equal(faces[len(bones)], [int(x.split('/')[0])-1 for x in fields[1:]])
        bones.append(active)
for face, a, b, c in tuples:
    assert np.array_equal(faces[face], (a, b, c))
    assert bones[face] == (4 if face in (342, 343, 730, 731) else 0)
    points = v[[a, b, c]].astype(float)
    assert np.cross(points[1]-points[0], points[2]-points[0])[1] > 0
    assert points[:, 1].min() >= 10.2001953125 and points[:, 1].max() <= 10.240234375
far_joined = (src / 'Lift01FarJoinedParts.g.cs').read_text()
body = far_joined.split('[] Fragments =')[1].split('};', 1)[0]
replaced = {int(m) for m in re.findall(r'new\(0, (\d+),', body)}
assert len(replaced) == 44 and removed.isdisjoint(replaced)
assert removed.isdisjoint({338, 339, 726, 727})
for face in range(len(faces)):
    p = v[faces[face]].astype(float)
    if p[:, 1].min() == p[:, 1].max() == 12.8779296875:
        assert face not in removed, 'Far side cap incorrectly removed'
print('LOD2: 8 fixed/moving road faces verified; 44 platform fragment recipes, '
      'native side caps and undersides preserved; 768 original faces remain before subdivision.')
