"""Verify Lift04 appearance inputs without treating a repeat patch as type support."""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
AUDIT_SHA = '47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('audit', type=Path, help='Original all-movable-audit.json')
    parser.add_argument('--game-data', type=Path,
                        default=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'))
    parser.add_argument('--output', type=Path,
                        default=ROOT / 'artifacts/material-remap/Lift04/appearance-support.json')
    args = parser.parse_args()
    assert digest(args.audit) == AUDIT_SHA
    audit = json.loads(args.audit.read_text(encoding='utf-8'))
    records = audit['selected']
    networks = [(key, value['data']) for key, value in records.items()
                if value['data'].get('m_Name') == 'LiftBridge04'
                and 'm_Sections' in value['data']]
    assert len(networks) == 1
    network_id, network = networks[0]
    section_ids = sorted({s['m_Section']['m_PathID'] for s in network['m_Sections']})
    # The exact measured source graph, not matching a selected road by name.
    assert section_ids == [120999, 121002, 121003, 121004]
    assert network['m_TrackType'] == 4
    def cid(reference):
        raw = struct.pack('<4i', *(reference['m_GuidPart' + str(i)] for i in range(1, 5)))
        return ''.join(format(b & 15, 'x') + format(b >> 4, 'x') for b in raw)

    bed_cids = {cid(records[str(pid)]['data']['m_SurfaceAssets'][0])
                for pid in (120985, 120981)}
    assert bed_cids == {'a3522ead5e61e02569f0884fb4607eec'}
    bed_cid = next(iter(bed_cids))
    bed_asset = audit['assets'][bed_cid]
    with zipfile.ZipFile(args.game_data / bed_asset['package']) as archive:
        current_bed = archive.read(bed_asset['entry'])
        assert archive.read(bed_asset['entry'] + '.cid').decode().strip() == bed_cid
    assert hashlib.sha256(current_bed).hexdigest() == bed_asset['sha256']
    assert bed_asset['template_hash'] == 17 and not bed_asset['is_vt_header']
    assert set(bed_asset['texture_reference_bytes']) == {
        '_BaseColorMap', '_NormalMap', '_MaskMap',
        '_WorldspaceAlbedo', '_WorldspaceNormalMap', '_WorldspaceMaskMap'}
    sections = []
    for sid in section_ids:
        section = records[str(sid)]['data']
        assert not section['m_SubSections']
        pieces = []
        for entry in section['m_Pieces']:
            pid = entry['m_Piece']['m_PathID']
            piece = records[str(pid)]['data']
            pieces.append(dict(pathId=pid, name=piece['m_Name'],
                               requireAll=entry['m_RequireAll'],
                               requireAny=entry['m_RequireAny'],
                               requireNone=entry['m_RequireNone'],
                               components=piece['components']))
        sections.append(dict(pathId=sid, name=section['m_Name'], pieces=pieces))
    bindings_path = ROOT / 'artifacts/prepared-materials/Lift04/shared-sampling/native-bindings.json'
    bindings = json.loads(bindings_path.read_text())
    mechanism = bindings['renderBindings'][:3]
    assert [b['originalSurfaceCids'][0] for b in mechanism] == [
        '6b973ac2133fa9bd8d897bc9eff3363f',
        '6b973ac2133fa9bd8d897bc9eff3363f',
        '3530c13c82fb42d7bd090005884e4956']
    result = dict(style='Lift04', sourceAuditSHA256=AUDIT_SHA,
                  sourceNetworkPathId=network_id, sourceTrackType=network['m_TrackType'],
                  nativeBedSource=dict(cid=bed_cid, currentPayloadVerified=True, **bed_asset),
                  sections=sections, mechanismBindings=mechanism,
                  bindingInventorySHA256=digest(bindings_path),
                  unsupportedAppearanceTypes=['Asphalt', 'Sidewalk', 'WheelTrack'],
                  rationale='The measured network is subway-only. The original mechanism bed is not evidence of asphalt, sidewalk or automobile wheel-rut support. Rails and sleepers retain selected-track appearance.',
                  unresolved='Ballast and per-LOD routing are implemented. Near/mid use one hundred complete authored UV cells with original-atlas wrap seams, independently finite longitudinal terminals, and native packed tangents. Original reset stations are retained; shared native wrap geometry needs acceptance. Far uses seven finite original atlas charts. Independent persistence requires integration validation.',
                  acceptance='Source dispatch evidence only; native generated rendering, dependency closure and human acceptance remain separate.')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(style='Lift04', sections=len(sections),
                          unsupported=result['unsupportedAppearanceTypes'],
                          output=str(args.output))))


if __name__ == '__main__':
    main()
