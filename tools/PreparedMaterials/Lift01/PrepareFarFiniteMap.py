"""Measure an offline-only finite trapezoid map; never clip runtime receivers by faces."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np
from PrepareNativeObjects import ROOT, GAME, descriptor as parse_descriptor
from VerifyDensity import native_bytes, hash_text
sys.path.insert(0,str(ROOT/'tools/PrepareRepeatUnits'))
from ExtractVtSources import Decoder


def main():
    frames_path=ROOT/'artifacts/material-remap/Lift01/floor-native-frames.json'
    frames=json.loads(frames_path.read_text())
    faces=[f for f in frames['faces'] if f['lod']==2 and f['face'] in (730,731)]
    assert len(faces)==2
    positions=np.array([f['positions'] for f in faces],float)
    uv=np.array([f['uv'] for f in faces],float)
    normals=np.array([f['normals'] for f in faces],float).reshape(-1,3)
    tangents=np.array([f['tangents'] for f in faces],float).reshape(-1,4)
    assert len(np.unique(normals,axis=0))==len(np.unique(tangents,axis=0))==1
    n=normals[0]/np.linalg.norm(normals[0]); t=tangents[0,:3];w=tangents[0,3]
    source_basis=np.column_stack([t,np.cross(n,t)*w,n])
    target_n=np.array([0.,1.,0.]);target_basis=np.column_stack([t,np.cross(target_n,t)*w,target_n])
    # The original slight mesh slope is geometric shading, not texture content.
    # Carry the original local tangent convention onto the receiver's own normal;
    # do not bake source geometry tilt into the texture normal.
    normal_transform=np.eye(3)
    assert np.max(abs(normal_transform@normal_transform.T-np.eye(3)))<1e-12
    zmin,zmax=positions[:,:,2].min(),positions[:,:,2].max()
    widths=[float(np.max(abs(positions[:,:,0][positions[:,:,2]==z]))) for z in (zmax,zmin)]
    length=float(zmax-zmin)
    panels=[]
    for face,p,u in zip(faces,positions,uv):
        affine=np.linalg.solve(np.c_[p[:,[0,2]],np.ones(3)],u)
        assert np.max(abs(np.c_[p[:,[0,2]],np.ones(3)]@affine-u))<1e-12
        panels.append(dict(face=face['face'],xz=p[:,[0,2]].tolist(),sourceUvAffine=affine.T.tolist()))
    checks=[]
    for p,u in zip(positions.reshape(-1,3),uv.reshape(-1,2)):
        v=(zmax-p[2])/length;half=widths[0]+v*(widths[1]-widths[0]);q=[(p[0]/half+1)/2,v]
        reconstructed=[(q[0]*2-1)*half,zmax-q[1]*length]
        assert np.max(abs(np.array(reconstructed)-p[[0,2]]))<1e-12
        checks.append(dict(parameter=q,sourceXZ=reconstructed,sourceUV=u.tolist()))
    # Preserve full original Texture files as authoring inputs. The final native
    # asset must be produced and verified separately, never bind these inputs.
    out=ROOT/'artifacts/material-remap/Lift01/far-finite'
    out.mkdir(parents=True,exist_ok=True)
    cid='e0101c8ca7e9dc14016d187d012704a4'
    raw=native_bytes(GAME,cid,'Blob')
    descriptor=out/'Original.Surface';descriptor.write_bytes(raw)
    assert raw[6]==1
    original_maps=parse_descriptor(raw[:6]+b'\0'+raw[7+276+(17 if raw[283]==255 else 1):])['textures']
    channels=[]
    assert raw[283]==255
    vt_cid=hash_text(raw[284:300]);vt=native_bytes(GAME,vt_cid,'VT')
    vt_path=out/(vt_cid+'.VTSurface');vt_path.write_bytes(vt)
    decoder=Decoder(GAME)
    extraction=[]
    for layer,name in enumerate(('_BaseColorMap','_NormalMap','_MaskMap')):
        texture,proof=decoder.recover(dict(kind='VTSurface',stack=0,layer=layer,name=name),vt,cid)
        assert struct.unpack_from('<4HB',texture)==(5,512,512,1,10)
        texture_path=out/(name+'.Texture');texture_path.write_bytes(texture)
        channels.append(dict(name=name,path=str(texture_path),sha256=hashlib.sha256(texture).hexdigest()))
        extraction.append(dict(name=name,originalAuthoringCid=original_maps[name],
            nativeVtSurfaceCid=vt_cid,nativeVtSurfacePath=str(vt_path),nativeVtSurfaceSHA256=hashlib.sha256(vt).hexdigest(),
            extractedTextureSHA256=hashlib.sha256(texture).hexdigest(),**proof))
    (out/'extraction.json').write_text(json.dumps(extraction,indent=2))
    recipe=dict(recipe='BridgeBuilder/RepeatUnits/3',style='Lift01',productionReady=False,
        requiredExtension='OfflineFiniteTrapezoid: parameter q maps to native XZ with linearly varying half-width, then the original two affine panels supply source UV; no runtime face partition.',
        units=[dict(id='Lift01-far-negative-finite',sourceCid=cid,sourceDigest=hashlib.sha256(raw).hexdigest(),
            descriptor=dict(path=str(descriptor),sha256=hashlib.sha256(raw).hexdigest()),channels=channels,
            min=uv.reshape(-1,2).min(axis=0).tolist(),max=uv.reshape(-1,2).max(axis=0).tolist(),
            physicalSize=[widths[1]*2,length],repeatAxes='None',finiteSourceGrid=False,
            normalTransform=normal_transform.reshape(-1).tolist(),
            offlineFiniteTrapezoid=dict(halfWidthAtV0=widths[0],halfWidthAtV1=widths[1],zAtV0=float(zmax),length=length,panels=panels),
            targetTangent=tangents[0].tolist(),
            normalFrameEvidence=dict(originalNormal=n.tolist(),originalTangent=tangents[0].tolist(),
                originalBasis=source_basis.tolist(),receiverNormalIsPreserved=True,
                transformReason='Identity in measured local tangent coordinates. The source mesh slope is not baked into pixels; explicit tangent +X and handedness -1 retain the texture frame on the receiving normal.'),
            evidence=dict(path=str(frames_path),sha256=hashlib.sha256(frames_path.read_bytes()).hexdigest()))],
        vertexChecks=checks,sourceTexelSpan=((uv.reshape(-1,2).max(axis=0)-uv.reshape(-1,2).min(axis=0))*512).tolist(),
        runtime='One finite negative moving chart; final contentMin/contentMax map to normalized finite coordinates. Source triangles are authoring recipe evidence only. No receiver-length input is accepted.',
        blocker='The generic /3 producer has no offlineFiniteTrapezoid parser. Do not feed this proposal into the generic producer: unknown fields would be ignored. Complete original VTSurface BC7 mip chains were recovered byte-preserving with the shared verified decoder; no mip-zero regeneration. Sampling footprints, guards, decoded normal angles and full mips still require validation before binding.')
    (out/'recipe-proposal.json').write_text(json.dumps(recipe,indent=2))
    print(out/'recipe-proposal.json')


if __name__=='__main__':main()
