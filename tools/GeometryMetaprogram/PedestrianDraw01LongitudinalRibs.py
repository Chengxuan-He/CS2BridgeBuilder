"""Measure exact native underside rib membership and emit its immutable BBN1 map.

Usage: script RESEARCH_DIRECTORY REPOSITORY_ROOT OUTPUT_DIRECTORY
Seven original internal rib centres are -3..3 m. Quarter-width shifts preserve
uniform internal intervals and the native unequal outer-edge clearance. Far LOD
uses a merged shell with no independent ribs; no near-detail geometry is invented.
"""
from pathlib import Path
import hashlib, json, struct, sys
import numpy as np

r, root, output = map(Path, sys.argv[1:])
output.mkdir(parents=True, exist_ok=True)
groups = ((22,23,24,25,37,38,39,226,227,228,229,235,236,237),
          (22,23,24,25,37,38,39,219,220,221,222,228,229,230), ())
hashes = ('491d8dae6420e0b5b00fbf0014ff74a8aff44c45a1bd48ab40708978888d7f05',
          '0b0544aff8720327910927fda2f398ce78c8e3e3d2a1262de8204acdcac34053',
          'd514d88bf5fd4d8453a81578b36f099887cf562ad55255d4ca97267add15952b')
ints=[]; floats=[]; rows=[]; fixtures=[]
for lod, ids in enumerate(groups):
    name='PedestrianDrawBridge01'+(f'_LOD{lod}' if lod else '')+' Mesh'
    assert hashlib.sha256((r/'movable-skin'/(name+'.Geometry')).read_bytes()).hexdigest()==hashes[lod]
    d=np.load(r/f'pedestrian01-main-{lod}-0.npz'); v=d['positions']; f=d['indices']; c=d['component']
    native_vertices=[]; native_faces=[]; bones=[]; bone=''
    for line in (r/'movable-skin'/(name+'.mesh-0.obj')).read_text().splitlines():
        words=line.split()
        if not words: continue
        if words[0]=='v': native_vertices.append(list(map(float,words[1:4])))
        elif words[0]=='g': bone=words[1]
        elif words[0]=='f':
            native_faces.append([int(x.split('/')[0])-1 for x in words[1:]])
            bones.append(bone)
    assert np.array_equal(v,native_vertices) and np.array_equal(f,native_faces)
    start=len(ints); ints.extend([len(v),0]); ranges=0
    members=[]
    for comp in ids:
        indices=np.flatnonzero(c==comp); p=v[indices]; faces=(c[f]==comp).all(1)
        assert len(indices)==120 and faces.sum()==60 and np.all((c[f]==comp).any(1)==faces)
        assert set(np.array(bones)[faces])=={'bone_1' if p[:,2].min()>0 else 'bone_2'}
        centre=float((p[:,0].min()+p[:,0].max())/2)
        assert centre in (-3,-2,-1,0,1,2,3)
        runs=np.split(indices,np.flatnonzero(np.diff(indices)!=1)+1)
        for run in runs:
            ints.extend([int(run[0]),int(run[-1]),len(floats)])
            ranges+=1
        floats.append(centre/4)
        members.append(dict(component=comp,vertices=indices.tolist(),centre=centre,width=float(np.ptp(p[:,0])),faces=np.flatnonzero(faces).tolist()))
    # Exact source faces at far moving interior have only the merged shell.
    if lod==2:
        x=np.unique(v[(np.abs(v[:,2])>1)&(np.abs(v[:,2])<50),0])
        assert not np.any(np.abs(x)<3.68)
    ints[start+1]=ranges
    rows.append(dict(lod=lod,source=name,sha256=hashes[lod],start=start,members=members))
    fixtures.append(dict(lod=lod,positions=v.tolist(),indices=f.reshape(-1).tolist(),members=members))
header=[len(ints),0,0,0,len(floats),0]
data=struct.pack('<Ii6i',0x314e4242,1,*header)+np.array(ints,dtype='<i4').tobytes()+np.array(floats,dtype='<f4').tobytes()
resource=root/'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_PedestrianDraw01LongitudinalRibs_cs.bin'
resource.write_bytes(data+hashlib.sha256(data).digest())
(output/'membership.json').write_text(json.dumps(rows,indent=2))
(output/'fixture.json').write_text(json.dumps(fixtures))
print('Measured 28 complete ribs, 3360 vertices, 1680 faces across near/middle; far merged shell retained.')
