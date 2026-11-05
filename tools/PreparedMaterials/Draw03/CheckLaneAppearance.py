"""Fixed-scale lane mapping and finished-PBR sampling previews (not game renders)."""
from pathlib import Path
import ctypes
import json
import struct
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from ComposeRoadPeriods import Writer
from PrepareRecipes import numbers
from InspectNativeBackground import read as native_background

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw03/lane-placement'
D=numbers(ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03LaneAppearance_cs.bin')['F32']
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
codec=Writer(game,OUT)
unit=json.loads((ROOT/'artifacts/material-remap/Draw03/full-cross-section-v3/composition.json').read_text())['units'][0]

def bands(centers,span,right,uniform=False):
    centers=[c for c in centers if span[0]<=c<=span[1]]
    sign=1 if right else -1
    result=[];cursor=span[0]
    def background(end):
        nonlocal cursor
        if end<=cursor:return
        result.append((cursor,end,1,0,False));cursor=end
    lanes=[]
    for i,center in enumerate(centers):
        positive=center>=0
        lane_sign=1 if positive else -1
        inner=(i==0 or centers[i-1]<0) if positive else (i+1==len(centers) or centers[i+1]>=0)
        outer=i+1==len(centers) if positive else i==0
        reference,lo,hi=D[2],D[3],D[4]
        if not uniform and inner and not outer:reference,lo,hi=D[10],D[4],D[9]
        elif not uniform and outer and not inner:reference,lo,hi=D[11],D[8],D[3]
        a,b=(D[1]-hi)/D[0],(D[1]-lo)/D[0]
        first,last=sorted((lane_sign*(a-reference),lane_sign*(b-reference)))
        low,high=max(span[0],center+first),min(span[1],center+last)
        scale=sign*lane_sign
        lanes.append((low,high,scale,sign*reference-scale*center,True))
    for i,lane in enumerate(lanes):
        low,high,scale,offset,_=lane
        if i>0 and lanes[i-1][1]>low:low=np.clip((centers[i-1]+centers[i])/2,low,lanes[i-1][1])
        if i+1<len(lanes) and lanes[i+1][0]<high:high=np.clip((centers[i]+centers[i+1])/2,lanes[i+1][0],high)
        low=max(low,cursor)
        if high<=low:continue
        background(low);result.append((low,high,scale,offset,True));cursor=high
    background(span[1]);return result

def read(cid):
    raw=(ROOT/'assets/Materials/Draw03/Shared'/f'{cid}.Texture').read_bytes()
    ver,w,h,depth,mips,fmt,dim,filt,wrap,aniso,size=struct.unpack_from('<4H6Bi',raw)
    assert ver==5 and fmt in (4,8)
    length=sum(max(1,w>>m)*max(1,h>>m)*4 for m in range(mips))
    output=ctypes.create_string_buffer(length)
    assert codec.decode(raw[18:],size,output,length,0)==length
    values=[];offset=0
    for mip in range(mips):
        ww,hh=max(1,w>>mip),max(1,h>>mip)
        values.append(np.frombuffer(output.raw,dtype=np.uint8,count=ww*hh*4,offset=offset).reshape(hh,ww,4).copy())
        offset+=ww*hh*4
    return values

def main():
    channels={k:read(v) for k,v in unit['bindings'].items() if k!='_ControlMask'}
    native,_=native_background()
    native_names={'_BaseColorMap':'BaseColor','_NormalMap':'Normal','_MaskMap':'MaskMap'}
    records=[];font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
    native_error=0
    for right in (False,True):
        for b in bands([-7.5,-4.5,-1.5,1.5,4.5,7.5],(-9,9),right):
            lo,hi,scale,offset,iswear=b
            if ((lo+hi)/2>=0)!=right:continue
            assert iswear, 'Native lane spacing must not invent background gaps'
            native_error=max(native_error,abs(lo*scale+offset-lo),abs(hi*scale+offset-hi))
    assert native_error==0
    for centers in ([],[0],[2],[-3,2,7],[-100,-4,0,4,100]):
        for right in (False,True):
            bs=bands(centers,(-9,11),right)
            assert bs[0][0]==-9 and bs[-1][1]==11
            assert all(b[1]>b[0] and (not b[4] or abs(b[2])==1) for b in bs)
    cases=[(str(s),[s*c for c in (-2.5,-1.5,-.5,.5,1.5,2.5)],(-s*3-.4,s*3+.4)) for s in (3,4,6)]
    saved=json.loads((ROOT/'artifacts/material-remap/shared/owner26-layout/layout.json').read_text())
    cases.append(('owner26',saved['CarLaneCenters'],saved['SurfaceSpan']))
    for spacing,centers,span in cases:
        xs=np.linspace(span[1],span[0],1000)
        source=np.zeros(len(xs));wear=np.zeros(len(xs),dtype=bool);rows=[]
        for right in (False,True):
            bs=bands(centers,span,right)
            assert bs[0][0]==span[0] and bs[-1][1]==span[1]
            for j,b in enumerate(bs):
                lo,hi,scale,offset,iswear=b
                if j:assert abs(lo-bs[j-1][1])<1e-6
                if iswear:assert abs(scale)==1
                choose=(xs>=lo)&(xs<=hi)&((xs>=0) if right else (xs<0))
                source[choose]=xs[choose]*scale+offset;wear[choose]=iswear
                if np.any(choose):rows.append(dict(start=lo,end=hi,sourceScale=scale,sourceOffset=offset,wear=iswear,right=right))
        texel=D[1]-abs(source)*D[0]
        assert texel[wear].min()>=D[8]-1e-3 and texel[wear].max()<=D[9]+1e-3
        v=unit['finiteUvOffset']+(texel-unit['sourceBounds'][1])/unit['textureSize'][1]
        images=[];boundaries=[]
        for name,levels in channels.items():
            texture=levels[0]
            ys=np.clip((v*texture.shape[0]-.5).astype(int),0,texture.shape[0]-1)
            pixels=texture[ys[:,None],np.arange(968)[None,:]]
            background=native[native_names[name]][0]
            bx=np.floor((xs*D[12]%1)*background.shape[1]).astype(int)
            bz=np.floor(((np.arange(968)+.5)/968*8*D[12]%1)*background.shape[0]).astype(int)
            pixels[~wear]=background[bz[None,:],bx[~wear,None]]
            image=Image.fromarray(pixels).convert('RGB')
            canvas=Image.new('RGB',(968,1040),'#14191f');canvas.paste(image,(0,40))
            draw=ImageDraw.Draw(canvas);draw.text((12,10),f'{spacing} m lane spacing | {name} | fixed physical wear',font=font,fill='white')
            canvas.save(OUT/f'lane-{spacing}m-{name[1:]}.png');images.append(canvas)
            errors=[]
            for mip,t in enumerate(levels):
                differences=[]
                for side in (False,True):
                    bs=bands(centers,span,side)
                    for left,right in zip(bs,bs[1:]):
                        x=left[1]
                        if (x>=0)!=side:continue
                        vv=[]
                        for b in (left,right):
                            if not b[4]:
                                bg=native[native_names[name]][min(mip,len(native[native_names[name]])-1)]
                                xx=int((x*D[12]%1)*bg.shape[1])
                                zz=np.floor((((np.arange(t.shape[1])+.5)/t.shape[1]*8*D[12])%1)*bg.shape[0]).astype(int)
                                vv.append(bg[zz,xx].astype(float)/255);continue
                            sx=b[2]*x+b[3]
                            sv=D[1]-abs(sx)*D[0]
                            uv=unit['finiteUvOffset']+(sv-unit['sourceBounds'][1])/unit['textureSize'][1]
                            yy=uv*t.shape[0]-.5;y0=int(np.floor(yy));f=yy-y0
                            aa=t[np.clip(y0,0,t.shape[0]-1)].astype(float)/255
                            bb=t[np.clip(y0+1,0,t.shape[0]-1)].astype(float)/255
                            vv.append(aa*(1-f)+bb*f)
                        differences.append(float(np.max(abs(vv[0]-vv[1]))))
                errors.append(dict(mip=mip,maxBoundaryChannelDelta=max(differences,default=0)))
            boundaries.append(dict(channel=name,levels=errors))
        records.append(dict(spacing=spacing,span=span,centers=centers,bands=rows,
            sourceTexelsPerMetre=D[0],longitudinalPeriodMetres=8,boundaryDiagnostics=boundaries))
    (OUT/'candidate-mapping.json').write_text(json.dumps(dict(
        scope='Finished full-PBR payload sampling with candidate piecewise projection; seam deltas are diagnostics, not accepted tolerances. No game render or cold-load claim.',
        nativeThreeMetreSourcePositionErrorMetres=native_error,
        edgeCases='No lanes, one-sided single lane, centerline lane, asymmetric centers, and out-of-span centers retain full coverage and unit-magnitude wear gradients.',
        surface=unit['surface'],background='Original native asphalt worldspace PBR, XZ 0.4 cycles/metre. Raw normal-map deltas are packed channel differences, not a tangent-frame angular comparison.',records=records),indent=2)+'\n')
    print('PASS fixed wear gradients / coverage / native finite bounds; boundary diagnostics retained')

if __name__=='__main__':main()
