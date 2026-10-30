"""Separate removable period stations from retained finite appearance domains.

This emits authoring evidence only. It never installs an unvalidated derivative.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/PedestrianDraw02'


def main():
    source = OUT / 'native-panel-recipe.json'
    recipe = json.loads(source.read_text())
    levels = []
    for level in recipe['levels']:
        repeated = [p for p in level['panels'] if p['repeatedV']]
        finite = [p for p in level['panels'] if not p['repeatedV']]
        assert len(repeated) == 32 and len(finite) == 20
        groups = {}
        for p in repeated:
            # Four physical half-leaves, not one domain for every atlas reset.
            key = (p['bone'], p['physicalMin'][0] + p['physicalMax'][0] > 0)
            groups.setdefault(key, []).append(p)
        assert len(groups) == 4
        domains = []
        for (bone, right), panels in sorted(groups.items()):
            panels.sort(key=lambda p: p['physicalMin'][1])
            domains.append(dict(bone=bone, rightHalf=right,
                sourcePanels=[p['firstFace'] for p in panels],
                physicalStart=min(p['physicalMin'][1] for p in panels),
                physicalEnd=max(p['physicalMax'][1] for p in panels),
                observedCellLengths=[p['physicalSize'][1] for p in panels],
                repeatU=False, repeatV=True,
                removedInternalPeriodStations=[p['physicalMax'][1] for p in panels[:-1]],
                projection='Continuous V; retain finite U and original orientation. No reset geometry.',
                phaseStatus='Native station quantization varies; validate accumulated phase against source samples.'))
        levels.append(dict(lod=level['lod'], repeatDomains=domains,
            retainedFinitePanels=[dict(firstFace=p['firstFace'], bone=p['bone'],
                physicalMin=p['physicalMin'], physicalMax=p['physicalMax']) for p in finite],
            removedPeriodBoundaryCount=sum(len(d['removedInternalPeriodStations']) for d in domains)))
    result = dict(schema=1, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
        levels=levels, status='AUTHORING_PLAN_NOT_ACCEPTED',
        candidateUnitPhysicalPeriod=recipe['levels'][0]['movingPhysicalPeriod'],
        retainedBoundaries=['Physical footprint and original motion ownership',
            'Finite fixed, tip and hinge appearance projections',
            'Finite transverse projection gradient where independently necessary'],
        outstanding=['All-mip filtered atlas-neighbour equivalence failed for the old candidate.',
            'Native hinge tangent-field residual remains unresolved.',
            'Final mapping must remove period-only stations, not repurpose them as material boundaries.',
            'Complete native graph loading and owner/package budgets remain acceptance gates.'])
    (OUT / 'repeat-replacement-plan.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(dict(lods=len(levels), removedPerLod=levels[0]['removedPeriodBoundaryCount'],
        repeatDomainsPerLod=4, retainedFinitePanelsPerLod=20, status=result['status'])))


if __name__ == '__main__':
    main()
