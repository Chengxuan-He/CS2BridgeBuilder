"""Extract the seven finite LOD2 bed charts; never bake or detect new periods."""
import hashlib
import json
import struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
audit = json.loads((ROOT / 'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
mesh_path = ROOT / 'artifacts/uv-density/native/LiftBridge04_LOD2 Mesh.npz'
mesh_sha = hashlib.sha256(mesh_path.read_bytes()).hexdigest()
assert mesh_sha == audit['lods'][2]['npzSha256'] == '10246dfc835353704c622d5217ddd3e3f0ad391c2ba99a6a2335977f63571f08'
mesh = np.load(mesh_path)
frame_script = ROOT / 'tools/PreparedMaterials/Lift04/AuditNearDomains.py'
frame_context = {'__file__': str(frame_script)}
exec(frame_script.read_text().split("data = (ROOT/")[0], frame_context)
geometry = (frame_context['research'] / 'movable-skin/LiftBridge04_LOD2 Mesh.Geometry').read_bytes()
assert hashlib.sha256(geometry).hexdigest() == audit['lods'][2]['geometrySha256']
frame_mesh = frame_context['namespace']['decode'](geometry)[0]
faces = {f['face']: f for f in audit['lods'][2]['faces']}
ids = [1568, 1570, 1156, 1158, 868, 1160, 846]
rows, checks = [], []
for face in ids:
    pair = [faces[face], faces[face + 1]]
    for original in pair:
        indices = mesh['mesh0_faces'][original['face']]
        assert np.array_equal(indices, original['indices'])
        assert np.array_equal(mesh['mesh0_positions'][indices], original['positions'])
        assert np.array_equal(mesh['mesh0_uv0'][indices], original['uv0'])
    p = np.array(pair[0]['positions'])
    uv = np.array(pair[0]['uv0'])
    affine = np.linalg.solve(np.c_[p[:, [0, 2]], np.ones(3)], uv)
    points = np.concatenate([f['positions'] for f in pair])
    coords = np.concatenate([f['uv0'] for f in pair])
    packed = affine.astype('<f4')
    error = np.max(np.abs(np.c_[points[:, [0, 2]], np.ones(6)] @ packed - coords))
    assert error * 512 < .0001, (face, error)
    assert np.min(points[:, 0]) == -5 and np.max(points[:, 0]) == 5
    rows.extend([points[:, 2].min(), points[:, 2].max(), 5,
                 *coords.min(axis=0), *coords.max(axis=0),
                 *packed[:, 0], *packed[:, 1]])
    ids = np.unique(frame_mesh['indices'][[face, face + 1]])
    words = np.unique(frame_mesh['tangentPacked'][ids].copy().view('<u4'))
    tangent = np.zeros((len(words), 4), dtype='<f4')
    frame_context['unpack'](words.ctypes.data, len(words), tangent.ctypes.data)
    decoded = np.unique(tangent, axis=0)
    assert len(decoded) == 1
    rows.extend(decoded[0])
    checks.append(dict(faces=[face, face + 1], bone=pair[0]['bone'],
                       maxNativePixelError=float(error * 512),
                       tangentPacked=[f'{w:08x}' for w in words], tangentDecoded=decoded[0].tolist()))
ints = np.array([len(checks), 17], dtype='<i4')
floats = np.array(rows, dtype='<f4')
raw = struct.pack('<II6i', 0x314e4242, 1, len(ints), 0, 0, 0, len(floats), 0) + ints.tobytes() + floats.tobytes()
target = ROOT / 'assets/HardcodedNumbers/Lift04/Types_Lift04_FarAppearance.bin'
target.write_bytes(raw + hashlib.sha256(raw).digest())
output = ROOT / 'artifacts/material-remap/Lift04/far-appearance-verification.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(dict(source='native-repeat-mechanism/audit.json LOD2', sourceMeshSHA256=mesh_sha,
    mapping='Original finite atlas charts; original 512 px textures; no new pixels.',
    checks=checks, payloadSHA256=hashlib.sha256(target.read_bytes()).hexdigest()), indent=2))
print(output)
