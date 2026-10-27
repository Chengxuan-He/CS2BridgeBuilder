"""Read native Windows-template vertex semantics; never modify style data or assets.

The caller must establish Surface template 2 and its unmodified shader550 mapping.
UV0.YZ supplies local interior-box coordinates, UV0.W its orientation; COLOR
supplies authored window/room selection. Source topology and native materials stay
unchanged. This audit is separate from ordinary Default-template UV0.XY density.
"""
import argparse,hashlib,json
from pathlib import Path
import numpy as np
from UvDensityAudit import NativeGeometry

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--geometry',type=Path,required=True)
p.add_argument('--mesh',type=int,required=True)
p.add_argument('--plugin',type=Path,required=True)
p.add_argument('--output',type=Path,required=True)
a=p.parse_args()
m=NativeGeometry(a.plugin).read(a.geometry,include_colors=True)[a.mesh]
uv=m['uv0'];faces=m['faces'];color=m.get('color_bytes',m.get('color'))
assert uv.shape[1]==4, 'Windows local mapping requires complete UV0.xyzw'
assert color is not None and len(color)==len(uv), 'Missing native window-selection COLOR'
assert np.isfinite(uv).all() and np.isfinite(color).all()
local=uv[faces,1:3].astype(float)
finite=np.all((local>=0)&(local<=1),axis=(1,2))
orientation=np.all(uv[faces,3]==uv[faces,3][:,:1],axis=1)
selection=np.all(color[faces]==color[faces][:,:1,:],axis=(1,2))
report={'geometry':str(a.geometry.resolve()),'sha256':hashlib.sha256(a.geometry.read_bytes()).hexdigest(),
 'mesh':a.mesh,'vertexCount':len(uv),'faceCount':len(faces),
 'uv0Min':uv.min(axis=0).astype(float).tolist(),'uv0Max':uv.max(axis=0).astype(float).tolist(),
 'localAxes':'UV0.YZ; source shader550 vertex152/fragment184',
 'finiteLocalFaces':np.flatnonzero(finite).tolist(),
 'constantOrientationFaces':np.flatnonzero(orientation).tolist(),
 'constantSelectionFaces':np.flatnonzero(selection).tolist(),
 'sourceLocalFaces':np.flatnonzero(finite&orientation&selection).tolist(),
 'uv0':uv.astype(float).tolist(),'colors':color.tolist(),'faces':faces.tolist(),
 'scope':'Read-only source evidence. Source Surface/template and shader variants must be verified independently. No output or visual acceptance claimed.'}
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k in ('mesh','vertexCount','faceCount','uv0Min','uv0Max')}))
print(f'Source local mapping: {int((finite&orientation&selection).sum())}/{len(faces)} faces')
