"""Extract original authoring textures from every installed built-in Blob archive.

Prepare one private CleanAsphalt trial. Candidate evidence remains unapproved;
the trial supplies the missing original-atlas/mip comparison, not acceptance.
"""
from pathlib import Path
import json
import zipfile

from PrepareNativeRebuild import digest, payload_metadata

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
OUT = ROOT / 'artifacts/agent-material-env/Draw03/repeat-unit-trial'
PROOF = ROOT / 'artifacts/prepared-materials/Draw03/repeat-units-v2'


def main():
    graph = json.loads((PROOF.parent/'rebuild-native-v1/native-rebuild.json').read_bytes())
    source = next(s for s in graph['sourceSurfaces'] if s['cid'] == '792a855e9d0c5c34d2c41d48b062139d')
    wanted = source['descriptor']['textureProperties']
    discovered, scans = {}, []
    for path in sorted((GAME/'Content').rglob('Blob*.cok')):
        scanned = 0
        with zipfile.ZipFile(path) as archive:
            for name in archive.namelist():
                if not name.endswith('.cid'):
                    continue
                scanned += 1
                cid = archive.read(name).decode('utf-8-sig').strip()
                if cid not in wanted.values():
                    continue
                data = archive.read(name[:-4])
                if cid in discovered and discovered[cid]['sha256'] != digest(data):
                    raise ValueError('Conflicting original CID in builtin archives')
                discovered[cid] = dict(cid=cid, path=str(path), entry=name[:-4], sha256=digest(data),
                                       bytes=len(data), metadata=payload_metadata(data,Path(name[:-4]).suffix))
                dest = OUT/'inputs'/(cid+'.Texture')
                dest.parent.mkdir(parents=True,exist_ok=True)
                dest.write_bytes(data)
            scans.append(dict(path=str(path),cidRecords=scanned))
    report = dict(archives=scans, required=wanted, found=list(discovered.values()),
                  missing=sorted(set(wanted.values())-set(discovered)), copiedOldProducts=0)
    (PROOF/'authoring-texture-scan.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    if report['missing']:
        raise ValueError('Original authoring Texture missing; full VT reader required')
    evidence_path=PROOF/'candidate-manifest.json'
    evidence=json.loads(evidence_path.read_bytes())
    candidate=next(c for c in evidence['candidates'] if c['candidateId']=='Draw03/CleanAsphalt')
    descriptor=PROOF.parent/'rebuild-native-v1/authoring-descriptors'/(source['cid']+'.Surface')
    manifest=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Draw03',units=[dict(
        id=candidate['candidateId'],sourceCid=source['cid'],sourceDigest=source['sha256'],
        min=candidate['region']['min'],max=candidate['region']['max'],physicalSize=candidate['physicalSize'],
        evidence=dict(path=str(evidence_path),sha256=digest(evidence_path.read_bytes())),
        descriptor=dict(path=str(descriptor),sha256=digest(descriptor.read_bytes())),
        channels=[dict(name=name,cid=cid,sha256=discovered[cid]['sha256'],path=str(OUT/'inputs'/(cid+'.Texture')))
                  for name,cid in wanted.items()],
        aliases=[m['legacyKey'] for m in evidence['roadRequests'] if m['candidateId']==candidate['candidateId']])])
    (OUT/'inputs/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    (PROOF/'private-trial-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(archives=len(scans),found=len(discovered),missing=report['missing'],
        sourceBytes=sum(d['bytes'] for d in discovered.values()),
        mips={cid:d['metadata'].get('mipCount') for cid,d in discovered.items()},manifest=str(OUT/'inputs/manifest.json')),indent=2))


if __name__=='__main__':
    main()
