"""Classify native finite UV0 atlas traversals independently of Retile outcomes."""
import collections
import hashlib
import io
import json
import struct
import sys
import zipfile
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'artifacts/agent-material-env/PedestrianDraw01/python-deps'))
sys.path.insert(0, str(ROOT / 'tools/PreparedMaterials/Draw01'))
from BuildUvCharts import domains, read_bbn
from MovableUvDomainFormat import write_bbn
from shapely.geometry import Polygon, box
from ExportSharedSampling import count, string, present, descriptor

STYLE = 'PedestrianDraw01'
BASE = ROOT / 'artifacts/prepared-materials' / STYLE
NUMBERS = ROOT / 'assets/HardcodedNumbers' / STYLE


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    mapping = json.loads((BASE / 'actual-retile/source-map.json').read_text())
    graph = json.loads((BASE / 'shared-sampling/native-bindings.json').read_text())
    topology_path = BASE / 'shared-sampling/native-charts.json'
    topology = json.loads(topology_path.read_text())
    template_path = ROOT / 'tools/PrepareLanes/NativeTemplates.json'
    templates = {t['hash']: t for t in json.loads(template_path.read_text())}
    windows_root = BASE.parent / 'shared-nontiling/windows'
    semantics_root = BASE.parent / 'shared-nontiling/native-semantics'
    windows_template = json.loads((semantics_root / 'template-2.json').read_text())
    assert windows_template['m_Name']=='Windows' and windows_template['m_Shader']==dict(m_FileID=0,m_PathID=550)
    template_binding = next(t for t in json.loads((semantics_root / 'templates-current.json').read_text()) if t['hash']==2)
    assert template_binding['verifiedAgainstCurrentResources'] and template_binding['nativePathId']==127
    vertex_shader = (semantics_root / '550-152.asm').read_text()
    fragment_shader = (semantics_root / '550-184.asm').read_text()
    assert 'mov o2.xyzw, v3.xyzw' in vertex_shader and 'mov o6.xyzw, v7.xyzw' in vertex_shader
    assert 'mad r9.xy, v2.zyzz' in fragment_shader and 'mul r7.xyz, r7.xyzx, v2.wwww' in fragment_shader
    assert 'mad r6.xyzw, v6.xyzw, l(255.000000' in fragment_shader
    windows_evidence = []
    # Read original built-in archives, so regeneration survives environment cleanup.
    archive_dirs = {Path(n['archive']).parent for n in graph['nativeDependencyGraph'].values() if 'archive' in n}
    wanted = {s['sourceCid'] for s in mapping}
    payloads = {}
    for directory in sorted(archive_dirs):
        for path in sorted(directory.glob('Blob*.cok')):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid'): continue
                    identity = archive.read(entry).decode().strip()
                    if identity not in wanted: continue
                    raw = archive.read(entry[:-4])
                    if identity in payloads: assert payloads[identity][0] == raw
                    payloads[identity] = raw, str(path), entry[:-4]
    sources = {}
    for identity in sorted(wanted):
        raw, archive, entry = payloads[identity]
        assert hashlib.sha256(raw).hexdigest() == graph['nativeDependencyGraph'][identity]['sha256']
        template, header, combined, bindings, keywords = descriptor(raw)
        reader = io.BytesIO(raw); reader.read(7)
        if header:
            reader.read(276)
            if present(reader): reader.read(16)
        properties = {}
        for name, size, fmt in [('float',4,'<f'),('int',4,'<i'),('vector',16,'<4f'),('color',16,'<4f')]:
            properties[name] = {}
            for _ in range(count(reader)):
                key = string(reader)
                properties[name][key] = struct.unpack(fmt,reader.read(size))
        # Unreviewed Surface overrides cannot inherit a template-only proof.
        overrides_safe = all(k == '_Snow' for p in properties.values() for k in p)
        native_template = templates.get(template)
        semantics = None
        if template in (0,1) and native_template and overrides_safe and set(keywords)<= {'_TANGENTSPACE_OCTO'}:
            saved = native_template['material']['m_SavedProperties']
            floats, colors = dict(saved['m_Floats']), dict(saved['m_Colors'])
            assert all(floats[k] == 0 for k in ('_UVBase','_UVDetail','_UVEmissive'))
            assert colors['_UV0Scale'] == dict(r=1.,g=1.,b=0.,a=0.)
            assert colors['_UV0Offset'] == dict(r=0.,g=0.,b=0.,a=0.)
            assert colors['_UVMappingMask'] == dict(r=1.,g=0.,b=0.,a=0.)
            semantics = dict(shader=native_template['material']['m_Shader'], uvChannel=0,
                scale=[1,1],offset=[0,0],mappingMask=[1,0,0,0],template=template)
        elif template==2 and overrides_safe and set(keywords)<= {'_TANGENTSPACE_OCTO'}:
            semantics = dict(shader=windows_template['m_Shader'],template=2,
                localCoordinates='UV0.YZ',orientation='UV0.W',selection='COLOR',
                nativeTemplateBinding=template_binding,templateSha256=sha(semantics_root/'template-2.json'),
                vertexShaderSha256=sha(semantics_root/'550-152.asm'),fragmentShaderSha256=sha(semantics_root/'550-184.asm'))
        sources[identity] = dict(cid=identity,sha256=hashlib.sha256(raw).hexdigest(),archive=archive,entry=entry,
            template=template,properties=properties,keywords=keywords,semantics=semantics,
            dependency=graph['nativeDependencyGraph'][identity],
            textureBindings=graph['sourceTextureBindings'].get(identity,{}))
    repeat_path = NUMBERS / 'Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.bin'
    values = read_bbn(repeat_path)[4]
    regions = [[float(values[i]) for i in (3,27,4,5)], [float(values[i]) for i in (3,25,4,5)]]
    repeats = [box(*r) for r in regions]
    encoded, records = [1,len(mapping)], []
    for source in mapping:
        render, mesh = source['render'],source['mesh']
        assert source['sourceDigest'] == sources[source['sourceCid']]['sha256']
        native_path = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        recipe_path = NUMBERS / ('UvDomain_' + render.replace(' ','_') + '_' + str(mesh) + '.bin')
        topo = next(m for m in topology['meshes'] if m['render']==render and m['mesh']==mesh)
        assert sha(native_path) == topo['nativeMeshSHA256']
        native = np.load(native_path)
        uv = native[f'mesh{mesh}_uv0'][:,:2].astype(float)
        faces = native[f'mesh{mesh}_faces']
        triangles = uv[faces]
        window_domains = None
        if sources[source['sourceCid']]['template']==2:
            proof_path = windows_root / (STYLE+'-'+render.replace(' ','_')+'-'+str(mesh)+'.json')
            proof = json.loads(proof_path.read_text())
            assert proof['render']==render and proof['mesh']==mesh and proof['style']==STYLE
            assert sha(Path(proof['geometry']))==proof['geometrySha256']
            full_uv, colors = np.array(proof['uv0']), np.array(proof['colors'])
            assert np.array_equal(faces,np.array(proof['indices'])) and np.array_equal(uv,full_uv[:,:2])
            local = full_uv[faces,1:3]
            finite = np.all((local>=0)&(local<=1),axis=(1,2))
            orientation = np.all(full_uv[faces,3]==full_uv[faces,3][:,:1],axis=1)
            selection = np.all(colors[faces]==colors[faces][:,:1,:],axis=(1,2))
            local_faces = np.flatnonzero(finite & orientation & selection).tolist()
            assert local_faces==proof['sourceLocalFaces']
            window_domains = [d['index'] for d in domains(recipe_path) if all(f in local_faces for f in d['faces'])]
            assert window_domains==proof['sourceLocalDomains']==[1,2]
            windows_evidence.append(dict(**proof,proofSha256=sha(proof_path),sourceCid=source['sourceCid'],sourceDigest=source['sourceDigest']))
        occurrences = [o for o in topology['occurrences'] if o['render']==render and o['mesh']==mesh]
        traversal = {}
        for occurrence in occurrences:
            points = triangles[occurrence['faces']]
            finite = bool(np.isfinite(points).all() and np.all(points>=0) and np.all(points<=1))
            for face in occurrence['faces']: traversal[face] = finite
        assert len(traversal)==len(faces)
        flags, groups = [], collections.defaultdict(list)
        for domain in domains(recipe_path):
            tri = triangles[domain['faces']]
            if sources[source['sourceCid']]['semantics'] is None:
                reason = 'unresolved-native-semantics'
            elif window_domains is not None:
                reason = 'non-tiling-native-windows-local-yz' if domain['index'] in window_domains else 'density-windows-local-repeat-traversal'
            elif not all(traversal[f] for f in domain['faces']):
                reason = 'unresolved-chart-traverses-native-atlas-boundary'
            elif source['sourceDigest']=='9339aa46ba254430ed01aa6c7f858bfc612dabb3bea70bdc01b31bfb7922ec3c' and any(Polygon(t).intersects(r) for t in tri for r in repeats):
                reason = 'density-known-repeat-region-or-overlap'
            else:
                reason = 'non-tiling-finite-native-uv0-chart'
            flags.append(int(reason in ('non-tiling-finite-native-uv0-chart','non-tiling-native-windows-local-yz')))
            groups[reason].append(domain['index'])
        encoded.extend([len(flags),*flags])
        records.append(dict(**source,lod=2 if '_LOD2' in render else 1 if '_LOD1' in render else 0,
            materialSlot=mesh,nativeSha256=sha(native_path),domainSha256=sha(recipe_path),
            domainCount=len(flags),uvMin=uv.min(axis=0).tolist(),uvMax=uv.max(axis=0).tolist(),
            nonTilingAxes=['UV0.Y','UV0.Z'] if window_domains is not None else ['U','V'],classifications=dict(groups),physicalChartOccurrences=len(occurrences)))
    out = BASE / 'non-tiling'; out.mkdir(exist_ok=True)
    resource = NUMBERS / 'PedestrianDraw01UvSampling.bin'
    raw = write_bbn(encoded,[])
    totals = collections.Counter()
    for record in records: totals.update({k:len(v) for k,v in record['classifications'].items()})
    assert totals['non-tiling-finite-native-uv0-chart']>0
    resource.write_bytes(raw)
    assert read_bbn(resource)[0].tolist()==encoded
    code = (ROOT / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01UvSampling.cs').read_text()
    code = code.replace('Draw01','PedestrianDraw01')
    marker = '// Embedded numerical arrays: PedestrianDraw01UvSampling'
    if marker not in code: code=code.replace('using System;',marker+'\nusing System;')
    start=code.index('        int index ='); end=code.index('        if (index < 0)',start)
    code=code[:start]+'        int index = (render, mesh) switch\n        {\n'+''.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},\n' for i,r in enumerate(records))+'            _ => -1\n        };\n'+code[end:]
    code=code.replace('values[1] != 25',f'values[1] != {len(records)}')
    (ROOT / 'src/BridgeBuilder/Bridges/Types/PedestrianDraw01/PedestrianDraw01UvSampling.cs').write_text(code)
    report=dict(style=STYLE,schema=1,totals=dict(totals),sources=list(sources.values()),meshes=records,
        resourceSha256=sha(resource),resourceBytes=len(raw),templateSha256=sha(template_path),
        nativeDependencyGraph=graph['nativeDependencyGraph'],
        windowsEvidence=windows_evidence,
        topologySha256=sha(topology_path),repeatResourceSha256=sha(repeat_path),repeatRegions=regions,
        proof='Native position/UV adjacency defines physical chart occurrences. Every admitted occurrence stays within the finite atlas in both axes; template UV0 identity and no Surface mapping override preserve that traversal. Registered repeat regions are excluded independently of Retile outcomes. Pixel periodicity and wrap settings are not predicates.',
        selectedRoads='This inventory contains prototype meshes only; selected-road appearance is outside this exception.',
        localization='No UI text or localization behavior changed; diagnostic messages are internal errors.',
        acceptance='Offline classification only. Native rendering, complete persisted-owner material closure and human acceptance remain pending.')
    (out / 'classification.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(totals=totals,bytes=len(raw),sha256=sha(resource))))


if __name__=='__main__': main()
