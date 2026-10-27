"""Record corrected VT provenance, exact channel reuse and bounded-unit gaps."""
import hashlib
import json
import shutil
from fractions import Fraction
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ENV = ROOT / 'artifacts/agent-material-env/Draw01'
OLD = ENV / 'repeat-units-v2'
NEW = ENV / 'repeat-units-control-corrected'
OUT = ROOT / 'artifacts/prepared-materials/Draw01/repeat-units-control-corrected'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    old = read(OLD / 'extracted-source/manifest.json')['units'][0]
    new = read(NEW / 'extracted-source/manifest.json')['units'][0]
    report = read(NEW / 'report.json')
    assert (report['producedTextures'], report['reusedTextures']) == (1, 3)
    changes = []
    for before, after in zip(old['channels'], new['channels']):
        assert before['name'] == after['name']
        assert sha(Path(after['path'])) == after['sha256']
        changed = before['sha256'] != after['sha256']
        assert changed == (after['name'] == '_ControlMask')
        changes.append(dict(channel=after['name'], oldSha256=before['sha256'],
                            correctedSha256=after['sha256'], changed=changed))
    old_products = {p['name']: p for p in read(OLD / 'report.json')['units'][0]['channels']}
    for product in report['units'][0]['channels']:
        if product['name'] == '_ControlMask':
            continue
        before = old_products[product['name']]
        assert before == product
        filename = product['cid'] + '.Texture'
        assert (OLD / 'output/Draw01/Shared' / filename).read_bytes() == (
            NEW / 'output/Draw01/Shared' / filename).read_bytes()
    requirements = read(ROOT / 'artifacts/prepared-materials/Draw01/genuine-unit-requirements/manifest.json')
    candidates = []
    for candidate in requirements['roadCandidates']:
        spans = [Fraction((b-a)*n) for a, b, n in zip(
            candidate['region']['min'], candidate['region']['max'], candidate['sourceDimensions'])]
        periods = [x.denominator for x in spans]
        dimensions = [int(x*p) for x, p in zip(spans, periods)]
        candidates.append(dict(id=candidate['candidateId'], sourceTexelSpan=[float(x) for x in spans],
                               minimumExactPeriods=periods, exactDimensions=dimensions,
                               fits16384=all(n <= 16384 for n in dimensions)))
    style = ROOT / 'assets/Materials/Draw01'
    files = sorted(p for p in style.rglob('*') if p.is_file())
    inventory = [dict(path=p.relative_to(style).as_posix(), bytes=p.stat().st_size, sha256=sha(p)) for p in files]
    for name in ['report.json', 'native-check.json', 'bake.log', 'native.log']:
        shutil.copyfile(NEW / name, OUT / name)
    shutil.copyfile(NEW / 'extracted-source/extraction.json', OUT / 'extraction.json')
    shutil.copyfile(OLD / 'inputs/manifest-vt-sources.json', OUT / 'manifest-vt-sources.json')
    result = dict(controlTailOffset='498 + high', channelComparison=changes,
                  fractionalCandidates=candidates, sourceMaterialFiles=len(files),
                  sourceMaterialBytes=sum(p.stat().st_size for p in files), files=inventory)
    (OUT / 'verification.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k != 'files'}, indent=2))


if __name__ == '__main__':
    main()
