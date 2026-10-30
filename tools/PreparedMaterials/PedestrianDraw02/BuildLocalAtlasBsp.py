"""Build a local measured-appearance BSP without global station extensions.

Only audited appearance-domain edge lines are candidates. No prototype faces or
triangle Boolean operations are emitted. Same-bone subtrees can collapse in the
motion tree while appearance retains its original source-UV resets.
"""
import json
import sys
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def area(p):
    if len(p)<3:return 0.
    a=np.asarray(p);return abs(float(np.sum(a[:,0]*np.roll(a[:,1],-1)-a[:,1]*np.roll(a[:,0],-1))))/2


def clip(p,line,positive):
    result=[]
    for a,b in zip(p,p[1:]+p[:1]):
        da=np.dot(line[:2],a)+line[2];db=np.dot(line[:2],b)+line[2]
        if (da>=0 if positive else da<=0):result.append(a)
        if (da<0<db) or (db<0<da):
            result.append((np.asarray(a)+(np.asarray(b)-a)*(da/(da-db))).tolist())
    clean=[]
    for point in result:
        if not clean or np.linalg.norm(np.asarray(clean[-1])-point)>1e-12:clean.append(point)
    if len(clean)>1 and np.linalg.norm(np.asarray(clean[0])-clean[-1])<1e-12:clean.pop()
    return clean


def main():
    continuous='--continuous' in sys.argv
    runtime='--runtime' in sys.argv
    domains=json.loads((OUT/('continuous-runtime-domains.json' if runtime else 'continuous-atlas-domains.json' if continuous else 'native-atlas-domains.json')).read_text())['levels'][0]['domains']
    entries=[(i,p) for i,d in enumerate(domains) for p in d.get('outlinePieces',[d['boundary']])]
    edge_owners={}
    for identity,points in entries:
        for a,b in zip(points,points[1:]+points[:1]):
            key=tuple(sorted((tuple(a),tuple(b))))
            edge_owners.setdefault(key,[]).append(identity)
    lines=[];seen=set()
    removed_edges=0
    outlines=entries
    if '--exact-outlines' in sys.argv:
        from ExactOutlineEdges import external_edges
        external,removed=external_edges(entries)
        (OUT/'exact-outline-cancellation.json').write_text(json.dumps(dict(exterior=external,removed=removed),indent=2)+'\n')
        outlines=[(-1,[e['a'],e['b']]) for e in external]
    for identity,points in outlines:
        for a,b in zip(points,points[1:]+points[:1]):
            owners=edge_owners.get(tuple(sorted((tuple(a),tuple(b)))),[])
            if continuous and len(owners)==2 and owners[0]==owners[1]:
                removed_edges+=1
                continue
            direction=np.asarray(b)-a
            if not np.any(direction):continue
            normal=np.array([-direction[1],direction[0]])
            normal/=np.linalg.norm(normal)
            c=-np.dot(normal,a)
            if normal[0]<0 or(normal[0]==0 and normal[1]<0):normal=-normal;c=-c
            key=tuple(np.round([*normal,c],12))
            if key in seen:continue
            seen.add(key);lines.append(dict(a=a,b=b,plane=[*normal,c]))
    nodes=[];stats=dict(splitDomainEvents=0,maxDepth=0)
    def build(region,entries,available,depth=0):
        stats['maxDepth']=max(stats['maxDepth'],depth)
        if not entries:return -1
        if len({i for i,p in entries})==1 and abs(sum(area(p) for i,p in entries)-area(region))<1e-8:
            return -2-entries[0][0]
        options=[]
        for index in available:
            line=lines[index]['plane']
            low=clip(region,line,False);high=clip(region,line,True)
            if min(area(low),area(high))<1e-12:continue
            left=[];right=[];splits=0
            for identity,p in entries:
                lp=clip(p,line,False);rp=clip(p,line,True)
                has_left=area(lp)>1e-12;has_right=area(rp)>1e-12
                if has_left:left.append((identity,lp))
                if has_right:right.append((identity,rp))
                splits+=has_left and has_right
            if not left and not right:continue
            low_area,high_area=area(low),area(high)
            low_fill,high_fill=sum(area(p) for _,p in left),sum(area(p) for _,p in right)
            # Isolate true footprint gaps before extending internal UV stations.
            # This also lets the motion tree collapse whole same-bone interiors.
            mixed_gap=low_fill*max(0,low_area-low_fill)/low_area+high_fill*max(0,high_area-high_fill)/high_area
            if '--group-score' in sys.argv:
                left_ids={i for i,p in left};right_ids={i for i,p in right}
                splits=len(left_ids&right_ids)
                balance=max(len(left_ids),len(right_ids))
            else:balance=max(len(left),len(right))
            options.append(((splits,mixed_gap,balance,abs(low_area-high_area)),index,low,high,left,right))
        if not options:
            raise AssertionError(('No measured separating boundary',depth,len(entries),area(region),sum(area(p) for _,p in entries)))
        _,index,low,high,left,right=min(options,key=lambda p:p[0])
        stats['splitDomainEvents']+=sum(i==j for i,_ in left for j,_ in right)
        rest=[i for i in available if i!=index]
        # Preorder, with negative values reserved for native-domain/gap leaves.
        at=len(nodes);nodes.append(None)
        negative=build(low,left,rest,depth+1);positive=build(high,right,rest,depth+1)
        nodes[at]=dict(line=index,negative=negative,positive=positive)
        return at
    bounds=[[-4.,-76.],[4.,-76.],[4.,76.],[-4.,76.]]
    root=build(bounds,entries,list(range(len(lines))))
    physical=[]
    def motion(at):
        if at<0:return -1 if at==-1 else -2-domains[-at-2]['bone']
        node=nodes[at];negative=motion(node['negative']);positive=motion(node['positive'])
        if negative==positive and negative<0:return negative
        index=len(physical);physical.append(dict(line=node['line'],negative=negative,positive=positive));return index
    physical_root=motion(root)
    result=dict(schema=1,root=root,bounds=bounds,lines=lines,nodes=nodes,physicalNodes=physical,physicalRoot=physical_root,stats=stats,removedInternalEdgeOccurrences=removed_edges,
        source='native-atlas-domains.json',limits=['Offline BSP candidate only; deformed-width topology and actual production clipping are unverified.',
        'Gap/coverage tolerances are numerical construction diagnostics, not permission to shift authored UV seams.'])
    (OUT/('continuous-local-bsp.json' if continuous else 'local-atlas-bsp.json')).write_text(json.dumps(result,indent=2)+'\n')
    if runtime and '--analysis-only' not in sys.argv:
        sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
        from MovableUvDomainFormat import write_bbn
        integers=[2,root,len(nodes),len(lines),len(domains)]
        integers.extend(d['representativeCell'] for d in domains)
        integers.extend(d['bone'] for d in domains)
        for node in nodes:integers.extend([node['line'],node['negative'],node['positive']])
        values=[]
        for line in lines:
            a,b=line['a'],line['b']
            weight=lambda x:0 if abs(x)<.0001 else (-1 if x<0 else 1)
            values.extend([*a,weight(a[0]),*b,weight(b[0]),*line['plane'][:2]])
        values.extend([-4,4,-76,76])
        (ROOT/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_LocalAppearance.bin').write_bytes(write_bbn(integers,values))
    print(json.dumps(dict(nodes=len(nodes),sourceLines=len(lines),**stats)))


if __name__=='__main__':main()
