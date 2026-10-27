"""Bounded native-template/affine feasibility check using real Lift05 samplers."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('mapping', type=Path)
    parser.add_argument('research', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    mapping = json.loads(args.mapping.read_text())
    evidence = []
    for template_id in (0, 2, 14):
        path = args.research / f'template-{template_id}.json'
        material = json.loads(path.read_text())
        shader_id = material['m_Shader']['m_PathID']
        shader_path = args.research / f'shader-{shader_id}-parsed.json'
        entry = dict(templateId=template_id, material=material['m_Name'],
                     templateSha256=hashlib.sha256(path.read_bytes()).hexdigest(), shaderPathId=shader_id)
        if shader_path.exists():
            shader = json.loads(shader_path.read_text())
            names = [p['m_Name'] for p in shader['m_PropInfo']['m_Props']]
            entry.update(shaderSha256=hashlib.sha256(shader_path.read_bytes()).hexdigest(),
                propertyCount=len(names), samplingPropertyNames=[n for n in names if re.search('uv|til|offset|atlas|region|rotat|repeat|wrap', n, re.I)],
                allPropertyNames=names)
        else: entry['shaderPropertyInspection'] = 'NOT_AVAILABLE'
        evidence.append(entry)
    representatives=[]
    for group in mapping['groups']:
        selected=[r for r in mapping['structureSamplers'] if r['sourceCid']==group['sourceCid']]
        for triangle in (False,True):
            records=[r for r in selected if r['triangleReflection']==triangle]
            if not records: continue
            record=records[0]; affine=record['sourceUvAffine']
            def direct(q):
                return [affine['origin'][i]+affine['columnX'][i]*q[0]+affine['columnY'][i]*q[1] for i in range(2)]
            def intended(q):
                local=[v % 1 for v in q]
                reflected=triangle and sum(local)>1
                if reflected: local=[1-v for v in local]
                return direct(local),reflected
            samples=[]
            for q in ([.2,.3],[.7,.6],[1.2,.3],[-.2,.3]):
                expected,reflected=intended(q); linear=direct(q)
                error=max(abs(a-b) for a,b in zip(expected,linear))
                samples.append(dict(localUv=q,regionRepeatSourceUv=expected,affineOnlySourceUv=linear,
                    maxUvDifference=error,requiresReflectedNormal=reflected))
            representatives.append(dict(key=record['key'],sourceCid=group['sourceCid'],triangle=triangle,
                samples=samples,affineOnlyEquivalent=all(s['maxUvDifference']==0 for s in samples)))
    assert any(not r['affineOnlyEquivalent'] for r in representatives)
    report=dict(status='CAPABILITY_GAP_NOT_IMPLEMENTED',
        method='Native template property inspection and exact source-coordinate counterexamples; not a rendering acceptance test',
        mappingSha256=hashlib.sha256(args.mapping.read_bytes()).hexdigest(),nativeTemplateEvidence=evidence,
        representativeSources=representatives,
        constraints=['Single full-source texture with native repeat wraps the full texture, not an arbitrary internal region',
            'Affine UV mapping alone cannot implement periodic local frac or diagonal triangle reflection',
            'Triangle reflected half must rotate tangent-space normals as well as sample coordinates',
            'Original full-atlas mip filtering and prepared region-isolated mip filtering are not interchangeable without proof',
            'Current required source rectangles/triangle affine records must survive independently from the texture identity'],
        sharedInterfaceRequired=['Native per-use region repeat with local-wrap-before-affine semantics',
            'Native triangular fold and corresponding tangent-space normal orientation',
            'Region-correct filtered mip behavior and Float32 gradient fidelity',
            'Loader/key schema separating source channel content from sampler metadata'],
        assetsModified=False,bakedTextures=0,customRuntimeShaderCreated=False,
        limitation='No supported native hook for these non-affine per-region operations has been established by this bounded check; absence of a named property alone is not proof that all native mechanisms are impossible')
    assert not args.output.exists()
    args.output.write_text(json.dumps(report,indent=2)+'\n')
    print(f'Lift05 finite check: {len(representatives)} real sampler cases; native templates inspected; common sampling capability required; no assets changed.')


if __name__ == '__main__': main()
