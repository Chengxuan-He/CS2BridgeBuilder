"""Check every original runtime outline piece against the new local tree."""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def main():
    tree=json.loads((OUT/'continuous-local-bsp.json').read_text())
    domains=json.loads((OUT/'continuous-runtime-domains.json').read_text())['levels'][0]['domains']
    def weight(x):return 0 if abs(x)<.0001 else -1 if x<0 else 1
    rows=[]
    for delta in (-.5,0,2,8):
        planes=[]
        for line in tree['lines']:
            a=np.array(line['a']);b=np.array(line['b'])
            a[0]+=weight(a[0])*delta;b[0]+=weight(b[0])*delta
            n=np.array([a[1]-b[1],b[0]-a[0]])
            n/=np.linalg.norm(n)
            if np.dot(n,line['plane'][:2])<0:n=-n
            planes.append(np.r_[n,-n@a])
        failures=[];tested=0
        for identity,d in enumerate(domains):
            for polygon in d['outlinePieces']:
                p=np.array(polygon)
                for v in p:v[0]+=weight(v[0])*delta
                centre=p.mean(axis=0)
                for vertex in p:
                    q=centre*.8+vertex*.2
                    at=tree['root']
                    while at>=0:
                        node=tree['nodes'][at];side=planes[node['line']]@np.r_[q,1]
                        at=node['positive' if side>=0 else 'negative']
                    actual=-at-2 if at<-1 else -1
                    tested+=1
                    if actual!=identity:failures.append(dict(expected=identity,actual=actual,point=q.tolist()))
        rows.append(dict(delta=delta,samples=tested,failures=failures))
    result=dict(status='PASS' if all(not r['failures'] for r in rows) else 'FAIL',widths=rows,
        limitation='Interior source-domain/associated bone identity check; native clipping remains separate.')
    (OUT/'runtime-bsp-coverage-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(status=result['status'],failures=[len(r['failures']) for r in rows])))
    assert result['status']=='PASS'


if __name__=='__main__':main()
