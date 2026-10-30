"""Extract reviewed compact native appearance quads and half-precision bounds.

Domains are authored strip/station cells, not arbitrary prototype-face clipping
inputs. Runtime construction must use their axis/hinge lines, not source faces.
"""
import itertools
import json
import struct
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def hull(points):
    points=sorted(set(tuple(p) for p in points))
    def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    def half(seq):
        result=[]
        for p in seq:
            while len(result)>=2 and cross(result[-2],result[-1],p)<=0:result.pop()
            result.append(p)
        return result
    return half(points)[:-1]+half(points[::-1])[:-1]


def affine(points,values):
    points=np.asarray(points,dtype=np.float64)
    center=points.mean(axis=0);scale=np.maximum(np.ptp(points,axis=0),1e-9)
    matrix=np.c_[(points-center)/scale,np.ones(len(points))]
    values=np.asarray(values,dtype=np.float64)
    half=values.astype(np.float16)
    assert np.array_equal(half.astype(np.float64),values)
    low=(np.nextafter(half,np.float16(-np.inf)).astype(np.float64)+values)/2
    high=(np.nextafter(half,np.float16(np.inf)).astype(np.float64)+values)/2
    coeff=np.linalg.lstsq(matrix,values,rcond=None)[0]
    def valid(c):
        sample=matrix@c
        return np.all(sample>=low-1e-12) and np.all(sample<=high+1e-12)
    if not valid(coeff):
        constraints=np.r_[matrix,-matrix]; bounds=np.r_[high,-low]
        candidates=[]
        for chosen in itertools.combinations(range(len(bounds)),3):
            a=constraints[list(chosen)]
            if abs(np.linalg.det(a))<1e-12:continue
            c=np.linalg.solve(a,bounds[list(chosen)])
            if valid(c):candidates.append(c)
        if candidates:coeff=min(candidates,key=lambda c:float(np.square(matrix@c-values).sum()))
    world=np.r_[coeff[:2]/scale,coeff[2]-np.dot(coeff[:2]/scale,center)]
    fp=world.astype(np.float32).astype(np.float64)
    sampled=np.c_[points,np.ones(len(points))]@fp
    return dict(coefficients=fp.tolist(),halfRoundingCompatible=bool(np.all(sampled>=low-1e-12) and np.all(sampled<=high+1e-12)),
        maxSourcePixelError=float(abs(sampled-values).max()*4096),
        sourceHalfIntervals=np.stack([low,high],axis=1).tolist())


def main():
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    levels=[]
    for level in recipe['levels']:
        native=Path(level['geometry']).read_bytes()
        header=struct.unpack_from('<QI14i5I',native,10)
        uv_format=(header[0]>>(4*4))&15
        assert uv_format==1, 'Half rounding evidence requires actual Float16 UV0 storage.'
        source_frames={t['face']:t for p in frames['levels'][level['lod']]['panels'] for t in p['sourceFrames']}
        domains=[]
        for panel in level['panels']:
            triangles=panel['triangles']
            # Reviewed topology: every authored cell is a two-triangle quad;
            # the outer fixed section has three collinear-edge triangles first.
            lengths=([3,2,2] if len(triangles)==7 else [2]*(len(triangles)//2))
            # Four audited non-affine hinge quads only: lower equal-V edge
            # slopes while upper equal-V edge is level. Their original internal
            # projection-gradient edge is a required measured appearance seam.
            if panel['firstFace'] in ({8522,11350,23554,26382} if level['lod']==0 else set()):
                lengths=[2,1,1,2]
            if level['lod']==1 and panel['bone']!=0 and len(triangles)==6:
                lengths=[2,1,1,2]
            assert sum(lengths)==len(triangles)
            at=0
            for count in lengths:
                group=triangles[at:at+count];at+=count
                vertices=sorted(set(tuple([p[0],p[2],*uv]) for t in group for p,uv in zip(t['position'],t['sourceUv'])))
                points=np.array(vertices)[:,:2];tex=np.array(vertices)[:,2:]
                frame_vertices=np.array([list([p[0],p[2]])+tangent for t in group for p,tangent in
                    zip(t['position'],source_frames[t['face']]['tangents'])])
                fm=np.c_[frame_vertices[:,:2],np.ones(len(frame_vertices))]
                tangent_coeff=np.linalg.lstsq(fm,frame_vertices[:,2:5],rcond=None)[0].astype(np.float32)
                predicted=fm@tangent_coeff;predicted/=np.linalg.norm(predicted,axis=1)[:,None]
                original=frame_vertices[:,2:5];original/=np.linalg.norm(original,axis=1)[:,None]
                angles=np.degrees(np.arccos(np.clip(np.sum(predicted*original,axis=1),-1,1)))
                domain=dict(panel=panel['firstFace'],bone=panel['bone'],repeatV=panel['repeatedV'],
                    sourceFaces=[t['face'] for t in group],boundary=hull(points),
                    u=affine(points,tex[:,0]),v=affine(points,tex[:,1]),
                    tangentCoefficients=tangent_coeff.tolist(),tangentHandedness=-1,
                    tangentAffineMaxAngleDegrees=float(angles.max()),
                    sourceVertices=vertices,sourceUvMin=panel['sourceUvMin'],sourceUvMax=panel['sourceUvMax'])
                domains.append(domain)
        levels.append(dict(lod=level['lod'],originalUvFormat='Float16x2',domains=domains))
    report=dict(levels=levels,status='MEASURED_DOMAINS_NOT_RUNTIME_ACCEPTANCE',
        limitations=['Half rounding compatibility is tested against exact adjacent Float16 values, not a raised universal pixel threshold.',
                    'Some boundary solutions lose feasibility after Float32 coefficient storage; those are explicitly incompatible.',
                    'Runtime partition coverage, width transforms, uploaded tangents and native render checks remain required.'])
    (OUT/'native-atlas-domains.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps([dict(lod=l['lod'],domains=len(l['domains']),incompatible=sum(not d['u']['halfRoundingCompatible'] or not d['v']['halfRoundingCompatible'] for d in l['domains']),
        maxPixelError=max(max(d['u']['maxSourcePixelError'],d['v']['maxSourcePixelError']) for d in l['domains'])) for l in levels]))


if __name__=='__main__':main()
