"""Export original mesh inputs for executing production geometry plans offline."""
from pathlib import Path
import re
import struct
import sys
import numpy as np

root, native, output = map(Path, sys.argv[1:])
cases=[]
def add(name,implementation,family,lod,mesh=0):
    data=np.load(native/(name+'.npz'))
    cases.append((name,implementation,int(family),int(lod),mesh,data))
for style, implementation in [('Draw02','Draw02ApproachGeometry'),('Draw03','Draw03ApproachGeometry'),('Lift01','Lift01ApproachStructure')]:
    source=root/f'src/BridgeBuilder/Bridges/Types/{style}/{style}ApproachSources.g.cs'
    for family,lod,name in re.findall(r'var p(\d+)l(\d+) = Create<(?:NetPiecePrefab|RenderPrefab)>\("([^"\r\n]+)"',source.read_text(encoding='utf-8-sig')):
        add(name,implementation,family,lod)
for style,prefix in [('Draw02','DrawBridge02'),('Draw03','DrawBridge03'),
                     ('Lift01','LiftBridge01'),('Lift02','LiftBridge02'),('Lift03','LiftBridge03'),
                     ('Lift04','LiftBridge04'),('Lift05','LiftBridge05'),
                     ('PedestrianDraw01','PedestrianDrawBridge01'),('PedestrianDraw02','PedestrianDrawBridge02')]:
    for lod in range(2 if style=='PedestrianDraw02' else 3):
        name=prefix+('' if lod==0 else f'_LOD{lod}')+' Mesh'
        data=np.load(native/(name+'.npz'))
        mesh_ids=sorted(int(re.fullmatch(r'mesh(\d+)_positions',key)[1]) for key in data.files if re.fullmatch(r'mesh(\d+)_positions',key))
        for mesh in mesh_ids:
            if style=='PedestrianDraw02': impl=style+'RenderAssembly#Structure'
            elif style=='Draw02': impl='Draw02RigidParts#Glass' if mesh else 'Draw02MovingShell'
            elif mesh: impl=style+'Glass'
            elif style in ('Draw03','Lift01','Lift03'): impl=style+('FarStructure' if lod==2 else 'DetailedStructure')
            else: impl=style+'StructurePlan'
            add(name,impl,0,lod,mesh)
for path in sorted(native.glob('*.npz')):
    name=path.stem
    if any(row[0]==name for row in cases): continue
    match=re.fullmatch(r'(DrawBridge\d\d|LiftBridge\d\d|PedestrianDrawBridge\d\d)(.*?)(?:_LOD(\d))? Mesh',name)
    if not match: raise ValueError(name)
    prefix,part,lod=match.groups();lod=int(lod or 0)
    style=prefix.replace('Bridge','')
    family=0
    if not part and style=='Draw01': impl='Draw01RoadReplacement#CreateMainMeshPlan'
    elif part.startswith('Bottom') and style=='Draw01':
        names=['Bottom300cm','BottomNode300cm','BottomEdge350cm','BottomEdgeNode350cm','BottomIntersectionMiddle200cm']
        family=names.index(part);impl='Draw01FixedBaseData#Plan'
    elif part.startswith('NetPillar'):
        family={'NetPillarTop':0,'NetPillar':1,'NetPillarBase':2}[part]
        if style=='Draw01': impl={0:'Draw01NetTopData#CreatePlan',1:'Identity',2:'Draw01NetFootingData#CreatePlan'}[family]
        elif style=='Draw03': impl='Draw03FarNetPillars' if lod==2 and family!=2 else 'Draw03NetPillarParts'
        elif style=='Lift03': impl='Lift03NetPillarAssembly#CreatePlan'
        else: impl=style+'NetPillarGeometry'
    elif part in ('Pillar','PillarBase'):
        family=int(part=='PillarBase')
        if style=='Draw01': impl='Draw01FootingWideningData#CreatePlan' if family else 'Draw01ColumnWideningData#CreatePlan'
        elif style=='Draw02': impl='Draw02SupportWideningData#CreatePlan'
        elif style=='Draw03': impl='Draw03FarSupports' if lod==2 else 'Draw03DetailedSupports'
        elif style=='Lift01': impl=('Lift01FarFooting' if lod==2 else 'Lift01Footing#CreateDetailed') if family else 'Identity'
        elif style in ('Lift02','Lift03'): impl=style+('FootingGeometry' if family else 'ColumnGeometry')
        elif style=='PedestrianDraw02': impl=('PedestrianDraw02FootingWideningData#CreatePlan' if family else ('PedestrianDraw02FarColumn#CreatePlan' if lod==2 else 'PedestrianDraw02ColumnWideningData#CreateDetailedPlan'))
        else: impl=style+'SupportGeometry'
    else: raise ValueError('Missing production binding: '+name)
    data=np.load(path)
    mesh_ids=sorted(int(re.fullmatch(r'mesh(\d+)_positions',key)[1]) for key in data.files if re.fullmatch(r'mesh(\d+)_positions',key))
    for mesh in mesh_ids:
        add(name,'Draw01Glass' if style=='Draw01' and not part and mesh else impl,family,lod,mesh)
add('LiftBridge01_LOD1 Mesh','Lift01FarTowerCore',0,1)
add('LiftBridge03_LOD1 Mesh','Lift03FarSupports#CreateSupplement',0,1)
add('DrawBridge01_LOD1 Mesh','Draw01UndersideSupplement',0,1)
with output.open('wb') as f:
    f.write(struct.pack('<ii',2,len(cases)))
    for name,implementation,family,lod,mesh,data in cases:
        for value in (name,implementation):
            encoded=value.encode('utf-8');f.write(struct.pack('<i',len(encoded)));f.write(encoded)
        p=np.asarray(data[f'mesh{mesh}_positions'],dtype='<f4');uv=np.asarray(data[f'mesh{mesh}_uv0'][:,:2],dtype='<f4')
        indices=np.asarray(data[f'mesh{mesh}_faces'],dtype='<i4').ravel()
        f.write(struct.pack('<5i',family,lod,mesh,len(p),len(indices)))
        f.write(p.tobytes());f.write(uv.tobytes());f.write(indices.tobytes())
print(f'Exported {len(cases)} actual geometry-plan inputs.')
