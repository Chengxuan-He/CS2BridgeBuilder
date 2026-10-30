"""Audit Lift05 native lane semantics and same-LOD finite atlas mappings.

This is evidence collection, not permission to repeat an arbitrary atlas crop.
No runtime assets or source classification tables are changed.
"""
import hashlib
import json
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
AUDIT = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'
SOURCE = ROOT / 'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism'
OUT = ROOT / 'artifacts/material-remap/Lift05'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert sha(AUDIT) == '47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'
    selected = json.loads(AUDIT.read_text(encoding='utf-8'))['selected']
    paths = ((121115, 121111, 29296, 121096, 186870, 'TrackLane'),
             (121114, 121107, 155384, 121101, 15607, 'PedestrianLane'))
    evidence = []
    for section, piece, lanes, lane, component, kind in paths:
        # Require actual PPtr relationships, not names/icons alone.
        for parent, child in ((section, piece), (piece, lanes), (lanes, lane), (lane, component)):
            def references(value):
                if isinstance(value, dict):
                    return value.get('m_PathID') == child or any(references(v) for v in value.values())
                return isinstance(value, list) and any(references(v) for v in value)
            assert references(selected[str(parent)]['data']), (parent, child)
        assert selected[str(component)]['class'] == kind
        evidence.append(dict(section=section, piece=piece, lane=lane,
            component=component, laneKind=kind, data=selected[str(component)]['data']))

    reports = []
    for lod in range(3):
        path = SOURCE / f'road-lod{lod}.json'
        data = json.loads(path.read_text())
        by_id = {f['id']: f for f in data['faces']}
        mappings = []
        triangles = []
        for face in data['faces']:
            xyz = np.array(face['positions'])
            uv = np.array(face['uv'])
            frame = np.column_stack((xyz[:, 0], xyz[:, 2], np.ones(3)))
            if np.linalg.matrix_rank(frame) != 3:
                continue
            fit = np.linalg.solve(frame, uv)
            residual = float(np.max(np.abs(frame @ fit - uv)))
            assert residual < 1e-10
            triangles.append(dict(face=face['id'], positions=face['positions'],
                uv=face['uv'], bone=face['bone'], affineXZ1=fit.tolist(),
                maxResidualUv=residual))
        # Mirrored central U is not one affine chart: measure each side separately.
        for tile in data['tiles']:
            for side in (-1, 1):
                faces = [by_id[i] for i in tile['faces']
                         if np.mean(np.array(by_id[i]['positions'])[:, 0]) * side > 0]
                if not faces:
                    continue
                xyz = np.array([f['positions'] for f in faces]).reshape(-1, 3)
                uv = np.array([f['uv'] for f in faces]).reshape(-1, 2)
                frame = np.column_stack((xyz[:, 0], xyz[:, 2], np.ones(len(xyz))))
                fit, _, rank, _ = np.linalg.lstsq(frame, uv, rcond=None)
                residual = np.max(np.abs(frame @ fit - uv))
                mappings.append(dict(tile=tile['id'], side=side, faces=[f['id'] for f in faces],
                    bone=tile['bone'], xyzMin=xyz.min(axis=0).tolist(), xyzMax=xyz.max(axis=0).tolist(),
                    uvMin=uv.min(axis=0).tolist(), uvMax=uv.max(axis=0).tolist(),
                    affineXZ1=fit.tolist(), rank=int(rank),
                    maxResidualTexels=float(residual * (512 if lod == 2 else 4096))))
        reports.append(dict(lod=lod, source=str(path.relative_to(ROOT)), sha256=sha(path),
            tiles=len(data['tiles']), finiteMappings=mappings,
            exactTriangleMappings=triangles,
            mappingScope='Candidate chart fits are diagnostic only. Triangle coefficients reproduce the native triangles exactly; applying them requires preserving each original triangle domain, not extending across atlas resets. Centroid half-groups are not physical partition boundaries.'))
    result = dict(style='Lift05', auditSource=str(AUDIT), auditSha256=sha(AUDIT),
        verifiedLaneSemantics=evidence, levels=reports,
        conclusion='The native graph proves track and pedestrian lane roles. Atlas-domain appearance assignment and width-preserving transport remain separate implementation obligations. Near/middle repeat resets cannot be replaced by a full-span affine mapping. Far LOD must use its own atlas.',
        localization='No user-facing text changed.')
    far = json.loads((SOURCE / 'road-lod2.json').read_text())
    far_faces = {f['id']: f for f in far['faces']}
    candidates = []
    for tile in far['tiles']:
        for half in (-1, 1):
            selected = [far_faces[i] for i in tile['faces']
                        if np.mean(np.array(far_faces[i]['positions'])[:, 2])*half > 0]
            xyz = np.array([f['positions'] for f in selected]).reshape(-1, 3)
            uv = np.array([f['uv'] for f in selected]).reshape(-1, 2)
            frame = np.column_stack((xyz[:, 0], xyz[:, 2], np.ones(len(xyz))))
            fit, _, rank, _ = np.linalg.lstsq(frame, uv, rcond=None)
            residual = float(np.max(np.abs(frame @ fit - uv))*512)
            candidates.append(dict(tile=tile['id'],half=half,faces=[f['id'] for f in selected],
                affineXZ1=fit.tolist(),maxResidualTexels=residual,
                sourceAffineExactToDoublePrecision=residual<1e-10,
                physicalSplit='Existing longitudinal half boundary Z=0; no prototype triangle clipping.'))
    result['farHalfProjectionReview'] = candidates
    result['farImplementationBoundary'] = (
        'The central chart has two exact affine longitudinal halves using the existing Z=0 appearance boundary. '
        'Both shoulder charts remain non-affine even after this split. Their original triangle gradients cannot '
        'be replaced by a least-squares chart fit without an approved mapping error, or reproduced by forbidden '
        'prototype-triangle Booleans. Far fixed-cap appearance and complete all-LOD road routing remain unresolved. '
        'This report does not enable partial production routing.')
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'native-road-appearance.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS native PPtr semantic chains; measured same-LOD atlas mappings:',
          [(r['lod'], len(r['finiteMappings'])) for r in reports])


if __name__ == '__main__':
    main()
