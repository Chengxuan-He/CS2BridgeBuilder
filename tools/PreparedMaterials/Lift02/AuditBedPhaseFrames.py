"""Inspect all original bed frames and /3 finite-grid phase before costly rebaking."""
from pathlib import Path
import hashlib,json,math,sys
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/PreparedMaterials/PedestrianDraw02'))
from AuditPanelFrames import packed,NativeGeometry
reader=NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift02/outputs/native-repeat-mechanism/native-repeat-mechanism.json').read_text())
records=[]
for lod in audit['lods']:
    path=Path(lod['geometryPath']); assert hashlib.sha256(path.read_bytes()).hexdigest()==lod['geometrySHA256']
    streams=packed(reader,path); mesh=np.load(ROOT/'artifacts/uv-density/native'/(lod['render']+'.npz'))
    fs=mesh['mesh0_faces'][lod['roadTopFaceIndices']]; positions=mesh['mesh0_positions']; groups=[]
    for sign in (-1,1):
        indices=np.unique(fs[np.mean(positions[fs,:,][...,0],axis=1)*sign>0])
        groups.append(dict(side=sign,vertices=len(indices),normalBits=[int(x) for x in np.unique(streams[1][indices])],tangentBits=[int(x) for x in np.unique(streams[2][indices])]))
    records.append(dict(lod=lod['lod'],geometrySha256=lod['geometrySHA256'],groups=groups))
y=.046112060546875*4096; span=(.05438232421875-.046112060546875)*4096
height=math.ceil(span/4)*4*2; start=math.floor(y-(height-span)/2)
phases=[]
for mip in range(9):
    extent=max(1,height>>mip); source_width=max(1,4096>>mip)
    coords=(start+(np.arange(extent)+.5)*height/extent)*source_width/4096-.5
    phases.append(dict(mip=mip,outputHeight=extent,sourceTexelCoordinates=coords.tolist(),maxCentreDistance=float(np.max(abs(coords-np.round(coords)))),stepSourceTexels=height/extent*source_width/4096))
result=dict(frames=records,grid=dict(sourceStart=y,sourceSpan=span,height=height,guardedStart=start,contentMin=(y-start)/height,contentMax=(y+span-start)/height,mips=phases),conclusion='Source-grid /3 is exact at mip0 only for this candidate; odd guard origin and non-power-of-two extent introduce deeper-level phase/spacing errors. Do not claim all-mip source-grid equivalence or rebake unchanged configuration.',scope='Actual original packed frame bits and source-coordinate arithmetic; decoded frame confirmation and runtime uploaded channels remain separate.')
out=ROOT/'artifacts/material-remap/Lift02/source-mapping/bed-phase-frames.json';out.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(frames=records,grid=result['grid']|{'mips':[dict(mip=x['mip'],maxCentreDistance=x['maxCentreDistance'],stepSourceTexels=x['stepSourceTexels']) for x in phases]}),indent=2))
