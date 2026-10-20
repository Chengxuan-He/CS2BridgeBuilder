"""Audit composed Draw02 structural maps against the native geometry.
Usage: python Draw02StructuralAudit.py RESEARCH_DIRECTORY
This does not claim complete road replacement, native rendering or visual acceptance.
"""
from pathlib import Path
import runpy
import sys
import tempfile
import numpy as np

research = Path(sys.argv[1])
scripts = Path(__file__).parent
modules = {}
with tempfile.TemporaryDirectory(prefix='draw02-audit-') as temporary:
    for name in ('MovingShell', 'RigidParts', 'FixedBodies', 'TransverseMembers', 'FixedCurbs', 'RoadTops', 'FixedSidewalks'):
        sys.argv = [name, str(research), str(Path(temporary) / (name + '.cs'))]
        modules[name] = runpy.run_path(str(scripts / ('Draw02' + name + '.py')))

for lod in range(3):
    d = np.load(research / f'draw02-main-lod{lod}-uv.npz')
    v, faces = d['positions'].astype(np.float32), d['indices'].reshape(-1, 3)
    mapping = {}
    def add(indices, weight):
        for i in indices:
            assert i not in mapping, ('Overlapping maps', lod, i)
            mapping[i] = weight
    for weight, ids in modules['MovingShell']['body_sides'][lod].items():
        add(ids, weight)
    for name in ('RigidParts', 'FixedBodies'):
        for weight, ids in modules[name]['tables'][lod][1].items():
            add(ids, weight)
    if lod < 2:
        for weight, ids in modules['TransverseMembers']['tables'][lod][1].items():
            add(ids, weight)
    nv, nf, removed, edges, interior, fragments, sha = modules['MovingShell']['records'][lod]
    tops = set(modules['RoadTops']['records'][lod][0])
    platform_faces, platform_edges, platform_fragments, platform_weights = modules['RoadTops']['platforms'][lod]
    assert not interior, 'Draw02 platform appending requires the recorded edge-only shell'
    assert not tops.intersection(i for i,_ in fragments)
    curbs = set(modules['FixedCurbs']['tables'][lod][0]) if lod < 2 else set()
    assert not curbs.intersection(i for i,_ in fragments), ('Curb removal overlaps retained fragments', lod)
    assert not curbs.intersection(tops), ('Curb records contain road tops', lod)
    removed = set(removed) | tops | curbs
    assert not set(platform_faces).intersection(removed), ('Exterior platform removed', lod)
    retained = faces[[i for i in range(nf) if i not in removed]]
    fragment_indices = [i if kind == 'v' else nv+i if kind == 'e' else nv+len(edges)+i
                        for _, polygon in fragments for kind,i in polygon]
    fragment_indices += [i if i < nv else i + len(edges)
                         for _, polygon in platform_fragments for i in polygon]
    output_faces = np.vstack((retained,np.array(fragment_indices).reshape(-1,3)))
    used = np.unique(output_faces)
    missing = [int(i) for i in used if i < nv and abs(v[i,0]) > 1e-5 and i not in mapping]
    assert not missing, ('Unmapped retained noncentre vertices',lod,missing)
    constructed = np.array([v[a]+(v[b]-v[a])*np.float32(t) for a,b,t in edges + platform_edges] +
                           [v[a]+(v[b]-v[a])*np.float32(wb)+(v[c]-v[a])*np.float32(wc)
                            for a,b,c,wb,wc in interior],dtype=np.float32)
    original = np.vstack((v,constructed))
    # Match the emitted exact corridor boundary, not rounded endpoint interpolation.
    original[nv+len(edges):,0] = np.array(platform_weights) * np.float32(9.5)
    weights = np.zeros(len(original),dtype=np.float32)
    for i,w in mapping.items():
        weights[i]=w
    weights[nv:]=list(modules['MovingShell']['boundary_weights'][lod]) + platform_weights
    # Every retained far fragment must occupy only the exterior platform; all raw
    # channels are interpolated from its own original triangle by the runtime builder.
    for face, polygon in platform_fragments:
        ids = np.array([i if i < nv else i + len(edges) for i in polygon]).reshape(-1,3)
        points = original[ids].astype(np.float64)
        assert np.all(points[:,:,0] >= 9.5) or np.all(points[:,:,0] <= -9.5)
        normals = np.cross(points[:,1]-points[:,0],points[:,2]-points[:,0])
        assert np.all(normals[:,1] > 0)
        source = v[faces[face]].astype(np.float64)
        source_area = np.linalg.norm(np.cross(source[1]-source[0],source[2]-source[0]))/2
        assert 0 < np.linalg.norm(normals,axis=1).sum()/2 < source_area
    for delta in (-.1,0,2,8):
        moved=original.copy()
        moved[:,0]+=weights*np.float32(delta)
        assert np.isfinite(moved).all()
        assert np.array_equal(moved[:,1:],original[:,1:])
        if delta == 0:
            assert np.array_equal(moved,original)
        before=original[output_faces].astype(np.float64)
        after=moved[output_faces].astype(np.float64)
        n0=np.cross(before[:,1]-before[:,0],before[:,2]-before[:,0])
        n1=np.cross(after[:,1]-after[:,0],after[:,2]-after[:,0])
        valid=np.linalg.norm(n0,axis=1)>1e-8
        assert np.all(np.linalg.norm(n1[valid],axis=1)>1e-8), ('Collapsed faces',lod,delta)
        assert np.all(np.sum(n0[valid]*n1[valid],axis=1)>0), ('Reversed faces',lod,delta)
    # The actual selected-road layout overrides only the recorded sidewalk nose
    # slab vertices. Test both sides independently and check the composed output,
    # including the road-top/curb removals and fixed exterior platform fragments.
    groups = modules['FixedSidewalks']['tables'][lod][0]
    for label, spans in (
        ('native', ((-9.5,-6),(6,9.5))),
        ('no sidewalk', ((0,0),(0,0))),
        ('left only', ((-9.5,-5),(0,0))),
        ('right only', ((0,0),(5,9.5))),
        ('asymmetric', ((-9.5,-5),(8,9.5)))):
        moved = original.copy()
        absent = set()
        changed_vertices = set()
        for side, slab_faces, vertices, minimum, fractions in groups:
            span = spans[0 if side < 0 else 1]
            a, b = map(np.float32,span)
            if b <= a:
                absent.update(map(int,slab_faces))
                continue
            changed_vertices.update(map(int,vertices))
            moved[vertices,0] = v[vertices,0] + (a-minimum) + (b-a-np.float32(3.5))*fractions
        retained_layout = faces[[i for i in range(nf) if i not in removed and i not in absent]]
        layout_faces = np.vstack((retained_layout,np.array(fragment_indices).reshape(-1,3)))
        if label == 'native':
            assert np.array_equal(moved,original)
        unchanged = [i for i in range(len(original)) if i not in changed_vertices]
        assert np.array_equal(moved[unchanged],original[unchanged])
        assert np.array_equal(moved[:,1:],original[:,1:])
        before, after = original[layout_faces].astype(float), moved[layout_faces].astype(float)
        n0 = np.cross(before[:,1]-before[:,0],before[:,2]-before[:,0])
        n1 = np.cross(after[:,1]-after[:,0],after[:,2]-after[:,0])
        valid = np.linalg.norm(n0,axis=1)>1e-8
        assert np.all(np.sum(n0[valid]*n1[valid],axis=1)>0), ('Sidewalk reversed face',lod,label)
        for side, slab_faces, vertices, minimum, fractions in groups:
            if spans[0 if side < 0 else 1] == (0,0):
                assert not np.isin(layout_faces,vertices).any(), ('Absent sidewalk remains',lod,label,side)
        print(f'PASS LOD{lod} {label}: {len(layout_faces)} structural triangles; independent sidewalk presence and winding')
    print(f'PASS LOD{lod}: {len(output_faces)} retained/fragment triangles, complete noncentre coverage, disjoint maps, no new collapse/reversal at deltas -0.1/0/2/8')
