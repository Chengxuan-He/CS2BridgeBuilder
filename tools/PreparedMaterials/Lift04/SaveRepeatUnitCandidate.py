"""Retain Lift04's measured repeat unit and its manual atlas-edge observations."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]


def main():
    env = ROOT / 'artifacts/agent-material-env/Lift04/repeat-units-v2'
    candidate = env / 'candidate'
    retained = ROOT / 'artifacts/prepared-materials/Lift04/rebuild-native/repeat-candidate'
    retained.mkdir(parents=True, exist_ok=True)
    for source, name in ((candidate/'report.json', 'report.json'),
                         (candidate/'native-check.json', 'native-check.json'),
                         (candidate/'bake.log', 'bake.log'),
                         (candidate/'check.log', 'check.log'),
                         (env/'extracted-source/extraction.json', 'extraction.json')):
        shutil.copyfile(source, retained/name)
    products = json.loads((candidate/'report.json').read_text())
    assert len(products['units']) == 1 and products['producedTextures'] == 3
    unit = products['units'][0]
    assert unit['periods'] == [1, 1]
    assert all((c['width'], c['height'], c['mips']) == (638, 640, 10) for c in unit['channels'])
    extraction = json.loads((env/'extracted-source/extraction.json').read_text())
    assert extraction['status'] == 'PASS' and len(extraction['sources']) == 3
    assert all(s['mips'] == 13 and len(s['mipSha256']) == 13 and s['cachedMidMipByteEqual'] for s in extraction['sources'])
    check = json.loads((candidate/'native-check.json').read_text())
    assert check['nativeHeaders'] and check['completeGraph'] and not check['gameVisualAcceptance']
    assert len(check['levels']) == 30
    files = []
    for path in sorted((candidate/'output/Lift04').rglob('*')):
        if not path.is_file():
            continue
        relative = path.relative_to(candidate/'output/Lift04')
        target = retained/'output/Lift04'/relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
        data = target.read_bytes()
        files.append(dict(path=relative.as_posix(), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
    channels = []
    for name in sorted({l['channel'] for l in check['levels']}):
        levels = [l for l in check['levels'] if l['channel'] == name]
        assert max(l['maxLinearError'] for l in levels) <= 0.005
        assert max(l['maxRepeatSeamError'] for l in levels) <= 0.0001
        channels.append(dict(name=name, maxPixelCenterError=max(l['maxLinearError'] for l in levels),
                             maxOriginalAtlasBoundaryDifference=max(l['maxOriginalAtlasBoundaryDifference'] for l in levels)))
    graph = json.loads((retained.parent/'graph.json').read_text())
    merged = {p['path']: p for p in graph['files']}
    for entry in files:
        if entry['path'] in merged:
            assert entry['sha256'] == merged[entry['path']]['sha256']
        merged[entry['path']] = entry
    summary = dict(style='Lift04', status='VERIFIED_REPEAT_UNIT_PENDING_HUMAN_ATLAS_EDGE_ACCEPTANCE',
                   sourceChannels=3, authoredSourceMips=13, outputMips=10, outputPixels=[638,640],
                   fileCount=len(files), bytes=sum(f['bytes'] for f in files), files=files, channelErrors=channels,
                   knownNativePlusCandidateFiles=len(merged), knownNativePlusCandidateBytes=sum(p['bytes'] for p in merged.values()),
                   densityUVByUse=[[159.5,160],[159.5,160],[167.60184710107748,160],[167.60184710107748,160]],
                   sourceAtlasesPackaged=False,
                   boundaryCriterion='Compare against original atlas neighbors at every mip and report differences for human visual acceptance under the accepted existing-atlas pattern policy.',
                   unresolved=['Human visual observation of atlas-boundary differences.',
                               'Out-of-chart structural receivers and full native renderer/owner lifecycle acceptance.'],
                   localization='Offline authoring and validation only; runtime UI/localization unchanged.')
    (retained/'summary.json').write_text(json.dumps(summary, indent=2)+'\n')
    print(json.dumps({k: summary[k] for k in ('status','fileCount','bytes','knownNativePlusCandidateFiles','knownNativePlusCandidateBytes','channelErrors')},indent=2))


if __name__ == '__main__':
    main()
