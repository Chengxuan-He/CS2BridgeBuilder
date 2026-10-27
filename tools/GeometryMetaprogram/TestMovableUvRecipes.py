"""Regression cases for sharing measured native rectangular sampling domains."""
import numpy as np
from MovableUvRecipes import rectangle_pairs, tessellated_rectangles, underside_uv
from pathlib import Path

uv = np.array([[0., 0.], [1., 0.], [1., 1.], [0., 1.]])
diagonal = np.array([[0, 1, 2], [0, 2, 3]])
for coordinates in (uv, uv * [7., 2.], uv + [-3., 8.]):
    patches = rectangle_pairs(diagonal, coordinates)
    assert len(patches) == 2
    assert np.array_equal(patches[0], patches[1])
    assert np.array_equal(patches[0], np.r_[coordinates.min(0), coordinates.max(0)])
assert len(rectangle_pairs(np.array([[0, 1, 2], [2, 0, 3]]), uv)) == 2
# Native UV/normal seams duplicate vertex indices. Every copy can reuse the
# same complete sampled rectangle without welding any physical geometry.
split_uv = uv[diagonal].reshape(-1, 2)
assert len(rectangle_pairs(np.array([[0, 1, 2], [3, 4, 5]]), split_uv)) == 2
duplicates = np.tile(split_uv, (3, 1))
shared = rectangle_pairs(np.arange(len(duplicates)).reshape(-1, 3), duplicates)
assert len(shared) == 6 and all(np.array_equal(b, [0, 0, 1, 1]) for b in shared.values())
assert not rectangle_pairs(np.array([[0, 1, 2], [3, 4, 5]]), np.tile(uv[[0, 1, 2]], (2, 1)))
# Equal-sized rectangles at different atlas locations must stay distinct.
separate = np.r_[split_uv, split_uv+[2., 0.]]
shared = rectangle_pairs(np.arange(12).reshape(-1, 3), separate)
assert len(shared) == 4 and not np.array_equal(shared[0], shared[2])
# Equal summed UV area is insufficient: these overlap below the diagonal and
# leave a gap above it despite having the same unit-square bounding box.
assert not rectangle_pairs(np.array([[0, 1, 2], [0, 1, 3]]), uv)
assert not rectangle_pairs(diagonal, uv * [0., 1.])
assert not rectangle_pairs(diagonal, uv @ np.array([[1., 1.], [-1., 1.]]))
skew = uv.copy()
skew[2, 0] += .01
assert not rectangle_pairs(diagonal, skew)
# A measured rectangle can contain more than two original triangles. Proving
# its boundary allows shared sampling without welding or subdividing geometry.
grid = np.array([[0., 0.], [1., 0.], [2., 0.], [0., 1.], [1., 1.], [2., 1.]])
grid_faces = np.array([[0, 1, 4], [0, 4, 3], [1, 2, 5], [1, 5, 4]])
for coordinates in (grid, grid*[7., 2.], grid+[-3., 8.]):
    patches = tessellated_rectangles(grid_faces, coordinates)
    assert len(patches) == 4
    assert all(np.array_equal(b, np.r_[coordinates.min(0), coordinates.max(0)]) for b in patches.values())
split = grid[grid_faces].reshape(-1, 2)
assert len(tessellated_rectangles(np.arange(12).reshape(-1, 3), split)) == 4
assert len(tessellated_rectangles(np.tile(grid_faces, (3, 1)), grid)) == 12
fan = np.r_[uv, [[.5, .5]]]
assert len(tessellated_rectangles(np.array([[0, 1, 4], [1, 2, 4], [2, 3, 4], [3, 0, 4]]), fan)) == 4
assert not tessellated_rectangles(np.array([[0, 1, 2], [0, 1, 3]]), uv)
assert not tessellated_rectangles(diagonal, skew)
hole_uv = np.array([(x, y) for y in range(4) for x in range(4)], dtype=float)
hole_faces = []
for y in range(3):
    for x in range(3):
        if (x, y) == (1, 1):
            continue
        a = y*4+x
        hole_faces.extend([[a, a+1, a+5], [a, a+5, a+4]])
assert not tessellated_rectangles(np.array(hole_faces), hole_uv)
# A disconnected remaining triangle cannot enlarge the complete left rectangle.
partial = tessellated_rectangles(grid_faces[:3], grid)
assert len(partial) == 2 and all(np.array_equal(b, [0, 0, 1, 1]) for b in partial.values())
# Exercise the production authoring loader as well as the rectangle helper.
root = Path(__file__).resolve().parents[2]
for name in ('DrawBridge01 Mesh.npz', 'DrawBridge01_LOD1 Mesh.npz'):
    original = np.zeros((100000, 2), dtype=np.float32)
    corrected = underside_uv(Path(name), 'mesh0_', original, root)
    assert corrected is not original and np.any(corrected != original)
    assert np.all(original == 0)
print('PASS measured rectangle sharing: offsets, aspect ratios, winding, overlap, degeneracy and nonrectangular UVs.')
