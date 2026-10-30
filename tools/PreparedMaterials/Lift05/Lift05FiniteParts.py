"""Measured complete upper-tower cap mappings, distinct from the deck soffit.

Face seeds identify the four surfaces on each half of both tower heads. Selection
is source-only and never depends on generated dimensions or failed receivers.
"""
import hashlib
import numpy as np
from shapely.geometry import Polygon
from shapely.ops import unary_union

SOURCES = {
    'LiftBridge05 Mesh': (
        '7a3fee2a56aebb23eee1127cf60808341f78e4d97feb6f3e45d1da2254690457',
        (1497, 2254, 2299, 2305, 3429, 3431, 3437, 3443,
         14486, 15245, 15290, 15296, 16420, 16422, 16428, 16434)),
    'LiftBridge05_LOD1 Mesh': (
        '3d9e1322f2d31c145fed4d6fa307071d22f892ea6766f1b67b7c75a875f81d20',
        (1143, 1584, 1626, 1632, 2140, 2142, 2148, 2154,
         8746, 9185, 9229, 9235, 9763, 9765, 9771, 9777)),
}

# Native terminal panels stop at the measured +/-8 m core seams. Their authored
# end intervals differ from the central four-metre repeat's UV rectangle. These
# are actual existing face/part boundaries, not cuts at an invented texture edge.
TERMINAL_SEEDS = {
    'LiftBridge05 Mesh': (
        2569, 2647, 3025, 3103, 3473, 3475, 3479, 3517, 3519, 3523, 3561, 3563, 3567, 3583, 3585, 3589,
        15560, 15638, 16016, 16094, 16464, 16466, 16470, 16508, 16510, 16514, 16552, 16554, 16558, 16574, 16576, 16580),
    'LiftBridge05_LOD1 Mesh': (
        1846, 1879, 1993, 2025, 2181, 2183, 2186,
        2213, 2214, 2216, 2239, 2241, 2243, 2254, 2256, 2259, 9457, 9491, 9612, 9646,
        9801, 9802, 9806, 9831, 9832, 9835, 9857, 9859, 9862, 9875, 9876, 9879),
}

PROFILE_SEEDS = (1873, 2019, 2273, 2287, 9434, 9639, 9846, 9892, 9906)


def repeated_profiles(render, mesh, native_path, positions, uv, faces, components):
    if render != 'LiftBridge05_LOD1 Mesh' or mesh != 0:
        return set(), []
    assert hashlib.sha256(native_path.read_bytes()).hexdigest() == SOURCES[render][0]
    face_parts = {f: i for i, members in enumerate(components) for f in members}
    admitted, records = set(), []
    for seed in PROFILE_SEEDS:
        members = components[face_parts[seed]]
        assert len(members) == 2
        triangles = uv[faces[members]].astype(float)
        points = positions[faces[members]].reshape(-1, 3)
        assert triangles[:, :, 0].min() == 0.6162109375
        assert triangles[:, :, 0].max() == 0.748046875
        assert triangles[:, :, 1].min() == 0.890625
        assert triangles[:, :, 1].max() == 0.8955078125
        assert not admitted.intersection(members)
        admitted.update(members)
        records.append(dict(seedFace=seed, faces=members, positions=positions[faces[members]].tolist(),
            sourceUvs=triangles.tolist(), xyzMin=points.min(0).tolist(), xyzMax=points.max(0).tolist(),
            repeatAxes=['U'], finiteAxes=['V'], sourcePeriodU=float(np.ptp(points[:, 0])),
            evidence='Measured central four-metre panel; native U resets between adjacent panels while V traverses its single authored merged profile.'))
    return admitted, records


def tower_caps(render, mesh, native_path, positions, uv, faces, components):
    if mesh != 0 or render not in SOURCES:
        return set(), []
    digest, seeds = SOURCES[render]
    assert hashlib.sha256(native_path.read_bytes()).hexdigest() == digest
    face_parts = {f: i for i, members in enumerate(components) for f in members}
    records, admitted = [], set()
    for ordinal, seed in enumerate(seeds):
        part = face_parts[seed]
        members = components[part]
        assert len(members) == (2, 6, 6, 10)[ordinal % 4]
        triangles = uv[faces[members]].astype(float)
        points = positions[faces[members]].reshape(-1, 3)
        shapes = [Polygon(t) for t in triangles]
        support = unary_union(shapes)
        # The complete native chart is an injective finite traversal, including
        # the triangles inside the unrelated deck-soffit rectangle. No reset,
        # repeated covering, hidden disconnected island or cropped support.
        assert support.geom_type == 'Polygon' and support.is_valid
        assert not support.interiors
        assert sum(s.area for s in shapes) == support.area
        assert np.all(triangles >= 0) and np.all(triangles <= 1)
        assert not admitted.intersection(members)
        admitted.update(members)
        records.append(dict(id=f'upper-tower-cap-{ordinal}', seedFace=seed, part=part,
            faces=members, nativeSha256=digest, vertexIndices=faces[members].tolist(),
            positions=positions[faces[members]].tolist(), sourceUvs=triangles.tolist(),
            xyzMin=points.min(0).tolist(), xyzMax=points.max(0).tolist(),
            uvBoundary=list(map(list, support.exterior.coords)), sourceUvArea=support.area,
            singleCover=True, nonTilingAxes=['U', 'V'],
            evidence='Complete upper-tower cap surface. Native UV0 is continuous and injective over this physical part. Reusing the deck-soffit atlas pixels is not a repeating deck mapping.'))
    assert len(admitted) == 96
    return admitted, records


def terminal_strips(render, mesh, native_path, positions, uv, faces, components):
    if mesh != 0 or render not in TERMINAL_SEEDS:
        return set(), []
    digest = SOURCES[render][0]
    assert hashlib.sha256(native_path.read_bytes()).hexdigest() == digest
    face_parts = {f: i for i, members in enumerate(components) for f in members}
    records, admitted = [], set()
    for ordinal, seed in enumerate(TERMINAL_SEEDS[render]):
        part = face_parts[seed]
        members = components[part]
        triangles = uv[faces[members]].astype(float)
        points = positions[faces[members]].reshape(-1, 3)
        shapes = [Polygon(t) for t in triangles]
        support = unary_union(shapes)
        assert support.geom_type == 'Polygon' and support.is_valid and not support.interiors
        assert sum(s.area for s in shapes) == support.area
        assert np.all(triangles >= 0) and np.all(triangles <= 1)
        assert points[:, 0].min() >= 8 or points[:, 0].max() <= -8
        assert np.any(np.abs(points[:, 0]) == 8)
        assert not admitted.intersection(members)
        admitted.update(members)
        records.append(dict(id=f'upper-terminal-panel-{ordinal}', seedFace=seed, part=part,
            faces=members, nativeSha256=digest, vertexIndices=faces[members].tolist(),
            positions=positions[faces[members]].tolist(), sourceUvs=triangles.tolist(),
            xyzMin=points.min(0).tolist(), xyzMax=points.max(0).tolist(),
            uvBoundary=list(map(list, support.exterior.coords)), sourceUvArea=support.area,
            singleCover=True, nonTilingAxes=['U', 'V'],
            evidence='Complete native terminal panel, ending at the existing +/-8 m core seam. This single finite authored end traversal does not reset at the internal four-metre crop edge; central repeat panels remain separately classified.'))
    return admitted, records

# Exhaustive native equivalent narrow profiles, independent of constructed scenarios.
SUPPLEMENTAL_SEEDS = {'LiftBridge05 Mesh': {'narrow': [2501, 2509, 2559, 2567, 2629, 2637, 2639, 2645, 2957, 2965, 3015, 3023, 3085, 3093, 3095, 3101, 3467, 3483, 3493, 3495, 3497, 3501, 3511, 3527, 3537, 3539, 3541, 3545, 3555, 3577, 3593, 3603, 3605, 3607, 3611, 3615, 3625, 3627, 3629, 3633, 15492, 15500, 15550, 15558, 15620, 15628, 15630, 15636, 15948, 15956, 16006, 16014, 16076, 16084, 16086, 16092, 16458, 16474, 16484, 16486, 16488, 16492, 16502, 16518, 16528, 16530, 16532, 16536, 16546, 16568, 16584, 16594, 16596, 16598, 16602, 16606, 16616, 16618, 16620, 16624], 'terminal': [3469, 3513, 3557, 3579, 16460, 16504, 16548, 16570]}, 'LiftBridge05_LOD1 Mesh': {'narrow': [1831, 1838, 1845, 1975, 1986, 2176, 2189, 2197, 2198, 2199, 2208, 2219, 2227, 2228, 2262, 2269, 2271, 2277, 2284, 2285, 9456, 9484, 9490, 9592, 9611, 9641, 9645, 9797, 9809, 9818, 9827, 9838, 9845, 9854, 9869, 9882, 9890, 9891, 9896, 9904], 'terminal': [2177, 2210, 2236, 2250, 9828, 9871]}}


def narrow_profile_parts(render, mesh, native_path, positions, uv, faces, components):
    if mesh != 0 or render not in SUPPLEMENTAL_SEEDS:
        return set(), [], set(), []
    assert hashlib.sha256(native_path.read_bytes()).hexdigest() == SOURCES[render][0]
    parts = {face: members for members in components for face in members}
    repeat_faces, terminal_faces, repeat_records, terminal_records = set(), set(), [], []
    for kind, seeds in SUPPLEMENTAL_SEEDS[render].items():
        for seed in seeds:
            members = parts[seed]
            triangles = uv[faces[members]].astype(float)
            points = positions[faces[members]].astype(float)
            shapes = [Polygon(t) for t in triangles]
            support = unary_union(shapes)
            assert support.geom_type == 'Polygon' and support.is_valid and not support.interiors
            assert support.area > 0 and sum(s.area for s in shapes) == support.area
            assert np.all(triangles >= [.6162109375, .892578125])
            assert np.all(triangles <= [.748046875, .8955078125])
            assert points[:, :, 1].min() > 72
            x = points[:, :, 0]
            if kind == 'narrow':
                assert len(members) <= 2 and abs(np.ptp(x) - 4) < .00001 and np.abs(x).max() <= 8
                assert np.ptp(triangles[:, :, 0]) == .1318359375
                physical = (x-x.min()) / np.ptp(x)
                authored = (triangles[:, :, 0]-.6162109375) / .1318359375
                assert min(np.max(np.abs(physical-authored)), np.max(np.abs(physical-(1-authored)))) < .00001
                # A complete connected native face/quad traverses V once. U spans
                # exactly the established four-metre native period with matching
                # physical endpoint phase; no extension or invented UV partition.
                repeat_faces.update(members)
                target = repeat_records
            else:
                assert (x.min() >= 8 or x.max() <= -8) and np.any(np.abs(x) == 8)
                terminal_faces.update(members)
                target = terminal_records
            target.append(dict(seedFace=seed, faces=members, nativeSha256=SOURCES[render][0],
                sourceUvs=triangles.tolist(), positions=points.tolist(),
                uvBoundary=list(map(list, support.exterior.coords)), singleCover=True,
                physicalPeriodU=float(np.ptp(x)) if kind == 'narrow' else None,
                axes=2 if kind == 'narrow' else 3,
                evidence='Complete native central four-metre profile; U matches measured period, V is single traversal.' if kind == 'narrow' else 'Complete native terminal chart outside the authored +/-8 metre seam.'))
    return repeat_faces, repeat_records, terminal_faces, terminal_records
