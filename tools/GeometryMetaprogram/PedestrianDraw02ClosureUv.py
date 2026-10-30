"""Verify authored PedestrianDraw02 end closures independently of receiver scenarios.

Read-only native decoding. Reviewed highest-detail face pairs identify the six
road-shell closures; LOD1 membership is recovered by exact source XYZ+UV tuples.
The runtime classifier consumes only the resulting original-domain membership.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import sys
import numpy as np
from UvDensityAudit import NativeGeometry

ROOT=Path(__file__).resolve().parents[2]
# Reviewed native shell faces, not domain IDs or changed-output observations.
NEAR_PARTS={
    'fixed-negative-end': [22471,22472,23452,23453],
    'moving-negative-end': [23586,23587,26414,26415],
    'moving-negative-lip': [24463,24464,27263,27264],
    'fixed-positive-end': [7439,7440,8420,8421],
    'moving-positive-end': [8554,8555,11382,11383],
    'moving-positive-lip': [9431,9432,12231,12232],
}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def triangle_key(positions,uv,face):
    return tuple(sorted(tuple(map(float,(*positions[i],*uv[i]))) for i in face))

def edges(positions,face):
    points=[tuple(map(float,positions[i])) for i in face]
    return [tuple(sorted((a,b))) for a,b in zip(points,points[1:]+points[:1])]

def measure(research,plugin):
    membership=research/'pedestrian02-complete-road-membership.json'
    roads=json.loads(membership.read_text())
    reader=NativeGeometry(plugin)
    npz_path=ROOT/'artifacts/prepared-materials/PedestrianDraw02/source-consolidation/native-charts.npz'
    chart_path=npz_path.with_suffix('.json')
    native=np.load(npz_path);chart_report=json.loads(chart_path.read_text())
    measured=[];near=None
    for lod in range(2):
        name='PedestrianDrawBridge02'+('_LOD1' if lod else '')+' Mesh'
        geometry=research/'movable-skin'/(name+'.Geometry')
        road=roads['levels'][lod]
        assert sha(geometry)==road['sha256']
        m=reader.read(geometry)[0];p=m['positions'].astype(np.float32);uv=m['uv0'][:,:2].astype(np.float32);faces=m['faces']
        record=next(r for r in chart_report['records'] if r['name']==name and r['mesh']==0);prefix=record['arrayPrefix']
        assert np.array_equal(p,native[prefix+'_positions']) and np.array_equal(uv,native[prefix+'_uv']) and np.array_equal(faces,native[prefix+'_faces'])
        if lod==0:near=(p,uv,faces)
        lookup=collections.defaultdict(list)
        for fi,face in enumerate(faces):lookup[triangle_key(p,uv,face)].append(fi)
        road_ids={f for region in road['regions'] for f in region['triangles']}
        road_edges=collections.defaultdict(list)
        for fi in road_ids:
            for edge in edges(p,faces[fi]):road_edges[edge].append(fi)
        parts=[]
        for part,near_ids in NEAR_PARTS.items():
            ids=[]
            for original in near_ids:
                candidates=lookup[triangle_key(near[0],near[1],near[2][original])]
                assert len(candidates)==1,(lod,part,original,candidates)
                ids.append(candidates[0])
            assert not road_ids.intersection(ids)
            quads=[]
            for at in (0,2):
                pair=ids[at:at+2];tri=uv[faces[pair]].astype(float)
                unique=set(map(tuple,tri.reshape(-1,2)))
                lo=tri.min((0,1));hi=tri.max((0,1))
                assert unique=={(lo[0],lo[1]),(lo[0],hi[1]),(hi[0],lo[1]),(hi[0],hi[1])}
                cross=(tri[:,1,0]-tri[:,0,0])*(tri[:,2,1]-tri[:,0,1])-(tri[:,1,1]-tri[:,0,1])*(tri[:,2,0]-tri[:,0,0])
                assert np.all(cross>0) or np.all(cross<0)
                assert abs(cross).sum()/2==float(np.prod(hi-lo)) and (lo>=0).all() and (hi<=1).all()
                # Two triangles cover their complete native rectangle once; no
                # overlap, reset or repeat traversal occurs within this half-cap.
                assert len(set(edges(p,faces[pair[0]])) & set(edges(p,faces[pair[1]])))==1
                quads.append(dict(sourceFaces=pair,uvBounds=[*lo.tolist(),*hi.tolist()],
                    nativePositions=p[faces[pair]].astype(float).tolist(),nativeUvs=tri.tolist(),
                    signedUvAreas=(cross/2).tolist(),singleTraversal=True))
            neighbors=sorted({f for fi in ids for e in edges(p,faces[fi]) for f in road_edges.get(e,[])})
            if part.endswith('-end'):assert len(neighbors)==2,(part,neighbors)
            else:assert not neighbors
            parts.append(dict(part=part,sourceFaces=ids,roadBoundaryNeighbors=neighbors,quads=quads))
        for part in parts:
            if not part['part'].endswith('-lip'):continue
            end=next(x for x in parts if x['part']==part['part'].replace('-lip','-end'))
            lip_edges={e for fi in part['sourceFaces'] for e in edges(p,faces[fi])}
            end_edges={e for fi in end['sourceFaces'] for e in edges(p,faces[fi])}
            assert len(lip_edges & end_edges)==2
            part['adjoiningClosure']=end['part']
        measured.append(dict(render=name,mesh=0,lod=lod,geometry=str(geometry),geometrySha256=sha(geometry),
            sourceSurfaceCid=record['sourceSurfaceCid'],roadFaces=sorted(road_ids),parts=parts))
    return dict(schema=1,style='PedestrianDraw02',sourceVersion='Game 1.6.2f1 native Geometry',
        nativeChartsSha256=sha(npz_path),roadMembershipSha256=sha(membership),nativeDecoderSha256=sha(plugin),
        axes=['U','V'],rule='Finite authored end-wall and underside-lip mappings on non-road faces. Shared atlas pixels do not make these local closure mappings repeat; actual road faces and the repeat rectangle remain unchanged.',meshes=measured)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('research',type=Path);parser.add_argument('plugin',type=Path);parser.add_argument('output',type=Path)
    args=parser.parse_args();result=measure(args.research,args.plugin)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+'\n')
    print('Verified 48 source closure faces, 24 single-traversal half-cap quads, exact LOD correspondence and road exclusion.')
