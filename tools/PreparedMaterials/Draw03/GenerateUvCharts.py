"""Create Draw03's recipe-indexed native chart loops and lazy runtime catalogue."""
from pathlib import Path
import collections
import hashlib
import json
import struct
import sys
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays
OUT=ROOT/'artifacts/prepared-materials/Draw03/native-charts'


def arrays(blob):
    assert hashlib.sha256(blob[:-32]).digest()==blob[-32:]
    counts=struct.unpack_from('<6i',blob,8)
    return np.frombuffer(blob,'<i4',counts[0],32),np.frombuffer(blob,'<f4',counts[4],32+4*counts[0])


def loops(boundary,positions,uv):
    # Follow physical endpoint identity, retaining distinct coincident UV loops.
    # No nearest-point welding, bounding rectangle or cross-part union.
    def key(index):return tuple(np.r_[positions[index],uv[index]].tolist())
    edges=collections.defaultdict(list)
    for a,b in np.asarray(boundary).reshape(-1,2):edges[key(a)].append((int(a),int(b)))
    result=[]
    while edges:
        start=min(edges);current=start;loop=[]
        while True:
            options=edges[current]
            a,b=options.pop()
            if not options:del edges[current]
            loop.append(uv[a].astype(float).tolist())
            current=key(b)
            if current==start:break
            if current not in edges:raise ValueError('Open native chart boundary')
        # Equal UVs on a physical boundary may be consecutive; removing them
        # changes no polygon or native coordinate precision.
        clean=[]
        for p in loop:
            if not clean or p!=clean[-1]:clean.append(p)
        if len(clean)>1 and clean[0]==clean[-1]:clean.pop()
        changed=True
        while changed and len(clean)>2:
            changed=False
            for i in range(len(clean)):
                a,b,c=np.asarray((clean[i-1],clean[i],clean[(i+1)%len(clean)]))
                e,f=b-a,c-b
                if e[0]*f[1]-e[1]*f[0]==0 and np.dot(e,f)>=0:
                    clean.pop(i);changed=True;break
        # Rank-one authored UV domains are real line segments, not constant UV.
        if len(clean)>=2:result.append(clean)
    return result


def run():
    manifest=json.loads((OUT/'manifest.json').read_bytes())
    reports=[];total=0
    for mesh in manifest['meshes']:
        recipe_name='UvDomain_'+mesh['render'].replace(' ','_')+'_'+str(mesh['mesh'])
        recipe=ROOT/'assets/HardcodedNumbers/Draw03'/(recipe_name+'.bin')
        if not recipe.exists():continue
        ri,_=decode_arrays(*arrays(recipe.read_bytes()));assert ri[0]==-2
        domains=[];at=2
        for _ in range(int(ri[1])):
            count=abs(int(ri[at+3]));domains.append(ri[at+4:at+4+count]);at+=4+count
        ci,_=arrays((OUT/mesh['file']).read_bytes());nf,nc=map(int,ci[1:3])
        fc=ci[3:3+nf];at=3+nf;boundaries=[]
        for _ in range(nc):
            count=int(ci[at+1])*2;boundaries.append(ci[at+2:at+2+count]);at+=2+count
        wanted=sorted({int(fc[f]) for d in domains for f in d})
        source=ROOT/'artifacts/uv-density/native'/(mesh['render']+'.npz')
        if not source.exists():source=ROOT/'artifacts/uv-density/native-additional'/(mesh['render']+'.npz')
        native=np.load(source);prefix=f"mesh{mesh['mesh']}_"
        p,uv=native[prefix+'positions'],native[prefix+'uv0'][:,:2]
        native_loops={chart:loops(boundaries[chart],p,uv) for chart in wanted}
        ids={chart:index for index,chart in enumerate(wanted)}
        mapped=[sorted({ids[int(fc[f])] for f in domain}) for domain in domains]
        ints=[len(domains),len(wanted)]+[0]*(len(domains)+len(wanted));doubles=[]
        for di,charts in enumerate(mapped):
            ints[2+di]=len(ints);ints.extend((len(charts),*charts))
        empty=[]
        for chart,index in ids.items():
            ints[2+len(domains)+index]=len(ints)
            rings=native_loops[chart];ints.append(len(rings))
            if not rings:empty.append(chart)
            for ring in rings:
                ints.extend((len(doubles),len(ring)))
                doubles.extend(v for point in ring for v in point)
        blob=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,0,len(doubles))
        blob+=np.asarray(ints,dtype='<i4').tobytes()+np.asarray(doubles,dtype='<f8').tobytes()
        blob+=hashlib.sha256(blob).digest()
        name=recipe_name.replace('UvDomain_','NativeUvCharts_')
        (OUT/(name+'.bin')).write_bytes(blob)
        reports.append({'render':mesh['render'],'mesh':mesh['mesh'],'resource':name,'domains':len(domains),
                        'charts':len(wanted),'loopCount':sum(len(v) for v in native_loops.values()),
                        'emptyCharts':empty,'bytes':len(blob),'sha256':hashlib.sha256(blob).hexdigest()})
        total+=len(blob)
    report={'schema':'Draw03NativeChartCatalogue/1','resources':reports,'totalBytes':total,
            'emptyChartCount':sum(len(x['emptyCharts']) for x in reports)}
    (OUT/'catalogue.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if '--publish' in sys.argv:
        for item in reports:
            name=item['resource']+'.bin'
            (ROOT/'assets/HardcodedNumbers/Draw03'/name).write_bytes((OUT/name).read_bytes())
        cases='\n'.join(f'            ("{r["render"]}", {r["mesh"]}) => "{r["resource"]}",' for r in reports)
        source='''// Generated by tools/PreparedMaterials/Draw03/GenerateUvCharts.py.
// Embedded numerical arrays: measured native UV charts
// Real native chart boundaries; the UV density-frame catalogue is separate.
using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
internal static class Draw03UvCharts
{
    private static readonly Dictionary<string, Entry> Loaded = new();
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        string? resource = (renderName, meshIndex) switch
        {
CASES
            _ => null
        };
        if (resource == null) return Array.Empty<MovableUvChart>();
        lock (Loaded)
        {
            if (!Loaded.TryGetValue(resource, out var entry))
                Loaded.Add(resource, entry = new Entry(resource));
            return entry.Domains[domain];
        }
    }
    private sealed class Entry
    {
        // The source arrays and chart instances remain owned by this lazy entry.
        private readonly HardcodedNumbers _data;
        internal readonly MovableUvChart[][] Domains;
        internal Entry(string resource)
        {
            _data = HardcodedNumbers.Load(resource);
            var integers = _data.I32;
            int domains = integers[0], count = integers[1];
            var charts = new MovableUvChart[count];
            for (int chart = 0; chart < count; chart++)
                charts[chart] = MovableUvChart.Read(integers, _data.F64, integers[2 + domains + chart]);
            Domains = new MovableUvChart[domains][];
            for (int domain = 0; domain < domains; domain++)
            {
                int at = integers[2 + domain];
                var selected = new MovableUvChart[integers[at++]];
                for (int i = 0; i < selected.Length; i++) selected[i] = charts[integers[at++]];
                Domains[domain] = selected;
            }
        }
    }
}
'''.replace('CASES',cases)
        (ROOT/'src/BridgeBuilder/Bridges/Types/Draw03/Draw03UvCharts.cs').write_text(source,encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='resources'},indent=2))


if __name__=='__main__':run()
