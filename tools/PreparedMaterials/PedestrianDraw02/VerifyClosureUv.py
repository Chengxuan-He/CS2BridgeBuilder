"""Check source-only closure proof, exact flag transitions and genuine-road exclusions."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import sys
import numpy as np
from ExportRecipes import numbers
from BuildUvSampling import ROOT, BASE, NUM, overlaps
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays

def flags(path):
    v=numbers(path)['I32'];assert v[:2]==(1,9);at=2;rows=[]
    for _ in range(v[1]):
        n=v[at];at+=1;rows.append(v[at:at+n]);at+=n
    assert at==len(v);return rows

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--baseline',type=Path,required=True)
    p.add_argument('--capture',type=Path)
    p.add_argument('--expected-changes',type=int,default=30)
    p.add_argument('--output',type=Path,required=True)
    args=p.parse_args()
    proof_path=Path(__file__).with_name('ClosureUvParts.json')
    proof=json.loads(proof_path.read_text());report=json.loads((BASE/'non-tiling/classification.json').read_text())
    extra=json.loads(Path(__file__).with_name('AdditionalClosureUv.json').read_text())
    chart=json.loads((BASE/'source-consolidation/native-charts.json').read_text());native=np.load(BASE/'source-consolidation/native-charts.npz')
    before=flags(args.baseline);after=flags(NUM/'PedestrianDraw02UvSampling.bin');changed={};protected=0
    for i,rec in enumerate(report['meshes']):
        assert len(before[i])==len(after[i])==rec['domainCount']
        changed[(rec['render'],rec['mesh'])]={d for d,(a,b) in enumerate(zip(before[i],after[i])) if a!=b}
        assert all(before[i][d]==0 and after[i][d]==1 for d in changed[(rec['render'],rec['mesh'])])
        c=next(r for r in chart['records'] if (r['name'],r['mesh'])==(rec['render'],rec['mesh']));pre=c['arrayPrefix'];uv=native[pre+'_uv'];faces=native[pre+'_faces']
        declared=next((r for r in proof['meshes'] if (r['render'],r['mesh'])==(rec['render'],rec['mesh'])),None)
        closure_faces=set() if declared is None else {f for part in declared['parts'] for f in part['sourceFaces']}
        more=next((r for r in extra['meshes'] if (r['render'],r['mesh'])==(rec['render'],rec['mesh'])),None)
        if more is not None:closure_faces.update(s['face'] for s in more['samples'])
        road_faces=set() if declared is None else set(declared['roadFaces'])
        v=numbers(NUM/('UvDomain_'+rec['render'].replace(' ','_')+'_'+str(rec['mesh'])+'.bin'));raw,_=decode_arrays(v['I32'],v['F32']);at=2
        for d in range(raw[1]):
            n=abs(int(raw[at+3]));at+=4;members=set(map(int,raw[at:at+n]));at+=n
            closure=members<=closure_faces
            if closure:assert after[i][d]==1 and not members&road_faces
            if d in changed[(rec['render'],rec['mesh'])]:assert closure
            if rec['sourceDigest']=='5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219' and not closure and any(overlaps(t,report['genuineRepeatRegion']) for t in uv[faces[sorted(members)]].astype(float)):
                assert after[i][d]==0;protected+=1
    count=sum(map(len,changed.values()))
    assert count==args.expected_changes and protected==216
    observations=[] if args.capture is None else [json.loads(line) for line in gzip.open(args.capture,'rt')]
    remaining=[r for r in observations if r['status']=='remaining']
    if args.capture is not None:
        assert len(remaining)==72
        indexes={(r['render'],r['mesh']):i for i,r in enumerate(report['meshes'])}
        assert all(after[indexes[(r['render'],r['mesh'])]][r['originalDomain']]==1 for r in remaining)
    result=dict(changedFlags=count,falseToTrue=count,trueToFalse=0,unchangedRepeatDomains=protected,
        priorRemainingObservations=len(remaining),priorRemainingNowSourceRetained=len(remaining),acceptedDomains=sum(sum(v) for v in after),
        unresolvedWindows=6,changes=[dict(render=k[0],mesh=k[1],domains=sorted(v)) for k,v in changed.items() if v],
        resourceSha256=hashlib.sha256((NUM/'PedestrianDraw02UvSampling.bin').read_bytes()).hexdigest(),
        scope='Source and metadata checks; actual production replay/native rendering remain separate.')
    args.output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='changes'}))

if __name__=='__main__':main()
