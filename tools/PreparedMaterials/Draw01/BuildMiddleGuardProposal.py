"""Explicit affine finite-U guard geometry for the measured middle unit."""
import json,hashlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
base=json.loads((OUT/'middle-eight-meter-panels.json').read_text())
panels=[];guards=[]
for i,p in enumerate(base['panels']):
    q=np.array(p['target']).reshape(3,2);q[:,0]=.25+.5*q[:,0]
    uv=np.array(p['source']).reshape(3,2)
    affine=np.linalg.solve(np.column_stack([q,np.ones(3)]),uv)
    panels.append({**p,'target':q.reshape(-1).tolist(),'sourceAffineRows':affine.T.tolist()})
    for a,b in [(0,1),(1,2),(2,0)]:
        if q[a,0]!=q[b,0] or q[a,0] not in (.25,.75):continue
        outside=0 if q[a,0]==.25 else 1
        oa=q[a].copy();oa[0]=outside
        ob=q[b].copy();ob[0]=outside
        polygon=[q[a],q[b],ob,oa]
        triangles=[]
        for indices in [(0,1,2),(0,2,3)]:
            t=np.array([polygon[j] for j in indices]);s=np.column_stack([t,np.ones(3)])@affine
            triangles.append({'target':t.reshape(-1).tolist(),'source':s.reshape(-1).tolist(),'sharedParentVertices':sum(any(np.array_equal(x,v) for v in q) for x in t)})
        guards.append({'adjacentPanel':i,'parentSourceFace':p['sourceFace'],'originalExteriorEdge':[q[a].tolist(),q[b].tolist()], 'polygon':[x.tolist() for x in polygon],'triangles':triangles,'sourceAffineRows':affine.T.tolist(),'requestedSourceUvOutside01':any(v<0 or v>1 for t in triangles for v in t['source'])})
samplers=json.loads((OUT/'actual-source-samplers.json').read_text())
assert all(c['wrapU']=='Clamp' and c['wrapV']=='Clamp' for c in samplers['channels'])
result={'status':'EXPLICIT_GUARD_PROPOSAL_NOT_BAKED','sourceSamplerEvidence':'actual-source-samplers.json','sourceSamplerEvidenceSha256':hashlib.sha256((OUT/'actual-source-samplers.json').read_bytes()).hexdigest(),'producerPanelSpaceContentMin':[.25,0],'producerPanelSpaceContentMax':[.75,1],'currentProducerTextureSpaceContentMin':[.375,0],'currentProducerTextureSpaceContentMax':[.625,1],'producerCoordinateExplanation':'Current finite-U shader uses q=saturate(2*textureU-.5); authoring content qU .25..75 therefore maps to textureU .375..625. Do not consume generic .25..75 as these content view bounds.','panels':panels,'guards':guards,'validationIssue':'Each rectangle shares one complete measured parent outer edge, but its second triangulation triangle has only one original parent vertex. Current per-triangle guard validator requires two. Validate the whole guard polygon provenance and inherited parent affine, rather than treating the second triangle as a separate unsupported region.','nativeSamplerPolicy':'Only explicit guards may extrapolate UV outside 0..1. Preserve source affine/derivatives and actual loaded Clamp sampler; do not clamp UV before SampleGrad/SampleLevel. Content remains inside original source coordinates.','runtimeCuts':0,'qualityAccepted':False}
(OUT/'middle-finite-guard-proposal.json').write_text(json.dumps(result,indent=2));print(len(panels),'content panels',len(guards),'guard rectangles',sum(g['requestedSourceUvOutside01'] for g in guards),'outside-source guards')
