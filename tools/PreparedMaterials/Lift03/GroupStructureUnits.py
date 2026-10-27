"""Group every measured Lift03 domain once; never turn a sampler or chart into a pattern."""
import collections
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/prepared-materials/Lift03/structure-semantic-v1'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def ref(path):
    return dict(path=str(path.resolve()),sha256=sha(path))


def main():
    path=OUT/'original-domain-receivers.json'
    evidence=json.loads(path.read_text())
    domains=evidence['domains']
    assert len({d['id'] for d in domains})==len(domains)
    # Match the full immutable source inventory independently of changed receivers.
    charts=ROOT/'artifacts/prepared-materials/Lift03/native-charts'
    source_report=json.loads((charts/'report.json').read_text())
    expected=set()
    native_evidence=[]
    for stream in source_report['streams']:
        detail_path=charts/(stream['stream']+'.json')
        detail=json.loads(detail_path.read_text())
        expected.update(f'{stream["name"]}|{stream["mesh"]}|{d["domain"]}' for d in detail['domains'])
        native_evidence.append(dict(render=stream['name'],mesh=stream['mesh'],lod=stream['lod'],
                                    sourceCid=stream['sourceCid'],domains=len(detail['domains']),evidence=ref(detail_path)))
    assert expected=={d['id'] for d in domains}, 'Incomplete original production inventory'
    inventory=dict(schema='BridgeBuilder/StructureDomains/1',style='Lift03',finiteScenarios=evidence['finiteScenarios'],
                   scope='Every original domain in all 20 material/LOD streams; receiver counts cover 144 sampled geometry cases, not every future width.',
                   domains=[{k:d[k] for k in ('id','sourceDigest','receivers')} for d in domains])
    inventory_path=OUT/'original-domains.json'
    inventory_path.write_text(json.dumps(inventory,separators=(',',':'))+'\n')
    grouped=collections.defaultdict(list)
    for domain in domains:
        if domain['receivers']==0: disposition='not-emitted'
        elif domain['remaining']==0: disposition='native-reuse'
        elif domain['chartCount']==0: disposition='unreliable-native-chart'
        elif domain['outsideAuthoredChart']>0: disposition='outside-mixed-atlas-island'
        elif domain['gradientFailure']>0: disposition='absolute-float32-gradient'
        else: disposition='conditioned-parent-phase'
        grouped[(domain['sourceDigest'],domain['render'],domain['mesh'],disposition)].append(domain)
    reasons={
      'not-emitted':'The recorded construction removes this source domain in all sampled cases; no material receiver is emitted. This is not a claim about unsampled future configurations.',
      'native-reuse':'Every sampled emitted receiver retains original raw UV/material or passes the production complete chart and Float32 gradient check. Keep the original native Surface and full texture graph.',
      'unreliable-native-chart':'The original physical XYZ/UV adjacency does not produce a reliable closed chart for this domain (open/non-manifold/ambiguous boundaries). A bounding rectangle would include unrelated atlas content. Required: verified original part boundary and semantic repeated detail, or native source mapping with the original pattern preserved.',
      'outside-mixed-atlas-island':'The actual width transform samples outside the original complete chart. The source atlas includes separate structural strips, panel seams, end features and road markings; the two measured road units do not contain the original domain. Required: semantic same-pattern membership/period evidence or a native representation preserving this original atlas detail, not a per-domain crop or global source rectangle.',
      'absolute-float32-gradient':'Containment alone passes but absolute atlas Float32 coordinates fail the production gradient guard. Reprojection loses source texel density. Required: numerically conditioned phase supported by a verified repeating source pattern, or equivalent native high-precision mapping; do not relax the density check.',
      'conditioned-parent-phase':'Constructed fragments require their exact unconditioned parent coordinates; a triangular-domain phase is not the period of either measured road rectangle. Required: a genuine pattern and original coverage proof for that parent, not conversion of the parent triangle into a new repeating texture.'}
    groups=[]
    receiver_evidence=ref(path)
    for index,((digest,render,mesh,kind),items) in enumerate(sorted(grouped.items())):
        groups.append(dict(id=f'Lift03-{index}-{kind}',kind='ORIGINAL' if kind in ('not-emitted','native-reuse') else 'NONREPEATING',
                           sourceDigest=digest,domains=[d['id'] for d in items],evidence=receiver_evidence,reason=reasons[kind],render=render,mesh=mesh))
    # The existing road units remain road units. No original structural domain
    # acquired new same-pattern evidence in this audit; do not bake unused units.
    manifest=dict(schema='BridgeBuilder/StructureUnits/1',style='Lift03',inventory=ref(inventory_path),units=[],groups=groups)
    (OUT/'manifest.json').write_text(json.dumps(manifest,separators=(',',':'))+'\n')
    files=[p for p in (ROOT/'assets/Materials/Lift03').rglob('*') if p.is_file()]
    total=sum(p.stat().st_size for p in files)
    assert total<=10_000_000
    report=dict(style='Lift03',originalDomains=len(domains),finiteScenarios=evidence['finiteScenarios'],sourceStreams=native_evidence,
                groupingClasses=collections.Counter(g['kind'] for g in groups),
                receivers=sum(d['receivers'] for d in domains),nativeReceivers=sum(d['original'] for d in domains),
                unresolvedReceivers=sum(d['remaining'] for d in domains),unresolvedDomains=sum(d['remaining']>0 for d in domains),
                preparedStructuralUnitBatch=[],newTextures=0,newRegions=0,
                unitDecision='No new structural periodic unit has sufficient original-pattern and complete-mip membership evidence. Existing two road units are not expanded; no artificial batch or arbitrary crop is baked.',
                files=len(files),bytes=total,remainingBudget=10_000_000-total,
                atlasEvidence=ref(OUT/'source-atlas.png'),pixelInspection='Original atlas contains distinct panel joints, end hardware, slots, patterned strips and road paint. A rectangular chart or similar base color is not repeated-detail evidence.',
                localization='Offline evidence only; no UI strings changed',productionGenerationVerified=False)
    (OUT/'summary.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ('originalDomains','finiteScenarios','receivers','nativeReceivers','unresolvedReceivers','unresolvedDomains','files','bytes','remainingBudget')},indent=2))


if __name__=='__main__':main()
