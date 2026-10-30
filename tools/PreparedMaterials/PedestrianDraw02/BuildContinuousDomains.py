"""Replace repeated station domains with four continuous native-unit uses."""
import json
from pathlib import Path
from copy import deepcopy
import numpy as np
from BuildAtlasDomains import affine

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'

def hull(points):
    points=sorted(set(map(tuple,points)))
    def half(sequence):
        result=[]
        for p in sequence:
            while len(result)>1:
                a,b=result[-2:]
                if (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])>0:break
                result.pop()
            result.append(p)
        return result
    return [list(p) for p in half(points)[:-1]+half(points[::-1])[:-1]]


def main():
    original=json.loads((OUT/'native-atlas-domains.json').read_text())
    replacement=json.loads((OUT/'repeat-replacement-plan.json').read_text())
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    merge_reports=[]
    for level,plan,source in zip(original['levels'],replacement['levels'],recipe['levels']):
        finite=[d for d in level['domains'] if not d['repeatV']]
        native={f['face']:f['tangents'] for p in frames['levels'][level['lod']]['panels'] for f in p['sourceFrames']}
        groups=[[i] for i in range(len(finite))]
        def edges(d):
            p=d['boundary']
            return {tuple(sorted((tuple(a),tuple(b)))) for a,b in zip(p,p[1:]+p[:1])}
        def width(d):
            xs=sorted({p[0] for p in d['boundary']})
            weights={0 if abs(x)<.0001 else (-1 if x<0 else 1) for x in xs}
            return ('translate',next(iter(weights))) if len(weights)==1 else ('stretch',*xs)
        accepted=[]
        changed=True
        while changed:
            changed=False
            for a in range(len(groups)):
                for b in range(a+1,len(groups)):
                    ids=groups[a]+groups[b];parts=[finite[i] for i in ids]
                    if len({p['bone'] for p in parts})!=1 or len({width(p) for p in parts})!=1:continue
                    if not any(edges(finite[i])&edges(finite[j]) for i in groups[a] for j in groups[b]):continue
                    ts=np.array([t for p in parts for face in p['sourceFaces'] for t in native[face]])
                    if not np.all(ts==ts[0]):continue
                    vertices=np.unique(np.array([v for p in parts for v in p['sourceVertices']]),axis=0)
                    u=affine(vertices[:,:2],vertices[:,2]);v=affine(vertices[:,:2],vertices[:,3])
                    if not(u['halfRoundingCompatible'] and v['halfRoundingCompatible']):continue
                    accepted.append(dict(members=ids,u=u,v=v,widthAffine=width(parts[0])))
                    groups[a]=ids;groups.pop(b);changed=True;break
                if changed:break
        merged=[]
        for ids in groups:
            d=deepcopy(finite[ids[0]])
            if len(ids)>1:
                vertices=np.unique(np.array([v for i in ids for v in finite[i]['sourceVertices']]),axis=0)
                d.update(outlinePieces=[finite[i]['boundary'] for i in ids],
                    boundary=hull(vertices[:,:2]),sourceVertices=vertices.tolist(),
                    sourceFaces=[face for i in ids for face in finite[i]['sourceFaces']],
                    u=affine(vertices[:,:2],vertices[:,2]),v=affine(vertices[:,:2],vertices[:,3]),
                    sourceUvMin=vertices[:,2:].min(axis=0).tolist(),sourceUvMax=vertices[:,2:].max(axis=0).tolist())
            merged.append(d)
        merge_reports.append(dict(lod=level['lod'],before=len(finite),after=len(merged),acceptedWholeGroupFits=accepted,
            variableFrameDomains=[i for i,p in enumerate(finite) if len({tuple(t) for face in p['sourceFaces'] for t in native[face]})>1]))
        finite=merged
        for group in plan['repeatDomains']:
            panels=[p for p in source['panels'] if p['firstFace'] in group['sourcePanels']]
            domains=[d for d in level['domains'] if d['repeatV'] and d['panel'] in group['sourcePanels']]
            points=np.array([v for d in domains for v in d['boundary']])
            boundary=hull(points)
            positive=group['bone']==1
            native_right=group['rightHalf'] if positive else not group['rightHalf']
            exemplar=next(p for p in recipe['levels'][0]['panels'] if p['firstFace']==(8556 if native_right else 11384))
            lo=exemplar['physicalMin'];size=exemplar['physicalSize'];direction=1 if positive else -1
            old=next(d for d in level['domains'] if d['repeatV'] and d['bone']==group['bone'])
            d=deepcopy(old)
            d.update(boundary=boundary,outlinePieces=[part['boundary'] for part in domains],preparedUnitIndex=1 if native_right else 2,
                sourceUvMin=[.25,0],sourceUvMax=[.75,1],
                u=dict(coefficients=[-.5*direction/size[0],0,.25+.5*(1+lo[0]/size[0])],halfRoundingCompatible=True),
                v=dict(coefficients=[0,-direction/size[1],1+lo[1]/size[1]],halfRoundingCompatible=True),
                tangentCoefficients=[[0,0,0],[0,0,0],[-direction,0,0]],tangentHandedness=-1)
            finite.append(d)
        level['domains']=finite
    (OUT/'continuous-atlas-domains.json').write_text(json.dumps(original,indent=2)+'\n')
    (OUT/'finite-whole-group-merges.json').write_text(json.dumps(merge_reports,indent=2)+'\n')
    print('Continuous domains:',[len(l['domains']) for l in original['levels']])


if __name__=='__main__':main()
