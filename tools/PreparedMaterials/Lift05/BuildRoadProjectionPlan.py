"""Build measured UV-projection plane trees; no runtime prototype geometry."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np
from ReviewRoadProjectionBoundaries import ROOT, SOURCE

EPS=np.finfo(float).eps

def area(poly):
    def cross(a,b):return a[0]*b[1]-a[1]*b[0]
    return abs(sum(cross(poly[i]-poly[0],poly[i+1]-poly[0]) for i in range(1,len(poly)-1)))/2 if len(poly)>2 else 0.

def values(poly,line):
    p=np.column_stack((poly,np.ones(len(poly))))
    v=p@line;noise=32*EPS*(np.abs(p)@np.abs(line))
    v[np.abs(v)<=noise]=0
    return v

def clip(poly,line,positive):
    v=values(poly,line)*(1 if positive else -1);out=[]
    for i,a in enumerate(poly):
        j=(i+1)%len(poly);b=poly[j]
        if v[i]>=0:out.append(a)
        if (v[i]>0 and v[j]<0) or (v[i]<0 and v[j]>0):
            out.append(a+(b-a)*(v[i]/(v[i]-v[j])))
    return np.array(out).reshape(-1,2)

def line_for(edge):
    (x,z),(xx,zz)=edge
    line=np.array([zz-z,x-xx,xx*z-x*zz]);line/=max(abs(line[0]),abs(line[1]))
    if line[np.flatnonzero(line)[0]]<0:line=-line
    return line

def make_tree(cell,fragments,lines,nodes,depth=0):
    assert depth<100
    if not fragments:return -1
    groups={g for g,p in fragments}
    covered=sum(area(p) for g,p in fragments);extent=max(1,float(np.max(np.abs(cell))))
    bound=256*EPS*extent*extent*(len(fragments)+len(cell))
    if len(groups)==1 and abs(covered-area(cell))<=bound:return -2-next(iter(groups))
    best=None
    for k,line in enumerate(lines):
        cv=values(cell,line)
        if not(np.min(cv)<0 and np.max(cv)>0):continue
        sides=[[],[]];cross=0
        for g,p in fragments:
            v=values(p,line)
            if np.min(v)>=0:sides[1].append((g,p))
            elif np.max(v)<=0:sides[0].append((g,p))
            else:
                cross+=1
                for side in (0,1):
                    q=clip(p,line,side==1)
                    if area(q)>0:sides[side].append((g,q))
        # Isolate source-free areas before cutting repeat/gradient planes. Such
        # areas retain the selected source and need no prototype subdivisions.
        score=(0 if not sides[0] or not sides[1] else 1,cross*8+max(map(len,sides)),cross,k)
        if best is None or score<best[0]:best=(score,k,sides)
    if best is None:raise ValueError(('Unresolved measured domain',depth,groups,covered,area(cell),cell.tolist()))
    _,k,sides=best;line=lines[k];remaining=lines[:k]+lines[k+1:]
    children=[make_tree(clip(cell,line,s==1),sides[s],remaining,nodes,depth+1) for s in (0,1)]
    if children[0]==children[1]:return children[0]
    result=len(nodes);nodes.append(dict(lineXZ1=line.tolist(),negative=children[0],positive=children[1]));return result

def locate(point,root,nodes):
    while root>=0:
        node=nodes[root];root=node['positive' if np.dot([*point,1],node['lineXZ1'])>=0 else 'negative']
    return -root-2

def coverage(cell,root,nodes,areas):
    if root<0:
        group=-root-2;areas[group]=areas.get(group,0)+area(cell);return
    node=nodes[root];line=np.array(node['lineXZ1'])
    for side,key in ((False,'negative'),(True,'positive')):
        child=clip(cell,line,side)
        if len(child)>2:coverage(child,node[key],nodes,areas)

def main():
    evidence=json.loads((ROOT/'artifacts/material-remap/Lift05/road-projection-boundaries.json').read_text())
    reports=[]
    for level in evidence['levels']:
        lod=level['lod'];source=json.loads((SOURCE/f'road-lod{lod}.json').read_text());faces={f['id']:f for f in source['faces']}
        groups=level['affineRegions'];by_face={f:g for g,ids in enumerate(groups) for f in ids}
        patches=[]
        for ids in groups:
            xyz=np.array([faces[f]['positions'] for f in ids]).reshape(-1,3);uv=np.array([faces[f]['uv'] for f in ids]).reshape(-1,2)
            patches.append(dict(faces=ids,coefficients=level['coefficients'][str(ids[0])],uvMin=uv.min(axis=0).tolist(),uvMax=uv.max(axis=0).tolist(),
                sourceMin=xyz.min(axis=0).tolist(),sourceMax=xyz.max(axis=0).tolist(),bone=faces[ids[0]]['bone']))
        nodes=[];bands=[];coverage_checks=[]
        removed={tuple(tuple(v) for v in x['edge']) for x in level['removedTriangulationEdges']}
        for low,high in ((-6.,-3.),(-3.,3.),(3.,6.)):
            selected=[f for f in faces.values() if low<np.mean(np.array(f['positions'])[:,0])<high]
            fragments=[(by_face[f['id']],np.array(f['positions'])[:,[0,2]]) for f in selected]
            lines=[];keys=set()
            for f in selected:
                p=np.array(f['positions'])[:,[0,2]]
                for a,b in ((0,1),(1,2),(2,0)):
                    edge=tuple(sorted((tuple(p[a]),tuple(p[b]))))
                    if edge in removed:continue
                    line=line_for(edge);key=tuple(line)
                    if key not in keys:keys.add(key);lines.append(line)
            cell=np.array([[low,-84.],[high,-84.],[high,84.],[low,84.]])
            root=make_tree(cell,fragments,lines,nodes)
            measured={};coverage(cell,root,nodes,measured)
            expected={g:sum(area(p) for group,p in fragments if group==g) for g,p in fragments}
            expected[-1]=area(cell)-sum(expected.values())
            maximum=max(abs(measured.get(g,0)-a) for g,a in expected.items())
            arithmetic_bound=256*EPS*84**2*(len(fragments)+len(nodes)+len(cell))
            assert maximum<=arithmetic_bound,(lod,low,maximum,arithmetic_bound)
            # The source boundary is necessary: shifting the measured plane by
            # 1 cm changes area beyond arithmetic uncertainty.
            if root>=0:
                original=nodes[root]['lineXZ1'][2];nodes[root]['lineXZ1'][2]=original+.01
                wrong={};coverage(cell,root,nodes,wrong);nodes[root]['lineXZ1'][2]=original
                assert max(abs(wrong.get(g,0)-a) for g,a in expected.items())>arithmetic_bound
            coverage_checks.append(dict(band=[low,high],maximumAreaError=maximum,arithmeticBound=arithmetic_bound,
                unsupportedArea=measured.get(-1,0),sourceArea=sum(expected.values())-expected[-1]))
            # Every original triangle is checked at interior barycentric samples;
            # edge equality has two legitimate adjacent mappings at a reset.
            for f in selected:
                p=np.array(f['positions'])[:,[0,2]]
                for weights in ((1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)):
                    assert locate(np.array(weights)@p,root,nodes)==by_face[f['id']],(lod,f['id'])
            bands.append(dict(minX=low,maxX=high,root=root))
        reports.append(dict(lod=lod,patches=patches,nodes=nodes,bands=bands,
            sourceSha256=level['sourceSha256'],sourceFaces=len(faces),coverage=coverage_checks))
        print('LOD',lod,'patches',len(patches),'nodes',len(nodes),'source samples PASS')
    out=ROOT/'artifacts/material-remap/Lift05/road-projection-plan.json'
    out.write_text(json.dumps(dict(levels=reports,scope='Offline measured plane construction. Production export excludes source faces, vertices and polygons.'),indent=2)+'\n')
    integers=[1,len(reports)];doubles=[]
    for level in reports:
        integers.extend([len(level['patches']),len(level['nodes']),*[b['root'] for b in level['bands']]])
        for patch in level['patches']:
            center=(patch['sourceMin'][0]+patch['sourceMax'][0])/2
            integers.append(0 if center<-3 else 2 if center>3 else 1)
            doubles.extend(np.array(patch['coefficients']).ravel().tolist()+patch['uvMin']+patch['uvMax'])
        for node in level['nodes']:
            integers.extend([node['negative'],node['positive']]);doubles.extend(node['lineXZ1'])
    raw=b'BBN1'+struct.pack('<7i',1,len(integers),0,0,0,0,len(doubles))
    raw+=struct.pack('<%di'%len(integers),*integers)+struct.pack('<%dd'%len(doubles),*doubles)
    raw+=hashlib.sha256(raw).digest()
    # Historical atlas-reset audit only; current continuous production metadata is
    # owned by InstallContinuousRoadCandidate.py. Never restore period partitions.
    target=ROOT/'artifacts/material-remap/Lift05/backups/continuous-road/Types_Lift05_RoadProjection.bin'
    if '--check' in sys.argv:assert target.read_bytes()==raw,'Road projection metadata differs.'
    else:(ROOT/'artifacts/material-remap/Lift05/road-projection-legacy.bin').write_bytes(raw)
    print('Projection-only BBN:',len(raw),'bytes; source geometry excluded')

if __name__=='__main__':main()
