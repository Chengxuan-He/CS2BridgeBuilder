"""Export Draw01's remaining source-unit requirements without inventing repeat evidence."""
import hashlib
import json
from collections import Counter
from pathlib import Path
import numpy as np

from ExportRecipes import source_recipes

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Draw01/genuine-unit-requirements'


def read(path):
    return json.loads((ROOT / path).read_text(encoding='utf-8-sig'))


def main():
    old = read('artifacts/prepared-repeat-draw01/inventory.json')
    density = read('artifacts/prepared-repeat-draw01/source-density.json')
    current, hashes = source_recipes(ROOT)
    for actual, expected in zip(old['units'], current):
        assert all(actual[k] == v for k, v in expected.items())
    assert len(current) == 4
    sources = read('artifacts/prepared-materials/Draw01/source-sharing/sampling-map.json')
    structural = read('artifacts/uv-density/structure-tessellated-sources.json')
    structural = [u for u in structural['units'] if u['style'] == 'Draw01']
    native = read('artifacts/prepared-materials/Draw01/rebuild-native-v1/producer.json')
    native_map = {x['sourceCid']: x for x in native['roots']}
    materials = ROOT / 'assets/Materials/Draw01'
    candidates = []
    mesh_path=ROOT/'artifacts/uv-density/native/DrawBridge01 Mesh.npz'
    mesh=np.load(mesh_path)
    uv=mesh['mesh0_uv0']; faces=mesh['mesh0_faces']; positions=mesh['mesh0_positions']
    def signature(face):
        return tuple(sorted(tuple(float(x) for x in point) for point in uv[face]))
    matching={}
    for face_id,face in enumerate(faces):matching.setdefault(signature(face),[]).append(face_id)
    names = ['asphalt-interior', 'metal-grating', 'stationary-sidewalk', 'stationary-carriageway-cap']
    for i, unit in enumerate(old['units']):
        projection = density['projections'][i]
        gradient = projection['projectedAtlasUvPerMetre']
        extents = [b-a for a,b in zip(unit['min'], unit['max'])]
        periods = [extents[0]/abs(gradient[0][0]), extents[1]/abs(gradient[1][1])]
        reference_face=projection['sourceFaces'][0]['face']
        reference_indices=faces[reference_face]
        reference_uv_order=np.lexsort(uv[reference_indices].T[::-1])
        reference_positions=positions[reference_indices][reference_uv_order].astype(float)
        repetitions=[]
        for face_id in matching[signature(reference_indices)]:
            indices=faces[face_id]; order=np.lexsort(uv[indices].T[::-1])
            candidate_positions=positions[indices][order].astype(float)
            delta=candidate_positions-reference_positions
            translation=delta.mean(axis=0)
            error=float(np.max(np.abs(delta-translation)))
            if error<0.002:
                repetitions.append(dict(face=face_id,translationMetres=translation.tolist(),translationResidualMetres=error))
        candidates.append(dict(
            candidateId=names[i], sourceCid=unit['sourceCid'], sourceDigest=unit['sourceDigest'],
            region=dict(min=unit['min'], max=unit['max']),
            sourceDimensions=[4096,4096], sourceChannels=old['sources'][0]['channels'],
            sourceAtlasUvPerMetre=gradient, sourceTexelsPerMetre=[abs(gradient[0][0])*4096,abs(gradient[1][1])*4096],
            proposedPhysicalPeriodMetres=periods, sourceFaces=projection['sourceFaces'],
            legacyRequestKeys=[unit['key']], appearanceRegions=unit['regions'], cutout=unit['cutout'],
            outputPath='Repeat/'+unit['key']+'/Repeat.Surface',
            repeatedNativeUvTriangle=dict(referenceFace=reference_face,placements=repetitions,
                interpretation='Exact same UV triangle and translated source geometry; evidence of repeated use, not pixel-seam or arbitrary crop-period equivalence.'),
            currentPayloadPresent=any((materials/'Repeat'/unit['key']).glob('*.Surface')),
            approvedGenuineUnit=False,
            missingEvidence=['authored periodic-boundary identity', 'all-channel filtered edges and full source mip pyramid'],
            mapping='One verified unit shared by every listed appearance region and all road LODs; axes/phase stay per use.'))
    counts = Counter(u['sourceCid'] for u in structural)
    groups = []
    for cid,count in sorted(counts.items()):
        source = next(s for s in sources['sources'] if s['cid']==cid)
        n = native_map[cid]
        groups.append(dict(sourceCid=cid,sourceDigest=source['sha256'],template=source['template'],
            legacySamplerCount=count,triangularSamplers=sum(u['triangle'] for u in structural if u['sourceCid']==cid),
            nativeSurfaceCid=n['cid'],nativeOperation=n['operation'],
            candidateRule='Keep source-contained native UV uses. Outside-chart uses require evidenced genuine units; no per-sampler bake.',
            genuineUnitsProven=0))
    missing = [u for u in structural if not any((materials/'Structure'/u['key']).glob('*.Surface'))]
    OUT.mkdir(parents=True,exist_ok=True)
    manifest = dict(schema='bridgebuilder.Draw01.genuine-unit-requirements/1',style='Draw01',
        preparedForBaking=False,currentSourceHashes=hashes,nativeMeshSha256=hashlib.sha256(mesh_path.read_bytes()).hexdigest(),roadCandidates=candidates,structuralSources=groups,
        legacyStructuralPotentialRequests=len(structural),missingLegacyStructuralKeys=len(missing),
        reachability='Potential immutable catalogue requests across widths, not counts of requests made by any one bridge. CanUseSource/chart containment bypasses some keys.',
        requiredProducer=['source VT all-mip/layer decoding with channel metadata',
            'source-unit extraction preserving original density and normal encoding with boundary verification',
            'one prepared payload per verified unit; map legacy requests to that unit plus per-use UV transform'],
        forbidden='Do not pass unapproved candidates or structural sampler list directly to the old per-sampler baker.')
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (OUT/'potential-structure-requests.json').write_text(json.dumps(dict(style='Draw01',requests=missing),indent=2)+'\n')
    print(json.dumps(dict(roadCandidates=len(candidates),structuralSources=len(groups),potentialStructuralRequests=len(structural),missingKeys=len(missing),output=str(OUT))))


if __name__ == '__main__':
    main()
