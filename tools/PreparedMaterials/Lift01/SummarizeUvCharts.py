"""Summarize existing native-chart and receiver evidence without rerunning geometry."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Lift01/native-charts'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    charts = json.loads((OUT / 'report.json').read_text())
    receiver = json.loads((OUT / 'receivers.json').read_text())
    binary = ROOT / 'assets/HardcodedNumbers/Lift01/Lift01UvCharts.bin'
    assert digest(binary) == charts['bbnSha256']
    cases = receiver['reports']
    result = {
        'style': 'Lift01',
        'chartResource': str(binary.relative_to(ROOT)),
        'chartBytes': binary.stat().st_size,
        'chartSha256': digest(binary),
        'chartReportSha256': digest(OUT / 'report.json'),
        'receiverReportSha256': digest(OUT / 'receivers.json'),
        'streams': charts['streamCount'],
        'domains': charts['domainCount'],
        'uniqueChartShapes': charts['uniqueChartShapes'],
        'uniqueDomainSets': charts['uniqueDomainSets'],
        'emptyRankOneDomains': sum(s['emptyDomains'] for s in charts['streams']),
        'allDomainResourcesCovered': charts['allDomainResourcesCovered'],
        'originalFaceCoverage': 'Exact 2D union coverage proved per physical component; original triangle fragments retained when proof fails.',
        'receiverInputs': receiver['evaluatedInputs'],
        'receiverCases': len(cases),
        'widthDeltas': sorted({r['delta'] for r in cases}),
        'changedReceivers': sum(r['changedReceivers'] for r in cases),
        'changedReceiversContained': sum(r['changedContained'] for r in cases),
        'oracle': 'Independent conservative boundary/hole containment; boundary epsilon 1e-12; not public native implementation.',
        'publicContainmentVerified': False,
        'nativeShaderRenderingVerified': False,
        'allGeneratedReceiversSupported': False,
        'texturesCreated': 0,
        'geometryChanged': False,
        'repeatInferred': False,
        'oldResourcesDeleted': 0,
        'remaining': [
            'Integrate shared MovableUvChart.Read/ContainsTriangle and original-UV material selection.',
            'Handle original rank-one UV domains explicitly; no invented 2D footprint.',
            'Handle generated fragment Float32 interpolation beyond original chart boundaries without silent clamping or chart expansion.',
            'Represent widened receivers outside original charts before retiring prepared Structure resources.',
            'Verify native shader, normal orientation, all mip boundaries and owner-local preview/create/regenerate/export/cold-load/uninstall flows.',
            'Measure the official SDK aggregate package; this chart resource size is not a package acceptance result.'
        ],
        'localization': 'No UI or generated display text changes; technical diagnostics retain existing localized failure handling.'
    }
    (OUT / 'delivery.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
