"""Prepare fresh Lift04 source-level inputs; never consume generated material files."""
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys

from VerifyCoverage import read_assets

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
RUN = ROOT/'artifacts/agent-material-env/Lift04/source-rebuild'


def main():
    config = RUN/'config'
    config.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(Path(__file__).with_name('ExportSharedSampling.py')),
                    '--repo',str(ROOT),'--game-data',str(GAME),'--output',str(config/'source-sampling.json')], check=True)
    catalog = json.loads((config/'native-bindings.json').read_text())
    manifest = dict(style='Lift04', sources=[dict(cid=s['cid'],sha256=s['sourceSHA256'],
        template=s['template'],mode='original-builtin-reference',
        renderUses=[dict(render=r['render'],mesh=i) for r in catalog['renderBindings']
                    for i,cid in enumerate(r['originalSurfaceCids']) if cid==s['cid']]) for s in catalog['surfaces']])
    (config/'sources.json').write_text(json.dumps(manifest,indent=2)+'\n')
    # Keep the verified source identities outside disposable environments.
    Path(__file__).with_name('native-sources.json').write_text(json.dumps(manifest,indent=2)+'\n')
    shared = dict(Paint='4b5500b1b5c270890d99149f7f62e51a',
                  Cable='106cc56e9f15a135bb6ea7b99d675c2d',
                  Road='1d7dad9f64c9fe55a809f1080c39fb92')
    source_ids = {s['cid'] for s in catalog['surfaces']} | set(shared.values())
    originals = read_assets(GAME, source_ids)
    source_dir = config/'original-descriptors'
    source_dir.mkdir(exist_ok=True)
    for cid, payload in originals.items():
        (source_dir/(cid+'.Surface')).write_bytes(payload)
    # Native bytes are verification inputs only. Compatible built-ins must remain
    # references, not packaged copies, including all original lower mip data.
    pixels = read_assets(GAME,{t['sourceTextureCid'] for t in catalog['textures']})
    for item in catalog['textures']:
        raw = pixels[item['sourceTextureCid']]
        assert hashlib.sha256(raw).hexdigest()==item['nativePayloadSHA256']
        assert len(raw)==item['nativeBytes']
    repeat = json.loads(Path(__file__).with_name('recipes.json').read_text())['units'][0]
    report = dict(schema='Lift04/SourceLevelRebuild/1',style='Lift04',gameData=str(GAME),
        output=str(RUN/'out'),allowOldProductStore=False,allowPerSamplerPixelBake=False,
        sourceDescriptorInputs=[dict(cid=cid,path=str(source_dir/(cid+'.Surface')),
            sha256=hashlib.sha256(raw).hexdigest(),template=struct.unpack_from('<i',raw,2)[0],
            packageOriginal=False) for cid,raw in sorted(originals.items())],
        prototypeBindings=catalog['renderBindings'],
        prototypeTexturePayloads=catalog['textures'],
        originalSourcePolicy='Compatible builtin Surfaces and complete native mip dependencies remain direct references; no pixel decode or conversion.',
        repeatRequests=[dict(sourceCid=repeat['sourceCid'],sourceDigest=repeat['sourceDigest'],
            regionMin=repeat['min'],regionMax=repeat['max'],cutout=repeat['cutout'],
            appearanceAliases=repeat['regions'],pixelPayloadKey='original-source-content',
            samplerImplementation='Requires shared native region mapping; never a per-region image fallback')],
        commonRequests=[dict(category=kind,sourceCid=cid,mode='Source-level descriptor conversion only where required; preserve full native texture graph') for kind,cid in shared.items()]+
            [dict(category='Lanes',mode='Shared original native template conversion inventory; no old generated lane files')],
        measured=dict(prototypeSurfaces=len(catalog['surfaces']),prototypePayloads=len(pixels),
            prototypeNativeBytes=sum(map(len,pixels.values())),newPixelFiles=0,newDescriptorFiles=0,
            oldGeneratedMaterialFilesRead=0),
        state='SOURCE_INPUTS_VERIFIED_AWAITING_SHARED_PRODUCER',
        localization='No runtime user-visible text or locale behavior changes.')
    (config/'rebuild.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report['measured']))


if __name__=='__main__':main()
