"""Measure the original Lift02 finite far charts and near finite cross section."""
import hashlib,json,struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift02/outputs/native-repeat-mechanism/native-repeat-mechanism.json').read_text())
catalog=json.loads((ROOT/'artifacts/material-remap/Lift02/source-mapping/classification.json').read_text())
hashes={r['render']:r['nativeSha256'] for r in catalog['routes']}
path=ROOT/'artifacts/uv-density/native/LiftBridge02_LOD2 Mesh.npz'
assert hashlib.sha256(path.read_bytes()).hexdigest()==hashes[path.stem]
mesh=np.load(path); ids=audit['lods'][2]['roadTopFaceIndices']; rows=[]; checks=[]
groups=[]
for face in ids[::2]:
    ix=mesh['mesh0_faces'][[face,face+1]]; p=mesh['mesh0_positions'][ix].reshape(-1,3).astype(float); uv=mesh['mesh0_uv0'][ix,:2].reshape(-1,2).astype(float)
    lo,hi=p[:,2].min(),p[:,2].max(); half=float(max(abs(p[:,0])))
    # Preserve the complete finite source domain at the established detailed
    # motion boundaries. No geometry changes or new period boundaries.
    a=-106 if lo < -100 else -82 if lo < 0 else 82
    b=106 if hi > 100 else 82 if hi > 0 else -82
    q=p.copy(); q[:,2]=a+(p[:,2]-lo)/(hi-lo)*(b-a)
    fit=np.linalg.solve(np.c_[q[:3,0],q[:3,2],np.ones(3)],uv[:3]).astype('<f4')
    error=float(np.max(abs(np.c_[q[:,0],q[:,2],np.ones(6)]@fit-uv))*512)
    assert error < .0001
    groups.append((a,[a,b,half,*uv.min(0),*uv.max(0),*fit[:,0],*fit[:,1]]))
    checks.append(dict(faces=[face,face+1],originalZ=[lo,hi],generatedZ=[a,b],halfWidth=half,maxPixelError=error))
for _,row in sorted(groups):rows.extend(row)
near_path=ROOT/'artifacts/uv-density/native/LiftBridge02 Mesh.npz'
assert hashlib.sha256(near_path.read_bytes()).hexdigest()==hashes[near_path.stem]
near=np.load(near_path); ix=near['mesh0_faces'][83248]; p=near['mesh0_positions'][ix]; uv=near['mesh0_uv0'][ix,:2]
near_values=[*uv.min(0).astype(float),*uv.max(0).astype(float),float(abs(p[:,0].min())),4.,.5]
vmin,vmax=near_values[1],near_values[3]
near_checks=[]
for lod in audit['lods'][:2]:
    source_path=ROOT/'artifacts/uv-density/native'/(lod['render']+'.npz')
    assert hashlib.sha256(source_path.read_bytes()).hexdigest()==hashes[lod['render']]
    source=np.load(source_path); ix=source['mesh0_faces'][lod['roadTopFaceIndices']]
    p=source['mesh0_positions'][ix].astype(float); uv=source['mesh0_uv0'][ix,:2].astype(float)
    v=(uv[:,:,1]-near_values[1])/(near_values[3]-near_values[1])
    expected_u=(uv[:,:,0]-near_values[0])/(near_values[2]-near_values[0])
    q=.5-p[:,:,2]/4
    error=abs((q-expected_u+.5)%1-.5)
    assert error.max()==0
    for delta in (0.,2.,8.):
        width=near_values[4]+delta
        sign=np.where(p[:,:,0]<0,1.,-1.)
        receiver_x=sign*(v-1)*width
        guarded=vmax+sign*receiver_x/width*(vmax-vmin)
        assert np.max(abs(guarded-(vmin+(vmax-vmin)*v)))<1e-12
        assert guarded.min()>=vmin and guarded.max()<=vmax
    near_checks.append(dict(lod=lod['lod'],sourceSha256=hashes[lod['render']],faces=len(ix),allOriginalUPhasesExact=True,finiteV=[vmin,vmax],widthDeltas=[0,2,8]))
prepared_path=ROOT/'artifacts/material-remap/Lift02/bed-unit-v3/report.json'
prepared=json.loads(prepared_path.read_text())['units'][0]
bounds=[(c['contentMin'],c['contentMax']) for c in prepared['channels']]
assert all(b==bounds[0] for b in bounds)
assert bounds[0][0][0]==0 and bounds[0][1][0]==1
near_values.extend([bounds[0][0][1],bounds[0][1][1]])
values=np.array(rows+near_values,dtype='<f4'); ints=np.array([3,13,39],dtype='<i4')
raw=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(values),0)+ints.tobytes()+values.tobytes()
dest=ROOT/'assets/HardcodedNumbers/Lift02/Types_Lift02_BedAppearance.bin';dest.write_bytes(raw+hashlib.sha256(raw).digest())
out=ROOT/'artifacts/material-remap/Lift02/source-mapping/bed-appearance.json'
out.write_text(json.dumps(dict(sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),checks=checks,near=near_values,nearChecks=near_checks,resourceSha256=hashlib.sha256(dest.read_bytes()).hexdigest(),mapping='Near/mid: continuous U 4m period, no period cuts; finite V uses the measured offline unit content bounds. Candidate filtering acceptance remains pending. Far: three complete finite native atlas charts at existing detailed motion boundaries.'),indent=2))
print(out)
