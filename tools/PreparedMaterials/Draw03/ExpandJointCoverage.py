"""Regenerate all eight evidenced Draw03 stepped joints; preserve native Y/Z and bones."""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np
from PrepareRecipes import numbers

ROOT=Path(__file__).resolve().parents[3]
RESEARCH=Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research')
OUT=ROOT/'artifacts/material-remap/Draw03/joint-coverage'
CODE=ROOT/'src/BridgeBuilder/Bridges/Types/Draw03/Draw03JointLayout.g.cs'
RESOURCE=ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03JointLayout_g_cs.bin'

def main():
    backup=ROOT/'artifacts/backups/Draw03-eight-joint-coverage'
    backup.mkdir(parents=True,exist_ok=True)
    for path in (CODE,RESOURCE):
        target=backup/path.name
        if not target.exists():target.write_bytes(path.read_bytes())
    previous=numbers(backup/RESOURCE.name)
    ints=list(previous['I32'][:5]);floats=list(previous['F32'][:7]);levels=[];fixtures=[]
    for lod,identities in enumerate(((705,706,1479,1480,2176,2177,2948,2949),(745,746,1519,1520,2264,2265,3036,3037))):
        source=RESEARCH/'movable-skin'/('DrawBridge03'+('_LOD1' if lod else '')+' Mesh.Geometry')
        sha=hashlib.sha256(source.read_bytes()).hexdigest()
        assert sha==('f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f','15b8419da13d6e99e1dc0591f12f4019f0be09c3b220a481d24720daf7b57e12')[lod]
        data=np.load(RESEARCH/f'draw03-main-{lod}.npz');v=data['positions'].astype(float);f=data['indices'].reshape(-1,3)
        parts=json.loads((RESEARCH/f'draw03-parts-{lod}.json').read_text())
        left=[];right=[];rows=[];car=[];groups=[]
        for identity,bone in zip(identities,(1,1,0,0,2,2,0,0)):
            ids=np.array(parts[identity]['vertices']);complete=np.all(np.isin(f,ids),axis=1)
            assert np.array_equal(complete,np.any(np.isin(f,ids),axis=1))
            faces=np.flatnonzero(complete);p=v[f[faces]]
            assert set(data['bones'][faces])=={bone}
            l=p[:,:,0].max(1)<=floats[0]*-1;r=p[:,:,0].min(1)>=floats[0];c=~(l|r)
            assert p[c,:,1].max()<=10.009033203125
            left.extend(faces[l].tolist());right.extend(faces[r].tolist());car.extend(faces[c].tolist())
            for index in sorted(ids):
                x=v[index,0];side=int(np.sign(x));walk=abs(x)>floats[0]
                fraction=(abs(x)-9)/3.5 if walk else abs(x)/floats[0]
                rows.append((int(index),side,int(walk),fraction))
            groups.append(dict(part=identity,bone=bone,faces=len(faces),carriagewayFaces=len(faces[c]),
                minimum=v[ids].min(0).tolist(),maximum=v[ids].max(0).tolist()))
        assert len({x[0] for x in rows})==len(rows)
        ints.extend([len(left),len(right),len(rows),*left,*right])
        for index,side,walk,fraction in rows:ints.extend([index,side,walk]);floats.append(fraction)
        # Native and absent/asymmetric sidewalks; preserve untouched geometry.
        tests=[]
        for a,b,delta in [((-12.5,-9),(9,12.5),0),((0,0),(0,0),6.75),((-12.5,-9),(0,0),0),((0,0),(9,12.5),0),((-19.25,-17.75),(15.25,19.25),6.75)]:
            moved=v.copy();removed=set(left if a[1]<=a[0] else [])|set(right if b[1]<=b[0] else [])
            for index,side,walk,fraction in rows:
                if side==0:continue
                span=a if side<0 else b;present=span[1]>span[0];width=span[1]-span[0];start=-span[1] if side<0 else span[0]
                edge=start+(floats[0]-9)/3.5*width if present else 12.5+delta
                shift=start-9+(width-3.5)*fraction if walk and present else (edge-floats[0])*(1 if walk else fraction)
                moved[index,0]=v[index,0]+side*shift
            assert np.array_equal(moved[:,1:],v[:,1:]) and not set(car)&removed
            if a==(0,0) and b==(0,0):assert moved[f[car],1].max()==10.009033203125
            if delta==0 and a==(-12.5,-9) and b==(9,12.5):assert np.array_equal(moved,v)
            tests.append(dict(left=a,right=b,delta=delta,retainedCarriagewayMaxY=float(moved[f[car],1].max()),removedRaisedFaces=len(removed)))
        levels.append(dict(lod=lod,sourceSha256=sha,groups=groups,vertices=len(rows),leftFaces=len(left),rightFaces=len(right),tests=tests))
        fixture=struct.pack('<2i',len(v),f.size)+v.astype('<f4').tobytes()+f.astype('<i4').tobytes()
        for values in (left,right,car,[r[0] for r in rows]):fixture+=struct.pack('<i',len(values))+np.asarray(values,dtype='<i4').tobytes()
        fixtures.append(fixture)
    payload=struct.pack('<8I',0x314e4242,1,len(ints),0,0,0,len(floats),0)+struct.pack('<'+'i'*len(ints),*ints)+struct.pack('<'+'f'*len(floats),*floats)
    RESOURCE.write_bytes(payload+hashlib.sha256(payload).digest())
    code=(backup/CODE.name).read_text().split('    // Geometry SHA-256:')[0]
    code=code.replace('0 => (Left: Left0, Right: Right0, Vertices: Vertices0)','0 => Near').replace('1 => (Left: Left1, Right: Right1, Vertices: Vertices1)','1 => Middle')
    code+='''    private static readonly (int[] Left, int[] Right, (int Index, int Side, bool Walk, float Fraction)[] Vertices) Near=ReadLevel(0), Middle=ReadLevel(1);
    private static (int[] Left, int[] Right, (int Index, int Side, bool Walk, float Fraction)[] Vertices) ReadLevel(int level)
    {
        var d=Numeric_Types_Draw03_Draw03JointLayout_g_cs.Values;
        int at=5, ft=7;
        for(int lod=0;lod<=level;lod++)
        {
            int nl=d.I32[at++], nr=d.I32[at++], nv=d.I32[at++];
            var left=new int[nl];var right=new int[nr];
            Array.Copy(d.I32,at,left,0,nl);at+=nl;Array.Copy(d.I32,at,right,0,nr);at+=nr;
            var vertices=new (int Index,int Side,bool Walk,float Fraction)[nv];
            for(int i=0;i<nv;i++)vertices[i]=(d.I32[at++],d.I32[at++],d.I32[at++]!=0,d.F32[ft++]);
            if(lod==level)return (left,right,vertices);
        }
        throw new InvalidOperationException("Draw03 joint level is not recorded.");
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03JointLayout_g_cs
internal static class Numeric_Types_Draw03_Draw03JointLayout_g_cs
{
    static Numeric_Types_Draw03_Draw03JointLayout_g_cs() {}
    internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Draw03_Draw03JointLayout_g_cs");
}
'''
    CODE.write_text(code)
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'native-joints.bin').write_bytes(b''.join(fixtures))
    (OUT/'coverage.json').write_text(json.dumps(dict(levels=levels,scope='Eight exclusive source joints at LOD0/1; existing materials/bones/YZ retained. Actual runtime plan replay remains separate.'),indent=2)+'\n')
    print([(x['lod'],x['vertices'],x['leftFaces'],x['rightFaces']) for x in levels])

if __name__=='__main__':main()
