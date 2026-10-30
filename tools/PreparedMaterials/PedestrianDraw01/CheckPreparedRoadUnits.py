"""Compare decoded prepared pixels against original per-face unit sampling."""
import argparse
import ctypes as ct
import hashlib
import json
import struct
from pathlib import Path
import numpy as np
from PIL import Image


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('stage',type=Path);p.add_argument('plugin',type=Path)
    a=p.parse_args();library=ct.CDLL(str(a.plugin))
    calc=library.decompress_calc_bound;calc.argtypes=[ct.c_void_p,ct.c_int64,ct.c_int];calc.restype=ct.c_int64
    decode=library.decompress_data;decode.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p,ct.c_int64,ct.c_int];decode.restype=ct.c_int64
    def load(path):
        raw=path.read_bytes();version,w,h,depth,mips,fmt,dim,filt,wrap,aniso=struct.unpack_from('<4H6B',raw)
        assert version==5 and depth==1 and dim==2
        length=struct.unpack_from('<i',raw,14)[0];data=raw[18:]
        if length>=0:
            assert len(data)==length
            capacity=calc(data,len(data),0);buffer=ct.create_string_buffer(capacity)
            assert decode(data,len(data),buffer,capacity,0)==capacity
            data=buffer.raw
        assert fmt in (108,109)
        levels=[];at=0
        for i in range(mips):
            x,y=max(1,w>>i),max(1,h>>i);size=((x+3)//4)*((y+3)//4)*16
            image=np.array(Image.frombytes('RGBA',(x,y),data[at:at+size],'bcn',7)).astype(float)/255;at+=size
            if fmt==108:image[:,:,:3]=np.where(image[:,:,:3]<=.04045,image[:,:,:3]/12.92,((image[:,:,:3]+.055)/1.055)**2.4)
            levels.append(image)
        assert at==len(data)
        return levels
    def sample(image,uv,repeat):
        h,w=image.shape[:2];xy=uv*np.array([w,h])-.5;i=np.floor(xy).astype(int);f=xy-i
        def at(dx,dy):
            x,y=i[:,0]+dx,i[:,1]+dy
            if repeat:x,y=x%w,y%h
            else:x,y=np.clip(x,0,w-1),np.clip(y,0,h-1)
            return image[y,x]
        return (at(0,0)*(1-f[:,0,None])+at(1,0)*f[:,0,None])*(1-f[:,1,None])+(at(0,1)*(1-f[:,0,None])+at(1,1)*f[:,0,None])*f[:,1,None]
    def normal(c):
        xy=np.column_stack([c[:,0]*c[:,3],c[:,1]])*2-1
        return np.column_stack([xy,np.sqrt(np.maximum(0,1-(xy*xy).sum(1)))])
    units=json.loads((a.stage/'near-road-units.json').read_text())['units']
    products=json.loads((a.stage/'report.json').read_text())['units']
    output=a.stage/'output/PedestrianDraw01';rows=[];cache={}
    for unit,product in zip(units,products):
        for channel in product['channels']:
            source=next(c for c in unit['channels'] if c['name']==channel['name'])
            if source['path'] not in cache:cache[source['path']]=load(Path(source['path']))
            original=cache[source['path']];prepared=load(output/'Shared'/(channel['cid']+'.Texture'))
            for mip,target in enumerate(prepared):
                # Interior, original center reset, period seam and guarded finite edges.
                q=np.array([(x,y) for x in [0,.00001,.01,.2,.49999,.5,.50001,.7,.99,.99999,1]
                            for y in [0,.00001,.001,.03,.25,.5,.75,.97,.999,.99999]])
                coords=np.zeros_like(q)
                for band in unit['bands']:
                    select=(q[:,0]>=band['start'])&(q[:,0]<band['end'] if band['end']<1 else q[:,0]<=1)
                    t=np.column_stack([(q[select,0]-band['start'])/(band['end']-band['start']),q[select,1]])
                    coords[select]=np.array(band['min'])+t*(np.array(band['max'])-band['min'])
                expected=sample(original[min(mip,len(original)-1)],coords,False)
                actual=sample(target,np.column_stack([q[:,0]*.5+.25,q[:,1]]),True)
                error=abs(actual-expected)
                row=dict(unit=unit['id'],channel=channel['name'],mip=mip,samples=len(q),maximumLinearChannelError=error.max(0).tolist(),rms=float(np.sqrt((error*error).mean())))
                worst=int(np.argmax(error.max(1)))
                row.update(worstUnitCoordinate=q[worst].tolist(),expected=expected[worst].tolist(),actual=actual[worst].tolist())
                if channel['name']=='_NormalMap':
                    na,nb=normal(actual),normal(expected)
                    cos=(na*nb).sum(1)/(np.linalg.norm(na,axis=1)*np.linalg.norm(nb,axis=1))
                    row['maximumNormalAngleDegrees']=float(np.degrees(np.arccos(np.clip(cos,-1,1))).max())
                rows.append(row)
    result=dict(scope='Source mip bilinear sampling versus decoded native prepared BC7, including repeat seam, internal source-UV reset and finite guards.',
        samples=sum(r['samples'] for r in rows),results=rows,
        acceptance='Measured differences only; no arbitrary tolerance or native rendering equivalence is asserted.')
    (a.stage/'filter-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(samples=result['samples'],maximumError=max(max(r['maximumLinearChannelError']) for r in rows),maximumNormalAngle=max(r.get('maximumNormalAngleDegrees',0) for r in rows))))


if __name__=='__main__':main()
