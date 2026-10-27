"""Finite source-rectangle UV seam partitioner for the common renderer to port.

The caller supplies density-preserving atlas UVs, not scaled source geometry.
Output vertices retain barycentric coordinates of the unchanged input face.
Only a proven rectangular native chart may call this routine. There is no
triangle-fold fallback and no texture operation. A caller-owned face budget
is mandatory; exceeding it reports the exact required candidate grid.
"""
import math
import numpy as np


def partition(atlas_uv, bounds, maximum_cells):
    uv = np.asarray(atlas_uv, dtype=np.float64)
    bounds = np.asarray(bounds, dtype=np.float64)
    if uv.shape != (3, 2) or bounds.shape != (4,) or not np.isfinite(uv).all():
        raise ValueError('Invalid native chart coordinates')
    span = bounds[2:] - bounds[:2]
    if not np.isfinite(bounds).all() or not np.all(span > 0) or maximum_cells < 1:
        raise ValueError('A finite rectangular native chart and positive budget are required')
    local = (uv-bounds[:2])/span
    # Closed geometry clips at an integer endpoint but does not create an empty
    # next cell. Negative origins and reflected source faces use the same rule.
    start = np.floor(local.min(0)).astype(np.int64)
    stop = np.maximum(start+1, np.ceil(local.max(0)).astype(np.int64))
    cells = int(np.prod(stop-start))
    if cells > maximum_cells:
        raise ValueError(f'Native seam grid requires {cells} cells; budget is {maximum_cells}')

    def clip(points, axis, value, greater):
        output = []
        for a,b in zip(points, points[1:]+points[:1]):
            da,db = a[axis]-value,b[axis]-value
            inside_a,inside_b = (da >= 0,db >= 0) if greater else (da <= 0,db <= 0)
            if inside_a: output.append(a)
            if inside_a != inside_b:
                p = a+(b-a)*(da/(da-db))
                p[axis] = value
                output.append(p)
        # Suppress duplicate seam vertices only; never drop a positive-area face
        # under an arbitrary world-space epsilon.
        unique=[]
        for p in output:
            if not unique or not np.array_equal(p,unique[-1]): unique.append(p)
        if len(unique)>1 and np.array_equal(unique[0],unique[-1]): unique.pop()
        return unique

    result=[]
    initial = np.column_stack((local,np.eye(3)))
    for x in range(int(start[0]),int(stop[0])):
        for y in range(int(start[1]),int(stop[1])):
            polygon = list(initial.copy())
            for axis,value,greater in ((0,x,True),(0,x+1,False),(1,y,True),(1,y+1,False)):
                polygon=clip(polygon,axis,value,greater)
                if len(polygon)<3: break
            for i in range(1,len(polygon)-1):
                points=np.asarray((polygon[0],polygon[i],polygon[i+1]))
                e,f=points[1,:2]-points[0,:2],points[2,:2]-points[0,:2]
                if e[0]*f[1]-e[1]*f[0] == 0: continue
                result.append({'barycentric':points[:,2:],
                               'sourceUv':bounds[:2]+(points[:,:2]-[x,y])*span,
                               'tile':(x,y)})
    return result
