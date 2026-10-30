"""Independent source-phase/finite-V checks on actual uploaded C# wrap captures."""
import hashlib
import json
import struct
import numpy as np
from ReviewStructuralSeams import ROOT, phases, motion


def ulp(value):
    value=np.float32(abs(value))
    return float(np.nextafter(value,np.float32(np.inf))-value)


def main():
    policy=json.loads((ROOT/'artifacts/prepared-materials/Lift05/non-tiling/classification.json').read_text())
    reports=[]
    for lod in range(2):
        a=np.load(ROOT/f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz')
        p=a['mesh0_positions'].astype(float);uv=a['mesh0_uv0'].astype(float);faces=a['mesh0_indices'];bones=a['mesh0_boneIndices'].ravel()
        render='LiftBridge05 Mesh' if lod==0 else 'LiftBridge05_LOD1 Mesh'
        parts=next(m for m in policy['meshes'] if m['render']==render and m['mesh']==0)['mixedProfileEvidence']
        by_face={face:part for part in parts for face in part['faces']}
        weights,_=motion(lod,len(p))
        for delta in (0,1,3,6,12):
            target=p.copy();target[:,0]=(p[:,0].astype(np.float32)+weights*np.float32(delta)).astype(float)
            offsets,_=phases(parts,p,uv,faces,target)
            profiles={}
            for part in parts:
                ids=faces[part['faces']].ravel();x=p[ids,0];u=uv[ids,0]
                low,high=float(u.min()),float(u.max());rate=float((u[np.argmax(x)]-u[np.argmin(x)])/(high-low)/np.ptp(x))
                anchor=int(ids[np.argmin(x)])
                profiles[part['seedFace']]=(low,high,rate,offsets[part['seedFace']]-rate*(target[anchor,0]-p[anchor,0]))
            path=ROOT/f'artifacts/material-remap/Lift05/native-wrap-inputs/capture-lod{lod}-delta{delta}.bin'
            raw=path.read_bytes();count=struct.unpack_from('<i',raw)[0];at=4;seen=set();errors=[];verrors=[];limits=[];vertices=0
            for _ in range(count):
                face,n=struct.unpack_from('<2i',raw,at);at+=8
                assert face in by_face and face not in seen;seen.add(face)
                low,high,rate,offset=profiles[by_face[face]['seedFace']];period=high-low;ids=faces[face]
                for _ in range(n):
                    x,y,z,u,v,bone,wa,wb,wc=struct.unpack_from('<5fI3d',raw,at);at+=48;vertices+=1
                    bary=np.array([wa,wb,wc]);source=bary@p[ids];source_uv=bary@uv[ids]
                    expected=(source_uv[0]-low)/period+rate*(x-source[0])+offset
                    actual=(u-low)/period
                    error=abs((actual-expected)-round(actual-expected))*period
                    # Bound only arithmetic/Float32 coordinate and UV storage error.
                    # This does not establish a texture filtering/material tolerance.
                    limit=2*ulp(u)+abs(rate)*period*4*(ulp(x)+max(ulp(k) for k in p[ids,0]))
                    assert low<=u<=high and error<=limit,(lod,delta,face,error,limit)
                    verror=abs(v-source_uv[1]);vlimit=2*ulp(v)+np.ptp(uv[ids,1])*np.finfo(np.float32).eps
                    assert verror<=vlimit,(lod,delta,face,verror,vlimit)
                    assert bone==int(bones[ids[0]])
                    # A visible wrong phase must be rejected, not hidden by the
                    # arithmetic bound or finite-axis classification.
                    bad=abs((actual+.001/period-expected)-round(actual+.001/period-expected))*period
                    assert bad>limit
                    errors.append(error*4096);verrors.append(verror*4096);limits.append(limit*4096)
            assert at==len(raw) and seen==set(by_face)
            reports.append(dict(lod=lod,halfWidthDelta=delta,sourceFaces=count,uploadedCorners=vertices,
                maximumUPhaseErrorTexels=max(errors),maximumFiniteVErrorTexels=max(verrors),maximumArithmeticBoundTexels=max(limits),
                sha256=hashlib.sha256(raw).hexdigest()))
    output=ROOT/'artifacts/material-remap/Lift05/native-wrap-phase-check.json'
    output.write_text(json.dumps(dict(passed=True,cases=reports,
        negativeControl='A 0.001 UV phase error is rejected in every checked corner.',
        limitation='Actual uploaded UV/geometry captures against original source mapping. Arithmetic bounds cover Float32 operations only, not filtering tolerance, game rendering or cold load.'),indent=2)+'\n')
    print('PASS 10 actual native captures: all 237 source faces, component U phase, finite V, bone, negative controls.')


if __name__=='__main__':main()
