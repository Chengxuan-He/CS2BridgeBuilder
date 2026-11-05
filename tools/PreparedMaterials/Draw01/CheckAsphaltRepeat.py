"""Full native mip/PBR validation of Draw01's source-density authored asphalt."""
import json, struct
import numpy as np
from PIL import Image
from StageCleanCap import ROOT, decode, sha

OUT = ROOT/'artifacts/material-remap/Draw01/asphalt-repeat34'


def normals(a):
    c=a.astype(float)/255
    x=c[:,:,0]*c[:,:,3]*2-1; y=c[:,:,1]*2-1
    n=np.stack((x,y,np.sqrt(np.maximum(0,1-x*x-y*y))),axis=2)
    return n/np.maximum(np.linalg.norm(n,axis=2)[:,:,None],1e-12)


def main():
    author=json.loads((OUT/'authoring.json').read_text()); channels=[]
    coordinates=np.load(OUT/'source-coordinates.npz')
    assert coordinates['x'].min()>=96 and coordinates['x'].max()<640
    assert coordinates['y'].min()==2778 and coordinates['y'].max()==3509
    for ch,record in enumerate(author['channels']):
        raw=(OUT/f'asphalt-{ch}.Texture').read_bytes()
        assert sha(raw)==record['sha256']
        assert struct.unpack_from('<HH',raw,2)==(512,732)
        assert list(raw[8:14])==[10,108 if ch==0 else 109,2,2,0,16]
        data=decode(raw); expected=(OUT/f'asphalt-{ch}.rgba').read_bytes()
        offset=eo=0; levels=[]
        for mip in range(10):
            w,h=max(1,512>>mip),max(1,732>>mip)
            size=((w+3)//4)*((h+3)//4)*16
            image=np.asarray(Image.frombytes('RGBA',(w,h),data[offset:offset+size],'bcn',(7,'BC7'))).copy()
            source=np.frombuffer(expected[eo:eo+w*h*4],np.uint8).reshape(h,w,4)
            diff=image.astype(float)-source
            level=dict(mip=mip,width=w,height=h,encodingRms=float(np.sqrt(np.mean(diff*diff))),encodingMax=float(np.abs(diff).max()),seamURms=float(np.sqrt(np.mean((image[:,0].astype(float)-image[:,-1])**2))),seamVRms=float(np.sqrt(np.mean((image[0].astype(float)-image[-1])**2))))
            level['interiorNeighborURms']=float(np.sqrt(np.mean(np.diff(image.astype(float),axis=1)**2))) if w>1 else 0
            level['interiorNeighborVRms']=float(np.sqrt(np.mean(np.diff(image.astype(float),axis=0)**2))) if h>1 else 0
            if ch==0:
                c=image.astype(int); count=int(((c[:,:,0]-c[:,:,2]>20)&(c[:,:,1]-c[:,:,2]>10)).sum())
                assert count==0; level['yellowPixels']=count
            if ch==1:
                angle=np.degrees(np.arccos(np.clip((normals(image)*normals(source)).sum(axis=2),-1,1)))
                level['normalAngleP95Degrees']=float(np.percentile(angle,95));level['normalAngleMaxDegrees']=float(angle.max())
            if ch==3: assert np.all(image[:,:,:3]==0) and np.all(image[:,:,3]==255)
            levels.append(level);offset+=size;eo+=w*h*4
        assert offset==len(data) and eo==len(expected)
        channels.append(dict(name=record['name'],levels=levels,bytes=len(raw)))
    result=dict(status='PASS',sourceCoordinateRangeExcludesCurbPaintAndConcrete=True,channels=channels,nativeTextureBytes=sum(c['bytes'] for c in channels),nativeVisualAcceptance=False)
    (OUT/'native-mip-validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print('PASS full native mip/PBR graph;',result['nativeTextureBytes'],'texture bytes')


if __name__=='__main__': main()
