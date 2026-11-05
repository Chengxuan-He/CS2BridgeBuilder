"""Stage source-density Draw01 physical-part repeats; never change source assets."""
import json
from pathlib import Path
import numpy as np
from PIL import Image
from StageCleanCap import ROOT, decode, sha

OUT=ROOT/'artifacts/material-remap/Draw01/crossframe47/repeat'

def main():
    manifest=json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/manifest.json').read_text())
    channels=manifest['units'][0]['channels']; sources=[]
    for c in channels:
        path=Path(c['path'].replace('C:\\Users\\admin\\Documents\\Codex\\CS2BridgeBuilder',str(ROOT)))
        raw=path.read_bytes(); assert sha(raw)==c['sha256']
        sources.append(np.asarray(Image.frombytes('RGBA',(4096,4096),decode(raw)[:4096*4096],'bcn',(7,'BC7'))).copy())
    # Keep the entire native four-metre V panel including its opaque transverse frame. Its diamond lattice has
    # measured wire spacing and near-recurrence at 123 U pixels; four
    # U cells retain weathering variation while excluding the separate longitudinal frame.
    recipes=[('grating',1042,2708,492,914,692/3.0269775390625,914/4)]
    records=[]
    for name,x,y,w,h,dx,dy in recipes:
        folder=OUT/name; folder.mkdir(parents=True,exist_ok=True)
        report=[]
        for index,(source,c) in enumerate(zip(sources,channels)):
            pixels=np.tile(source[y:y+h,x:x+w],(2,1,1)).copy()
            Image.fromarray(pixels).save(folder/f'channel-{index}.png')
            if index==0:
                Image.fromarray(pixels[:,:,3]).save(folder/'alpha.png')
                yellow=(pixels[:,:,0]>100)&(pixels[:,:,1]>70)&(pixels[:,:,2]<pixels[:,:,1]*.65)
                if name!='grating':assert not yellow.any(),'Paint entered pure donor'
            work=pixels.astype(np.float32)/255
            if index==0:work[:,:,:3]=np.where(work[:,:,:3]<=.04045,work[:,:,:3]/12.92,((work[:,:,:3]+.055)/1.055)**2.4)
            elif index==1:
                xy=np.stack((work[:,:,0]*work[:,:,3],work[:,:,1]),axis=2)*2-1
                work[:,:,:3]=np.concatenate((xy,np.sqrt(np.maximum(0,1-(xy*xy).sum(axis=2)))[:,:,None]),axis=2)
            levels=[]; coverage=float((pixels[:,:,3]>=128).mean())
            with (folder/f'channel-{index}.rgba').open('wb') as stream:
                while True:
                    result=work.copy()
                    if index==0:result[:,:,:3]=np.where(result[:,:,:3]<=.0031308,result[:,:,:3]*12.92,1.055*np.maximum(result[:,:,:3],0)**(1/2.4)-.055)
                    elif index==1:
                        n=result[:,:,:3]; n/=np.maximum(np.linalg.norm(n,axis=2)[:,:,None],1e-12)
                        nx=(n[:,:,0]*.5+.5).copy();ny=(n[:,:,1]*.5+.5).copy()
                        result[:,:,0]=1;result[:,:,1]=ny;result[:,:,2]=ny;result[:,:,3]=nx
                    # Native cutout mip preservation: scale alpha only, retain the
                    # native 0.5 threshold and relative source alpha. At 1 texel,
                    # average opacity is retained; exact fractional coverage is
                    # unrepresentable by binary cutout and is reported separately.
                    if name=='grating' and index==0 and levels and result.shape[0]*result.shape[1]>1:
                        alpha=result[:,:,3]; target=int(round(coverage*alpha.size))
                        if 0<target<alpha.size:
                            sorted_alpha=np.sort(alpha,axis=None)
                            boundary=(sorted_alpha[-target]+sorted_alpha[-target-1])*.5
                            if boundary>0:result[:,:,3]=np.clip(alpha*((127.5/255)/boundary),0,1)
                    encoded=np.clip(np.rint(result*255),0,255).astype('uint8');stream.write(encoded.tobytes())
                    hh,ww=work.shape[:2];levels.append([ww,hh])
                    if ww==hh==1:break
                    size=(max(1,ww//2),max(1,hh//2))
                    work=np.stack([np.asarray(Image.fromarray(work[:,:,k],mode='F').resize(size,Image.Resampling.BOX)) for k in range(4)],axis=2)
            report.append(dict(channel=c['name'],sourceSha256=c['sha256'],mips=levels))
        records.append(dict(part=name,width=w,height=h*2,sourceRectangle=[x,y,w,h],sourceCopiesV=2,originalPeriodV=4,pixelsPerMetre=[dx,dy],physicalSize=[w/dx,h*2/dy],channels=report))
    (OUT/'authoring.json').write_text(json.dumps(dict(status='STAGED_NOT_ACCEPTED',sourceCoordinatesShared=True,originalTexelDensity=True,authoredPeriods=True,parts=records),indent=2)+'\n')

if __name__=='__main__':main()
