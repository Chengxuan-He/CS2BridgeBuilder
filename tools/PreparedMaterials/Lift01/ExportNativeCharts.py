"""Export exact Lift01 source UV domains and edge-connected chart evidence."""
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/prepared-materials/Lift01/native-object-candidates'


def f32(x):
    return struct.unpack('<f', struct.pack('<f',x))[0]


def point(p):
    return tuple(f32(x) for x in p)


def edge(a,b):
    return tuple(sorted((a,b)))


def main():
    manifest = json.loads((OUT/'manifest.json').read_text())
    groups=[]
    for source in manifest['sources']:
        units=source['samplers']; parent=list(range(len(units)))
        polygons=[]; edges=defaultdict(list)
        def find(n):
            while parent[n] != n:
                parent[n] = parent[parent[n]]; n=parent[n]
            return n
        for i,u in enumerate(units):
            if u['triangle']:
                poly=[point(u[k]) for k in ('a','b','c')]
            else:
                x0,y0,x1,y1=map(f32,u['bounds'])
                poly=[(x0,y0),(x1,y0),(x1,y1),(x0,y1)]
            polygons.append(poly)
            for a,b in zip(poly,poly[1:]+poly[:1]):
                if a != b:
                    edges[edge(a,b)].append(i)
        for ids in edges.values():
            for i in ids[1:]:
                parent[find(i)]=find(ids[0])
        components=defaultdict(list)
        for i in range(len(units)): components[find(i)].append(i)
        charts=[]
        for ids in components.values():
            members=set(ids); vertices=[v for i in ids for v in polygons[i]]
            boundary=[list(e) for e,owners in edges.items() if len([i for i in owners if i in members]) == 1]
            charts.append(dict(keys=[units[i]['key'] for i in ids],polygons=[polygons[i] for i in ids],
                               bounds=[min(p[0] for p in vertices),min(p[1] for p in vertices),max(p[0] for p in vertices),max(p[1] for p in vertices)],
                               boundaryEdges=boundary,repeatProven=False))
        groups.append(dict(sourceCid=source['sourceCid'],objectDescriptorCid=source['outputCid'],
                           sourceDigest=source['sourceSha256'],samplerCount=len(units),charts=charts))
    repeat_file=ROOT/'tools/PreparedMaterials/Lift01/repeat-recipes.json'
    repeats=json.loads(repeat_file.read_text())
    result=dict(style='Lift01',schema='BridgeBuilder/NativeAtlasCharts/1',sources=groups,
                source='Existing production source UV domains; connectivity uses identical Float32 shared edges without tolerance',
                meaning='Edge-connected recorded sampler subsets, not proof of complete authored mesh chart topology or periodicity',
                nativeTextureSettings='The six network original textures use Clamp U/V, Trilinear, anisotropy 16; do not substitute whole-texture Repeat',
                knownRoadRepeatRegions=repeats['units'],roadRepeatInventorySha256=hashlib.sha256(repeat_file.read_bytes()).hexdigest(),
                roadRepeatMeaning='Measured road repeating region; independent of the network structure charts. Original region-local sampling still requires shared native implementation.',
                generatedGeometryChanged=False)
    (OUT/'uv-charts.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(samplers=sum(g['samplerCount'] for g in groups),charts=sum(len(g['charts']) for g in groups),
                         boundaryEdges=sum(len(c['boundaryEdges']) for g in groups for c in g['charts']),
                         roadRepeatUnits=len(repeats['units']),roadRepeatAliases=len(repeats['units'][0]['regions']))))


if __name__ == '__main__': main()
