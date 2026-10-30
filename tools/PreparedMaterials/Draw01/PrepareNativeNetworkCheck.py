"""Extract verified Draw01 native mesh/header fixtures without game construction."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--plugin', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    evidence = json.loads((ROOT / 'artifacts/material-resume/Draw01/native-network-sources.json').read_text())
    audit_path = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'
    assert hashlib.sha256(audit_path.read_bytes()).hexdigest() == evidence['sourceAuditSha256']
    audit = json.loads(audit_path.read_text())
    decoder = NativeGeometry(args.plugin)
    families = list(dict.fromkeys(row['root'] for row in evidence['sources']))
    rows = []
    with zipfile.ZipFile(evidence['archive']) as archive:
        for source in evidence['sources']:
            cid = source['sourceCid']
            raw = archive.read(source['sourceEntry'])
            assert hashlib.sha256(raw).hexdigest() == source['sourceSha256']
            assert struct.unpack_from('<Hi?', raw) == (1, 17, False)
            assert archive.read(source['sourceEntry'] + '.cid').decode().strip() == cid
            (args.output / (cid + '.Surface')).write_bytes(raw)
            geometry = audit['assets'][source['geometryCid']]
            raw_geometry = archive.read(geometry['entry'])
            assert hashlib.sha256(raw_geometry).hexdigest() == source['geometrySha256']
            path = args.output / (source['geometryCid'] + '.Geometry')
            path.write_bytes(raw_geometry)
            meshes = decoder.read(path)
            assert len(meshes) == 1
            p, uv, faces = (meshes[0][key] for key in ('positions', 'uv0', 'faces'))
            assert uv.dtype.str == '<f2' and uv.shape[1] == 2
            fixture = struct.pack('<ii', len(p), faces.size) + p.astype('<f4').tobytes() + uv.astype('<f2').tobytes() + faces.astype('<i4').tobytes()
            filename = source['render'].replace(' ', '_') + '.mesh'
            (args.output / filename).write_bytes(fixture)
            rows.append(dict(render=source['render'], cid=cid, family=families.index(source['root']), lod=source['lod'], template=17, mesh=filename,
                surfaceUri=source['sourceEntry'], surfaceSha256=source['sourceSha256'], geometryCid=source['geometryCid'], geometrySha256=source['geometrySha256'], meshSha256=hashlib.sha256(fixture).hexdigest(), vertices=len(p), indices=faces.size))
    assert len(rows) == 13
    (args.output / 'inputs.json').write_text(json.dumps(dict(rows=rows), indent=2) + '\n')
    print(f'Prepared {len(rows)} verified native mesh/header pairs.')


if __name__ == '__main__':
    main()
