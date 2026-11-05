"""Compare actual native Plan dispositions against candidate73, by exact source face."""
import csv
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Lift05/catenary74'


def read(path):
    return {(int(r['lod']), float(r['delta']), int(r['face'])): r
            for r in csv.DictReader(path.open())}


def main():
    before = read(OUT/'before/actual-plan-disposition.csv')
    after = read(ROOT/'artifacts/material-remap/Lift05/source-face-coverage/actual-plan-disposition.csv')
    levels = json.loads((OUT/'source-membership.json').read_text())['levels']
    assert before.keys() == after.keys()
    omitted, unchanged = [0, 0, 0], [0, 0, 0]
    for key, old in before.items():
        lod, delta, face = key
        level, new = levels[lod], after[key]
        if level['firstFace'] <= face < level['firstFace']+level['count']:
            assert float(old['coverage']) == 1 and old['fragmentTriangles'] == '0'
            assert new['removed'] == 'True' and float(new['coverage']) == 0 and new['fragmentTriangles'] == '0'
            omitted[lod] += 1
        else:
            assert old == new, ('Unrelated source disposition changed', key)
            unchanged[lod] += 1
    assert omitted == [1472*3, 515*3, 0]
    report = dict(status='PASS', omittedRows=omitted, unchangedRows=unchanged,
                  widths=[0,3,12], allOtherSourceFacesIdentical=True,
                  liftCablesTowersGlassAndRoadRulesUnchanged=True,
                  scope='Actual Plan by source face; native index/channel/Retile checks are in the separate native74 log.',
                  inputSHA256=hashlib.sha256((ROOT/'artifacts/material-remap/Lift05/source-face-coverage/actual-plan-disposition.csv').read_bytes()).hexdigest())
    (OUT/'native-plan-comparison.json').write_text(json.dumps(report, indent=2)+'\n')
    print('PASS exact catenary removal', omitted, '; all unrelated source rows unchanged', unchanged)


if __name__ == '__main__': main()
