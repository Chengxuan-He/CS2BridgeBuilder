"""Lower measured Lift03 chart loops to the common native generation interface."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import sys
import subprocess

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'tools/PreparedMaterials/Lift03'))
from ExportRecipes import numbers

INPUT = ROOT/'artifacts/prepared-materials/Lift03/native-charts'
RESOURCE = ROOT/'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03UvCharts_cs.bin'
SOURCE = ROOT/'src/BridgeBuilder/Bridges/Types/Lift03/Lift03UvCharts.cs'


def cross(a,b,c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def intersects(a,b,c,d):
    if max(a[0],b[0]) < min(c[0],d[0]) or max(c[0],d[0]) < min(a[0],b[0]): return False
    if max(a[1],b[1]) < min(c[1],d[1]) or max(c[1],d[1]) < min(a[1],b[1]): return False
    x,y,z,w = cross(a,b,c),cross(a,b,d),cross(c,d,a),cross(c,d,b)
    return ((x<=0<=y) or (y<=0<=x)) and ((z<=0<=w) or (w<=0<=z))


def loops(chart, uv):
    if not chart['topologyEdgesPaired']: return None,'non-manifold-edges'
    edges = chart['boundary']
    outgoing, incoming = {}, Counter()
    for a,b in edges:
        if a==b or a in outgoing: return None,'ambiguous-boundary-vertex'
        outgoing[a]=b;incoming[b]+=1
    if not outgoing or any(incoming[v]!=1 for v in outgoing) or set(incoming)!=set(outgoing):
        return None,'open-boundary'
    remaining=set(outgoing);result=[]
    while remaining:
        start=min(remaining);at=start;loop=[]
        while at in remaining:
            remaining.remove(at);loop.append(at);at=outgoing[at]
        if at!=start or len(loop)<3: return None,'invalid-cycle'
        points=[uv[i] for i in loop]
        area=sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(points,points[1:]+points[:1]))
        if area==0:return None,'zero-area-cycle'
        result.append(loop)
    segments=[]
    for l,loop in enumerate(result):
        for i,(a,b) in enumerate(zip(loop,loop[1:]+loop[:1])):
            segments.append((l,i,len(loop),a,b))
    for i,(la,ia,na,a,b) in enumerate(segments):
        for lb,ib,nb,c,d in segments[i+1:]:
            if la==lb and ((ia+1)%na==ib or (ib+1)%nb==ia):
                # Adjacent collinear edges may continue, but cannot double back.
                shared=set((a,b))&set((c,d))
                if len(shared)!=1:return None,'overlapping-adjacent-edges'
                p=next(iter(shared));q=b if a==p else a;r=d if c==p else c
                if cross(uv[q],uv[p],uv[r])==0 and sum((uv[q][axis]-uv[p][axis])*(uv[r][axis]-uv[p][axis]) for axis in (0,1))>0:
                    return None,'overlapping-adjacent-edges'
                continue
            if intersects(uv[a],uv[b],uv[c],uv[d]):return None,'intersecting-boundary-loops'
    return [[uv[i] for i in loop] for loop in result],None


def main():
    report=json.loads((INPUT/'report.json').read_text())
    streams=report['streams'];integers=[1,len(streams)]+[0]*len(streams);doubles=[]
    sets={};empty=len(integers);integers.append(0);sets[()]=empty
    results=[];domain_total=accepted_total=0
    for si,s in enumerate(streams):
        detail=json.loads((INPUT/(s['stream']+'.json')).read_text())
        data=numbers(INPUT/(s['stream']+'.bin'))
        uv=list(zip(data['F32'][::2],data['F32'][1::2]))
        used=sorted({chart for d in detail['domains'] for chart in d['charts']})
        offsets={};rejected={}
        for c in used:
            rings,reason=loops(detail['charts'][c],uv)
            if reason:
                rejected[str(c)]=reason;continue
            offset=len(integers);integers.append(len(rings))
            for ring in rings:
                integers.extend((len(doubles),len(ring)))
                doubles.extend(value for p in ring for value in p)
            offsets[c]=offset
        domain_sets=[]
        for i,domain in enumerate(detail['domains']):
            assert domain['domain']==i
            # A chart reference is confined to the current render/mesh stream.
            key=tuple(offsets[c] for c in domain['charts'] if c in offsets)
            if key not in sets:
                sets[key]=len(integers);integers.extend((len(key),*key))
            domain_sets.append(sets[key])
        integers[2+si]=len(integers);integers.extend((len(domain_sets),*domain_sets))
        domain_total+=len(domain_sets);accepted_total+=len(offsets)
        results.append(dict(stream=s['stream'],render=s['name'],mesh=s['mesh'],domains=len(domain_sets),
                            referencedCharts=len(used),acceptedCharts=len(offsets),rejected=rejected,
                            domainsWithCharts=sum(integers[o]>0 for o in domain_sets),
                            tableOffset=integers[2+si],nativeSourceCid=s['sourceCid']))
    payload=bytearray(b'BBN1')+struct.pack('<7i',1,len(integers),0,0,0,0,len(doubles))
    payload+=struct.pack('<'+'i'*len(integers),*integers)+struct.pack('<'+'d'*len(doubles),*doubles)
    payload+=hashlib.sha256(payload).digest()
    RESOURCE.write_bytes(payload)
    cases='\n'.join('            ("'+s['name']+'", '+str(s['mesh'])+') => '+str(i)+',' for i,s in enumerate(streams))
    code='''// Generated by tools/GeometryMetaprogram/Lift03RuntimeUvCharts.py.
// Embedded numerical arrays: Types_Lift03_Lift03UvCharts_cs
using System;
using System.Collections.Concurrent;

namespace BridgeBuilder.Bridges;

internal static class Lift03UvCharts
{
    internal static MovableUvChart[] For(string renderName,int meshIndex,int domain)
    {
        int stream=(renderName,meshIndex) switch
        {
CASES
            _ => throw new InvalidOperationException("Unrecorded Lift03 chart source: "+renderName+"/"+meshIndex)
        };
        var data=Data.Values.I32;
        int table=data[stream+2];
        if(domain<0 || domain>=data[table])throw new ArgumentOutOfRangeException(nameof(domain));
        return Data.Sets.GetOrAdd(data[table+domain+1],ReadSet);
    }
    private static MovableUvChart[] ReadSet(int offset)
    {
        var values=Data.Values;
        int count=values.I32[offset];
        if(count==0)return Array.Empty<MovableUvChart>();
        var result=new MovableUvChart[count];
        for(int i=0;i<count;i++)
            result[i]=Data.Charts.GetOrAdd(values.I32[offset+i+1],
                at=>MovableUvChart.Read(values.I32,values.F64,at));
        return result;
    }
    private static class Data
    {
        static Data()
        {
            if(Values.I32[0]!=1)throw new InvalidOperationException("Unsupported Lift03 chart schema.");
        }
        internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift03_Lift03UvCharts_cs");
        internal static readonly ConcurrentDictionary<int,MovableUvChart> Charts=new();
        internal static readonly ConcurrentDictionary<int,MovableUvChart[]> Sets=new();
    }
}
'''.replace('CASES',cases)
    SOURCE.write_text(code,encoding='utf-8')
    result=dict(style='Lift03',status='production-input-written',resource=str(RESOURCE.relative_to(ROOT)),
                resourceBytes=len(payload),resourceSha256=hashlib.sha256(payload).hexdigest(),
                source=str(SOURCE.relative_to(ROOT)),streams=results,domains=domain_total,acceptedCharts=accepted_total,
                distinctChartSets=len(sets),sourceNativeFloat32PromotedExactlyToFloat64=True,
                intervalSubdivision=False,repeatInference=False,pixelFilesCreated=0,
                actualReceiverCoverage='pending public MovableUvChart implementation')
    (INPUT/'runtime-integration.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='streams'},indent=2))
    # Recover only source-verified unions for domains whose original chart set
    # is empty. Keep this in normal regeneration so measured recovery is durable.
    preparation = ROOT/'tools/PreparedMaterials/Lift03'
    subprocess.run([sys.executable, str(preparation/'StageUnionCharts.py')], check=True)
    subprocess.run([sys.executable, str(preparation/'IntegrateUnionCharts.py'), '--apply'], check=True)


if __name__=='__main__':main()
