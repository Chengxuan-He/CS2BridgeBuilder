"""Generate Lift05 native source identities and separate immutable sampling records."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile
from MapSharedSampling import surface


def main():
    p=argparse.ArgumentParser();p.add_argument('repo',type=Path);p.add_argument('game',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();repo=a.repo.resolve();assert not a.output.exists()
    mapping=json.loads((repo/'artifacts/prepared-materials/Lift05/shared-sampling/source-sampling.json').read_text())
    render=(repo/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05SourceRenders.cs').read_text(encoding='utf-8-sig')
    ids={v for group in re.findall(r'new\[\] \{ ([^}]+) \}',render) for v in re.findall(r'"([0-9a-f]{32})"',group)}
    assert len(ids)>=len(mapping['sources'])
    raw={}
    for family in ('Game','BridgesAndPorts'):
        for path in (a.game/'Content'/family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as z:
                for entry in z.namelist():
                    if not entry.endswith('.Surface.cid'):continue
                    cid=z.read(entry).decode().strip()
                    if cid not in ids:continue
                    data=z.read(entry[:-4]);assert cid not in raw or raw[cid]==data;raw[cid]=data
    assert raw.keys()==ids
    a.output.mkdir(parents=True);(a.output/'native-sources').mkdir()
    sources=[]
    for cid,data in sorted(raw.items()):
        sources.append(dict(cid=cid,sha256=hashlib.sha256(data).hexdigest(),ownership='builtin',native=surface(data)))
        (a.output/'native-sources'/f'{cid}.Surface').write_bytes(data)
    samplers=[]
    for item in mapping['structureSamplers']:
        sample=item['originalSampling']
        bits={name:[struct.pack('<f',v).hex() for v in sample[name]] for name in ('bounds','a','b','c')}
        samplers.append(dict(key=item['key'],sourceSurfaceCid=item['sourceCid'],kind='structure',
            triangleCoordinates=sample['triangle'],float32LittleEndian=bits,
            policy='bind original only when actual receiver containment and Float32 gradient fidelity pass',
            unsupported='receiver outside original source domain or insufficient Float32 gradient precision',
            legacyTriangleFoldIsPrototypeRequirement=False))
    for item in mapping['repeatSamplers']:
        samplers.append(dict(sourceSurfaceCid=item['sourceCid'],kind='fixed-road-repeat',
            float32LittleEndian={name:[struct.pack('<f',v).hex() for v in item[name]] for name in ('min','max')},
            cutout=item['cutout'],rows=item['rows'],bands=item['bands'],
            policy='requires validated native region repetition and object-template compatibility before replacing prepared repeat'))
    manifest=dict(schema='Lift05/NativeSourceSampling/1',sources=sources,samplers=samplers,
        newTexturePayloads=0,sourceOwnership='builtin native CIDs retained; non-builtin snapshots belong to existing common persistence',
        activeUnsupportedBranches=['unvalidated source-domain extension','Float32 precision failures','fixed-road local repetition'])
    (a.output/'native-source-sampling.json').write_text(json.dumps(manifest,indent=2)+'\n')
    lines=['// Generated from verified Lift05 native Surface bytes; no texture copies.','using System.Collections.Generic;','',
           'namespace BridgeBuilder.Bridges;','internal static class Lift05NativeSourceCatalog','{',
           '    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>','    {']
    lines.extend(f'        ["{s["cid"]}"] = "{s["sha256"]}",' for s in sources)
    lines.extend(['    };','}',''])
    (repo/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05NativeSourceCatalog.g.cs').write_text('\n'.join(lines),encoding='utf-8')
    print(f'Lift05 native bindings: {len(sources)} original source identities; {len(samplers)} exact sampling records; no texture payloads.')


if __name__=='__main__':main()
