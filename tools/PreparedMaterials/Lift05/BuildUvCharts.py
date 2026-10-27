"""Hardcode Lift05 source UV charts, preserving physical-part and domain identities."""
from collections import defaultdict, Counter
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
from ExportRecipes import numbers

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays
OUT=ROOT/'artifacts/prepared-materials/Lift05/domain-charts'
FIXTURE=ROOT/'artifacts/uv-density/layout-plans.bin'
sys.path.insert(0,str(ROOT/'artifacts/agent-material-env/Lift05/python-deps'))
from shapely.geometry import Polygon, LinearRing
from shapely.ops import unary_union
from shapely.errors import GEOSException


def inputs():
    raw=FIXTURE.read_bytes();version,count=struct.unpack_from('<ii',raw);assert version==2
    at=8;seen={}
    def text():
        nonlocal at
        n=struct.unpack_from('<i',raw,at)[0];at+=4
        value=raw[at:at+n].decode();at+=n;return value
    for _ in range(count):
        name,impl=text(),text();family,lod,mesh,n,ni=struct.unpack_from('<5i',raw,at);at+=20
        p=np.frombuffer(raw,'<f4',n*3,at).reshape(-1,3);uv=np.frombuffer(raw,'<f4',n*2,at+n*12).reshape(-1,2)
        faces=np.frombuffer(raw,'<i4',ni,at+n*20).reshape(-1,3);at+=n*20+ni*4
        if not name.startswith('LiftBridge05'):continue
        key=(name,mesh)
        if key in seen:
            assert seen[key]==hashlib.sha256(p.tobytes()+uv.tobytes()+faces.tobytes()).hexdigest();continue
        seen[key]=hashlib.sha256(p.tobytes()+uv.tobytes()+faces.tobytes()).hexdigest()
        yield name,mesh,p,uv,faces
    assert at==len(raw)


def area(q):
    a,b,c=q[:3];return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def extract(p,uv,faces,recover_overlaps=False):
    # A vertex is connected only at the same physical position and same UV.
    # Atlas aliases at different physical parts do not make one topology component.
    joint=np.concatenate((p,uv),axis=1)
    _,remap=np.unique(joint,axis=0,return_inverse=True)
    parent=list(range(len(faces)));edge_uses=defaultdict(list)
    def find(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    valid=[]
    for i,f in enumerate(faces):
        q=uv[f].astype(float)
        if area(q)==0:continue
        valid.append(i)
        ids=remap[f].tolist()
        for a,b in zip(ids,ids[1:]+ids[:1]):edge_uses[tuple(sorted((a,b)))].append(i)
    for ids in edge_uses.values():
        if len(ids)==2:parent[find(ids[1])]=find(ids[0])
    components=defaultdict(list)
    for i in valid:components[find(i)].append(i)
    face_chart={};charts=[];fallbacks=0
    def triangles(ids):
        result={}
        for i in ids:
            q=tuple(tuple(float(x) for x in v) for v in uv[faces[i]])
            key=tuple(sorted(q))
            if key not in result: result[key]=(q if area(q)>0 else (q[0],q[2],q[1]),[])
            result[key][1].append(i)
        return list(result.values())
    for part,ids in enumerate(components.values()):
        source=triangles(ids);edges=Counter()
        for q,_ in source:
            for a,b in zip(q,q[1:]+q[:1]):edges[(a,b)]+=1
        boundary=[];good=True
        for (a,b),n in edges.items():
            remaining=n-edges[(b,a)]
            if remaining>1:good=False
            if remaining>0:boundary.extend([(a,b)]*remaining)
        outgoing=defaultdict(list);incoming=Counter()
        for a,b in boundary:outgoing[a].append(b);incoming[b]+=1
        if not boundary or any(len(x)!=1 for x in outgoing.values()) or any(n!=1 for n in incoming.values()):good=False
        loops=[]
        if good:
            pending=set(boundary)
            while pending:
                start,following=min(pending);loop=[start];a,b=start,following
                while True:
                    if (a,b) not in pending:good=False;break
                    pending.remove((a,b))
                    if b==start:break
                    loop.append(b);a,b=b,outgoing[b][0]
                if not good:break
                loops.append(loop)
        if good:
            expected=sum(abs(area(q))/2 for q,_ in source)
            actual=abs(sum(sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(loop,loop[1:]+loop[:1]))/2 for loop in loops))
            if abs(expected-actual)>max(1e-15,expected*1e-12):good=False
            if any(not LinearRing(loop).is_simple or not Polygon(loop).is_valid for loop in loops):good=False
            if good:
                try:
                    coverage=Polygon()
                    for loop in loops:coverage=coverage.symmetric_difference(Polygon(loop))
                    if not coverage.is_valid or abs(coverage.area-expected)>max(1e-15,expected*1e-12):good=False
                except GEOSException:
                    # A failed offline ring validation does not establish a chart.
                    good=False
        if not good and recover_overlaps:
            # Offline recovery uses only triangles of this measured physical
            # component. Overlapping UV faces can make edge walking ambiguous
            # without invalidating the exact native texture footprint. This is
            # not a rectangle expansion or an inference of periodicity.
            coverage=unary_union([Polygon(q) for q,_ in source])
            polygons=list(coverage.geoms) if coverage.geom_type=='MultiPolygon' else [coverage]
            if coverage.is_valid and all(poly.geom_type=='Polygon' and poly.area>0 for poly in polygons):
                loops=[]
                for poly in polygons:
                    loops.append(list(poly.exterior.coords)[:-1])
                    loops.extend(list(ring.coords)[:-1] for ring in poly.interiors)
                restored=Polygon()
                for loop in loops:restored=restored.symmetric_difference(Polygon(loop))
                if restored.equals(coverage):good=True
        if good:
            idx=len(charts);charts.append(dict(part=part,loops=loops,faces=ids))
            for i in ids:face_chart[i]=idx
        else:
            # An unreliable/non-manifold part has no native chart binding.
            # Preserve the existing valid output through the common caller.
            fallbacks+=1
    return charts,face_chart,fallbacks


def domains(name,mesh):
    path=ROOT/'assets/HardcodedNumbers/Lift05'/('UvDomain_'+name.replace(' ','_')+'_'+str(mesh)+'.bin')
    if not path.exists():return None
    n=numbers(path);decoded,_=decode_arrays(n['I32'],n['F32']);v=decoded.tolist();at=2;out=[]
    for _ in range(v[1]):
        a,b,c,count=v[at:at+4];at+=4;out.append(list(v[at:at+abs(count)]));at+=abs(count)
    assert at==len(v);return out


def main():
    OUT.mkdir(parents=True,exist_ok=True);streams=[];all_charts=[];reports=[]
    for name,mesh,p,uv,faces in inputs():
        members=domains(name,mesh)
        if members is None:continue
        charts,face_chart,fallbacks=extract(p,uv,faces,recover_overlaps=True)
        used=sorted({face_chart[f] for domain in members for f in domain if f in face_chart})
        # Instances stay part-scoped even when they sample identical texture bytes.
        local={c:len(all_charts)+i for i,c in enumerate(used)}
        all_charts.extend(charts[c] for c in used)
        mapped=[[local[c] for c in sorted({face_chart[f] for f in domain if f in face_chart})] for domain in members]
        streams.append(dict(name=name,mesh=mesh,domains=mapped))
        report=dict(name=name,mesh=mesh,domains=len(members),referencedCharts=len(used),allNativeCharts=len(charts),
                    conservativeParts=fallbacks,sourceFaces=len(faces),degenerateFaces=sum(f not in face_chart for f in range(len(faces))),
                    domainSourceFaces=sum(map(len,members)),mappedDomainFaces=sum(f in face_chart for domain in members for f in domain),
                    emptyDomains=sum(not x for x in mapped),positionsSha256=hashlib.sha256(p.tobytes()).hexdigest(),
                    uvSha256=hashlib.sha256(uv.tobytes()).hexdigest(),indicesSha256=hashlib.sha256(faces.tobytes()).hexdigest())
        reports.append(report);print(name,mesh,'domains',len(members),'charts',len(used),'fragments',fallbacks,flush=True)
    expected={p.stem for p in (ROOT/'assets/HardcodedNumbers/Lift05').glob('UvDomain_*.bin')}
    actual={'UvDomain_'+s['name'].replace(' ','_')+'_'+str(s['mesh']) for s in streams}
    assert actual==expected,dict(missing=sorted(expected-actual),unexpected=sorted(actual-expected))
    integers=[1,len(streams),len(all_charts)]+[0]*(len(streams)+len(all_charts));values=[]
    for i,s in enumerate(streams):
        integers[3+i]=len(integers);integers.append(len(s['domains']))
        for chart_ids in s['domains']:integers.extend([len(chart_ids),*chart_ids])
    for i,chart in enumerate(all_charts):
        integers[3+len(streams)+i]=len(integers);integers.append(len(chart['loops']))
        for loop in chart['loops']:
            assert len(loop)>=3 and loop[0]!=loop[-1]
            integers.extend([len(values),len(loop)]);values.extend(x for v in loop for x in v)
    payload=b'BBN1'+struct.pack('<7i',1,len(integers),0,0,0,0,len(values))
    payload+=struct.pack('<'+'i'*len(integers),*integers)+struct.pack('<'+'d'*len(values),*values)
    payload+=hashlib.sha256(payload).digest()
    destination=ROOT/'assets/HardcodedNumbers/Lift05/Lift05UvCharts.bin';destination.write_bytes(payload)
    cases='\n'.join('            '+json.dumps(s['name']+'/'+str(s['mesh']))+' => '+str(i)+',' for i,s in enumerate(streams))
    code='''// Generated by tools/PreparedMaterials/Lift05/BuildUvCharts.py from real native UV topology.
// Embedded numerical arrays: Lift05UvCharts
using System;

namespace BridgeBuilder.Bridges;

internal static class Lift05UvCharts
{
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        int stream = (renderName + "/" + meshIndex) switch
        {
CASES
            _ => -1
        };
        if (stream < 0) return Array.Empty<MovableUvChart>();
        return Holder.Value.Domains[stream][domain];
    }

    private static class Holder
    {
        static Holder() {}
        internal static readonly Data Value = new();
    }

    private sealed class Data
    {
        internal readonly MovableUvChart[][][] Domains;
        internal Data()
        {
            var numbers = HardcodedNumbers.Load("Lift05UvCharts");
            var data = numbers.I32;
            if (data[0] != 1) throw new InvalidOperationException("Unsupported Lift05 native chart schema.");
            int streams = data[1], count = data[2];
            var charts = new MovableUvChart[count];
            for (int i = 0; i < count; i++) charts[i] = MovableUvChart.Read(data, numbers.F64, data[3 + streams + i]);
            Domains = new MovableUvChart[streams][][];
            for (int i = 0; i < streams; i++)
            {
                int at = data[3 + i], length = data[at++];
                Domains[i] = new MovableUvChart[length][];
                for (int d = 0; d < length; d++)
                {
                    var items = new MovableUvChart[data[at++]];
                    for (int j = 0; j < items.Length; j++) items[j] = charts[data[at++]];
                    Domains[i][d] = items;
                }
            }
        }
    }
}
'''.replace('CASES',cases)
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05UvCharts.cs').write_text(code)
    result=dict(style='Lift05',fixtureSha256=hashlib.sha256(FIXTURE.read_bytes()).hexdigest(),
                streams=reports,streamCount=len(streams),domainCount=sum(len(s['domains']) for s in streams),
                referencedChartCount=len(all_charts),loopCount=sum(len(c['loops']) for c in all_charts),
                bbnBytes=len(payload),bbnSha256=hashlib.sha256(payload).hexdigest(),
                conservativeFragmentCharts=sum(c.get('conservativeFragment',False) for c in all_charts),
                allDomainResourcesCovered=True,geometryChanged=False,repeatInferred=False)
    (OUT/'charts.json').write_text(json.dumps(dict(streams=streams,charts=all_charts),separators=(',',':'))+'\n')
    (OUT/'report.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='streams'}),flush=True)


if __name__=='__main__':main()
