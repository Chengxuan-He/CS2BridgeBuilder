"""Export Lift04's actual immutable layer inputs without rasterizing receivers."""
import hashlib
import json
from pathlib import Path
import struct
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/layer-decoupling/Lift04/outputs'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    resource = ROOT / 'assets/HardcodedNumbers/Lift04/Types_Lift04_Lift04RoadAppearance_cs.bin'
    raw = resource.read_bytes()
    assert raw[:4] == b'BBN1' and hashlib.sha256(raw[:-32]).digest() == raw[-32:]
    assert struct.unpack_from('<6i', raw, 8) == (4, 0, 0, 0, 24, 0)
    ints = struct.unpack_from('<4i', raw, 32)
    f = np.array(struct.unpack_from('<24f', raw, 48), dtype=np.float32)
    moving_length = np.float32(f[5] - f[4])
    # Exact identity-road-frame evaluation of Lift04RoadAppearance.Patches.
    arguments = [(-f[12], f[13], f[14], f[15]),
                 (f[16], f[17], f[18], -f[19]),
                 (-f[20], moving_length, f[4], -f[21]),
                 (f[22], moving_length, f[4], -f[23])]
    patches = []
    for index, (direction, length, start, lateral) in enumerate(arguments):
        u = np.array([f[6], f[7], direction / length, -start / length], dtype=np.float32)
        v = np.array([lateral / f[8], f[9], f[10], f[11]], dtype=np.float32)
        assert u[2] != 0 and v[0] != 0
        patches.append(dict(index=index, min=f[:2].tolist(), max=f[2:4].tolist(),
                            U=u.tolist(), V=v.tolist(), repeat=True, bands=None,
                            physicalPeriodLongitudinal=float(abs(1 / float(u[2]))),
                            physicalPeriodTransverse=float(abs(1 / float(v[0]))),
                            projectionPrecision='IEEE754 binary32',
                            transform='transpose(TRS(roadToObject))*projection'))
    native_path = ROOT / 'artifacts/uv-density/native/LiftBridge04 Mesh.npz'
    native = np.load(native_path)
    face_ids = [4198, 4199, 28426, 28427, 10581, 10582, 33328, 33329]
    faces = native['mesh0_faces'][face_ids]
    evidence = json.loads((ROOT / 'artifacts/prepared-materials/Lift04/rebuild-native/repeat-unit-evidence.json').read_text())
    unit = next(u for u in evidence['units'] if u['id'] == 'Lift04RoadUnit')
    assert unit['min'] == f[:2].tolist() and unit['max'] == f[2:4].tolist()
    positions = native['mesh0_positions'].astype(np.float64)
    precision = []
    for patch, (direction, length, start, lateral) in zip(patches, arguments):
        # Measure projection arithmetic only. These values do not include filtering,
        # skinning, arbitrary world placement, or justify a new acceptance threshold.
        z = np.linspace(positions[:, 2].min(), positions[:, 2].max(), 8193, dtype=np.float32)
        x = np.linspace(positions[:, 0].min(), positions[:, 0].max(), 8193, dtype=np.float32)
        measured_u = np.float32(np.float32(z * np.float32(patch['U'][2])) + np.float32(patch['U'][3]))
        measured_v = np.float32(x * np.float32(patch['V'][0]))
        reference_u = z.astype(float) * float(direction) / float(length) - float(start) / float(length)
        reference_v = x.astype(float) * float(lateral) / float(f[8])
        precision.append(dict(patch=patch['index'], sourceTexelExtent=unit['baseLevelPixelExtent'],
            measuredObjectBounds=[positions.min(0).tolist(), positions.max(0).tolist()], samplesPerAxis=len(z),
            maximumProjectionErrorSourceTexels=[float(np.max(abs(measured_u-reference_u))*unit['baseLevelPixelExtent'][0]),
                                               float(np.max(abs(measured_v-reference_v))*unit['baseLevelPixelExtent'][1])],
            acceptedTolerance=None, scope='Binary32 local projection vs exact binary input arithmetic; native filtering and world-frame error remain unmeasured'))
    capabilities_path = ROOT / 'artifacts/prepared-materials/Draw03/native-layer-capabilities.json'
    capabilities = json.loads(capabilities_path.read_text())
    relevant = [dict(shader=s['name'], pathId=s['pathId'], templates=s['templates'],
                     textures=s['textures'], animationKeywords=s['animationKeywords'])
                for s in capabilities['shaders'] if s['pathId'] in (571, 581)]
    report = dict(schema=1, style='Lift04',
        sourceResource=dict(path=resource.relative_to(ROOT).as_posix(), sha256=digest(resource),
                            appearancePartitionIntegers=ints),
        patches=patches, sourceUnit=unit, projectionPrecisionMeasurements=precision,
        nativeFaces=dict(path=native_path.relative_to(ROOT).as_posix(), sha256=digest(native_path),
                         mesh=0, ids=face_ids, indices=faces.tolist(),
                         positions=native['mesh0_positions'][faces].astype(float).tolist(),
                         uv0=native['mesh0_uv0'][faces].astype(float).tolist()),
        compose=dict(allLods=True, prototypeRoad=False, movingRoadSource='actual selected-road Surface',
                     movingProjection='OriginalProjection evaluated native UVs',
                     fixedSource='prototype Surface or prepared genuine unit',
                     worldspaceFrame='closedWorld * TRS(roadToObject)',
                     paint='separate native skinned mesh; Compose paint list is empty',
                     physicalBones='unchanged; separate from four appearance indices'),
        nativeCapability=dict(inventorySha256=digest(capabilities_path), shaders=relevant,
                              actualIndependentLayerObjectRenderVerified=False),
        regressionRequirements=[
            'Keep four exact patch projections and mirrored orientation across every LOD.',
            'Keep prepared texture dimensions and payload identity independent of receiver length.',
            'Selected-road base/control and worldspace layers retain separate coordinates and periods.',
            'Finite or constant control sampling must not reject an unrelated repeating layer.',
            'Compare local boundary and supported-extent cumulative error per source channel.',
            'Do not use a universal 0.01-texel threshold or invent a replacement value from this fixture.',
            'Preserve packed normal frame, physical bones, native dependency graph and source isolation.'],
        pending=['Actual selected-road Compose receiver capture', 'Measured per-layer tolerance',
                 'Native independently sampled object material, procedural skinning and cold load'],
        localization='No user-facing text or localization behavior changed.')
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'source-fixture.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(patches=len(patches), sourceFaces=len(face_ids),
                          periods=[p['physicalPeriodLongitudinal'] for p in patches],
                          nativeObjectLayersVerified=False)))


if __name__ == '__main__':
    main()
