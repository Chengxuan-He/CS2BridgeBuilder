"""Check local authored-line BSP coverage and bone ownership before integration."""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    plan=json.loads((OUT/'local-atlas-bsp.json').read_text())
    domains=json.loads((OUT/'native-atlas-domains.json').read_text())['levels'][0]['domains']
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    triangles={t['face']:t for p in recipe['levels'][0]['panels'] for t in p['triangles']}
    def weight(x):return 0 if abs(x)<.0001 else (-1 if x<0 else 1)
    rows=[]
    for delta in (-.5,0.,2.,8.):
        planes=[]
        for source in plan['lines']:
            a=np.array(source['a']);b=np.array(source['b'])
            a[0]+=weight(a[0])*delta;b[0]+=weight(b[0])*delta
            normal=np.array([-(b-a)[1],(b-a)[0]])
            normal/=np.linalg.norm(normal)
            if np.dot(normal,source['plane'][:2])<0:normal=-normal
            planes.append(np.r_[normal,-np.dot(normal,a)])
        def classify(p):
            at=plan['root']
            while at>=0:
                node=plan['nodes'][at]
                d=np.dot(planes[node['line']],np.r_[p,1])
                at=node['positive' if d>=0 else 'negative']
            return -1 if at==-1 else -at-2
        errors=[];tested=0
        for domain_index,d in enumerate(domains):
            for face in d['sourceFaces']:
                p=np.asarray(triangles[face]['position'])[:,[0,2]].copy()
                for v in p:v[0]+=weight(v[0])*delta
                for bary in ([1/3]*3,[.6,.2,.2],[.2,.6,.2],[.2,.2,.6]):
                    point=np.asarray(bary)@p;actual=classify(point)
                    if actual!=domain_index:errors.append(dict(face=face,expected=domain_index,actual=actual,point=point.tolist()))
                    tested+=1
        rows.append(dict(halfWidthDelta=delta,interiorSamples=tested,wrongDomains=errors))
    result=dict(status='PASS' if all(not r['wrongDomains'] for r in rows) else 'FAIL',widths=rows,
        limits=['Source domain interior coverage only; native Float32 clipping/shape/normal/complexity checks remain mandatory.'])
    (OUT/'local-atlas-bsp-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(status=result['status'],wrong=[len(r['wrongDomains']) for r in rows])))


if __name__=='__main__':main()
