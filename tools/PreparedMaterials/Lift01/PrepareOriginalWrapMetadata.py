"""Describe genuine original road-only V resets; never create runtime geometry or pixels."""
import hashlib
import json
import math
from pathlib import Path
import numpy as np
from ExportRecipes import numbers
ROOT=Path(__file__).resolve().parents[3]


def main():
    data_path=ROOT/'assets/HardcodedNumbers/Lift01/Types_Lift01_HalfFloorDomains.bin'
    f=numbers(data_path)['F32']
    audit_path=ROOT/'artifacts/layer-decoupling/Lift01/outputs/native-repeat-mechanism/audit.json'
    audit=json.loads(audit_path.read_text())
    domains=[];coverage=[];unique_seams=set()
    for lod in (0,1):
        for moving in (0,1):
            segments=(0,1,2) if not moving else (3,4)
            for half in (0,1):
                direction=1 if half==0 else -1
                for side in (0,1):
                    group=moving*4+half*2+side
                    for segment in segments:
                        lo,hi,rate,bias=f[74+4*segment:78+4*segment]
                        finite=segment not in (1,3)
                        cuts=[]
                        if not finite:
                            qlo,qhi=sorted([lo*rate+bias,hi*rate+bias])
                            for k in range(math.floor(qlo)+1,math.ceil(qhi)):
                                z=(k-bias)/rate
                                if lo+1e-5<z<hi-1e-5:
                                    cuts.append(dict(objectZ=z*direction,normalizedVInteger=k))
                                    unique_seams.add((moving,half,round(z*direction,8)))
                        for panel in range(2 if lod==0 else 1):
                            if lod==1:slope=(f[2]-f[0])/f[6];intercept=f[0];x0,x1=0,f[6]
                            elif panel==0:slope=(f[1]-f[0])/f[5];intercept=f[0];x0,x1=0,f[5]
                            else:slope=(f[2]-f[1])/(f[6]-f[5]);intercept=f[1]-slope*f[5];x0,x1=f[5],f[6]
                            domains.append(dict(lod=lod,bone=4 if moving else 0,half=half,side=side,panel=panel,
                                absoluteZ=[lo,hi],absoluteX=[x0,x1],sourceUObjectGradient=[slope*(-1 if side==0 else 1),0,0,intercept],
                                sourceVNormalizedObjectGradient=[0,0,rate*direction,bias],
                                sourceBounds=[f[0],f[3],f[2],f[4]],
                                tangent=list(f[10+(lod*8+group)*4:14+(lod*8+group)*4]),
                                repeatU=False,repeatV=not finite,sourceUv=finite,seams=cuts,
                                material='Original Surface bcdd054871e3bcb50f793ccefc50304d',
                                boundaryRule='Each genuine period cell retains its own 0/1 endpoint; never apply frac to both shared-edge vertices without assigning the cell.'))
    for level in audit['reports'][:2]:
        for face in level['sourceFaces']:
            p=np.array(face['xyz']);uv=np.array(face['uv']);center=p.mean(0)
            matches=[d for d in domains if d['lod']==level['lod'] and d['bone']==face['bones'][0] and
                d['half']==int(center[2]<0) and d['side']==int(center[0]>0) and
                d['absoluteZ'][0]<=abs(center[2])<=d['absoluteZ'][1] and d['absoluteX'][0]<=abs(center[0])<=d['absoluteX'][1]]
            assert len(matches)==1
            d=matches[0];points=np.c_[p,np.ones(3)];u=points@np.array(d['sourceUObjectGradient']);v=points@np.array(d['sourceVNormalizedObjectGradient'])
            original=(uv[:,1]-f[3])/(f[4]-f[3]);error=v-original
            if d['repeatV']:
                # All three native face corners must use one genuine period cell.
                cell=round(float(np.mean(error)));error-=cell
                assert np.max(abs(error))<2e-6
            assert np.max(abs(error))<2e-6 and np.max(abs(u-uv[:,0]))<1e-7
            coverage.append(dict(lod=level['lod'],face=face['face'],finite=d['sourceUv'],maxNormalizedVError=float(np.max(abs(error)))))
    report=dict(sourceGeometryAuditSHA256=hashlib.sha256(audit_path.read_bytes()).hexdigest(),
        measuredResourceSHA256=hashlib.sha256(data_path.read_bytes()).hexdigest(),
        roadOnly=True,supportedInputAppearance='Ballast',noEmbeddedPaintEvidence='terminal-paint-inspection.json',
        originalSurfaceCid='bcdd054871e3bcb50f793ccefc50304d',newTextureCount=0,
        domains=domains,verifiedFaces=coverage,uniqueInternalLongitudinalResetPlanes=len(unique_seams),
        scope='Only original regular road domains reset V. Original finite terminals and original physical U panels remain distinct. No prototype-face Boolean, no arbitrary subdivision, no full-span pixels.',
        runtimeInterfacePending=True)
    out=ROOT/'artifacts/material-remap/Lift01/original-wrap-metadata.json'
    out.write_text(json.dumps(report,indent=2));print(out);print('domains',len(domains),'faces',len(coverage),'internal planes',len(unique_seams))


if __name__=='__main__':main()
