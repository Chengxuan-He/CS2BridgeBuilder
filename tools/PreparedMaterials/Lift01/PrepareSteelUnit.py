"""Prepare one measured painted-steel base unit and its native descriptor variants."""
from pathlib import Path
import hashlib
import json
import shutil
import struct
import numpy as np
from BuildUvCharts import inputs

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/prepared-materials/Lift01/structure-semantic'
ENV=ROOT/'artifacts/agent-material-env/Lift01/structure-semantic/unit'


def ref(p):return dict(path=str(p.resolve()),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
def save(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n')


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    name,mesh,p,uv,faces=next(x for x in inputs() if x[0]=='LiftBridge01Net Mesh')
    chosen=[12,13,396,397]
    points=p[faces[chosen]];coords=uv[faces[chosen]]
    low=coords.min(axis=(0,1));high=coords.max(axis=(0,1))
    assert low.tolist()==[0.481201171875,0.2587890625]
    assert high.tolist()==[0.50634765625,0.348876953125]
    period=[float(np.ptp(points[:,:,1])),float(np.ptp(points[:,:,2]))]
    assert np.ptp(points[:,:,0])==0
    coverage=[1536/4096,1060/4096,2293/4096,1761/4096]
    old=ROOT/'artifacts/prepared-materials/Lift01/repeat-units-v2/candidate/producer-input.json'
    channels=json.loads(old.read_text())['units'][0]['channels']
    for c in channels:assert ref(Path(c['path']))['sha256']==c['sha256']
    evidence=dict(style='Lift01',semanticPattern='Fine red protective-coating grain on the original planar steel girder web; no brick, paving, wood, bolts or grating detail is in this measured base unit.',
        render=name,mesh=mesh,faces=chosen,positions=points.tolist(),uv=coords.tolist(),min=low.tolist(),max=high.tolist(),physicalPeriod=period,
        pixels=((high-low)*4096).tolist(),texelsPerMeter=((high-low)*4096/period).tolist(),
        coverage=coverage,coverageMeaning='Conservative interior of the same red-coating atlas region, identified in the original BaseColor atlas and matched to original source UV domains. Coverage is not the repeat period; source-specific templates are retained.',
        sourceImage=ref(ROOT/'artifacts/agent-material-env/Lift01/structure-semantic/source-atlas.png'),
        boundaryAcceptance='Existing-atlas coating noise and edge-pattern differences are recorded for human review. This does not authorize changing density, normal interpretation, detail-bearing atlas regions or geometry.',
        sourceChannels=channels,sourceMipEvidence=ref(ROOT/'artifacts/prepared-materials/Lift01/native-object-candidates/native-check.json'),
        sourceFixture=ref(ROOT/'artifacts/uv-density/layout-plans.bin'))
    save(OUT/'steel-evidence.json',evidence)
    graph=json.loads((ROOT/'artifacts/prepared-materials/Lift01/rebuild-native-v1/config/source-graph.json').read_text())
    sources={s['cid']:s for s in graph['sources']}
    ids=['bcdd054871e3bcb50f793ccefc50304d','368eb4e7fa7b81f6e28ee321c76dd9f2','6cea3c92f68858af086f7811c19021b6','bdd173229f9bad122a065fc6032c6e4d']
    units=[]
    for cid in ids:
        s=sources[cid]
        path=ROOT/'artifacts/prepared-materials/Lift01/rebuild-native-v1/sources'/f'{cid}.Surface'
        if s['template']==33:path=ROOT/'assets/Materials/Lift01/NativeSources/2d94a491d55ac415c29591597618b5ef/Object.Surface'
        assert path.is_file()
        units.append(dict(id='Lift01.PaintedSteelWeb.'+cid,sourceCid=cid,sourceDigest=s['sha256'],min=low.tolist(),max=high.tolist(),physicalSize=period,evidence=ref(OUT/'steel-evidence.json'),descriptor=ref(path),channels=channels,aliases=[]))
    save(ENV/'inputs/manifest.json',dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift01',units=units))
    save(OUT/'producer-input.json',dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift01',units=units))
    (OUT/'inputs.json').write_bytes((ROOT/'artifacts/agent-material-env/Lift01/structure-semantic/inputs.json').read_bytes())
    shutil.copyfile(ROOT/'artifacts/agent-material-env/Lift01/structure-semantic/receivers.json',OUT/'receivers-before.json')
    # The native checker reads every original/output mip, not generated mip chains.
    code=(ROOT/'tools/PreparedMaterials/Lift01/RepeatUnitCheck.Editor.cs').read_text()
    code=code.replace('Lift01RepeatUnitCheck','Lift01SteelUnitCheck').replace('manifest.units.Single()','manifest.units.First()').replace('baked.units.Single()','baked.units.First()')
    code=code.replace('output.width==432&&output.height==860&&output.mipmapCount==10','output.width==103&&output.height==369&&output.mipmapCount==9')
    code=code.replace('Assets/Lift01SteelUnitCheck.compute','Assets/Lift01RepeatUnitCheck.compute').replace('30 channel/mip levels','27 channel/mip levels')
    (ROOT/'tools/PreparedMaterials/Lift01/SteelUnitCheck.Editor.cs').write_text(code)
    shutil.copyfile(ROOT/'tools/PreparedMaterials/Lift01/SteelUnitCheck.Editor.cs',ROOT/'artifacts/agent-material-env/Lift01/unity/Assets/Editor/Lift01SteelUnitCheck.cs')
    print(json.dumps(dict(units=len(units),distinctPixelUnits=1,pixels=evidence['pixels'],physicalPeriod=period,manifest=str(ENV/'inputs/manifest.json'))))


if __name__=='__main__':main()
