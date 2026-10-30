"""Interpret the original-GPU boundary pairs using native normal/mask semantics."""
import hashlib
import json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
base=ROOT/'artifacts/material-remap/Lift04'
pairs=base/'native-boundary-pairs.json'
data=json.loads(pairs.read_text())
shader=ROOT/'artifacts/layer-decoupling/shared/outputs/native-repeat-mechanism/571-2701.asm'
text=shader.read_text()
assert 't3.xywz' in text and 'mul r8.x, r8.x, r8.z' in text
assert 't4.xywz' in text
template=json.loads((ROOT/'artifacts/layer-decoupling/shared/outputs/native/template-0.json').read_text())
defaults=dict(template['m_SavedProperties']['m_Floats'])
assert defaults['_Metallic']==defaults['_Smoothness']==1 and defaults['_AlphaCutoff']==.5
def pixels(rows): return np.array([[r[c] for c in 'rgba'] for r in rows])
def normal(p):
    xy=np.c_[p[:,0]*p[:,3],p[:,1]]*2-1
    z=np.sqrt(1-np.minimum(1,np.sum(xy*xy,axis=1)))
    v=np.c_[xy,z]
    return v/np.linalg.norm(v,axis=1)[:,None]
records=[]
for record in data['records']:
    a,b=pixels(record['original']),pixels(record['repeated']);delta=np.abs(a-b)
    r=dict(channel=record['channel'],rawComponentMaximum=dict(zip('RGBA',delta.max(axis=0).tolist())),edges=[])
    if record['channel']=='_NormalMap':
        angle=np.degrees(np.arccos(np.clip(np.sum(normal(a)*normal(b),axis=1),-1,1)))
        r.update(decode='Native shader 571: X=2*(R*A)-1, Y=2*G-1, Z=sqrt(1-saturate(X^2+Y^2)); compare normalized directions.',maximumAngleDegrees=float(angle.max()),meanAngleDegrees=float(angle.mean()))
    for edge,name in enumerate(['U min','U max','V min','V max']):
        s=slice(edge*64,(edge+1)*64);d=delta[s];item=dict(edge=name)
        if record['channel']=='_NormalMap':
            item.update(maximumAngleDegrees=float(angle[s].max()),meanAngleDegrees=float(angle[s].mean()),packedComponents=dict(zip('RGBA',d.max(axis=0).tolist())))
        elif record['channel']=='_BaseColorMap':
            item.update(maximumLinearRGB=float(d[:,:3].max()),maximumAlpha=float(d[:,3].max()),
                originalAlphaRange=[float(a[s,3].min()),float(a[s,3].max())],
                repeatedAlphaRange=[float(b[s,3].min()),float(b[s,3].max())],
                cutoffHalfClassificationChanges=int(np.count_nonzero((a[s,3]>=.5)!=(b[s,3]>=.5))))
        else:
            item.update(metallicRedMax=float(d[:,0].max()),ambientOcclusionGreenMax=float(d[:,1].max()),smoothnessAlphaMax=float(d[:,3].max()),unusedBlueMax=float(d[:,2].max()))
        r['edges'].append(item)
    records.append(r)
(base/'native-boundary-semantics.json').write_text(json.dumps(dict(gpu=data['gpu'],records=records,
    shaderSHA256=hashlib.sha256(shader.read_bytes()).hexdigest(),samplePairsSHA256=hashlib.sha256(pairs.read_bytes()).hexdigest(),
    sourceDefaultAlphaTestEnabled=bool(defaults['_AlphaCutoffEnable']),
    cutoffMeaning='Native source template defaults to opaque, cutoff disabled. Classification at 0.5 additionally checks the proposed cutout consumer; it is not a claim that the original Surface alpha-tests.',
    limitation='These 256 same-location mip-zero comparisons do not establish a perceptual tolerance or a whole-domain/mip-chain error bound.'),indent=2))
print([(r['channel'],r.get('maximumAngleDegrees'),r['rawComponentMaximum']) for r in records])
