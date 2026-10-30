"""Independently compare persisted finite candidate pixels to original native mip samples."""
import ctypes as ct
import hashlib
import json
import math
import struct
from pathlib import Path
import numpy as np
from PIL import Image
from PrepareNativeObjects import ROOT, GAME


def linear(x):
    return np.where(x<=.04045,x/12.92,((x+.055)/1.055)**2.4)


def texture(path):
    raw=path.read_bytes();version,w,h,d,mips,fmt=struct.unpack_from('<4H2B',raw)
    assert version==5 and d==1
    packed=struct.unpack_from('<i',raw,14)[0];data=raw[18:]
    if packed!=-1:
        assert packed==len(data)
        dll=ct.CDLL(str(GAME/'Plugins/x86_64/PipelinePlugin.dll'))
        calc=dll.decompress_calc_bound;calc.argtypes=[ct.c_void_p,ct.c_int64,ct.c_int];calc.restype=ct.c_int64
        dec=dll.decompress_data;dec.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p,ct.c_int64,ct.c_int];dec.restype=ct.c_int64
        size=calc(data,len(data),0);dest=ct.create_string_buffer(size)
        assert dec(data,len(data),dest,size,0)==size
        data=dest.raw
    levels=[];offset=0
    for mip in range(mips):
        width,height=max(1,w>>mip),max(1,h>>mip)
        if fmt in (108,109):
            size=((width+3)//4)*((height+3)//4)*16
            pixels=np.asarray(Image.frombytes('RGBA',(width,height),data[offset:offset+size],'bcn',(7,'BC7'))).astype(float)/255
        else:
            assert fmt in (4,8),fmt
            size=width*height*4;pixels=np.frombuffer(data[offset:offset+size],np.uint8).reshape(height,width,4).astype(float)/255
        if fmt in (4,108):pixels[:,:,:3]=linear(pixels[:,:,:3])
        levels.append(pixels);offset+=size
    assert offset==len(data)
    return levels,dict(width=w,height=h,mips=mips,format=fmt,filter=raw[11],wrap=raw[12],sha256=hashlib.sha256(raw).hexdigest())


def bilinear(image,uv):
    h,w=image.shape[:2];p=uv*np.array([w,h])-.5;i=np.floor(p).astype(int);f=p-i
    lo=np.maximum(0,np.minimum(i,[w-1,h-1]));hi=np.maximum(0,np.minimum(i+1,[w-1,h-1]))
    return (image[lo[:,1],lo[:,0]]*(1-f[:,0,None])+image[lo[:,1],hi[:,0]]*f[:,0,None])*(1-f[:,1,None])+(image[hi[:,1],lo[:,0]]*(1-f[:,0,None])+image[hi[:,1],hi[:,0]]*f[:,0,None])*f[:,1,None]


def sample(levels,uv,lod):
    lod=np.maximum(0,np.minimum(lod,len(levels)-1));low=np.floor(lod).astype(int);result=np.zeros((len(uv),4))
    for level in np.unique(low):
        selected=low==level;t=(lod[selected]-level)[:,None]
        result[selected]=bilinear(levels[level],uv[selected])*(1-t)+bilinear(levels[min(level+1,len(levels)-1)],uv[selected])*t
    return result


def main():
    root=ROOT/'artifacts/material-remap/Lift01/far-finite'
    recipe=json.loads((root/'recipe-proposal.json').read_text())['units'][0]
    result=json.loads((root/'report-v2.json').read_text())['units'][0]
    shape=recipe['offlineFiniteTrapezoid'];panels=shape['panels']
    records=[]
    for product in result['channels']:
        channel=next(c for c in recipe['channels'] if c['name']==product['name'])
        source,sh=texture(Path(channel['path']));target,th=texture(root/'candidate-v2/Lift01/Shared'/(product['cid']+'.Texture'))
        assert sh['mips']==10 and sh['width']==sh['height']==512 and th['wrap']==1
        for mip,actual in enumerate(target):
            h,w=actual.shape[:2];yy,xx=np.mgrid[:h,:w];q=np.c_[(xx.ravel()+.5)/w,(yy.ravel()+.5)/h]
            q=np.clip(q*2-.5,0,1)
            half=shape['halfWidthAtV0']+q[:,1]*(shape['halfWidthAtV1']-shape['halfWidthAtV0'])
            points=np.c_[(q[:,0]*2-1)*half,shape['zAtV0']-shape['length']*q[:,1]]
            uv=np.empty_like(points);lod=np.zeros(len(q));covered=np.zeros(len(q),bool)
            for panel in panels:
                vertices=np.array(panel['xz']);bary=(points-vertices[0])@np.linalg.inv(np.column_stack([vertices[1]-vertices[0],vertices[2]-vertices[0]])).T
                pick=(bary[:,0]>=-1e-9)&(bary[:,1]>=-1e-9)&(bary.sum(1)<=1+1e-9)&~covered
                # Original barycentric interpolation is the independent reference.
                source_uv=np.array(next(f for f in json.loads((ROOT/'artifacts/material-remap/Lift01/floor-native-frames.json').read_text())['faces'] if f['lod']==2 and f['face']==panel['face'])['uv'])
                uv[pick]=source_uv[0]+bary[pick,0,None]*(source_uv[1]-source_uv[0])+bary[pick,1,None]*(source_uv[2]-source_uv[0])
                a=np.array(panel['sourceUvAffine'])[:,:2]
                dx=np.c_[2*half[pick],np.zeros(sum(pick))]@a.T*512*2/w
                dy=np.c_[(2*q[pick,0]-1)*(shape['halfWidthAtV1']-shape['halfWidthAtV0']),np.full(sum(pick),-shape['length'])]@a.T*512*2/h
                lod[pick]=np.log2(np.maximum(np.linalg.norm(dx,axis=1),np.linalg.norm(dy,axis=1)))
                covered|=pick
            assert covered.all()
            expected=sample(source,uv,lod)
            if product['name']=='_NormalMap' and not np.array_equal(np.array(recipe['normalTransform']).reshape(3,3),np.eye(3)):
                xy=np.c_[expected[:,0]*expected[:,3],expected[:,1]]*2-1
                n=np.c_[xy,np.sqrt(np.maximum(0,1-(xy*xy).sum(1)))]@np.array(recipe['normalTransform']).reshape(3,3).T
                n/=np.linalg.norm(n,axis=1)[:,None]
                expected=np.c_[n[:,:2]*.5+.5,np.ones((len(n),2))]
            error=abs(expected-actual.reshape(-1,4))
            records.append(dict(channel=product['name'],mip=mip,width=w,height=h,maxLinearPixelError=float(error.max()),meanLinearPixelError=float(error.mean()),sourceLodRange=[float(lod.min()),float(lod.max())]))
    output=dict(candidateKey=result['key'],checks=records,productionQualified=False,
        scope='Native serialized BC7 source mip bytes and persisted candidate decoded; independent CPU original-face barycentrics plus source trilinear sampling. This is not a native renderer or boundary acceptance claim.',
        remaining='Dense filtered finite edges and interior, normal angular error, shader sampling parity, and independent asset loading must still pass.')
    (root/'pixel-comparison-v2.json').write_text(json.dumps(output,indent=2))
    print('Levels',len(records),'max error',max(r['maxLinearPixelError'] for r in records))


if __name__=='__main__':main()
