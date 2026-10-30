"""Audit source-local mixed profile mappings, native channels and uncovered charts."""
import collections
import gzip
import hashlib
import io
import json
from pathlib import Path
import struct
import sys
import numpy as np
from PIL import Image
from Lift04UvChartBindings import read_domains
from MovableUvDomainFormat import read_bbn

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'artifacts/material-resume/Lift04/outputs'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def levels(raw):
    version,w,h,depth,mips,fmt,dimension,filtering,wrap,aniso,compression=struct.unpack_from('<4H6Bi',raw)
    assert version==5 and fmt in (108,109) and compression==-1 and depth==1
    at=18
    for mip in range(mips):
        size=((w+3)//4)*((h+3)//4)*16; header=[0]*31
        header[:7]=[124,0x81007,h,w,size,0,1];header[18:21]=[32,4,int.from_bytes(b'DX10','little')];header[26]=0x1000
        dds=b'DDS '+struct.pack('<31I',*header)+struct.pack('<5I',98,3,0,1,0)+raw[at:at+size]
        yield np.asarray(Image.open(io.BytesIO(dds)).convert('RGBA')).astype(float)
        at+=size;w=max(1,w//2);h=max(1,h//2)
    assert at==len(raw)

def sample(a,u,v):
    x=u*a.shape[1]-.5;y=v*a.shape[0]-.5
    ix=np.floor(x).astype(int);iy=np.floor(y).astype(int);fx=x-ix;fy=y-iy
    return sum(a[np.clip(iy+dy,0,a.shape[0]-1),np.clip(ix+dx,0,a.shape[1]-1)]*((fx if dx else 1-fx)*(fy if dy else 1-fy))[:,None] for dx in (0,1) for dy in (0,1))

def main():
    BASE.mkdir(parents=True,exist_ok=True)
    classification=json.loads((ROOT/'artifacts/prepared-materials/Lift04/non-tiling/classification.json').read_text())
    capture=ROOT/'artifacts/prepared-materials/shared-nontiling/actual-final2/styles/Lift04.regions.json.classifications.ndjson.gz'
    remaining=[r for r in map(json.loads,gzip.open(capture,'rt')) if r['status']=='remaining']
    details=[];uncovered=[];replay=collections.Counter();negative=collections.Counter()
    raw=(ROOT/'assets/HardcodedNumbers/Lift04/Lift04UvSampling.bin').read_bytes();ints,_=read_bbn(raw);at=2
    prior,_=read_bbn((ROOT/'artifacts/material-resume/Lift04/env/original-files/Lift04UvSampling.bin').read_bytes());oldat=2
    for mesh in classification['meshes']:
        name,mi=mesh['render'],mesh['mesh'];count=int(ints[at]);flags=ints[at+1:at+1+count];at+=count+1
        oldcount=int(prior[oldat]);oldflags=prior[oldat+1:oldat+1+oldcount];oldat+=oldcount+1
        assert count==oldcount and np.all(flags[oldflags==1]==3)
        source=ROOT/'artifacts/uv-density/native'/(name+'.npz');native=np.load(source)
        xyz=native[f'mesh{mi}_positions'].astype(float);uv=native[f'mesh{mi}_uv0'][:,:2].astype(float);faces=native[f'mesh{mi}_faces']
        domains=read_domains(ROOT/'assets/HardcodedNumbers/Lift04'/('UvDomain_'+name.replace(' ','_')+'_'+str(mi)+'.bin'))
        for index,fs in enumerate(domains):
            points=uv[faces[list(fs)]];lo=points.min((0,1));hi=points.max((0,1))
            if flags[index]==1:
                assert np.all(points>=0) and np.all(points<=1)
                metrics=[]
                for f in fs:
                    p=xyz[faces[f]];q=uv[faces[f]];dq=q[1:]-q[0]
                    assert np.linalg.det(dq)!=0, (name,mi,index,f,'rank-deficient native UV')
                    derivative=np.linalg.solve(dq,p[1:]-p[0])
                    metrics.append(float(np.linalg.norm(derivative[1])*(404/4096)))
                details.append(dict(render=name,mesh=mi,domain=index,faces=list(fs),u=[float(lo[0]),float(hi[0])],v=[float(lo[1]),float(hi[1])],vPeriodMetres=[min(metrics),max(metrics)] if metrics else None))
                assert np.all(hi>lo), (name,mi,index,'zero-span native UV bounds')
            for unit in classification['repeatRegions']:
                if unit['id'] in ('beam','Lift04RoadUnit') and mesh['sourceDigest']==unit['sourceDigest'] and np.all(points>=unit['min']) and np.all(points<=unit['max']):
                    assert flags[index]==0;negative[unit['id']]+=1
            rr=[r for r in remaining if r['render']==name and r['mesh']==mi and r['originalDomain']==index]
            if rr:
                for r in rr:replay[r['reason']+'/mask'+str(flags[index])]+=1
                if rr[0]['reason']=='outside-recorded-coverage':
                    vertices=sorted(set(int(v) for v in faces[list(fs)].flatten()))
                    uncovered.append(dict(render=name,mesh=mi,domain=index,faces=list(fs),nativeSha256=sha(source),uvPixels=(np.concatenate((lo,hi))*4096).tolist(),positions=xyz[vertices].tolist(),uv=uv[vertices].tolist()))
    assert replay['contained-but-mapping-rejected/mask1']==153
    assert replay['outside-recorded-coverage/mask1']==84
    assert replay['outside-recorded-coverage/mask3']==12
    assert replay['outside-recorded-coverage/mask0']==0
    assert len(details)==4618 and all(r['vPeriodMetres'] is not None for r in details)
    patterns=sorted(set(tuple(r['uvPixels']) for r in uncovered))
    manifest=json.loads((ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/extracted-source/manifest.json').read_text())['units'][0]
    comparisons=[]
    for channel in manifest['channels']:
        path=Path(channel['path']);assert sha(path)==channel['sha256']
        for mip,a in enumerate(levels(path.read_bytes())):
            for bounds in patterns:
                x0,y0,x1,y1=np.asarray(bounds)/4096
                # Include native rectangle boundaries, near-edge points and
                # deterministic interior points. This records errors, no invented
                # tolerance turns approximate content into a claimed exact pass.
                u,v=np.meshgrid(np.linspace(x0,x1,67),np.linspace(y0,y1,257));u=u.ravel();v=v.ravel()
                repeated=y0+np.mod(v-y0,404/4096)
                diff=np.abs(sample(a,u,v)-sample(a,u,repeated))
                edge=(u==x0)|(u==x1)|(v==y0)|(v==y1)
                comparisons.append(dict(channel=channel['name'],mip=mip,boundsPixels=bounds,maxEncodedDifference=float(diff.max()),meanEncodedDifference=float(diff.mean()),edgeMax=float(diff[edge].max()),sourceMipSize=list(a.shape[:2]),samples=len(u)))
    report=dict(schema=2,style='Lift04',mixedDomains=len(details),mixed=details,negativeControls=dict(negative),replayedRemaining=dict(replay),uncoveredDomains=uncovered,exactUncoveredRectanglesPixels=patterns,originalChannelMipComparisons=comparisons,sourceUnit=manifest['id'],sourceDigest=manifest['sourceDigest'],notes='Mixed U is retained from original native UV interpolation; V keeps its measured source gradient and 404-texel physical period. Channel comparisons document the historical uncovered native continuation against the established profile phase; they do not claim exact seam/filter equality. Two exact native beam rectangles extend coverage; four finite end fascias retain both axes. No source pixels changed.',resourceSha256=sha(ROOT/'assets/HardcodedNumbers/Lift04/Lift04UvSampling.bin'))
    (BASE/'mixed-axis-source-evidence.json').write_text(json.dumps(report,separators=(',',':'))+'\n')
    print(json.dumps(dict(mixedDomains=len(details),negativeControls=dict(negative),replayedRemaining=dict(replay),uncoveredDomains=len(uncovered),channelMipRegionComparisons=len(comparisons))))

if __name__=='__main__':main()
