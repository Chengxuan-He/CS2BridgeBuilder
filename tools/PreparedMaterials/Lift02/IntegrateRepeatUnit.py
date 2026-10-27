"""Install only the hash-verified Lift02 fixed-end unit; validate its style graph."""
import json
from pathlib import Path
from IntegrateStructures import ROOT, refs, sha

base = ROOT / 'artifacts/prepared-materials/Lift02'
source = base / 'repeat-units-v2/trial/output/Lift02'
target = ROOT / 'assets/Materials/Lift02'
summary = json.loads((base / 'repeat-units-v2/trial-summary.json').read_text())
correction_path = base / 'repeat-units-v2/cutout-descriptor.json'
correction = json.loads(correction_path.read_text()) if correction_path.exists() else None
if correction:
    assert correction['status'] == 'installed-native-reader-validated'
    summary['files'] = [f for f in correction['formalFiles'] if f['path'].startswith(('RepeatUnits/','Shared/'))]
original = base / 'rebuild-native-v1/output/Lift02'
graph = json.loads((base / 'rebuild-native-v1/native-graph.json').read_text())
builtin = {d['cid'] for surface in graph['descriptors'] for d in surface['dependencies'] if d['scope'] == 'builtin'}
planned = []
identities = {}
for sidecar in target.rglob('*.cid'):
    payload = Path(str(sidecar)[:-4])
    identity = sidecar.read_text().strip()
    digest = sha(payload.read_bytes())
    assert identity not in identities, 'Duplicate destination CID: ' + identity
    identities[identity] = digest
for f in summary['files']:
    relative = Path(f['path'])
    candidate = (base / 'repeat-units-v2/cutout-descriptor' / relative
                 if correction and relative.parts[0] == 'RepeatUnits' else source / relative)
    data = candidate.read_bytes()
    assert len(data) == f['bytes'] and sha(data) == f['sha256'], 'Unverified candidate: ' + str(relative)
    destination = target / relative
    assert not destination.exists() or destination.read_bytes() == data, 'Path conflict: ' + str(relative)
    if relative.suffix != '.cid':
        identity = Path(str(candidate) + '.cid').read_text().strip()
        assert identity not in identities or identities[identity] == sha(data), 'CID conflict: ' + identity
    planned.append((destination, data))
for p in original.rglob('*'):
    if p.is_file():
        assert (target / p.relative_to(original)).read_bytes() == p.read_bytes(), 'Original library changed'
added = []
for destination, data in planned:
    if not destination.exists():
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        added.append(destination.relative_to(target).as_posix())
local = {}
for sidecar in target.rglob('*.cid'):
    payload = Path(str(sidecar)[:-4])
    identity = sidecar.read_text().strip()
    assert identity not in local, 'Duplicate CID payload'
    local[identity] = payload.relative_to(target).as_posix()
edges = []
for surface in target.rglob('*.Surface'):
    for identity in refs(surface.read_bytes()):
        assert identity in local or identity in builtin, 'Unresolved reference: ' + identity
        edges.append({'surface':surface.relative_to(target).as_posix(),'cid':identity,'scope':'local' if identity in local else 'builtin'})
files = []
for p in target.rglob('*'):
    if p.is_dir():
        assert any(p.iterdir()), 'Empty directory'
    else:
        assert p.suffix in ('.Surface','.Texture','.cid'), 'Unexpected payload'
        if p.suffix != '.cid':
            assert Path(str(p)+'.cid').is_file(), 'Missing identity'
        files.append({'path':p.relative_to(target).as_posix(),'bytes':p.stat().st_size,'sha256':sha(p.read_bytes())})
report = {'style':'Lift02','status':'integrated-fixed-end-unit-structural-gaps-remain',
          'candidateHashVerified':True,'baseLibraryByteIdentical':True,'completeDependencyClosure':True,
          'addedFiles':added,'files':files,'fileCount':len(files),'totalBytes':sum(f['bytes'] for f in files),
          'surfaceCount':len(list(target.rglob('*.Surface'))),'textureCount':len(list(target.rglob('*.Texture'))),
          'uniquePayloadCids':len(local),'edges':edges,'materialUVScale':[1,0.125,0,0],
          'descriptorCorrection':correction['surfaceCid'] if correction else None,
          'unmappedStructuralKeys':1618,'structuralAliasesAdded':0,
          'atlasBoundaryDifferences':'Recorded in trial/native-check.json; accepted for human observation, not pixel equality.',
          'localization':'No user-visible text changed; existing localization preserved.',
          'validationLimits':'Common build, game visual acceptance and generated-owner cold-load tests are not performed by this integration.'}
(base / 'repeat-units-v2/integration.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ('status','fileCount','totalBytes','surfaceCount','textureCount','uniquePayloadCids','unmappedStructuralKeys')}))
