"""Adapt the frozen producer to the real middle 8m unit and bounded paint mask."""
import copy,hashlib,json,math,sys
import numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
EVIDENCE=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
PRODUCER=ROOT/'artifacts/material-remap/shared/outputs/producer-A1DC321DAA29E0468CA1143DDDC1A3D7DD92BEA35AC99E58E6402EC5085D0728'
source=(PRODUCER/'PrepareRepeatUnits.Editor.cs').read_text()
source=source.replace('public static class PrepareRepeatUnits','public static class Draw01PrepareMiddleRepeat')
source=source.replace('public string repeatAxes;','public string repeatAxes;public double[] paintRect,donorU;public int[] minimumPixelSize;')
source=source.replace('w.Write(px);w.Write(py);', 'w.Write(px);w.Write(py);if(unit.minimumPixelSize!=null){foreach(int n in unit.minimumPixelSize)w.Write(n);}')
source=source.replace('int width=Pixel(span.X,bc7),height=Pixel(span.Y,bc7);','int width=Pixel(span.X,bc7),height=Pixel(span.Y,bc7);if(u.minimumPixelSize!=null){if(u.minimumPixelSize.Length!=2||u.minimumPixelSize.Any(n=>n<=0))throw new InvalidDataException("Invalid minimum pixel grid.");width=Math.Max(width,Pixel(u.minimumPixelSize[0],bc7));height=Math.Max(height,Pixel(u.minimumPixelSize[1],bc7));}')
source=source.replace('w.Write(channel.encoding??"");','w.Write(channel.encoding??"");if(unit.paintRect!=null){foreach(double n in unit.paintRect)w.Write(n);foreach(double n in unit.donorU)w.Write(n);}')
source=source.replace('unit.sourceGradientSampling,SamplingPanels(unit));','unit.sourceGradientSampling,SamplingPanels(unit),unit.paintRect,unit.donorU);')
source=source.replace('Panel[] panels=null)','Panel[] panels=null,double[] paintRect=null,double[] donorU=null)')
source=source.replace('program.SetInt("EncodeSrgb",srgb?1:0);','program.SetInt("EncodeSrgb",srgb?1:0);\n            program.SetInt("ReplacePaint",paintRect==null?0:1);\n            program.SetVector("PaintRect",paintRect==null?Vector4.zero:new Vector4((float)paintRect[0],(float)paintRect[1],(float)paintRect[2],(float)paintRect[3]));\n            program.SetVector("DonorU",donorU==null?Vector4.zero:new Vector4((float)donorU[0],(float)donorU[1],0,0));')
source=source.replace('Assets/RepeatUnit.compute','Assets/Draw01MiddleRepeat.compute')
assert 'if(unit.paintRect!=null)' in source
assert 'SamplingPanels(unit),unit.paintRect,unit.donorU)' in source and source.count('program.SetInt("ReplacePaint"')==1
kernel=(PRODUCER/'RepeatUnit.compute').read_text().replace('int PanelCount;','int PanelCount;\nint ReplacePaint;\nfloat4 PaintRect,DonorU;')
kernel=kernel.replace('    float4 c;\n','    if(ReplacePaint!=0){float2 footprint=.5*exp2(max(0,lod))/SourceSize;if(uv.x>=PaintRect.x-footprint.x&&uv.x<=PaintRect.y+footprint.x&&uv.y>=PaintRect.z-footprint.y&&uv.y<=PaintRect.w+footprint.y)uv.x=clamp(uv.x+DonorU.x-PaintRect.x,DonorU.x,DonorU.y);}\n    float4 c;\n')
assert kernel.count('if(ReplacePaint!=0)')==1
directional='--directional' in sys.argv
tag='middle8-directional' if directional else 'middle8'
frozen=OUT/(tag+'-baker');frozen.mkdir(exist_ok=True)
(frozen/'PrepareMiddleRepeat.Editor.cs').write_text(source)
(frozen/'Draw01MiddleRepeat.compute').write_text(kernel)
proposal=json.loads((EVIDENCE/'middle-finite-guard-proposal.json').read_text())
u=copy.deepcopy(json.loads((OUT/'manifest.json').read_text())['units'][0])
u.update(id='middle-asphalt-eight-metre',repeatAxes='V',finiteSourceGrid=False,finiteGridMip=0,sourceGradientSampling=True,physicalSize=[3.36602783203125,8],panelContentMin=[.25,0],panelContentMax=[.75,1],allowClampedSourceGuards=True,aliases=[])
u['panels']=[{k:p[k] for k in ('target','source','normalTransform')} for p in proposal['panels']]
u['guardPolygons']=[]
for g in proposal['guards']:
    import numpy as np
    q=np.array(g['polygon']);affine=np.array(g['sourceAffineRows']);uv=np.column_stack([q,np.ones(len(q))])@affine.T
    u['guardPolygons'].append({'adjacentPanel':g['adjacentPanel'],'target':q.reshape(-1).tolist(),'source':uv.reshape(-1).tolist()})
u['paintRect']=[787/4096,810/4096,2778/4096,3510/4096];u['donorU']=[403/4096,426/4096]
for c in u['channels']:c['encoding']=''
if directional:
    previous=json.loads((OUT/'middle8-report.json').read_text())['units'][0]['channels'][0]
    metrics=json.loads((OUT/'middle8-validation.json').read_text())['density']
    sigma=max(d['singularValues'][0] for d in metrics if d['mip']==0)
    u['minimumPixelSize']=[math.ceil(previous['width']*sigma),math.ceil(previous['height']*sigma)]
evidence={'sourceGuardProposal':'../native-atlas-road/middle-finite-guard-proposal.json','sourceSamplerEvidence':'../native-atlas-road/actual-source-samplers.json','unitScope':'Only positive-left original middle 8m cell; no transition or other-quarter extension is authorized by this recipe.','density':'Frozen producer Span computes per-panel source-texel Jacobian column lengths; its grid uses the maximum for each output axis. Explicit panelContent bounds prevent double padding.','paint':'Bounded original donor composition, not recovery of unknown substrate.','bakerSha256':hashlib.sha256(source.encode()).hexdigest(),'kernelSha256':hashlib.sha256(kernel.encode()).hexdigest(),'qualityAccepted':False}
if directional:evidence['directionalGrid']={'previousBaseGrid':[previous['width'],previous['height']],'previousMaximumSingularValue':sigma,'minimumPixelSize':u['minimumPixelSize'],'unchanged':'Source UV, physical 8m unit, normal frames, explicit guard content and original sampler. Coarse mip footprint must be considered at its actual source LOD, not compared unscaled to one full-resolution texel.'}
ep=OUT/(tag+'-evidence.json');ep.write_text(json.dumps(evidence,indent=2))
u['evidence']={'path':str(ep),'sha256':hashlib.sha256(ep.read_bytes()).hexdigest()}
control=copy.deepcopy(u);control['id']+='-control';control.pop('paintRect');control.pop('donorU')
(OUT/(tag+'-manifest.json')).write_text(json.dumps({'recipe':'BridgeBuilder/RepeatUnits/3','style':'Draw01','units':[u,control]},indent=2))
print(frozen)
