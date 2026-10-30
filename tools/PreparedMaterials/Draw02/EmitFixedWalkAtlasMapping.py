"""Measure fixed cap/slope/walk domains without borrowing an adjacent chart."""
import hashlib,json,struct
from pathlib import Path
import numpy as np
from EmitFarMapping import interior_line

ROOT=Path(__file__).resolve().parents[3]
E=ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
floats=[];reports=[];candidates=[]
for lod in (0,1):
    m=np.load(E/f'lod{lod}-native.npz');p=m['mesh0_positions'].astype(float);uv=m['mesh0_uv'].astype(float);f=m['mesh0_indices'];bones=m['mesh0_bones'][:,0]
    q=p[f];normals=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0])
    for end in (1,-1):
        for side in (-1,1):
            eligible=np.where((bones[f[:,0]]==0)&(normals[:,1]>0)&
                ((q[:,:,0]*side).min(1)>=6.49)&((q[:,:,0]*side).max(1)<=9.50001)&
                ((q[:,:,2]*end).min(1)>=78.74))[0]
            for part in range(4):
                faces=[]
                for face in eligible:
                    z=q[face,:,2]*end;t=uv[f[face]]
                    match=(z.min()<79 and z.max()<79.96) if part==0 else (
                        z.min()>79.94 and z.min()<79.96 and z.max()<80.02) if part==1 else (
                        z.min()==80 and z.max()==82) if part==2 else (z.min()>=82 and z.max()<=90 and t[:,1].min()>=.685 and t[:,1].max()<=.869)
                    if match:faces.append(int(face))
                expected=4 if part==3 else (1 if lod==1 and part==1 else 2)
                assert len(faces)==expected,(lod,end,side,part,faces)
                ids=np.unique(f[faces]);points=p[ids];tex=uv[ids]
                cu,check=interior_line(points[:,0],tex[:,0])
                if part==3:
                    vmin,vmax=tex[:,1].min(),tex[:,1].max()
                    cv=np.array((-(vmax-vmin)/4*end,vmax+(vmax-vmin)/4*82),np.float32)
                else:cv,_=interior_line(points[:,2],tex[:,1])
                # Packed U, V, source bounds, longitudinal end, minimum source |X|.
                outerSlope,outerOffset=0.,9.5
                if len(faces)==1:
                    low,high=(points[:,2]*end).min(),(points[:,2]*end).max()
                    outerSlope=(abs(points[:,0]).min()-9.5)/(high-low)
                    outerOffset=9.5-outerSlope*low
                row=[*cu,*cv,*tex.min(0),*tex.max(0),float((points[:,2]*end).max()),float(abs(points[:,0]).min()),outerSlope,outerOffset]
                floats.extend(float(v) for v in row)
                domain=((0 if end==1 else 1)*2+(side+1)//2)*4+part
                reports.append(dict(lod=lod,domain=domain,part=part,faces=faces,profile=row,
                    sourceHalfU=check,repeatV=part==3,finiteU=True))
                for face in faces:
                    point=p[f[face]].mean(0)
                    # The final 11.7 mm of the ramp lies below the flat walk;
                    # its visible domain ends at Z80, established by native depth.
                    if part==1 and point[2]*end>=80:continue
                    candidates.append(dict(lod=lod,domain=domain,x=float(point[0]),z=float(point[2]),
                        u=float(point[0]*cu[0]+cu[1])))
floats.extend([6.5999755859375,9.5,79.9453125,80.,82.,90.])
blob=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(floats),0)+struct.pack('<%df'%len(floats),*floats)
blob+=hashlib.sha256(blob).digest()
(ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02FixedWalkAtlasAppearance_cs.bin').write_bytes(blob)
out=ROOT/'artifacts/material-remap/Draw02/near-cells'
(out/'fixed-walk-atlas-plan.json').write_text(json.dumps(reports,indent=2,default=lambda v:float(v))+'\n')
(out/'fixed-walk-atlas-candidates.json').write_text(json.dumps(dict(points=candidates))+'\n')
print('PASS: 32 fixed sidewalk cap/slope/terminal/repeat domains measured independently by LOD.')
