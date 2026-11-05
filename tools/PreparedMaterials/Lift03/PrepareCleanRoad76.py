"""Prepare Lift03's own unpainted road donors and complete native mip chains.
New bounded periods are authored, not inferred original repeats. No owner inputs.
"""
from pathlib import Path
import sys, json, hashlib, struct
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/PreparedMaterials/Draw03'))
import ComposeRoadPeriods as native
native.RECIPE='BridgeBuilder/Lift03/CleanRoad76/1'
OUT=ROOT/'artifacts/material-remap/Lift03/appearance76'
SOURCE=ROOT/'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2/input'

def weights(old,new):
    lo=np.arange(new)*old/new; hi=np.arange(1,new+1)*old/new
    return np.maximum(0,np.minimum(hi[:,None],np.arange(old)+1)-np.maximum(lo[:,None],np.arange(old)))/(old/new)
def resize(a,w,h):
    return np.einsum('ya,abc,xb->yxc',weights(a.shape[0],h),a,weights(a.shape[1],w),optimize=True)
def normal(a):
    p=a.astype(np.float32)/255;x=p[:,:,0]*p[:,:,3]*2-1;y=p[:,:,1]*2-1
    n=np.stack((x,y,np.sqrt(np.maximum(0,1-x*x-y*y))),2)
    return n/np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-20)
def normal_bytes(n):
    n=n/np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-20)
    return native.encoded(np.stack((np.ones(n.shape[:2]),(n[:,:,1]+1)/2,(n[:,:,1]+1)/2,(n[:,:,0]+1)/2),2),False)
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((SOURCE/'manifest.json').read_text())['units'][0]
    sources={};records={}
    for c in manifest['channels']:
        raw=(SOURCE/(c['cid']+'.Texture')).read_bytes(); assert hashlib.sha256(raw).hexdigest()==c['sha256']
        sources[c['name']]=next(native.bc7(raw))[1]
        records[c['name']]={'cid':c['cid'],'sha256':c['sha256']}
    writer=native.Writer(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'),OUT/'Materials/Lift03')
    zero,_=writer.texture([np.zeros((1,1,4),dtype=np.uint8)],False)
    result=[]
    for name,bounds in [('CleanRoadBackground',(2880,40,3008,1032)),('CleanLaneWear',(3104,40,3528,1032))]:
        x0,y0,x1,y1=bounds; bindings={'_ControlMask':zero};report={}
        yy,xx=np.indices((y1-y0,x1-x0));xx+=x0;yy+=y0
        np.savez_compressed(OUT/(name+'-source-map.npz'),sourceX=xx,sourceY=yy)
        base=sources['_BaseColorMap'][yy,xx];assert not np.all(base[:,:,:3]>180,axis=2).any()
        profile=base[:,:,:3].mean((0,2));low,high=np.quantile(profile,[.05,.95]);centers=[]
        if name=='CleanLaneWear':
            for fraction in (.2,.4,.5,.6,.8):
                chosen=np.flatnonzero(profile>=low+(high-low)*fraction)
                centers.append(dict(fraction=fraction,first=int(chosen.min()+x0),last=int(chosen.max()+x0),center=float((chosen.min()+chosen.max()+1)/2+x0)))
        for key,a in sources.items():
            pixels=a[yy,xx];levels=[pixels];color=key=='_BaseColorMap';norm=key=='_NormalMap'
            value=normal(pixels) if norm else native.linear(pixels,color)
            w,h=pixels.shape[1],pixels.shape[0]
            while w>1 or h>1:
                w,h=max(1,w//2),max(1,h//2); value=resize(value,w,h)
                if norm:value=value/np.maximum(np.linalg.norm(value,axis=2,keepdims=True),1e-20)
                levels.append(normal_bytes(value) if norm else native.encoded(value,color))
            bindings[key],size=writer.texture(levels,color)
            Image.fromarray(pixels).save(OUT/(name+key+'.png'))
            report[key]=dict(bytes=size,mips=[dict(width=p.shape[1],height=p.shape[0],sha256=hashlib.sha256(p.tobytes()).hexdigest(),uSeamMean=np.abs(p[:,0].astype(float)-p[:,-1]).mean(0).tolist(),vSeamMean=np.abs(p[0].astype(float)-p[-1]).mean(0).tolist()) for p in levels])
        cid,size=writer.surface(bindings,name,template=1)
        result.append(dict(name=name,surface=cid,bindings=bindings,sourceBounds=bounds,density=[124.,124.359375],physicalPeriod=[(x1-x0)/124.,(y1-y0)/124.359375],visualCenterMeasurements=centers,visualCenterPixel=float(np.median([r['center'] for r in centers])) if centers else None,channels=report))
    report=dict(recipe=native.RECIPE,sourceSurface=manifest['sourceCid'],sources=records,units=result,filter='Exact area integration on periodic unit; linear-light BaseColor; decoded normalized packed normal; linear mask/control.',originalPixelsChanged=False,authoredPeriods=True,pending=['Native reader/upload','Lane mapping and all-LOD production integration','Native game visual filtering/motion','Package and owner size'])
    (OUT/'prepared.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps([dict(name=u['name'],center=u['visualCenterPixel'],period=u['physicalPeriod'],surface=u['surface']) for u in result]))
if __name__=='__main__':main()
