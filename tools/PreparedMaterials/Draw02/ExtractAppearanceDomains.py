"""Measure real grating/solid domains; never generate runtime triangle Booleans."""
import json
from pathlib import Path
from collections import defaultdict
import numpy as np
root=Path(__file__).resolve().parents[3];e=root/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism';out=root/'artifacts/material-remap/Draw02/near-cells'
result=[]
for lod in (0,1):
    m=np.load(e/f'lod{lod}-native.npz');p=m['mesh0_positions'].astype(float);f=m['mesh0_indices'];uv=m['mesh0_uv'].astype(float);bones=m['mesh0_bones'][:,0]
    original=json.loads((e/f'lod{lod}-analysis.json').read_text());groups=defaultdict(list)
    # LOD1 merges some flat road tops with raised edges. The same measured road
    # chart on an upward-facing sloped surface remains part of its appearance
    # domain; a constant-Y filter would silently omit 61 native road triangles.
    for face in range(len(f)):
        points=p[f[face]];tex=uv[f[face]];bone=int(bones[f[face,0]])
        if bone==0 or np.cross(points[1]-points[0],points[2]-points[0])[1]<=0:continue
        if points[:,1].min()<9.79999 or points[:,1].max()>10.00001:continue
        lo,hi=tex.min(0),tex.max(0)
        if lo[0]>=.513 and hi[0]<=.590 and lo[1]>=.273 and hi[1]<=.476:family='grating'
        elif lo[0]>=.592 and hi[0]<=.613 and lo[1]>=.273 and hi[1]<=.476:family='solid'
        else:continue
        # Authored transverse modules are evidenced by the complete LOD0 strip.
        # Choosing the nearest measured centre labels an existing feature, not a
        # permission to split source geometry at that centre or at UV-fit errors.
        centers=np.array([-5.25,-3.75,-2.25,-.75,.75,2.25,3.75,5.25]) if family=='grating' else np.array([-5.95,-4.5,-3.,-1.5,0.,1.5,3.,4.5,5.95])
        index=int(np.argmin(abs(centers-points[:,0].mean())))
        groups[(bone,family,index)].append(face)
    domains=[]
    for (bone,family,index),faces in sorted(groups.items()):
        vertices=np.unique(f[faces]);points=p[vertices];tex=uv[vertices]
        # A single transverse affine U is a useful feasibility test. V repeats
        # in the original atlas; it is deliberately excluded from the fit.
        basis=np.column_stack((points[:,0],np.ones(len(points))))
        coefficients=np.linalg.lstsq(basis,tex[:,0],rcond=None)[0];mapped=basis@coefficients
        half=tex[:,0].astype(np.float16);lower=(tex[:,0]+np.nextafter(half,np.float16(-np.inf)).astype(float))/2;upper=(tex[:,0]+np.nextafter(half,np.float16(np.inf)).astype(float))/2
        x=points[:,0];delta=x[None,:]-x[:,None];nonzero=delta!=0;width=(upper-lower)/2
        def feasible(margin):
            lo,hi=lower+width*margin,upper-width*margin
            rhs=hi[None,:]-lo[:,None]
            if np.any(rhs[~nonzero]<0):return None
            ratios=np.divide(rhs,delta,out=np.zeros_like(rhs),where=nonzero)
            minimum=np.max(ratios[delta<0]);maximum=np.min(ratios[delta>0])
            if minimum>maximum:return None
            slope=(minimum+maximum)/2;low=np.max(lo-slope*x);high=np.min(hi-slope*x)
            if low>high+1e-14:return None
            return [float(slope),float((low+high)/2)]
        feasible_zero=feasible(0);left,right=0.,1.
        if feasible_zero is not None:
            for _ in range(40):
                mid=(left+right)/2
                if feasible(mid) is None:right=mid
                else:left=mid
        interior=feasible(left) if feasible_zero is not None else None
        interval32=None
        if interior is not None:
            c32=np.asarray(interior,dtype=np.float32)
            actual=(points[:,0].astype(np.float32)*c32[0]+c32[1]).astype(float)
            interval32=dict(coefficients=c32.tolist(),strictlyInside=bool(np.all((actual>lower)&(actual<upper))),
                sameHalfBits=bool(np.array_equal(actual.astype(np.float16).view(np.uint16),half.view(np.uint16))),
                maximumPixelError=float(np.max(abs(actual-tex[:,0]))*4096))
        edges=defaultdict(list)
        for face in faces:
            ids=f[face]
            for i in range(3):
                # Appearance coverage is projected in XZ; preserve selected-road
                # height and do not copy the original slope into its geometry.
                a=tuple([p[ids[i],0],0.,p[ids[i],2]])
                b=tuple([p[ids[(i+1)%3],0],0.,p[ids[(i+1)%3],2]])
                edges[tuple(sorted((a,b)))].append(face)
        boundary=[dict(a=list(k[0]),b=list(k[1]),face=v[0])for k,v in edges.items()if len(v)==1]
        graph=defaultdict(set)
        for edge in boundary:
            a,b=tuple(edge['a']),tuple(edge['b']);graph[a].add(b);graph[b].add(a)
        changed=True
        while changed:
            changed=False
            for vertex,neighbors in list(graph.items()):
                if len(neighbors)!=2:continue
                a,b=neighbors;v=np.array(vertex);pa,pb=np.array(a),np.array(b)
                if np.linalg.norm(np.cross(pa-v,pb-v))>1e-8:continue
                graph[a].remove(vertex);graph[b].remove(vertex);graph[a].add(b);graph[b].add(a);del graph[vertex];changed=True;break
        simple=sorted(set(tuple(sorted((a,b)))for a,neighbors in graph.items()for b in neighbors))
        stations=sorted(set(float(v[2])for edge in boundary for v in (edge['a'],edge['b'])))
        domains.append(dict(bone=bone,family=family,transverseFeature=index,faces=faces,triangleCount=len(faces),
            boundarySegments=boundary,boundaryStations=stations,
            simplifiedBoundaryEdges=[[list(a),list(b)]for a,b in simple],
            simplifiedBoundaryEdgeCount=len(simple),boundaryBranchVertices=sum(len(n)!=2 for n in graph.values()),
            affineU=coefficients.tolist(),maximumSourcePixelError=float(np.max(abs(mapped-tex[:,0]))*4096),
            sourceIntervalAffineU=interior,sourceIntervalMaximumInteriorMargin=left if interior is not None else None,
            actualFloat32IntervalCheck=interval32,
            verticesOutsideHalfStorageIntervals=int(np.sum((mapped<lower)|(mapped>upper))),
            uvMinimum=tex.min(0).tolist(),uvMaximum=tex.max(0).tolist()))
    result.append(dict(lod=lod,realTransverseDomains=len(domains),domains=domains,
        note='These are offline feature measurements, not generated partition permission; boundary simplification and source-interval fits require separate validation.'))
    print(lod,len(domains),[(d['bone'],d['family'],d['transverseFeature'],d['triangleCount'],round(d['maximumSourcePixelError'],3),d['verticesOutsideHalfStorageIntervals'])for d in domains])
(out/'appearance-domain-measurements.json').write_text(json.dumps(result,indent=2)+'\n')
