"""Separate measured Draw03 repeat candidates from proven tileability evidence.

No baking occurs. Source geometry, typed sampling arrays and original Surface
identity are inputs; prior generated texture pixels are never used as evidence.
"""
from pathlib import Path
import collections
import hashlib
import json
import numpy as np

from PrepareRecipes import SOURCE_CID, numbers, units


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Draw03/repeat-units-v2'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    graph_path = ROOT / 'artifacts/prepared-materials/Draw03/rebuild-native-v1/native-rebuild.json'
    graph = json.loads(graph_path.read_bytes())
    source = next(s for s in graph['sourceSurfaces'] if s['cid'] == SOURCE_CID)
    npz_path = ROOT / 'artifacts/uv-density/native/DrawBridge03 Mesh.npz'
    data = np.load(npz_path)
    positions = data['mesh0_positions'].astype(np.float64)
    uv = data['mesh0_uv0'][:, :2].astype(np.float64)
    faces = data['mesh0_faces']
    measured = {}
    for face in (58046, 58054, 58174, 58180):
        ids = faces[face]
        p, q = positions[ids], uv[ids]
        frame = np.linalg.solve(q[1:] - q[0], p[1:] - p[0])
        measured[face] = dict(face=face, vertices=ids.tolist(), positions=p.tolist(), uv=q.tolist(),
            worldPerUv=frame.tolist(), metresPerUv=np.linalg.norm(frame, axis=1).tolist(),
            texelsPerMetre=(4096 / np.linalg.norm(frame, axis=1)).tolist())

    # Match actual source edges by exact positions, then inspect genuine atlas-U
    # restarts across adjacent authored top faces. This does not prove mip seams.
    edges = collections.defaultdict(list)
    for fi, triangle in enumerate(faces):
        p, q = positions[triangle], uv[triangle]
        if np.ptp(p[:, 1]) != 0 or p[0, 1] not in (10.0, 10.199951171875):
            continue
        for i in range(3):
            a, b = i, (i + 1) % 3
            pa, pb = tuple(p[a]), tuple(p[b])
            if pa == pb:
                continue
            qa, qb = q[a], q[b]
            if pa > pb:
                pa, pb, qa, qb = pb, pa, qb, qa
            edges[pa, pb].append((fi, qa, qb))
    OUT.mkdir(parents=True, exist_ok=True)
    records, mappings = [], []
    recipes = units(ROOT, source['sha256'])
    for name in sorted({u['purpose'] for u in recipes}):
        uses = [u for u in recipes if u['purpose'] == name]
        base = uses[0]
        low, high = np.array(base['min']), np.array(base['max'])
        face = measured[58046 if name == 'Grating' else 58054]
        periods = (np.array(face['metresPerUv']) * (high - low)).tolist()
        restarts = []
        for edge, entries in edges.items():
            if len(entries) != 2:
                continue
            a, b = entries
            for first, second in ((a, b), (b, a)):
                if (np.all(np.array([first[1][0], first[2][0]]) == low[0])
                        and np.all(np.array([second[1][0], second[2][0]]) == high[0])
                        and first[1][1] == second[1][1] and first[2][1] == second[2][1]
                        and max(first[1][1], first[2][1]) >= low[1]
                        and min(first[1][1], first[2][1]) <= high[1]):
                    restarts.append(dict(faces=[first[0], second[0]], positions=edge,
                                         atlasUvA=[first[1].tolist(), first[2].tolist()],
                                         atlasUvB=[second[1].tolist(), second[2].tolist()]))
        record = dict(candidateId='Draw03/' + name, sourceCid=SOURCE_CID, sourceSha256=source['sha256'],
            region=dict(min=base['min'], max=base['max'], pixelsAt4096=(np.r_[low, high] * 4096).tolist()),
            physicalSize=periods, physicalSizeMeaning='Measured region extent; only a repeat period after boundary/mip verification',
            metresPerUv=face['metresPerUv'], texelsPerMetre=face['texelsPerMetre'], prototypeFace=face['face'],
            sourcePayloads=source['descriptor']['dependencies'],
            boundaryEvidence=dict(nativeUResets=len(restarts), samples=restarts[:8],
                fullMipSeamVerified=False, normalSeamVerified=False,
                note='Exact authored U restarts establish native geometry reuse only; they do not certify cropped V edges or mip filtering'),
            unitApproval='pending-original-channel-full-mip-boundary-evidence',
            runtimeSizeDependsOnRoad=False,
            warnings=['Do not bake as a genuine unit until the missing boundary evidence is supplied'])
        if name in ('LaneWear', 'BetweenLaneWear'):
            record['widthRule'] = 'Keep native-width wear; clip narrow lanes. Gap uses fixed 32 source rows, with affine repeats and phase reset per actual gap, never a full-width bake.'
        records.append(record)
        for use in uses:
            mappings.append(dict(legacyRequestKind='RoadRepeat/1', legacyKey=use['key'],
                candidateId=record['candidateId'], cutout=use['cutout'],
                textureVariantRequiredByCutout=False, rows=False, bands=None,
                sampler='Draw03RoadAppearance / Draw03RoadWearUnits affine U,V; preserve per-use phase'))
    inventory_path = ROOT / 'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_bytes())
    structural = [u for u in inventory['units'] if u['style'] == 'Draw03']
    structure_map = [dict(legacyKey=u['key'], sourceCid=u['sourceCid'], sourceSha256=u['sourceDigest'],
        candidateId=None, operation='Native source/chart mapping required; this triangle/rectangle is not a proven repeat unit') for u in structural]
    manifest = dict(schema='Draw03GenuineRepeatEvidence/2', style='Draw03',
        sourceEvidence=dict(nativeGraph=str(graph_path.relative_to(ROOT)), nativeGraphSha256=sha(graph_path),
            geometryCid='9c94f97e5ecbdc7a530390b6fb5c27ab', mesh=0,
            npz=str(npz_path.relative_to(ROOT)), npzSha256=sha(npz_path)),
        prototypeFaces=list(measured.values()), candidates=records, roadRequests=mappings,
        channels='Original Surface default VT stack: base, tangent normal and mask; retain original per-channel format/color semantics and full mip chain',
        readyForBake=False, reasons=['Candidate crop bounds alone do not establish periodicity',
            'Original full-mip boundary and normal verification is pending the new shared reader'],
        pendingCalls=['MovableSurfaceAtlas periodic chart -> BridgePreparedMaterials.Repeat',
            'MovableStructureTiles non-source units -> BridgePreparedMaterials.Structure'],
        structureLegacyRequestCount=len(structural), approvedStructureRepeatUnits=0,
        geometryPolicy='Keep existing actual lane/gap appearance partitions; never split on texture periods')
    (OUT / 'candidate-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    (OUT / 'structure-request-map.json').write_text(json.dumps(structure_map, separators=(',', ':')) + '\n', encoding='utf-8')
    summary = dict(candidates=len(records), legacyRoadRequests=len(mappings), structuralRequests=len(structural),
        approvedUnits=0, bakedTextures=0,
        candidatePhysicalSizes={r['candidateId']: r['physicalSize'] for r in records},
        nativeUResetEvidence={r['candidateId']: r['boundaryEvidence']['nativeUResets'] for r in records})
    (OUT / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
