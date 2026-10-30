"""Read-only finite-domain shared-edge precision and actual frame audit."""
import json
from pathlib import Path
import numpy as np
from BuildAtlasDomains import affine

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    data=json.loads((OUT/'native-atlas-domains.json').read_text())
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    rows=[]
    for level,framelevel in zip(data['levels'],frames['levels']):
        domains=[d for d in level['domains'] if not d['repeatV']]
        native={f['face']:f['tangents'] for p in framelevel['panels'] for f in p['sourceFrames']}
        edges={};pairs=set()
        for index,d in enumerate(domains):
            p=d['boundary']
            for a,b in zip(p,p[1:]+p[:1]):
                edge=tuple(sorted((tuple(a),tuple(b))))
                for other in edges.get(edge,[]):pairs.add((other,index))
                edges.setdefault(edge,[]).append(index)
        candidates=[]
        for a,b in sorted(pairs):
            first,second=domains[a],domains[b]
            if first['bone']!=second['bone']:continue
            vertices=np.array(first['sourceVertices']+second['sourceVertices'])
            u=affine(vertices[:,:2],vertices[:,2]);v=affine(vertices[:,:2],vertices[:,3])
            ts=np.array([t for d in (first,second) for face in d['sourceFaces'] for t in native[face]])
            candidates.append(dict(domains=[a,b],faces=[first['sourceFaces'],second['sourceFaces']],
                uvHalfCompatible=u['halfRoundingCompatible'] and v['halfRoundingCompatible'],
                uMaxPixel=u['maxSourcePixelError'],vMaxPixel=v['maxSourcePixelError'],
                uniqueNativeTangents=len(np.unique(ts,axis=0)),
                exactConstantFrame=bool(np.all(ts==ts[0])),bone=first['bone']))
        all_t=np.array([t for p in framelevel['panels'] for f in p['sourceFrames'] for t in f['tangents']])
        rows.append(dict(lod=level['lod'],finiteDomains=len(domains),adjacentSameBone=candidates,
            normalBits=sorted({n for p in framelevel['panels'] for n in p['normalBits']}),
            handedness=sorted(set(all_t[:,3])),uniqueNativeTangents=len(np.unique(all_t,axis=0)),
            maxCanonicalAngle=max(p['originalFrameAngleDegrees']['max'] for p in framelevel['panels']),
            maxDomainAffineAngle=max(d['tangentAffineMaxAngleDegrees'] for d in domains)))
    (OUT/'finite-merge-audit.json').write_text(json.dumps(dict(levels=rows,
        limitations=['Compatible pair fits do not establish a transitive multi-domain fit.',
            'Nonconstant source frames cannot be silently replaced by the representative-cell tangent.']),indent=2)+'\n')
    print(json.dumps([dict(lod=r['lod'],pairs=len(r['adjacentSameBone']),uvCompatible=sum(p['uvHalfCompatible'] for p in r['adjacentSameBone']),constantAndCompatible=sum(p['uvHalfCompatible'] and p['exactConstantFrame'] for p in r['adjacentSameBone']),uniqueT=r['uniqueNativeTangents'],maxAngle=r['maxCanonicalAngle']) for r in rows]))


if __name__=='__main__':main()
