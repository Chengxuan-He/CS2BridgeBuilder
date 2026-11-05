"""Tie Draw03 lane appearance to native lane metadata, not albedo extrema."""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
AUDIT = Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json')
OUT = ROOT / 'artifacts/material-remap/Draw03/lane-placement'


def main():
    raw = AUDIT.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == '47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'
    records = json.loads(raw)['selected']
    root = records['118597']['data']
    sections = [s for s in root['m_Sections'] if not s['m_RequireAll']]
    piece = records['120675']['data']
    guid=piece['m_SurfaceAssets'][0]
    guid_bytes=struct.pack('<4i',*[guid['m_GuidPart'+str(i)] for i in range(1,5)])
    network_surface=''.join(f'{v&15:x}{v>>4:x}' for v in guid_bytes)
    assert network_surface=='1d7dad9f64c9fe55a809f1080c39fb92'
    lanes = records['162720']['data']['m_Lanes']
    assert piece['m_Width'] == 3 and len(lanes) == 1 and lanes[0]['m_Position']['x'] == 0
    car_sections = [s for s in sections if s['m_Section']['m_PathID'] in (118591, 118592, 118593)]
    assert len(car_sections) == 6
    for section in car_sections:
        parts = records[str(section['m_Section']['m_PathID'])]['data']['m_Pieces']
        assert any(p['m_Piece']['m_PathID'] == 120675 and not p['m_RequireAll'] for p in parts)
    source = ROOT / 'artifacts/uv-density/native/DrawBridge03 Mesh.npz'
    mesh = np.load(source)
    ids = mesh['mesh0_faces'][58054]
    xyz = mesh['mesh0_positions'][ids].astype(float)
    uv = mesh['mesh0_uv0'][ids, :2].astype(float)
    matrix = np.column_stack((xyz[:, 0], xyz[:, 2], np.ones(3)))
    mapping = np.linalg.solve(matrix, uv)
    # The material orientation is mirrored by side. This source face establishes
    # its own side; no assumption about pixel-array display direction is needed.
    sign = np.sign(xyz[:, 0].mean())
    placements = []
    for center in (1.5, 4.5, 7.5):
        x = sign * center
        bounds = sorted((sign * (center-1.5), sign * (center+1.5)))
        placements.append(dict(centerMetres=x, sourceLaneBoundsMetres=bounds,
            sourceVAtCenter=float(np.array([x, xyz[0, 2], 1]) @ mapping[:, 1]),
            sourceVBounds=[float(np.array([b, xyz[0, 2], 1]) @ mapping[:, 1]) for b in bounds]))
    OUT.mkdir(parents=True, exist_ok=True)
    result = dict(audit=str(AUDIT), auditSha256=hashlib.sha256(raw).hexdigest(),
        sourceMeshSha256=hashlib.sha256(source.read_bytes()).hexdigest(), nativeRoot=118597,
        nativePiece=120675, nativeLaneComponent=162720, nativeLane=lanes[0],
        nativeNetworkSurface=network_surface,
        sections=car_sections, nativeObjectPlacement=records['134666'],
        centersMetres=[-7.5,-4.5,-1.5,1.5,4.5,7.5], laneWidthMetres=3,
        sourceFace=58054, position=xyz.tolist(), sourceUv=uv.tolist(),
        uvAffineXZ=mapping.tolist(), placements=placements,
        conclusion='Native three-metre lanes establish placement. The historical rows 818..1194 are albedo extrema, not lane metadata or a transverse repeat proof.',
        scope='Offline provenance and projection evidence; no generated renderer or visual acceptance.')
    (OUT / 'native-lanes.json').write_text(json.dumps(result, indent=2)+'\n')
    template=json.loads((AUDIT.parent/'template-17.json').read_text())
    scale=dict(template['m_SavedProperties']['m_Floats'])['_WorldspaceUVScale']
    # The native lane remains provenance; the user-requested visual alignment
    # uses the independently measured full wear shape, not the crop midpoint.
    center=json.loads((ROOT/'artifacts/material-remap/Draw03/wear-centering/measurement.json').read_text())['visualCenterPixel']
    reference=(float(np.float32(mapping[2,1]*4096))-center)/float(np.float32(mapping[0,1]*4096))
    values=[mapping[0,1]*4096,mapping[2,1]*4096,reference,818,1194,802,834,4096,443.75,1581,1.5,7.5,scale,128]
    payload=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(values),0)+struct.pack('<14f',*values)
    payload+=hashlib.sha256(payload).digest()
    (ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03LaneAppearance_cs.bin').write_bytes(payload)
    print(json.dumps(dict(centers=result['centersMetres'],placements=placements,uvAffineXZ=mapping.tolist()),indent=2))


if __name__ == '__main__':
    main()
