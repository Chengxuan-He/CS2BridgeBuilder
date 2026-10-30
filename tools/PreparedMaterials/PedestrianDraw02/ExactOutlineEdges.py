"""Cancel only exactly coincident opposite edges of the same final domain.

Float source values are converted to exact rational numbers. No tolerance,
coordinate welding, convex hull or interior gap filling is used.
"""
from fractions import Fraction as F


def external_edges(entries):
    lines={}
    for identity,polygon in entries:
        for a,b in zip(polygon,polygon[1:]+polygon[:1]):
            a=tuple(map(F,a));b=tuple(map(F,b))
            dx,dz=b[0]-a[0],b[1]-a[1]
            if dx==0 and dz==0:continue
            if dx:
                key=('x',dz/dx,a[1]-dz/dx*a[0]);axis=0
            else:key=('z',a[0]);axis=1
            lines.setdefault(key,[]).append((identity,a,b,axis))
    result=[];removed=[]
    for key,edges in lines.items():
        axis=edges[0][3]
        coordinates=sorted({v[axis] for _,a,b,_ in edges for v in (a,b)})
        for lo,hi in zip(coordinates,coordinates[1:]):
            owners=[e for e in edges if min(e[1][axis],e[2][axis])<=lo and max(e[1][axis],e[2][axis])>=hi]
            if not owners:continue
            same=len(owners)==2 and owners[0][0]==owners[1][0]
            opposite=same and (owners[0][2][axis]-owners[0][1][axis])*(owners[1][2][axis]-owners[1][1][axis])<0
            _,a,b,_=owners[0]
            def point(t):
                f=(t-a[axis])/(b[axis]-a[axis])
                return [float(a[j]+f*(b[j]-a[j])) for j in range(2)]
            entry=dict(a=point(lo),b=point(hi),owners=[e[0] for e in owners])
            if opposite:removed.append(entry)
            else:result.append(entry)
    return result,removed
