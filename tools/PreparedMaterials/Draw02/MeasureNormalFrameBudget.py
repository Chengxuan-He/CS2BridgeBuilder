"""Quantify native AG-normal sampling with original versus projected frames.

This does not invent a texture quantization tolerance. Every budget comes from
the actual native packed frame set in that domain. The conservative bound is
valid for every normalized normal, including filtered AG samples near Z=0.
"""
import ctypes as c,json,math
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Draw02/near-cells'
domains=json.loads((OUT/'appearance-domain-measurements.json').read_text())
frames=json.loads((OUT/'native-atlas-frame-audit.json').read_text())
dll=c.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
fn=dll.math_array_oct15_to_fp32tangents;fn.argtypes=[c.c_void_p,c.c_int64,c.c_void_p];fn.restype=None
def tangent(bits):
    encoded=np.array([int(bits,16)],np.uint32);decoded=np.zeros((1,4),np.float32)
    fn(encoded.ctypes.data,1,decoded.ctypes.data)
    return decoded[0].astype(float)
def unit(v):return v/np.linalg.norm(v,axis=-1,keepdims=True)
def unpack(sample):
    # Native RG-or-AG convention: multiply R*A before signed XY and Z rebuild.
    xy=np.column_stack((sample[:,0]*sample[:,3],sample[:,1]))*2-1
    z=np.sqrt(np.maximum(0,1-np.minimum(1,(xy*xy).sum(1))))
    return unit(np.column_stack((xy,z)))
mips=[np.fromfile(OUT/'normal-source-mips'/f'{i}.rgba',np.uint8).reshape(max(1,4096>>i),max(1,4096>>i),4).astype(float)/255 for i in range(13)]
normal=np.array([0.,1.,0.]);nominal=np.array([-1.,0.,0.]);results=[]
for level in domains:
    for index,domain in enumerate(level['domains']):
        evidence=next(r for r in frames if r['kind']=='moving-car'and r['lod']==level['lod']and r['domain']==index)
        assert evidence['originalPackedNormals']==['7fff0000']
        originals=[tangent(bits)for bits in evidence['originalPackedTangents']]
        hand=originals[0][3];assert all(v[3]==hand for v in originals)
        reference=np.column_stack((nominal,np.cross(normal,nominal)*hand,normal))
        matrices=[np.column_stack((t[:3],np.cross(normal,t[:3])*hand,normal))for t in originals]
        perturbation=max(float(np.linalg.norm(matrix-reference,ord=2))for matrix in matrices)
        bound=2*perturbation/(1-perturbation)
        worst=0.;worstAngle=0.;samples=0;minimumZ=1.;maximumAGProduct=0.
        lo=np.array(domain['uvMinimum']);hi=np.array(domain['uvMaximum'])
        for mip,image in enumerate(mips):
            h,w=image.shape[:2];x0=max(0,int(math.floor(lo[0]*w-.5))-1);x1=min(w,int(math.ceil(hi[0]*w-.5))+2)
            y0=max(0,int(math.floor(lo[1]*h-.5))-1);y1=min(h,int(math.ceil(hi[1]*h-.5))+2)
            yy,xx=np.meshgrid(np.arange(y0,y1),np.arange(x0,x1),indexing='ij')
            a=image[yy,xx];b=image[yy,np.minimum(xx+1,w-1)];d=image[np.minimum(yy+1,h-1),xx];e=image[np.minimum(yy+1,h-1),np.minimum(xx+1,w-1)]
            filtered=[a,(a+b)*.5,(a+d)*.5,(a+b+d+e)*.25]
            if mip<12:
                coarse=mips[mip+1];ch,cw=coarse.shape[:2]
                cx=(xx+.5)*cw/w-.5;cy=(yy+.5)*ch/h-.5;ix=np.floor(cx).astype(int);iy=np.floor(cy).astype(int);fx=(cx-ix)[...,None];fy=(cy-iy)[...,None]
                c00=coarse[np.clip(iy,0,ch-1),np.clip(ix,0,cw-1)];c10=coarse[np.clip(iy,0,ch-1),np.clip(ix+1,0,cw-1)]
                c01=coarse[np.clip(iy+1,0,ch-1),np.clip(ix,0,cw-1)];c11=coarse[np.clip(iy+1,0,ch-1),np.clip(ix+1,0,cw-1)]
                filtered.append((a+(c00*(1-fx)+c10*fx)*(1-fy)+(c01*(1-fx)+c11*fx)*fy)*.5)
            for values in filtered:
                rgba=values.reshape(-1,4);n=unpack(rgba);minimumZ=min(minimumZ,float(n[:,2].min()));maximumAGProduct=max(maximumAGProduct,float((rgba[:,0]*rgba[:,3]).max()))
                baseline=n@reference.T
                for matrix in matrices:
                    actual=unit(n@matrix.T);delta=np.linalg.norm(actual-baseline,axis=1)
                    cross=np.linalg.norm(np.cross(actual,baseline),axis=1);dot=(actual*baseline).sum(1)
                    worst=max(worst,float(delta.max()));worstAngle=max(worstAngle,float(np.arctan2(cross,dot).max())*180/np.pi)
                samples+=len(n)
        assert worst<=bound+1e-12
        # Transfer the measured frame perturbation through each receiver's
        # orthonormal UV frame. Its operator norm is unchanged by orientation.
        tiltChecks=0;maxTiltBoundDifference=0.
        for roll in (-45.,-15.,0.,15.,45.):
            for grade in (-45.,-15.,0.,15.,45.):
                n=unit(np.array([math.sin(math.radians(roll)),1.,math.sin(math.radians(grade))]))
                v=np.array([0.,0.,-1. if domain['bone']==1 else 1.]);t=np.cross(v,n);t*=np.sign(np.dot(nominal,t));t=unit(t)
                target=np.column_stack((t,np.cross(n,t)*hand,n));rotation=target@reference.T
                for matrix in matrices:
                    delta=float(np.linalg.norm(rotation@(matrix-reference),ord=2))
                    maxTiltBoundDifference=max(maxTiltBoundDifference,abs(delta-float(np.linalg.norm(matrix-reference,ord=2))))
                    tiltChecks+=1
        assert maxTiltBoundDifference<1e-12
        results.append(dict(lod=level['lod'],domain=index,family=domain['family'],sampledNormals=samples,
            originalPackedTangents=evidence['originalPackedTangents'],mipLevels=13,
            filters=['native pixel centres','bilinear half-U','bilinear half-V','bilinear cell centre','trilinear half-LOD at same UV'],
            nativeFrameOperatorDifference=perturbation,conservativeNormalizedVectorBudget=bound,
            maximumSampledNormalizedVectorError=worst,maximumSampledAngleDegrees=worstAngle,
            minimumDecodedNormalZ=minimumZ,maximumSampledRedAlphaProduct=maximumAGProduct,
            receiverFrameTransportChecks=tiltChecks,maximumTiltOperatorDifference=maxTiltBoundDifference,
            bitIdentical=perturbation==0))
        print(level['lod'],index,'angle',worstAngle,'actual',worst,'bound',bound,flush=True)
(OUT/'normal-frame-budget.json').write_text(json.dumps(dict(normalDecode='normalize(float3((R*A,G)*2-1,sqrt(1-saturate(dot(XY,XY)))))',
    note='Source-frame variation budget, not a universal 1/255 threshold. Receiver tilt checks cover transport through orthonormal projected frames; no singular UV frame is admitted.',domains=results),indent=2)+'\n')
