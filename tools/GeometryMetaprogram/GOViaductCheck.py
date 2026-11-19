"""Check emitted source provenance, all-LOD coverage and measured width mappings."""
from pathlib import Path
import argparse, hashlib, json, struct, zipfile, tempfile
import numpy as np
from UvDensityAudit import NativeGeometry
from GOViaduct import ENTRIES, REMOVED, CLIPPED_FAR

def read(path):
    raw=path.read_bytes();assert hashlib.sha256(raw[:-32]).digest()==raw[-32:]
    counts=struct.unpack_from('<6i',raw)
    offset=24;arrays=[]
    for count,kind in zip(counts,('<i4','<u4','<i8','<u8','<f4','<f8')):
        arrays.append(np.frombuffer(raw,dtype=kind,count=count,offset=offset));offset+=count*np.dtype(kind).itemsize
    assert offset==len(raw)-32
    return arrays

def main():
    p=argparse.ArgumentParser();p.add_argument('archive');p.add_argument('game');p.add_argument('recipe');a=p.parse_args()
    folder=Path(a.recipe);decoder=NativeGeometry(Path(a.game)/'Plugins/x86_64/PipelinePlugin.dll');results=[]
    with zipfile.ZipFile(a.archive) as archive,tempfile.TemporaryDirectory() as tmp:
        for lod,entry in enumerate(ENTRIES):
            path=Path(tmp)/entry;path.write_bytes(archive.read(entry));mesh=decoder.read(path)[0]
            coverage=np.zeros(len(mesh['faces']),dtype='i4');parts=[]
            for part in range(5):
                def data(kind,index):return read(folder/f'Types_GOViaduct_{lod}_{part}_{kind}.bin')[index]
                origin=data('Origins',4).reshape(-1,3);coeff=data('Displacement',4)
                gradients=np.column_stack([data(k,4) for k in ('Frames','ShearY','ShearZ')])
                sources=data('SourceVertices',0);corners=data('Corners',0).reshape(-1,3);omitted=data('Omitted',0)
                retained=np.ones(len(mesh['faces']),bool);retained[omitted]=False;coverage+=retained
                f=corners[retained]
                original=mesh['positions'][sources]
                expected_uv=mesh['uv0'][sources].astype('f4')
                if lod==2 and part in (1,2):
                    st=data('SourceTriangles',0).reshape(-1,3);w=data('Weights',4).reshape(-1,3)
                    original=np.einsum('ij,ijk->ik',w.astype('f8'),mesh['positions'][st].astype('f8')).astype('f4')
                    expected_uv=np.einsum('ij,ijk->ik',w.astype('f8'),mesh['uv0'][st].astype('f8')).astype('f4')
                    f=data('Triangles',0).reshape(-1,3);sf=data('SourceFaces',0)
                    assert set(sf)==set(np.flatnonzero(retained))
                    for fi in CLIPPED_FAR:
                        selected=f[sf==fi]
                        if len(selected):
                            assert original[selected][:,:,1].max()<=8
                            assert original[selected][:,:,1].min()==np.float32(-40.001953125)
                            # Every output fragment retains the source winding and plane;
                            # the deep wall stays present, while its top fascia is absent.
                            src=mesh['positions'][mesh['faces'][fi]].astype('f8')
                            n=np.cross(src[1]-src[0],src[2]-src[0])
                            out=original[selected].astype('f8')
                            normals=np.cross(out[:,1]-out[:,0],out[:,2]-out[:,0])
                            assert np.all(normals@n>0)
                            ratio=np.linalg.norm(normals,axis=1).sum()/np.linalg.norm(n)
                            assert .9<ratio<1
                else:assert np.array_equal(sources[f],mesh['faces'][retained])
                if part<3:expected_uv[expected_uv[:,1]==1,1]=np.float32(.99951171875)
                np.testing.assert_allclose(data('Uv0',4).reshape(-1,2),expected_uv,atol=1e-7)
                expected=np.column_stack((original[:,2],original[:,1]-np.float32(9.5),-original[:,0]))
                if part:expected[:,2]+=(101.5 if part<3 else 91)*(1 if part in (1,3) else -1)
                assert np.array_equal(origin.view('u4'),expected.astype('f4').view('u4'))
                _,inverse=np.unique(origin,axis=0,return_inverse=True)
                low=np.full(inverse.max()+1,np.inf);high=-low.copy();np.minimum.at(low,inverse,coeff);np.maximum.at(high,inverse,coeff)
                assert np.array_equal(low,high)
                central=(original[:,1]>15)&(np.abs(original[:,2])<=np.float32(1.20001220703125));assert np.all(coeff[central]==0)
                rigid=(np.abs(original[:,2])>=np.float32(11.8));assert np.all(coeff[rigid]==np.sign(original[rigid,2]))
                tests=[]
                for width in (22.39990234375,8,32,64,-4):
                    delta=(width-22.39990234375)/2;target=origin.copy();target[:,0]=(origin[:,0].astype('f8')+coeff.astype('f8')*delta).astype('f4')
                    assert np.isfinite(target).all();assert np.array_equal(target[:,1:].copy().view('u4'),origin[:,1:].copy().view('u4'))
                    if delta==0:assert np.array_equal(target.view('u4'),origin.view('u4'))
                    assert np.array_equal(target[central],origin[central]);np.testing.assert_allclose(target[rigid,0]-origin[rigid,0],np.sign(original[rigid,2])*delta,atol=4e-6)
                    edges=origin[f[:,1:]].astype('f8')-origin[f[:,0,None]].astype('f8');dc=coeff[f[:,1:]].astype('f8')-coeff[f[:,0,None]].astype('f8')
                    residual=np.max(np.abs(np.einsum('fij,fj->fi',edges,gradients[f[:,0]])-dc));assert residual<2e-6,residual
                    tests.append(dict(width=width,frameResidual=float(residual),bounds=[target.min(0).tolist(),target.max(0).tolist()]))
                # Native BatchMeshHelpers reverses local X/Z before normalization:
                # course t=.5-localZ/referenceLength. SecondaryObject placement instead
                # follows +Z tangent, so rigid objects require the matching Y half-turn.
                cap_faces=np.all(np.abs(original[f,0])==112,axis=1)
                for scale in (.6,.8,1,1.2,1.4):
                    q=origin.astype('f8').copy()
                    q[:,0]*=-1
                    station=0 if part==0 else (1 if part in (1,3) else -1)*(101.5 if part<3 else 91)
                    if part<3:q[:,2]=station*scale-q[:,2]*scale
                    else:
                        q[:,2]=station*scale-q[:,2]
                        np.testing.assert_allclose(np.ptp(q,axis=0),np.ptp(origin.astype('f8'),axis=0),atol=1e-12)
                    expected_world=np.column_stack((-original[:,2],original[:,1]-np.float32(9.5),original[:,0])).astype('f8')
                    if part<3:expected_world[:,2]*=scale
                    else:expected_world[:,2]+=station*(scale-1)
                    np.testing.assert_allclose(q,expected_world,atol=1e-12)
                    if part in (1,2):
                        # Source exterior closure faces must remain at the OUTER road end,
                        # not move onto the arch joint while leaving the abutment open.
                        assert cap_faces.any()
                        cap=q[f[cap_faces]]
                        np.testing.assert_allclose(cap[:,:,2],np.sign(station)*112*scale,atol=1e-12)
                        assert np.all(np.abs(cap[:,:,2]-np.sign(station)*91*scale)>20*scale)
                        # The previous reversed Front/Back assignment must fail this oracle.
                        wrong=q[:,2]-2*station*scale
                        assert np.max(np.abs(wrong-expected_world[:,2]))>=203*scale-1e-9
                    if scale==1:np.testing.assert_array_equal(q,expected_world)
                parts.append(dict(part=part,faces=len(f),vertices=len(origin),seamConflicts=0,tests=tests))
            road=np.all(mesh['positions'][mesh['faces']][:,:,1]==np.float32(9.5),axis=1)
            assert int(road.sum())==(8,12,6)[lod]
            road[list(REMOVED[lod])]=True
            assert np.all(coverage[road]==0) and np.all(coverage[~road]==1)
            # Independent spatial oracle for the complete recessed roadway assembly.
            # The earlier upper-sheet-only exclusion passed face accounting while leaving
            # 504 actual metal-rod faces at each joint. Identify this source domain here,
            # not from REMOVED, so restoring that incomplete list fails the regression.
            q=mesh['positions'][mesh['faces']]
            if lod==0:
                rods=(np.abs(q[:,:,0]).min(1)>88.1)&(np.abs(q[:,:,0]).max(1)<=91)&\
                     (q[:,:,1].min(1)>9.3)&(q[:,:,1].max(1)<=9.5)&\
                     (np.abs(q[:,:,2]).max(1)<11.21)
                assert int(rods.sum())==1300
                for side in (-1,1):
                    end=rods&(q[:,:,0].mean(1)*side>0)
                    assert int(end.sum())==146+504 and np.all(coverage[end]==0)
            elif lod==1:
                assert np.all(coverage[[3432,3433,3704,3705]]==0)
            else:
                assert np.all(coverage[np.all(q[:,:,1]==np.float32(9.5),axis=1)]==0)
            results.append(dict(lod=lod,sourceFaces=len(coverage),omittedRoadFaces=int(road.sum()),parts=parts))
    print(json.dumps(results,indent=2));print('PASS all LOD exact disjoint source-face coverage, channel provenance, seams, native-oriented per-vertex source correspondence, exterior closure placement, and rigid foundation frames at scales .6/.8/1/1.2/1.4, reference identity, signed widths and affine frames')

if __name__=='__main__':main()
