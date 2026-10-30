"""Fit four finite charts inside the original Float16 rounding intervals."""
import hashlib
import itertools
import json
import struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]


def fit(points, values):
    original = values.astype(np.float16)
    lower = (np.nextafter(original, np.float16(-np.inf)).astype(float) + original.astype(float)) / 2
    upper = (np.nextafter(original, np.float16(np.inf)).astype(float) + original.astype(float)) / 2
    scale = (upper - lower) / 2
    a = np.r_[np.c_[points, scale], np.c_[-points, scale]]
    b = np.r_[upper, -lower]
    best = None
    for ids in itertools.combinations(range(len(b)), 4):
        try:
            x = np.linalg.solve(a[list(ids)], b[list(ids)])
        except np.linalg.LinAlgError:
            continue
        if np.max(a @ x - b) < 1e-12 and (best is None or x[3] > best[3]):
            best = x
    assert best is not None and best[3] > 0, 'No common affine within original storage intervals'
    coefficients = best[:3].astype(np.float32)
    reconstructed = points @ coefficients
    assert np.array_equal(reconstructed.astype(np.float16).view(np.uint16), original.view(np.uint16))
    return coefficients, float(best[3]), float(np.max(abs(reconstructed-values))*512)


def main():
    path = ROOT / 'artifacts/uv-density/native/LiftBridge01_LOD2 Mesh.npz'
    mesh = np.load(path)
    rows, checks = [], []
    fold=None
    for ids in [[708,709], [730,731], [342,343], [320,321]]:
        f = mesh['mesh0_faces'][ids]
        p = mesh['mesh0_positions'][f].reshape(-1,3).astype(float)
        uv = mesh['mesh0_uv0'][f,:2].reshape(-1,2).astype(float)
        points = np.c_[p[:,[0,2]], np.ones(6)]
        try:
            u, um, ue = fit(points, uv[:,0])
        except AssertionError:
            assert ids==[730,731]
            fields=[]
            for part in range(2):
                local=points[part*3:part*3+3];target=uv[part*3:part*3+3]
                u,um,ue=fit(local,target[:,0]);v,vm,ve=fit(local,target[:,1])
                rows.extend([p[:,2].min(),p[:,2].max(),abs(p[:,0]).max(),
                    *uv.min(axis=0),*uv.max(axis=0),*u,*v])
                fields.append(np.c_[u,v])
            shared=[q for q in points[:3] if any(np.array_equal(q,r) for r in points[3:])]
            assert len(shared)==2
            a,b=shared;line=np.array([b[1]-a[1],a[0]-b[0],b[0]*a[1]-a[0]*b[1]])
            if np.dot(line,points[:3].mean(0))>0:line=-line
            line/=np.linalg.norm(line[:2]);fold=line.astype(np.float32)
            sampled=np.array([a*(1-t)+b*t for t in np.linspace(0,1,1025)])
            continuity=float(np.max(abs(sampled@fields[0]-sampled@fields[1]))*512)
            assert continuity<.0001
            checks.append(dict(faces=ids, commonFloat32AffineReencodesAllOriginalFloat16Bits=False,
                reason='No common affine inside original Float16 intervals; one measured continuous gradient fold is necessary and sufficient.',
                positions=p.tolist(), uv=uv.tolist(),runtimeDomains=2,additionalGradientBoundaries=1,
                runtimeBoundary=fold.tolist(),gradientDifference=(fields[0]-fields[1]).tolist(),
                continuitySamples=1025,maxFoldContinuityPixelError=continuity,
                bothPiecewiseAffinesReencodeOriginalBits=True))
            continue
        v, vm, ve = fit(points, uv[:,1])
        rows.extend([p[:,2].min(), p[:,2].max(), abs(p[:,0]).max(),
                     *uv.min(axis=0), *uv.max(axis=0), *u, *v])
        gradients = []
        for j in range(2):
            exact = np.linalg.solve(points[j*3:j*3+3], uv[j*3:j*3+3])
            gradients.append(dict(face=ids[j], affine=exact.tolist(),
                maxWholeDomainPixelDifference=float(np.max(abs(points @ exact-points @ np.c_[u,v]))*512)))
        checks.append(dict(faces=ids, commonFloat32AffineReencodesAllOriginalFloat16Bits=True,
                           normalizedInteriorMargin=[um,vm], maxEndpointPixelError=[ue,ve], sourceFaceGradients=gradients))
    assert len(rows)==5*13 and fold is not None
    rows.extend(fold)
    ints = np.array([5,13], dtype='<i4')
    floats = np.array(rows, dtype='<f4')
    raw = struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(floats),0)+ints.tobytes()+floats.tobytes()
    target=ROOT/'assets/HardcodedNumbers/Lift01/Types_Lift01_FarAppearance.bin'
    target.write_bytes(raw+hashlib.sha256(raw).digest())
    report = ROOT/'artifacts/material-remap/Lift01/far-appearance-verification.json'
    report.write_text(json.dumps(dict(meshSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        domains='Four original finite charts plus one necessary continuous UV-gradient boundary in the negative moving chart. Five affine domains, no prototype-face Boolean.', checks=checks,
        allEightSourceFacesReencodeOriginalFloat16=True,additionalGradientBoundaries=1,readyForRuntime=True,
        payloadSHA256=hashlib.sha256(target.read_bytes()).hexdigest()),indent=2))
    print(report)


if __name__=='__main__':
    main()
