"""Exact rational audit of authored fixed-walkway UV-gradient boundaries.
No receiver geometry or production assets are modified.
"""
import argparse, hashlib, json
from collections import defaultdict
from fractions import Fraction as F
from pathlib import Path


def affine(face, channel):
    xyz=face["positions"];uv=face["uv0"]
    x0,z0=map(F,(xyz[0][0],xyz[0][2]));u0=F(uv[0][channel])
    dx1=F(xyz[1][0])-x0;dz1=F(xyz[1][2])-z0;du1=F(uv[1][channel])-u0
    dx2=F(xyz[2][0])-x0;dz2=F(xyz[2][2])-z0;du2=F(uv[2][channel])-u0
    determinant=dx1*dz2-dx2*dz1
    a=(du1*dz2-du2*dz1)/determinant;b=(dx1*du2-dx2*du1)/determinant
    return a,b,u0-a*x0-b*z0


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("repository",type=Path);p.add_argument("output",type=Path);args=p.parse_args()
    source=args.repository/"artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json"
    evidence=json.loads(source.read_text());levels=[]
    for level in evidence["levels"][:2]:
        faces=[f for f in level["walkwayFaces"] if f["bone"]==0]
        planes={f["face"]:(affine(f,0),affine(f,1)) for f in faces};edges=defaultdict(list)
        parent={f["face"]:f["face"] for f in faces}
        def root(a):
            while parent[a]!=a:a=parent[a]
            return a
        for f in faces:
            for a,b in ((0,1),(1,2),(2,0)):
                edge=tuple(sorted((tuple(f["positions"][a]),tuple(f["positions"][b]))))
                values={tuple(f["positions"][i]):tuple(f["uv0"][i]) for i in (a,b)}
                edges[edge].append((f["face"],tuple(values[v] for v in edge)))
        gradients=[];wraps=[];flat=[]
        for edge,adj in edges.items():
            if len(adj)!=2:continue
            (a,ua),(b,ub)=adj
            row=dict(edge=edge,faces=[a,b],uv=ua)
            if ua!=ub:wraps.append(row)
            elif planes[a]!=planes[b]:
                row["leftAffine"]=[[str(v) for v in axis] for axis in planes[a]]
                row["rightAffine"]=[[str(v) for v in axis] for axis in planes[b]]
                row["kind"]="continuous UV; different derivative; not a wrap reset"
                gradients.append(row)
            else:parent[root(b)]=root(a);flat.append(row)
        groups=defaultdict(list)
        for f in faces:groups[root(f["face"])].append(f["face"])
        # Distinct supporting lines do not imply permission to extend their finite segments.
        lines=set()
        for g in gradients:
            (x0,_,z0),(x1,_,z1)=g["edge"]
            a=F(z1)-F(z0);b=F(x0)-F(x1);c=-(a*F(x0)+b*F(z0));scale=a if a else b
            lines.add(tuple(str(v/scale) for v in (a,b,c)))
        levels.append(dict(lod=level["lod"],fixedFaces=len(faces),minimalConnectedAffineCharts=len(groups),
            continuousGradientSegments=len(gradients),distinctGradientSupportingLines=len(lines),existingWrapSegments=len(wraps),
            redundantSameGradientEdges=len(flat),charts=list(groups.values()),gradientSegments=gradients,
            wraps=wraps,gradientsPerChart={str(f):[[str(v) for v in axis] for axis in p] for f,p in planes.items()}))
    result=dict(sourceSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),method="Exact rational coordinates and UVs; adjacent equal affine maps merged. No epsilon threshold.",levels=levels,
        authorization="Existing road exception permits UV-wrap planes only. Continuous UV-gradient segments are not wrap resets; implementing new receiver cuts here needs explicit scoped authorization.",
        minimum="Each retained gradient segment is mathematically necessary for exact piecewise-affine sampling of the original native atlas. Extending its line beyond its authored support would add unjustified cuts.")
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,indent=2)+"\n")
    print(json.dumps([{k:v for k,v in l.items() if k not in ("charts","gradientSegments","wraps","gradientsPerChart")} for l in levels]))

if __name__=="__main__":main()
