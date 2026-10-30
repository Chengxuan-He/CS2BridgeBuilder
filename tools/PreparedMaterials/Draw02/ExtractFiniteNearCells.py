"""Extract complete authored lateral layouts over one native longitudinal cell."""
import hashlib,json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
E=ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
out=ROOT/'artifacts/material-remap/Draw02/near-cells'
out.mkdir(parents=True,exist_ok=True)
records=[]
bake_units=[]
for lod in (0,1):
    m=np.load(E/f'lod{lod}-native.npz');p=m['mesh0_positions'].astype(float);f=m['mesh0_indices'];uv=m['mesh0_uv'].astype(float)
    q=p[f]
    for name,y,z0,z1 in [('moving-carriageway',9.800048828125,40,44),('moving-sidewalk',10,40,44),('fixed-road',9.800048828125,82,86),('fixed-sidewalk',10,82,86)]:
        ids=np.where((np.ptp(q[:,:,1],axis=1)<1e-8)&(np.abs(q[:,0,1]-y)<1e-6)&
            (q[:,:,2].min(1)>=z0)&(q[:,:,2].max(1)<=z1)&(np.ptp(q[:,:,2],axis=1)>3.99))[0]
        cells={}
        for face in ids:
            points=q[face]; normals=np.cross(points[1]-points[0],points[2]-points[0])
            if normals[1]<=0:continue
            xmin,xmax=points[:,0].min(),points[:,0].max()
            if abs(xmin)>9.5001 or abs(xmax)>9.5001:continue
            key=(xmin,xmax)
            row=cells.setdefault(key,dict(minX=xmin,maxX=xmax,faces=[],minimumUV=[1.,1.],maximumUV=[0.,0.],projections=[]))
            row['faces'].append(int(face));row['minimumUV']=np.minimum(row['minimumUV'],uv[f[face]].min(0)).tolist();row['maximumUV']=np.maximum(row['maximumUV'],uv[f[face]].max(0)).tolist()
            row['projections'].append(np.linalg.solve(np.column_stack((points[:,0],points[:,2],np.ones(3))),uv[f[face]]).tolist())
        bands=sorted(cells.values(),key=lambda b:-b['maxX'])
        records.append(dict(lod=lod,name=name,z0=z0,z1=z1,bands=bands))
        full=np.where((np.ptp(q[:,:,1],axis=1)<1e-8)&(np.abs(q[:,0,1]-y)<1e-6)&
            (q[:,:,2].min(1)<z1)&(q[:,:,2].max(1)>z0))[0]
        full=[i for i in full if np.cross(q[i,1]-q[i,0],q[i,2]-q[i,0])[1]>0 and
              q[i,:,0].min()>=(-9.5001 if 'sidewalk' not in name else 0) and q[i,:,0].max()<=9.5001]
        points=q[full]; xmin,xmax=float(points[:,:,0].min()),float(points[:,:,0].max())
        density=0
        for face in full:
            coeff=np.linalg.solve(np.column_stack((q[face,:,0],q[face,:,2],np.ones(3))),uv[f[face]])
            density=max(density,float(np.linalg.norm(coeff[0]))*4096)
        xstart,xspan=(xmin,xmax-xmin) if 'sidewalk' in name else (xmax,xmin-xmax)
        triangles=np.concatenate((q[full][:,:,(0,2)],uv[f[full]]),axis=2).flatten().tolist()
        width=int(np.ceil(density*(xmax-xmin)/4))*8 # finite-axis guard, BC7 alignment
        bake_units.append(dict(id=f'lod{lod}-{name}',width=width,height=828 if name.startswith('moving') else 752,
            physical=[xstart,z1,xspan,-4.],triangles=triangles,faces=[int(i) for i in full],sourceGeometry=str(E/f'lod{lod}-native.npz')))
        print(lod,name,len(bands),[(b['minX'],b['maxX']) for b in bands])
report=dict(schema='Draw02FiniteNearCells/1',records=records,
    sourceSurface='8a28ab8d9526e258efbcb81c3bbc936b',repeatAxis='V',finiteAxis='U',
    sourceGeometry=[json.loads((E/f'lod{i}-analysis.json').read_text())['sha256'] for i in (0,1)])
(out/'native-cells.json').write_text(json.dumps(report,indent=2)+'\n')
channels=json.loads((ROOT/'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/manifest.json').read_text())['units'][0]['channels']
for c in channels: assert hashlib.sha256(Path(c['path']).read_bytes()).hexdigest()==c['sha256']
(out/'bake-input.json').write_text(json.dumps(dict(channels=channels,units=bake_units),indent=2)+'\n')
