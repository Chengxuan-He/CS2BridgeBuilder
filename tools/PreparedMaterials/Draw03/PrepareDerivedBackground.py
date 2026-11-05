"""Prepare the user-approved Draw03 background, with shared PBR donor coordinates."""
import json,hashlib
import numpy as np
from CheckLaneAppearance import ROOT,unit,read,D,game
from ComposeRoadPeriods import Writer,linear,encoded

OUT=ROOT/'artifacts/material-remap/Draw03/derived-background'
def weights(old,new):
    lo=np.arange(new)*old/new;hi=np.arange(1,new+1)*old/new
    return np.maximum(0,np.minimum(hi[:,None],np.arange(old)+1)-np.maximum(lo[:,None],np.arange(old)))/(old/new)
def resize(value,w,h):
    return np.einsum('ya,abc,xb->yxc',weights(value.shape[0],h),value,weights(value.shape[1],w),optimize=True)
def normals(p):
    x=p[:,:,0]*p[:,:,3]*2-1;y=p[:,:,1]*2-1
    n=np.stack((x,y,np.sqrt(np.maximum(0,1-x*x-y*y))),2)
    return n/np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-20)
def normal_bytes(n):
    n=n/np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-20)
    return encoded(np.stack((np.ones(n.shape[:2]),(n[:,:,1]+1)/2,(n[:,:,1]+1)/2,(n[:,:,0]+1)/2),2),False)
def main():
    OUT.mkdir(exist_ok=True)
    mapping=ROOT/'artifacts/material-remap/Draw03/background-proposal/source-coordinate-map.npz'
    coords=np.load(mapping);sx=coords['sourceX']-3084
    sy=coords['sourceY']-unit['sourceBounds'][1]+round(unit['finiteUvOffset']*unit['textureSize'][1])
    writer=Writer(game,OUT/'Materials/Draw03');bindings={};reports={};total=0
    for key,cid in unit['bindings'].items():
        if key=='_ControlMask':bindings[key]=cid;continue
        source=read(cid)[0];pixels=source[sy,sx];levels=[pixels];record=[]
        color=key=='_BaseColorMap';normal=key=='_NormalMap'
        value=normals(pixels.astype(float)/255) if normal else linear(pixels,color)
        w,h=pixels.shape[1],pixels.shape[0]
        while w>1 or h>1:
            w,h=max(1,w//2),max(1,h//2)
            value=resize(value,w,h)
            if normal:value=value/np.maximum(np.linalg.norm(value,axis=2,keepdims=True),1e-20)
            levels.append(normal_bytes(value) if normal else encoded(value,color))
        for i,p in enumerate(levels):
            def edge(a,b):return np.abs(a.astype(float)-b.astype(float)).mean(0).tolist()
            record.append(dict(mip=i,width=p.shape[1],height=p.shape[0],sha256=hashlib.sha256(p.tobytes()).hexdigest(),
                uEdgeMeanEncodedBytes=edge(p[:,0],p[:,-1]),vEdgeMeanEncodedBytes=edge(p[0],p[-1])))
        bindings[key],size=writer.texture(levels,color);total+=size;reports[key]=record
    surface,size=writer.surface(bindings,'DerivedRoadBackground');total+=size
    report=dict(surface=surface,bindings=bindings,nativeBytesIncludingSidecars=total,textureSize=[968,128],
        sourceSurface=unit['surface'],sourceCoordinateMapSha256=hashlib.sha256(mapping.read_bytes()).hexdigest(),
        periodMetres=[8,128/D[0]],mips=reports,
        filter='Exact area integration per axis; sRGB RGB decoded to linear; packed normals decoded, averaged and normalized; alpha/mask linear.',
        scope='New user-approved transverse background period. No claim of source V-repeat equivalence or endpoint continuity to wear.',
        pending=['Native readers and uploaded UV/tangent consumer','Complete owner closure','Game lighting and filtered seam visual acceptance'])
    (OUT/'composition.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(surface=surface,bytes=total)))
if __name__=='__main__':main()
