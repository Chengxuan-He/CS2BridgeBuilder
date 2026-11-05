"""Review-only source-pixel background quilting; NOT an approved native V repeat."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from CheckLaneAppearance import ROOT,D,unit,read,bands
from InspectNativeBackground import read as read_native

OUT=ROOT/'artifacts/material-remap/Draw03/background-proposal'

def cut(cost):
    h,w=cost.shape;best=None
    for start in range(h):
        state=np.full(h,np.inf);state[start]=cost[start,0];parents=np.zeros((h,w),int)
        for x in range(1,w):
            prev=np.stack((np.r_[np.inf,state[:-1]],state,np.r_[state[1:],np.inf]))
            choice=prev.argmin(0);parents[:,x]=np.arange(h)+choice-1
            state=prev[choice,np.arange(h)]+cost[:,x]
        if best is None or state[start]<best[0]:
            path=np.zeros(w,int);path[-1]=start
            for x in range(w-1,0,-1):path[x-1]=parents[path[x],x]
            best=(state[start],path)
    return best[1]

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    channels={k:read(v)[0] for k,v in unit['bindings'].items() if k!='_ControlMask'}
    native,_=read_native();native_names={'_BaseColorMap':'BaseColor','_NormalMap':'Normal','_MaskMap':'MaskMap'}
    width,height=968,128
    y,x=np.indices((height,width));offset=int(round(unit['finiteUvOffset']*unit['textureSize'][1]))
    sy=offset+802-unit['sourceBounds'][1]+y%32
    phases=np.array([0,241,483,725]);sx=(x+phases[y//32])%width
    # Join different phase patches by a minimum-error cut across all PBR channels.
    # Every output pixel still selects one original source coordinate, never a tint.
    for boundary in (0,32,64,96):
        rows=(np.arange(boundary-4,boundary+4)%height)
        old_phase=phases[((boundary-1)%height)//32];new_phase=phases[boundary//32]
        oldx=np.broadcast_to((np.arange(width)+old_phase)%width,(8,width))
        newx=np.broadcast_to((np.arange(width)+new_phase)%width,(8,width))
        yy=sy[rows]
        cost=sum(((a[yy,oldx].astype(float)-a[yy,newx].astype(float))/255)**2 for a in channels.values()).sum(2)
        path=cut(cost)
        sx[rows]=np.where(np.arange(8)[:,None]<=path[None,:],oldx,newx)
    quilt={k:a[sy,sx] for k,a in channels.items()}
    for k,a in quilt.items():Image.fromarray(a).save(OUT/(k[1:]+'-unit.png'))
    np.savez_compressed(OUT/'source-coordinate-map.npz',sourceX=sx+3084,sourceY=sy-offset+unit['sourceBounds'][1])
    layout=json.loads((ROOT/'artifacts/material-remap/shared/owner28-height/layout-and-heights.json').read_text())
    # The saved eight-lane graph has explicit 4 m lanes and a 2.5 m median.
    centers=[-15.25,-11.25,-7.25,-3.25,3.25,7.25,11.25,15.25];span=(-19.25,19.25)
    xs=np.linspace(span[1],span[0],1400);zs=(np.arange(968)+.5)/968*8
    source=np.zeros(len(xs));wear=np.zeros(len(xs),bool)
    for right in (False,True):
        for lo,hi,scale,shift,iswear in bands(centers,span,right):
            choose=(xs>=lo)&(xs<=hi)&((xs>=0) if right else (xs<0))
            source[choose]=xs[choose]*scale+shift;wear[choose]=iswear
    sv=D[1]-abs(source)*D[0];vv=unit['finiteUvOffset']+(sv-unit['sourceBounds'][1])/unit['textureSize'][1]
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16);images=[]
    diagnostics={}
    def difference(a,b):
        delta=np.abs(a.astype(float)-b.astype(float))
        return dict(meanPerChannel=delta.mean(0).tolist(),maxPerChannel=delta.max(0).tolist(),units='encoded byte absolute difference; not perceptual or normal-angle error')
    for name,a in channels.items():
        rows=np.clip((vv*a.shape[0]-.5).astype(int),0,a.shape[0]-1)
        rendered=a[rows[:,None],np.arange(width)[None,:]]
        original=rendered.copy();background=native[native_names[name]][0]
        bx=np.floor(xs*.4*1024).astype(int)%1024;bz=np.floor(zs*.4*1024).astype(int)%1024
        original[~wear]=background[bz[None,:],bx[~wear,None]]
        # New authored transverse period, at the original source pixel density.
        by=np.floor(xs*D[0]).astype(int)%height
        rendered[~wear]=quilt[name][by[~wear,None],np.arange(width)[None,:]]
        edges=np.flatnonzero(wear[1:]!=wear[:-1])
        diagnostics[name]=dict(unitU=difference(quilt[name][:,0],quilt[name][:,-1]),
            unitV=difference(quilt[name][0],quilt[name][-1]),
            interiorU=difference(quilt[name][:,1:].reshape(-1,4),quilt[name][:,:-1].reshape(-1,4)),
            interiorV=difference(quilt[name][1:].reshape(-1,4),quilt[name][:-1].reshape(-1,4)),
            interfaces=[dict(x=float((xs[e]+xs[e+1])/2),before=difference(original[e],original[e+1]),
                proposal=difference(rendered[e],rendered[e+1])) for e in edges],
            unchangedWearPixels=bool(np.array_equal(original[wear],rendered[wear])))
        canvas=Image.new('RGB',(width*2,1440),'#14191f');draw=ImageDraw.Draw(canvas)
        draw.text((12,10),'Outer-edge fix: current native asphalt background',font=font,fill='white')
        draw.text((width+12,10),'PROPOSAL ONLY: atlas-source PBR grain / fixed wear',font=font,fill='white')
        canvas.paste(Image.fromarray(original).convert('RGB'),(0,40));canvas.paste(Image.fromarray(rendered).convert('RGB'),(width,40))
        canvas.save(OUT/(name[1:]+'-owner28-before-after.png'))
    report=dict(status='REVIEW_ONLY_NOT_AUTHORIZED_FOR_PRODUCTION',sourceSurface=unit['surface'],
        sourceBounds=[3084,802,4052,834],outputSize=[width,height],
        longitudinalPeriodMetres=8,proposedTransversePeriodMetres=height/D[0],
        sourcePixelsPerMetre=D[0],sameCoordinatesForAllPbr=True,pixelColorEdits=0,pixelRotationOrScaling=False,
        phaseOffsetsPixels=phases.tolist(),sourceMap='source-coordinate-map.npz',
        scope='One mip-zero proposal, original source PBR pixels selected at a shared coordinate. No native asset, mip chain, or source V-repeat equivalence claimed.',
        owner=layout.get('owner'),centers=centers,span=span,
        uncompressedRgbaThreeChannelFullMipUpperBound=int(width*height*4*3*4/3)+128,
        currentOwnerMaterialBytes=6585809,remainingOwnerBudgetBytes=3414191,
        conflict='Material contract section 3 preserves source finite/repeat mode and rejects arbitrary crop-as-repeat. This proposal intentionally authors a new finite-background V period; explicit narrow authorization is needed.',
        remaining='Native full-mip preparation, U/V seam and PBR-frame validation, surface-edge transition choice, exact compressed graph measurement, and game visual review.')
    report['sourceMapSha256']=hashlib.sha256((OUT/'source-coordinate-map.npz').read_bytes()).hexdigest()
    (OUT/'boundary-diagnostics.json').write_text(json.dumps(diagnostics,indent=2)+'\n')
    (OUT/'proposal.json').write_text(json.dumps(report,indent=2)+'\n')
    print(OUT/'BaseColorMap-owner28-before-after.png')

if __name__=='__main__':main()
