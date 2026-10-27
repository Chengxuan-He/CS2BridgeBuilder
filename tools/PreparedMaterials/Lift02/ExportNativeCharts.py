"""Extract actual indexed UV charts for Lift02 original-Surface reuse."""
from pathlib import Path
from collections import defaultdict,Counter
import hashlib,json,struct,sys
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays,read_bbn
OUT=ROOT/'artifacts/prepared-materials/Lift02/source-sharing'

def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def signed(ps):return sum(cross(ps[i],ps[(i+1)%len(ps)]) for i in range(len(ps)))*.5

def canonical(loop):
    # Orientation is immaterial to the public even/odd containment representation.
    seq=[tuple(map(float,p)) for p in loop]
    n=seq.index(min(seq));a=seq[n:]+seq[:n]
    seq.reverse();n=seq.index(min(seq));b=seq[n:]+seq[:n]
    return tuple(min(a,b))

def simple(loop):
    seg=[]
    for i,a in enumerate(loop):
        b=loop[(i+1)%len(loop)];seg.append((min(a[0],b[0]),max(a[0],b[0]),min(a[1],b[1]),max(a[1],b[1]),i,a,b))
    active=[]
    for s in sorted(seg):
        active=[t for t in active if t[1]>=s[0]]
        for t in active:
            if abs(s[4]-t[4]) in (1,len(loop)-1):continue
            if s[3]<t[2] or t[3]<s[2]:continue
            a,b=s[5:];c,d=t[5:]
            ab=np.subtract(b,a);cd=np.subtract(d,c)
            x=cross(ab,np.subtract(c,a));y=cross(ab,np.subtract(d,a))
            z=cross(cd,np.subtract(a,c));w=cross(cd,np.subtract(b,c))
            if x*y<=0 and z*w<=0:return False
        active.append(s)
    return True

def source_charts(faces,uv):
    valid=[];edges=defaultdict(list);parent=list(range(len(faces)))
    def find(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    for i,f in enumerate(faces):
        if signed(uv[f])==0:continue
        valid.append(i)
        for a,b in zip(f,np.roll(f,-1)):edges[tuple(sorted((int(a),int(b))))].append(i)
    for fs in edges.values():
        if len(fs)==2:parent[find(fs[0])]=find(fs[1])
    groups=defaultdict(list)
    for f in valid:groups[find(f)].append(f)
    result={};stats=Counter()
    for fs in groups.values():
        edges_count=Counter();oriented={};orient=set()
        for f in fs:
            orient.add(signed(uv[faces[f]])>0)
            for a,b in zip(faces[f],np.roll(faces[f],-1)):
                edge=tuple(sorted((int(a),int(b))));edges_count[edge]+=1;oriented[edge]=(int(a),int(b))
        nexts=defaultdict(list);incoming=Counter()
        for edge,n in edges_count.items():
            if n==1:
                a,b=oriented[edge];nexts[a].append(b);incoming[b]+=1
        good=bool(nexts) and len(orient)==1 and max(edges_count.values())<=2 and all(len(v)==1 and incoming[k]==1 for k,v in nexts.items())
        loops=[]
        if good:
            unused=set(nexts)
            while unused:
                start=min(unused);v=start;loop=[]
                while v in unused:
                    unused.remove(v);loop.append(tuple(uv[v]));v=nexts[v][0]
                if v!=start or len(loop)<3 or not simple(loop):good=False;break
                loops.append(canonical(loop))
        if good:
            key=tuple(sorted(loops));stats['indexedCharts']+=1
            for f in fs:result[f]=key
        else:
            stats['ambiguousComponents']+=1
            for f in fs:result[f]=(canonical(uv[faces[f]]),)
    stats['sourceFaces']=len(faces);stats['nondegenerateUvFaces']=len(valid)
    return result,stats

def read_domains(path):
    # read_bbn verifies checksum/layout before decoding either measured schema.
    data,_=decode_arrays(*read_bbn(path.read_bytes()))
    out=[];at=2
    for _ in range(data[1]):
        n=abs(int(data[at+3]));at+=4;out.append(data[at:at+n].tolist());at+=n
    assert at==len(data)
    return out

def main():
    ints=[];doubles=[];chart_offsets={};set_offsets={};routes=[];records=[];face_charts={}
    def chart_offset(chart):
        if chart not in chart_offsets:
            offset=len(ints);ints.append(len(chart))
            for loop in chart:
                ints.extend((len(doubles),len(loop)));doubles.extend(v for p in loop for v in p)
            chart_offsets[chart]=offset
        return chart_offsets[chart]
    def set_offset(charts):
        keys=tuple(sorted({chart_offset(c) for c in charts}))
        if keys not in set_offsets:
            offset=len(ints);ints.extend((len(keys),*keys));set_offsets[keys]=offset
        return set_offsets[keys]
    root=ROOT/'assets/HardcodedNumbers/Lift02'
    for path in sorted(root.glob('UvDomain_*.bin')):
        key=path.stem[len('UvDomain_'):];name,mesh=key.rsplit('_',1);name=name.replace('_Mesh',' Mesh');mesh=int(mesh)
        native=ROOT/'artifacts/uv-density/native'/f'{name}.npz';npz=np.load(native)
        faces=npz[f'mesh{mesh}_faces'];uv=npz[f'mesh{mesh}_uv0'][:,:2].astype(float)
        mapping,stats=source_charts(faces,uv);domains=read_domains(path)
        domain_sets=[set_offset(mapping[f] for f in fs if f in mapping) for fs in domains]
        offset=len(ints);ints.extend(domain_sets);routes.append((name,mesh,offset,len(domains)))
        records.append({'render':name,'mesh':mesh,'domainCount':len(domains),'nativeSha256':hashlib.sha256(native.read_bytes()).hexdigest(),
                        'domainSha256':hashlib.sha256(path.read_bytes()).hexdigest(),**stats})
        face_charts[(name,mesh)]=(mapping,domains)
    body=struct.pack('<8i',0x314e4242,1,len(ints),0,0,0,0,len(doubles))+np.array(ints,dtype='<i4').tobytes()+np.array(doubles,dtype='<f8').tobytes()
    body+=hashlib.sha256(body).digest();resource='Types_Lift02_Lift02UvCharts_cs'
    (root/(resource+'.bin')).write_bytes(body)
    route='\n'.join(f'            ("{name}", {mesh}) => {offset},' for name,mesh,offset,n in routes)
    cs='// Generated from native indexed UV charts by ExportNativeCharts.py.\nusing System;\nusing System.Collections.Concurrent;\nnamespace BridgeBuilder.Bridges;\ninternal static class Lift02UvCharts\n{\n    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)\n    {\n        int offset=(renderName,meshIndex) switch\n        {\nROUTES\n            _ => throw new InvalidOperationException("Unrecorded Lift02 UV chart source: "+renderName+"/"+meshIndex)\n        };\n        return Data.Sets.GetOrAdd(Data.Values.I32[offset+domain], ReadSet);\n    }\n    private static MovableUvChart[] ReadSet(int offset)\n    {\n        var values=Data.Values;\n        var result=new MovableUvChart[values.I32[offset]];\n        for(int i=0;i<result.Length;i++)\n            result[i]=Data.Charts.GetOrAdd(values.I32[offset+i+1], o=>MovableUvChart.Read(values.I32,values.F64,o));\n        return result;\n    }\n    private static class Data\n    {\n        internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift02_Lift02UvCharts_cs");\n        internal static readonly ConcurrentDictionary<int,MovableUvChart> Charts=new();\n        internal static readonly ConcurrentDictionary<int,MovableUvChart[]> Sets=new();\n    }\n}\n'.replace('ROUTES',route)
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift02/Lift02UvCharts.cs').write_text(cs)
    report={'style':'Lift02','resourceBytes':len(body),'uniqueCharts':len(chart_offsets),'uniqueChartSets':len(set_offsets),'I32':len(ints),'F64':len(doubles),
            'resourceSha256':hashlib.sha256(body).hexdigest(),'routes':[dict(render=n,mesh=m,offset=o,domains=d) for n,m,o,d in routes],'sources':records,
            'rule':'Native indexed-edge connected components, uniform UV orientation and simple closed boundary loops; ambiguous components keep individual source triangles. Never join disconnected parts by coincident UV coordinates.'}
    (OUT/'native-chart-integration.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k not in ['sources','routes']},indent=2))
if __name__=='__main__':main()
