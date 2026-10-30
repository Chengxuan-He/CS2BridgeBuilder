"""Trace all native near/mid bed domains to the actual removal and packed-frame data."""
import hashlib
import json
import struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
research = ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research'
audit = json.loads((ROOT/'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
parser_path = ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/audit.py'
parser = parser_path.read_text().split('def analyze')[0]
parser = parser.replace('(0,4,13)', '(0,1,2,4,13)').replace("formats={0:'<f4',1:'<f2',10:'<u4',11:'<i4'}", "formats={0:'<f4',1:'<f2',5:'<i2',10:'<u4',11:'<i4'}")
parser = parser.replace("{0:'positions',4:'uv',13:'bones'}", "{0:'positions',1:'normalPacked',2:'tangentPacked',4:'uv',13:'bones'}")
namespace = {'__file__':str(parser_path)}
exec(parser, namespace)
ct = namespace['c']
unpack = namespace['dll'].math_array_oct15_to_fp32tangents
unpack.argtypes = [ct.c_void_p, ct.c_int, ct.c_void_p]
unpack.restype = None
data = (ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_Lift04RoadTops_g_cs.bin').read_bytes()
counts = struct.unpack_from('<6i', data, 8)
ints = np.frombuffer(data, '<i4', counts[0], 32)
result=[]
for lod in (0,1):
    original=research/'movable-skin'/('LiftBridge04'+('_LOD1' if lod else '')+' Mesh.Geometry')
    raw=original.read_bytes()
    assert hashlib.sha256(raw).hexdigest()==audit['lods'][lod]['geometrySha256']
    mesh=namespace['decode'](raw)[0]
    header=struct.unpack_from('<QI14i5I',raw,10)
    assert (header[0] & 15)==0 and ((header[0] >> 16) & 15)==1
    removed=ints[6+lod*800:806+lod*800].reshape(200,4)
    assert np.array_equal(mesh['indices'][removed[:,0]],removed[:,1:])
    road_faces=set(map(int,removed[:,0]))
    rows=[]
    for tile in audit['lods'][lod]['orientationTiles']:
        faces=tile['faces'];ids=np.unique(mesh['indices'][faces]);p=mesh['positions'][ids].astype(float);uv=mesh['uv'][ids].astype(float)
        affine=np.linalg.lstsq(np.c_[p[:,[0,2]],np.ones(len(ids))],uv,rcond=None)[0]
        owner=[f in road_faces for f in faces]
        assert all(owner) or not any(owner)
        packed = np.unique(mesh['tangentPacked'][ids].copy().view('<u4'))
        decoded = np.zeros((len(packed),4),dtype='<f4')
        unpack(packed.ctypes.data,len(packed),decoded.ctypes.data)
        rows.append(dict(**tile,role='generated-road' if all(owner) else 'retained-structure',
            affineXZ=affine.tolist(),maxAffinePixelError=float(np.max(np.abs(np.c_[p[:,[0,2]],np.ones(len(ids))]@affine-uv))*4096),
            normalPacked=[f'{x:08x}' for x in np.unique(mesh['normalPacked'][ids].copy().view('<u4'))],
            tangentPacked=[f'{x:08x}' for x in packed], tangentDecoded=np.unique(decoded,axis=0).tolist()))
    covered={f for r in rows if r['role']=='generated-road' for f in r['faces']}
    missing=sorted(road_faces-covered)
    extra=[]
    for face in missing:
        ids=mesh['indices'][face]
        extra.append(dict(face=face,positions=mesh['positions'][ids].tolist(),uv=mesh['uv'][ids].tolist(),
            normalPacked=[f'{x:08x}' for x in np.unique(mesh['normalPacked'][ids].copy().view('<u4'))],
            tangentPacked=[f'{x:08x}' for x in np.unique(mesh['tangentPacked'][ids].copy().view('<u4'))]))
    result.append(dict(lod=lod,geometrySHA256=hashlib.sha256(raw).hexdigest(),
        nativePositionFormat='Float32', nativeUvFormat='Float16',
        roadRemovalFaces=len(road_faces),auditCoveredRoadFaces=len(covered),
        retainedStructureFaces=sum(len(r['faces']) for r in rows if r['role']=='retained-structure'),
        domains=rows,roadFacesOutsideOriginalAudit=extra))
out=ROOT/'artifacts/material-remap/Lift04/near-domain-audit.json'
out.write_text(json.dumps(dict(levels=result,
    precisionRule='UV source storage is Float16; positions are Float32. A mapping must respect original UV rounding intervals. Do not infer a half-float position tolerance from visually quantized coordinates.',
    scope='All 192 original audit tiles per LOD plus every road-removal face omitted by that audit. Domain grouping and output validation remain separate.'),indent=2))
print([(r['lod'],r['roadRemovalFaces'],r['auditCoveredRoadFaces'],r['retainedStructureFaces'],len(r['roadFacesOutsideOriginalAudit'])) for r in result])
