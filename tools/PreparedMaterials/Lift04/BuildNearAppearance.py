"""Pack exact authored road reset stations and complete cells, never source triangles."""
import hashlib,json,struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
base=ROOT/'artifacts/material-remap/Lift04'
audit=json.loads((base/'near-domain-audit.json').read_text())
original=json.loads((ROOT/'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
uvmin=np.array([.4697265625,.8251953125]);extent=np.array([.15576171875,.15625])
ints=[2,2,50,24];floats=[];checks=[];allstrips=[]
for level in audit['levels']:
    lod=level['lod'];faces={f['face']:f for f in original['lods'][lod]['faces']};buckets={}
    for tile in level['domains']:
        if tile['role']=='generated-road':buckets.setdefault((tile['positionMin'][2],tile['positionMax'][2]),[]).append(tile)
    assert len(buckets)==50
    covered=[];strips=[];maxerror=0
    for (zmin,zmax),pair in sorted(buckets.items()):
        assert len(pair)==2
        pair.sort(key=lambda t:t['positionMin'][0])
        points=[set(tuple(p) for f in t['faces'] for p in faces[f]['positions']) for t in pair]
        shared=sorted(points[0]&points[1],key=lambda p:p[2]);assert len(shared)==2
        assert shared[0][2]==zmin and shared[1][2]==zmax
        slope=(shared[1][0]-shared[0][0])/(zmax-zmin);offset=shared[0][0]-slope*zmin
        center=np.array([slope,offset],dtype=np.float32)
        assert np.max(np.abs(np.array([zmin,zmax])*center[0]+center[1]-np.array(shared)[:,0]))<1e-10
        floats.extend([zmin,zmax,*center]);cells=[]
        for side,tile in enumerate(pair):
            covered+=tile['faces'];ps=np.concatenate([faces[f]['positions'] for f in tile['faces']]);uv=np.concatenate([faces[f]['uv0'] for f in tile['faces']])
            q=(uv-uvmin)/extent
            low=np.unique(q[ps[:,2]==zmin,0]);high=np.unique(q[ps[:,2]==zmax,0]);assert len(low)==len(high)==1
            u=np.array([0,(high[0]-low[0])/(zmax-zmin),low[0]-(high[0]-low[0])*zmin/(zmax-zmin)],dtype=np.float32)
            edgeq=np.unique([q[i,1] for i,p in enumerate(ps) if tuple(p) in shared]);assert len(edgeq)==1
            outer=tile['positionMin'][0] if side==0 else tile['positionMax'][0]
            outerq=np.unique(q[ps[:,0]==outer,1]);assert len(outerq)==1
            centermean=(shared[0][0]+shared[1][0])/2
            gradient=(outerq[0]-edgeq[0])/(outer-centermean)
            v=np.array([gradient,-gradient*center[0],edgeq[0]-gradient*center[1]],dtype=np.float32)
            x=np.c_[ps[:,[0,2]],np.ones(len(ps))].astype(np.float32)
            predicted=np.c_[x@u,x@v]*extent+uvmin
            stored=uv.astype(np.float16)
            assert np.array_equal(predicted.astype(np.float16),stored),(lod,tile['faces'],predicted,uv)
            absolute=np.c_[u*extent[0],v*extent[1]].astype(np.float32);absolute[2]+=uvmin.astype(np.float32)
            packed_projection=x@absolute
            assert np.array_equal(packed_projection.astype(np.float16),stored),(lod,tile['faces'],'absolute atlas coefficients')
            err=float(np.max(np.abs(predicted-uv))*4096);maxerror=max(maxerror,err)
            tangent=tile['tangentDecoded'];assert len(tangent)==1
            floats.extend([*u,*v,*tangent[0]])
            cells.append(dict(side=side,faces=tile['faces'],u=u.tolist(),v=v.tolist(),tangent=tangent[0],maximumPixelError=err,repeatU=False,repeatV=True))
        strips.append(dict(zmin=zmin,zmax=zmax,centerLine=center.tolist(),sharedOriginalEdge=shared,cells=cells))
    expected=[f for t in level['domains'] if t['role']=='generated-road' for f in t['faces']]
    assert len(covered)==len(set(covered))==200 and set(covered)==set(expected)
    union=[]
    for s in strips:
        if union and s['zmin']==union[-1][1]:union[-1]=(union[-1][0],s['zmax'])
        else:
            assert not union or s['zmin']>union[-1][1]
            union.append((s['zmin'],s['zmax']))
    assert union==[(-96.,-80.),(-79.849609375,79.849609375),(80.,96.)]
    checks.append(dict(lod=lod,physicalStrips=50,completeCells=100,roadFaces=200,retainedLowerFaces=184,maximumPixelError=maxerror,originalUvHalfBitsPreserved=True,authoredLongitudinalSeamDisplacementMetres=0,coverageBySide=union))
    allstrips.append(dict(lod=lod,strips=strips))
payload=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(floats),0)+np.asarray(ints,dtype='<i4').tobytes()+np.asarray(floats,dtype='<f4').tobytes()
target=ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_NearAppearance.bin';target.write_bytes(payload+hashlib.sha256(payload).digest())
(base/'near-interface-verification.json').write_text(json.dumps(dict(checks=checks,resourceSHA256=hashlib.sha256(target.read_bytes()).hexdigest(),materialPayloadStatus='Original atlas only; exact authored longitudinal road reset stations, finite U per complete source cell, independently repeating V. Native validation pending.'),indent=2))
(base/'near-exact-cells.json').write_text(json.dumps(dict(levels=allstrips),indent=2))
print(checks)
