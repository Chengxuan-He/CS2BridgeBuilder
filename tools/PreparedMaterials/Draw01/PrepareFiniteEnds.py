"""Prepare finite asphalt endpoint candidates with a bounded two-axis paint mask."""
import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).parent
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
s=(HERE/'PrepareBoundedNearUnits.Editor.cs').read_text()
s=s.replace('Draw01PrepareBoundedNearUnits','Draw01PrepareFiniteEnds')
s=s.replace('paintU, donorU;', 'paintU, donorU, paintV;')
s=s.replace('foreach(double n in unit.donorU)w.Write(n);','foreach(double n in unit.donorU)w.Write(n);foreach(double n in unit.paintV)w.Write(n);')
s=s.replace('unit.paintU,unit.donorU);','unit.paintU,unit.donorU,unit.paintV);')
s=s.replace('double[] donorU=null)','double[] donorU=null,double[] paintV=null)')
s=s.replace('program.SetInt("SourceWidth",source.width);','program.SetInt("SourceWidth",source.width);\n            program.SetInt("SourceHeight",source.height);\n            program.SetVector("PaintV",paintV==null?Vector4.zero:new Vector4((float)paintV[0],(float)paintV[1],0,0));')
s=s.replace('Assets/Draw01BoundedUnit.compute','Assets/Draw01FiniteEnds.compute')
c=(HERE/'Draw01BoundedUnit.compute').read_text().replace('int SourceWidth;','int SourceWidth,SourceHeight;\nfloat4 PaintV;')
c=c.replace('if(uv.x>=PaintDonorU.x-footprint&&uv.x<=PaintDonorU.y+footprint)', 'float footprintV=.5*exp2(max(0,lod))/SourceHeight;if(uv.x>=PaintDonorU.x-footprint&&uv.x<=PaintDonorU.y+footprint&&uv.y>=PaintV.x-footprintV&&uv.y<=PaintV.y+footprintV)')
assert 'unit.paintV);' in s and 'foreach(double n in unit.paintV)' in s and 'uv.y>=PaintV.x' in c
(HERE/'PrepareFiniteEnds.Editor.cs').write_text(s)
(HERE/'Draw01FiniteEnds.compute').write_text(c)
m=json.loads((OUT/'bounded-paired-manifest.json').read_text())
for i,u in enumerate(m['units']):
    u['id']='finite-asphalt-ends'+('-control' if i else '')
    u['repeatAxes']='None'
    if not i:u['paintV']=[2778/4096,3510/4096]
    u['aliases']=[]
(OUT/'finite-ends-manifest.json').write_text(json.dumps(m,indent=2))
(OUT/'finite-ends-provenance.json').write_text(json.dumps({'meaning':'The two actual finite endpoint domains view one bounded source-asphalt region with native pixel guards on both axes. No road-length pixels or new periodic seam. The full original 4m source region is retained; no invented crop repeat.','paint':'Defined same-source donor composition, not recovery of unknown substrate. Two-axis mask prevents unrelated source guard regions being edited.','productionConnected':False,'samplingAccepted':False,'bakerSha256':hashlib.sha256(s.encode()).hexdigest(),'kernelSha256':hashlib.sha256(c.encode()).hexdigest()},indent=2))
