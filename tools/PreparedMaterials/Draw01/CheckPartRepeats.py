"""Measure every staged native mip against its authored inputs, including alpha."""
import json
import numpy as np
from PIL import Image
from StageCleanCap import ROOT,decode

def main():
    p=ROOT/'artifacts/material-remap/Draw01/head42/part-repeats';rows=[]
    for part in json.loads((p/'authoring.json').read_text())['parts']:
        for c in range(4):
            raw=(p/part['part']/f'channel-{c}.Texture').read_bytes();block=decode(raw)
            ref=(p/part['part']/f'channel-{c}.rgba').read_bytes();off=ro=0
            for mip,(w,h) in enumerate(part['channels'][c]['mips']):
                n=((w+3)//4)*((h+3)//4)*16
                a=np.array(Image.frombytes('RGBA',(w,h),block[off:off+n],'bcn',(7,'BC7'))).astype(float)
                b=np.frombuffer(ref[ro:ro+w*h*4],dtype=np.uint8).reshape(h,w,4).astype(float)
                record=dict(part=part['part'],channel=c,mip=mip,size=[w,h],max=float(abs(a-b).max()),rms=float(np.sqrt(((a-b)**2).mean())))
                if c==0:
                    record.update(alphaCrossings=int(((a[:,:,3]>=128)!=(b[:,:,3]>=128)).sum()),coverage=float((a[:,:,3]>=128).mean()),authoredCoverage=float((b[:,:,3]>=128).mean()))
                    if part['part']!='grating':
                        yellow=(a[:,:,0]>100)&(a[:,:,1]>70)&(a[:,:,2]<a[:,:,1]*.65)
                        assert not yellow.any(),'Embedded marking colour in concrete/platform mip'
                if c==1:
                    def normal(v):
                        v=v/255;xy=np.stack((v[:,:,0]*v[:,:,3],v[:,:,1]),axis=2)*2-1
                        nn=np.concatenate((xy,np.sqrt(np.maximum(0,1-(xy*xy).sum(axis=2)))[:,:,None]),axis=2)
                        return nn/np.maximum(np.linalg.norm(nn,axis=2)[:,:,None],1e-12)
                    record['normalMaxDegrees']=float(np.degrees(np.arccos(np.clip((normal(a)*normal(b)).sum(axis=2),-1,1))).max())
                rows.append(record);off+=n;ro+=w*h*4
            assert off==len(block) and ro==len(ref)
    (p/'native-mip-check.json').write_text(json.dumps(dict(status='MEASURED_NATIVE_LOSSY',levels=rows,limitations=['BC7 alpha threshold crossings are measured, not claimed zero.','One texel cannot represent fractional cutout coverage; average alpha is retained.','Native game visual/temporal acceptance remains required.']),indent=2)+'\n')
    print('PASS complete native decoding and clean cap/platform colour checks:',len(rows),'channel mip levels')
    print('grating coverage:',[(r['mip'],round(r['coverage'],4),r['alphaCrossings']) for r in rows if r['part']=='grating' and r['channel']==0])

if __name__=='__main__':main()
