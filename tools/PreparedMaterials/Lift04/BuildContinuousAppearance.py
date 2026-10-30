"""Pack minimum evidenced sampling domains and offline unit content intervals."""
import hashlib
import json
import struct
from pathlib import Path
import numpy as np
import sys

ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'artifacts/material-remap/Lift04'
sys.path.insert(0,str(ROOT/'artifacts/material-remap/environments/Lift04/python-deps'))
from scipy.optimize import linprog
original=json.loads((ROOT/'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
audit=json.loads((BASE/'near-domain-audit.json').read_text())
levels=json.loads((BASE/'near-domain-groups.json').read_text())['levels']
rows=[];checks=[]
for level in levels:
    groups=sorted(level['domains'],key=lambda g:(g['min'][2],g['min'][0]))
    assert len(groups)==20
    for i in range(0,20,2):
        left,right=groups[i:i+2]
        assert left['min'][2]==right['min'][2] and left['max'][2]==right['max'][2]
        assert left['tangent']==right['tangent'] and left['repeatLongitudinal']==right['repeatLongitudinal']
        faces={f['face']:f for f in original['lods'][level['lod']]['faces']}
        x=[];y=[];radius=[]
        midpoint=(left['min'][2]+left['max'][2])/2
        phase_right=round(np.dot(right['v'],[0,midpoint,1])-np.dot(left['v'],[0,midpoint,1]))
        for side,group in enumerate((left,right)):
            members=sorted([t for t in audit['levels'][level['lod']]['domains'] if set(t['faces']).issubset(group['faces'])],key=lambda t:t['positionMin'][2])
            for phase,tile in enumerate(members):
                for face in tile['faces']:
                    f=faces[face];p=np.array(f['positions']);uv=np.array(f['uv0'],dtype=np.float16)
                    lower=(uv.astype(float)+np.nextafter(uv,np.float16(-np.inf)).astype(float))/2
                    upper=(uv.astype(float)+np.nextafter(uv,np.float16(np.inf)).astype(float))/2
                    q=((lower+upper)/2-[.4697265625,.8251953125])/[.15576171875,.15625]
                    q[:,0]+=np.sign(tile['affineXZ'][1][0])*phase
                    q[:,1]-=side*phase_right
                    x.extend(np.c_[p[:,[0,2]],np.ones(3)]);y.extend(q);radius.extend((upper-lower)/2/[.15576171875,.15625])
        x=np.array(x);y=np.array(y);radius=np.array(radius);coeff=[];budgets=[];errors=[]
        for axis in range(2):
            result=linprog([0,0,0,1],A_ub=np.r_[np.c_[x,-radius[:,axis]],np.c_[-x,-radius[:,axis]]],b_ub=np.r_[y[:,axis],-y[:,axis]],bounds=[(None,None)]*3+[(0,None)],method='highs')
            assert result.success
            c=result.x[:3].astype(np.float32)
            used=np.max(np.abs(x.astype(np.float32)@c-y[:,axis])/radius[:,axis]);assert used<1,(level['lod'],i,axis,used)
            budgets.append(float(used));errors.append(float(np.max(np.abs(x.astype(np.float32)@c-y[:,axis]))*[638,640][axis]))
            coeff.extend(c)
        rows.extend([left['min'][2],left['max'][2],int(left['repeatLongitudinal']),*coeff,*left['tangent']])
        checks.append(dict(lod=level['lod'],domain=i//2,sourceFaces=left['faces']+right['faces'],
            removedRightIntegerPhase=phase_right,sourcePixelError=errors,sourceHalfUlpFraction=budgets,
            identicalNativeTangent=True,centrePartitionRequired=False))
ints=[1,2,10,13]
payload=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(rows),0)+np.asarray(ints,dtype='<i4').tobytes()+np.asarray(rows,dtype='<f4').tobytes()
target=ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_ContinuousAppearance.bin'
target.write_bytes(payload+hashlib.sha256(payload).digest())
(BASE/'offline-road-unit/joint-centre-phase-proof.json').write_text(json.dumps(dict(checks=checks,
    conclusion='Both sides jointly fit source quantization after one integer V phase lift; same native tangent. No physical centre edge is moved: the reset-only centre partition is removed.'),indent=2))
print(target,hashlib.sha256(target.read_bytes()).hexdigest())
