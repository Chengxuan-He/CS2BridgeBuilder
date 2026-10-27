"""Generate lazy native UV boundary charts from measured PedestrianDraw02 faces."""
from collections import defaultdict
import hashlib
import json
import math
from pathlib import Path
import struct
import sys

import numpy as np
from ExportRecipes import numbers

root = Path(sys.argv[1]).resolve()
sys.path.insert(0, str(root / 'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays
directory = root/'artifacts/prepared-materials/PedestrianDraw02/source-consolidation'
report = json.loads((directory/'native-charts.json').read_text())
data = np.load(directory/'native-charts.npz')


def cross(a,b,c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def on(a,b,p):
    return cross(a,b,p)==0 and min(a[0],b[0])<=p[0]<=max(a[0],b[0]) and min(a[1],b[1])<=p[1]<=max(a[1],b[1])


def intersects(a,b,c,d):
    x,y,z,w = cross(a,b,c),cross(a,b,d),cross(c,d,a),cross(c,d,b)
    return (x*y<0 and z*w<0) or on(a,b,c) or on(a,b,d) or on(c,d,a) or on(c,d,b)


def inside(p, ring):
    hit = False
    for a,b in zip(ring,ring[1:]+ring[:1]):
        if on(a,b,p):
            return None
        if (a[1]>p[1]) != (b[1]>p[1]) and p[0] < (b[0]-a[0])*(p[1]-a[1])/(b[1]-a[1])+a[0]:
            hit = not hit
    return hit


def boundaries(positions,uv,faces,members):
    edges = defaultdict(list)
    triangles = uv[faces[members]].astype(float)
    signs = [cross(*q) for q in triangles]
    if not signs or any(x==0 for x in signs) or (min(signs)<0<max(signs)):
        return None,'degenerate-or-folded-uv'
    for fi in members:
        face = faces[fi]
        for v,w in zip(face,np.roll(face,-1)):
            a,b = tuple(map(float,(*positions[v],*uv[v]))),tuple(map(float,(*positions[w],*uv[w])))
            edges[tuple(sorted((a,b)))].append((tuple(map(float,uv[v])),tuple(map(float,uv[w]))))
    if any(len(v)>2 for v in edges.values()):
        return None,'nonmanifold-edge'
    if any(len(v)==2 and v[0]!=(v[1][1],v[1][0]) for v in edges.values()):
        return None,'inconsistent-interior-edge'
    graph = defaultdict(list)
    for uses in edges.values():
        if len(uses)!=1:
            continue
        a,b = uses[0]
        if a==b:
            return None,'collapsed-boundary'
        graph[a].append(b); graph[b].append(a)
    if not graph or any(len(v)!=2 or v[0]==v[1] for v in graph.values()):
        return None,'nonmanifold-or-absent-boundary'
    remaining = set(graph)
    loops = []
    while remaining:
        start = min(remaining); ring = [start]; previous = start; current = min(graph[start])
        while current!=start:
            if current not in remaining or current in ring:
                return None,'unclosed-boundary'
            ring.append(current)
            nxt = graph[current][0] if graph[current][0]!=previous else graph[current][1]
            previous,current = current,nxt
        remaining.difference_update(ring)
        if len(ring)<3:
            return None,'short-boundary'
        loops.append(ring)
    segments = [(li,i,a,b) for li,r in enumerate(loops) for i,(a,b) in enumerate(zip(r,r[1:]+r[:1]))]
    for at,(li,i,a,b) in enumerate(segments):
        for lj,j,c,d in segments[at+1:]:
            if li==lj and (abs(i-j)==1 or {i,j}=={0,len(loops[li])-1}):
                # Adjacent collinear edges may continue but must not double back.
                shared = set((a,b)) & set((c,d))
                if len(shared)!=1:
                    return None,'overlapping-adjacent-boundary'
                common = next(iter(shared)); ends = list((set((a,b,c,d))-shared))
                if len(ends)==2 and on(common,ends[0],ends[1]):
                    return None,'folded-adjacent-boundary'
                if len(ends)==2 and on(common,ends[1],ends[0]):
                    return None,'folded-adjacent-boundary'
                continue
            if intersects(a,b,c,d):
                return None,'self-intersecting-or-touching-loops'
    area = 0.0
    for i,ring in enumerate(loops):
        depth = sum(bool(inside(ring[0],other)) for j,other in enumerate(loops) if i!=j)
        signed = math.fsum(a[0]*b[1]-a[1]*b[0] for a,b in zip(ring,ring[1:]+ring[:1]))/2
        if signed==0:
            return None,'zero-area-loop'
        area += (-1 if depth%2 else 1)*abs(signed)
    source_area = math.fsum(abs(x)/2 for x in signs)
    if abs(area-source_area)>max(1e-20,source_area*1e-12):
        return None,'overlapping-or-incomplete-uv-area'
    for q in triangles:
        center = tuple(np.mean(q,axis=0))
        values = [inside(center,ring) for ring in loops]
        if None not in values and sum(values)%2!=1:
            return None,'source-face-outside-rings'
    return loops,None


streams, chart_records, rejected = [], [], defaultdict(int)
for record in report['records']:
    prefix=record['arrayPrefix']
    positions,uv,faces,face_chart=(data[prefix+'_'+key] for key in ('positions','uv','faces','face_chart'))
    path = root/'assets/HardcodedNumbers/PedestrianDraw02'/('UvDomain_'+record['name'].replace(' ','_')+'_'+str(record['mesh'])+'.bin')
    values=numbers(path)
    raw, _bounds = decode_arrays(values['I32'], values['F32'])
    assert raw[0]==-2
    domains=[]; at=2
    for _ in range(raw[1]):
        a,b,c,count=raw[at:at+4]; at+=4
        members=raw[at:at+abs(count)]; at+=abs(count)
        domains.append(sorted({int(face_chart[face]) for face in members}))
    assert at==len(raw)
    needed=sorted({ci for domain in domains for ci in domain})
    valid={}; reasons={}
    for ci in needed:
        loops,reason=boundaries(positions,uv,faces,record['charts'][ci]['faces'])
        if reason:
            rejected[reason]+=1; reasons[ci]=reason
        else:
            valid[ci]=len(chart_records);chart_records.append(loops)
    # An unreliable chart in a domain invalidates that domain's candidate set.
    mapped=[[valid[ci] for ci in domain] if all(ci in valid for ci in domain) else [] for domain in domains]
    streams.append(dict(name=record['name'],mesh=record['mesh'],domains=mapped,
                        recipeSHA256=hashlib.sha256(path.read_bytes()).hexdigest(),
                        referencedCharts=len(needed),reliableCharts=len(valid),rejected=reasons))

i32=[1,len(streams)]+[0]*len(streams)+[len(chart_records)]+[0]*len(chart_records)
f64=[]
for si,stream in enumerate(streams):
    i32[2+si]=len(i32);i32.append(len(stream['domains']))
    for domain in stream['domains']:
        i32.append(len(domain));i32.extend(domain)
for ci,loops in enumerate(chart_records):
    i32[3+len(streams)+ci]=len(i32);i32.append(len(loops))
    for ring in loops:
        i32.extend((len(f64),len(ring)))
        f64.extend(x for point in ring for x in point)
body=b'BBN1'+struct.pack('<i6i',1,len(i32),0,0,0,0,len(f64))+struct.pack('<'+'i'*len(i32),*i32)+struct.pack('<'+'d'*len(f64),*f64)
resource=root/'assets/HardcodedNumbers/PedestrianDraw02/PedestrianDraw02UvCharts.bin'
resource.write_bytes(body+hashlib.sha256(body).digest())
decoded=numbers(resource)
assert decoded['I32']==tuple(i32) and decoded['F64']==tuple(f64)
assert {ci for stream in streams for domain in stream['domains'] for ci in domain}==set(range(len(chart_records)))
assert all(len(ring)>=3 and len(set(ring))==len(ring) and ring[0]!=ring[-1]
           for loops in chart_records for ring in loops)
switch='\n'.join('            ('+json.dumps(s['name'])+', '+str(s['mesh'])+') => Data.Values['+str(i)+'],' for i,s in enumerate(streams))
source='''// Generated by tools/PreparedMaterials/PedestrianDraw02/GenerateUvCharts.py.
// Embedded numerical arrays: PedestrianDraw02UvCharts
// Exact native UV chart boundaries; no repeat inference or geometry subdivision.
using System;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw02UvCharts
{
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        var domains = (renderName, meshIndex) switch
        {
SWITCH
            _ => null
        };
        return domains != null && (uint)domain < (uint)domains.Length
            ? domains[domain] : Array.Empty<MovableUvChart>();
    }

    private static class Data
    {
        static Data() { }
        internal static readonly MovableUvChart[][][] Values = Load();
        private static MovableUvChart[][][] Load()
        {
            var data = HardcodedNumbers.Load("PedestrianDraw02UvCharts");
            var ints = data.I32;
            if (ints[0] != 1) throw new InvalidOperationException("Unsupported PedestrianDraw02 UV chart data.");
            int streams = ints[1], count = ints[2 + streams];
            var charts = new MovableUvChart[count];
            for (int i = 0; i < count; i++)
                charts[i] = MovableUvChart.Read(ints, data.F64, ints[3 + streams + i]);
            var result = new MovableUvChart[streams][][];
            for (int s = 0; s < streams; s++)
            {
                int at = ints[2 + s], domains = ints[at++];
                result[s] = new MovableUvChart[domains][];
                for (int d = 0; d < domains; d++)
                {
                    int length = ints[at++];
                    if (length == 0) { result[s][d] = Array.Empty<MovableUvChart>(); continue; }
                    var selected = new MovableUvChart[length];
                    for (int i = 0; i < length; i++) selected[i] = charts[ints[at++]];
                    result[s][d] = selected;
                }
            }
            return result;
        }
    }
}
'''.replace('SWITCH',switch)
(root/'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02UvCharts.cs').write_text(source,encoding='utf-8')
result=dict(style='PedestrianDraw02',streams=streams,sharedCharts=len(chart_records),rejected=dict(rejected),
            bytes=resource.stat().st_size,sha256=hashlib.sha256(resource.read_bytes()).hexdigest(),
            domains=sum(len(s['domains']) for s in streams),coveredDomains=sum(bool(d) for s in streams for d in s['domains']),
            loops=sum(len(c) for c in chart_records),points=sum(len(r) for c in chart_records for r in c),
            bitExactBbnRoundTrip=True,unreferencedCharts=0)
(directory/'embedded-chart-report.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='streams'}))

# Recover only independently verified source unions after preserving the original
# reliable chart baseline. This is part of normal regeneration, not a one-off BBN
# patch; no receiver scenario selects the admitted source shapes.
import subprocess
subprocess.run([sys.executable, str(root/'tools/PreparedMaterials/PedestrianDraw02/StageUnionCharts.py')], check=True)
subprocess.run([sys.executable, str(root/'tools/PreparedMaterials/PedestrianDraw02/StageUnionCharts.py'), '--exact-intersections'], check=True)
subprocess.run([sys.executable, str(root/'tools/PreparedMaterials/PedestrianDraw02/IntegrateUnionCharts.py'), '--apply', '--exact-intersections'], check=True)
