"""Measure Draw01 repeat projection density against actual native road faces."""
import argparse
import hashlib
import json
import re
from pathlib import Path

import numpy as np

from ExportRecipes import evaluate, numbers, source_recipes, sampler_key


def arguments(text):
    depth, start, result = 0, 0, []
    for i, char in enumerate(text):
        if char == '(':
            depth += 1
        elif char == ')':
            depth -= 1
        elif char == ',' and depth == 0:
            result.append(text[start:i])
            start = i + 1
    result.append(text[start:])
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('native_mesh', type=Path)
    parser.add_argument('inventory', type=Path)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    code = (args.repository / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01ClosedRoad.cs').read_text(encoding='utf-8-sig')
    values = numbers(args.repository / 'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01ClosedRoad_cs.bin')
    names = {'halfWidthDelta': 0.0}
    for name in ('asphaltMin', 'asphaltWidth', 'concreteWidth'):
        names[name] = evaluate(re.search(r'(?:float|var) ' + name + r' = (.*);', code)[1], values, names)
    block = code.split('var positive = new[]', 1)[1].split('// Native right carriageway', 1)[0]
    expressions = []
    for line in block.splitlines():
        line = line.strip().rstrip(',')
        if not line.startswith('Project('):
            continue
        expressions.append(arguments(line[len('Project('):-1]))
    def project(delta):
        scope = dict(names, halfWidthDelta=delta)
        return [[np.array([evaluate(v, values, scope) for v in arguments(vector[vector.index('(')+1:-1])])
                 for vector in vectors] for vectors in expressions]
    patches = project(0.0)
    assert len(patches) == 5
    # Width changes may move the sampling phase, but must not change either
    # derivative or the immutable source crop. Include both narrowing/widening.
    checked_deltas = (-8.0, -0.5, 0.0, 0.5, 16.0, 48.0)
    for delta in checked_deltas:
        for original, adjusted in zip(patches, project(delta)):
            assert np.array_equal(original[0], adjusted[0]) and np.array_equal(original[1], adjusted[1])
            assert all(np.array_equal(original[axis][:3], adjusted[axis][:3]) for axis in (2, 3))
    inventory = json.loads(args.inventory.read_text())
    recipes, _ = source_recipes(args.repository)
    assert len(inventory['units']) == len(recipes) == 4
    for unit, recipe in zip(inventory['units'], recipes):
        assert all(unit[name] == recipe[name] for name in recipe)
        assert unit['key'] == sampler_key(unit['sourceDigest'], unit['min'], unit['max'], unit['cutout'])
    # The mask-classifier's opaque conversion sees carriageway appearance only.
    # Its moving physical domains end before the fixed-cap material boundaries.
    partition_path = args.repository / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01RoadPartitions.cs'
    partition_code = partition_path.read_text(encoding='utf-8-sig')
    partition = numbers(args.repository / 'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01RoadPartitions_cs.bin')
    assert 'TryBind(moving, carriagewayAppearance' in code
    assert 'bones[face.Bone].m_ConnectionID != 0' in code
    assert 'new Partition(moving + offset)' in partition_code
    assert 'Half(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[10]), Half(Numeric_Types_Draw01_Draw01RoadPartitions_cs.Values.I32[11])' in partition_code
    assert partition['I32'][6:12] == (0, 3, 5, 6, 7, 0)
    assert partition['F32'][22] < partition['F32'][12] and partition['F32'][25] < partition['F32'][13]
    assert partition['F32'][28] < partition['F32'][12] and partition['F32'][31] < partition['F32'][13]
    mesh = np.load(args.native_mesh)
    positions, uv, faces = (mesh['mesh0_' + name].astype(np.float64 if name != 'faces' else np.int64)
                            for name in ('positions', 'uv0', 'faces'))
    # Exact source-face evidence. The sidewalk rectangle is visible in native
    # faces 4865/4866; its source U orientation mirrors the opposite side, so
    # compare the two-dimensional metric, not one side's UV phase or sign.
    source_pairs = ((1512, 1513), (1819, 1820), (4865, 4866), (2674, 2675), (2367, 2368))
    results = []
    for patch_index, (minimum, maximum, u, v) in enumerate(patches):
        gradient = (maximum - minimum)[:, None] * np.stack((u[[0, 2]], v[[0, 2]]))
        actual = []
        for face_id in source_pairs[patch_index]:
            ids = faces[face_id]
            xz = positions[ids][:, [0, 2]]
            delta_xz = xz[1:] - xz[0]
            delta_uv = uv[ids][1:] - uv[ids][0]
            native_gradient = np.linalg.solve(delta_xz, delta_uv).T
            native_metric = native_gradient.T @ native_gradient
            emitted_metric = gradient.T @ gradient
            relative = float(np.max(np.abs(np.linalg.eigvals(np.linalg.solve(native_metric, emitted_metric))-1)))
            if relative > 0.00001:
                raise ValueError(f'Draw01 patch {patch_index} density differs from native face {face_id}: {relative}')
            actual.append(dict(face=face_id, nativeAtlasUvPerMetre=native_gradient.tolist(), relativeMetricError=relative))
        results.append(dict(patch=patch_index, min=minimum.tolist(), max=maximum.tolist(),
                            projectedAtlasUvPerMetre=gradient.tolist(), sourceFaces=actual))
    report = dict(style='Draw01', uniqueRecipes=4, appearanceRegions=14, checkedNativeFaces=10,
                  checkedHalfWidthDeltas=list(checked_deltas), widthIndependentSourceDensity=True,
                  opaqueClassifierRegions=[0, 5, 7, 12], opaqueClassifierRecipe=inventory['units'][0]['key'],
                  fixedCapStartsAt=partition['F32'][13], movingRoadEnd=partition['F32'][25],
                  movingSidewalkEnd=partition['F32'][31],
                  projections=results, nativeMeshSHA256=hashlib.sha256(args.native_mesh.read_bytes()).hexdigest(),
                  inventorySHA256=hashlib.sha256(args.inventory.read_bytes()).hexdigest(),
                  limits='Native source and recorded projection comparison only. Cropped output pixel alignment, all output LODs and in-game appearance require separate checks.')
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('PASS Draw01 repeat source density: 10 native faces, 5 measured projections, 4 exact recipes; no geometry changes.')


if __name__ == '__main__':
    main()
