"""Verify full pure-road payloads, unchanged source records and physical metrics."""
import ctypes
import json
import struct
from pathlib import Path
import numpy as np
from PrepareCleanRoad import ROOT, OUT, digest, native
from ExportRecipes import numbers


def main():
    a = json.loads((OUT/'authoring.json').read_text())
    writer = native.Writer(
        Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'), OUT)
    report = []
    for part in a['parts']:
        for ch in part['channels']:
            p = ROOT/'assets/Materials/Lift05/Shared'/(ch['cid']+'.Texture')
            raw = p.read_bytes()
            version,w,h,depth,mips,fmt,dim,filt,wrap,aniso,size = struct.unpack_from('<4H6Bi',raw)
            assert (version,depth,dim,filt,wrap,aniso)==(5,1,2,2,0,16)
            assert fmt==(4 if ch['srgb'] else 8) and mips==max(w,h).bit_length()
            count=sum(max(1,w>>m)*max(1,h>>m)*4 for m in range(mips))
            out=ctypes.create_string_buffer(count)
            assert size==len(raw)-18 and writer.decode(raw[18:],size,out,count,0)==count
            at=0; tests=[]
            for m in range(mips):
                ww,hh=max(1,w>>m),max(1,h>>m)
                image=np.frombuffer(out.raw[at:at+ww*hh*4],np.uint8).reshape(hh,ww,4)
                assert digest(image.tobytes())==ch['decodedMipHashes'][m]
                if ch['name']=='_MaskMap':assert not image[...,:3].any(), 'Rail metallic contamination'
                f=image.astype(float)
                tests.append(dict(mip=m,size=[ww,hh],seamURms=float(np.sqrt(np.mean((f[:,0]-f[:,-1])**2))),
                                  seamVRms=float(np.sqrt(np.mean((f[0]-f[-1])**2))),
                                  interiorURms=float(np.sqrt(np.mean(np.diff(f,axis=1)**2))) if ww>1 else 0,
                                  interiorVRms=float(np.sqrt(np.mean(np.diff(f,axis=0)**2))) if hh>1 else 0))
                at+=ww*hh*4
            assert at==count
            report.append(dict(part=part['part'],channel=ch['name'],sha256=digest(raw),encodingError=0,mips=tests))
    before=numbers(OUT/'before/Types_Lift05_RoadProjection.bin')
    after=numbers(ROOT/'assets/HardcodedNumbers/Lift05/Types_Lift05_RoadProjection.bin')
    old=json.loads((OUT.parent/'road-projection-plan.json').read_text())
    ev=json.loads((OUT.parent/'road-continuous-candidate/mapping-evidence.json').read_text())
    i,j=2,0;unchanged=0;metric_errors=[]
    for lod,(o,e) in enumerate(zip(old['levels'],ev['levels'])):
        count,nodes=before['I32'][i:i+2]
        assert before['I32'][i:i+5]==after['I32'][i:i+5];i+=5
        groups=e['continuousComponents']+[[p['patch']] for p in e['patches'] if p['mode']=='original-finite']
        for p,g in enumerate(groups):
            band,unit=after['I32'][i:i+2]
            if band!=1:
                assert before['I32'][i:i+2]==after['I32'][i:i+2]
                assert before['F64'][j:j+10]==after['F64'][j:j+10];unchanged+=1
            else:
                part=a['parts'][unit-4];dims=np.array(part['donorRectangle'][2:])
                original=np.array(o['patches'][g[0]]['coefficients'])[:2]*part['sourceSize']
                actual=np.array(after['F64'][j:j+6]).reshape(3,2)[:2]*dims
                error=float(np.max(abs(actual-original)));metric_errors.append(error)
                assert error<1e-12
            i+=2;j+=10
        assert before['I32'][i:i+nodes*2]==after['I32'][i:i+nodes*2]
        assert before['F64'][j:j+nodes*3]==after['F64'][j:j+nodes*3]
        i+=nodes*2;j+=nodes*3
    result=dict(status='PASS',channels=report,changedCentralPatches=len(metric_errors),unchangedSidePatches=unchanged,
                maximumTexelJacobianError=max(metric_errors),partitionTreesIdentical=True,
                candidateGeometryChanged=False, sourceRailMetalPresent=False,
                limitations='Native resource/upload and live near/far/animated appearance are separate checks. Seam measurements are reported, not a universal tolerance approval.')
    (OUT/'complete-mip-validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print('PASS',len(report),'complete channel chains;',len(metric_errors),'central gradients;',unchanged,'unchanged side domains')


if __name__=='__main__':main()
