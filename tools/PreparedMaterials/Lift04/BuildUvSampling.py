"""Measure original Lift04 UV0 traversals and emit the prototype-only retention table."""
import collections
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/GeometryMetaprogram'))
from Lift04UvChartBindings import read_domains
from MovableUvDomainFormat import write_bbn, read_bbn
from VerifyCoverage import read_assets

spec = importlib.util.spec_from_file_location('native_descriptor_reader', ROOT/'tools/PreparedMaterials/PedestrianDraw01/ExportSharedSampling.py')
reader = importlib.util.module_from_spec(spec); spec.loader.exec_module(reader)
BASE = ROOT/'artifacts/prepared-materials/Lift04'
NUMBERS = ROOT/'assets/HardcodedNumbers/Lift04'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def occurrences(xyz, uv, faces):
    # Physical position AND authored UV adjacency: disconnected atlas reuse does
    # not become a repeated traversal merely because UV coordinates coincide.
    vertex = [tuple(p)+tuple(q) for p,q in zip(xyz.tolist(),uv.tolist())]
    edges = collections.defaultdict(list)
    parent = list(range(len(faces)))
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]; i = parent[i]
        return i
    for f, corners in enumerate(faces):
        keys = [vertex[v] for v in corners]
        for a,b in zip(keys,keys[1:]+keys[:1]): edges[tuple(sorted((a,b)))].append(f)
    for uses in edges.values():
        for other in uses[1:]: parent[find(other)] = find(uses[0])
    groups = collections.defaultdict(list)
    for i in range(len(faces)): groups[find(i)].append(i)
    return list(groups.values())

def main():
    mapping = json.loads((BASE/'actual-retile/source-map.json').read_text())
    graph = json.loads((BASE/'shared-sampling/native-bindings.json').read_text())
    topology_path=BASE/'shared-sampling/native-charts.json'
    topology=json.loads(topology_path.read_text())
    templates_path = ROOT/'tools/PrepareLanes/NativeTemplates.json'
    templates = {t['hash']:t for t in json.loads(templates_path.read_text())}
    window_root=ROOT/'artifacts/prepared-materials/shared-nontiling'
    window_template_path=window_root/'native-semantics/template-2.json'
    window_template=json.loads(window_template_path.read_text())
    current_templates=json.loads((window_root/'native-semantics/templates-current.json').read_text())
    assert window_template['m_Name']=='Windows' and window_template['m_Shader']==dict(m_FileID=0,m_PathID=550)
    assert next(t for t in current_templates if t['hash']==2)['verifiedAgainstCurrentResources']
    requested={s['sourceCid'] for s in mapping}|{t['sourceTextureCid'] for t in graph['textures']}
    payloads = read_assets(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'), requested)
    for texture in graph['textures']:
        assert hashlib.sha256(payloads[texture['sourceTextureCid']]).hexdigest()==texture['nativePayloadSHA256']
    raw_sources={s['sourceCid']:payloads[s['sourceCid']] for s in mapping}
    sources = {}
    for identity, raw in raw_sources.items():
        original = next(s for s in graph['surfaces'] if s['cid']==identity)
        assert hashlib.sha256(raw).hexdigest()==original['sourceSHA256']
        template, header, combined, bindings, keywords = reader.descriptor(raw)
        stream=io.BytesIO(raw); stream.read(7)
        if header:
            stream.read(276)
            if reader.present(stream): stream.read(16)
        properties={}
        for category,size,fmt in [('float',4,'<f'),('int',4,'<i'),('vector',16,'<4f'),('color',16,'<4f')]:
            props=properties[category]={}
            for _ in range(reader.count(stream)):
                key=reader.string(stream); props[key]=struct.unpack(fmt,stream.read(size))
        semantics=None
        if template in (0,1) and all(k=='_Snow' for p in properties.values() for k in p) and set(keywords)<={'_TANGENTSPACE_OCTO'}:
            material=templates[template]['material']; saved=material['m_SavedProperties']
            floats,colors=dict(saved['m_Floats']),dict(saved['m_Colors'])
            assert all(floats[k]==0 for k in ('_UVBase','_UVDetail','_UVEmissive'))
            assert colors['_UV0Scale']==dict(r=1.,g=1.,b=0.,a=0.)
            assert colors['_UV0Offset']==dict(r=0.,g=0.,b=0.,a=0.)
            assert colors['_UVMappingMask']==dict(r=1.,g=0.,b=0.,a=0.)
            texture_transforms={k:v for k,v in saved['m_TexEnvs'] if k in ('DefaultPVTStack','ExtendedPVTStack','_BaseColorMap','_NormalMap','_MaskMap')}
            assert all(v['m_Scale']==dict(x=1.,y=1.) and v['m_Offset']==dict(x=0.,y=0.) for v in texture_transforms.values())
            semantics=dict(template=template,shader=material['m_Shader'],uvChannel=0,scale=[1,1],offset=[0,0],mappingMask=[1,0,0,0],textureTransforms=texture_transforms)
        elif identity=='5a7696889d52714b56ac38b90d16c2de':
            assert template==2 and header is None and not any(properties.values()) and keywords==['_TANGENTSPACE_OCTO']
            semantics=dict(template=2,shader=window_template['m_Shader'],uvChannel=0,localCoordinates='YZ',preserveChannels='UV0 XYZW and COLOR',templateSha256=sha(window_template_path))
        sources[identity]=dict(**original,properties=properties,keywords=keywords,semantics=semantics)
    # Explicit known repeat regions remain density-controlled. Conservatively
    # exclude intersecting source triangles, including partially covered domains.
    evidence_paths=[BASE/'beam-repeat/source-evidence.json',ROOT/'tools/PreparedMaterials/Lift04/repeat-units-v2.json',BASE/'structure-inventory/profile-manifest.json']
    beam=json.loads(evidence_paths[0].read_text())
    units=[dict(id='beam',sourceDigest=beam['sourceDigest'],min=beam['coverageMin'],max=beam['coverageMax'])]
    units+=json.loads(evidence_paths[1].read_text())['units']
    units+=json.loads(evidence_paths[2].read_text())['units']
    # Full authored profile coverage, rather than only one longitudinal period.
    for unit in units:
        if unit['id'].startswith('Lift04-'):
            proof_path=BASE/'structure-inventory'/(unit['id'][7:]+'-evidence.json')
            if proof_path.exists():
                proof=json.loads(proof_path.read_text()); unit['min']=proof['coverageBounds'][:2];unit['max']=proof['coverageBounds'][2:]
    encoded=[1,len(mapping)]; records=[]; totals=collections.Counter()
    for source in mapping:
        name,mi=source['render'],source['mesh']
        native_path=ROOT/'artifacts/uv-density/native'/(name+'.npz')
        domain_path=NUMBERS/('UvDomain_'+name.replace(' ','_')+'_'+str(mi)+'.bin')
        topo=next(m for m in topology['meshes'] if m['render']==name and m['mesh']==mi)
        assert sha(native_path)==topo['sourceNPZSHA256'] and source['sourceCid']==topo['sourceSurfaceCid']
        native=np.load(native_path); uv=native[f'mesh{mi}_uv0'][:,:2].astype(float)
        xyz=native[f'mesh{mi}_positions']; faces=native[f'mesh{mi}_faces']; triangles=uv[faces]
        window_proof=None; window_domains=set()
        if source['sourceCid']=='5a7696889d52714b56ac38b90d16c2de':
            proof_path=window_root/'windows'/('Lift04-'+name.replace(' ','_')+'-1.json')
            window_proof=json.loads(proof_path.read_text())
            assert window_proof['render']==name and mi==window_proof['mesh']==1
            assert sha(Path(window_proof['geometry']))==window_proof['geometrySha256']
            assert np.array_equal(faces,np.asarray(window_proof['indices']))
            complete_uv=np.asarray(window_proof['uv0'],dtype=np.float32)
            assert np.array_equal(uv,complete_uv[:,:2])
            assert np.isfinite(complete_uv).all() and np.all(complete_uv[:,1:3]>=0) and np.all(complete_uv[:,1:3]<=1)
            assert np.all(complete_uv[:,3]==1) and len(window_proof['colors'])==len(uv)
            window_domains=set(window_proof['sourceLocalDomains'])
            assert window_domains==set(range(4)) and window_proof['sourceLocalFaces']==list(range(len(faces)))
            window_proof['evidenceSha256']=sha(proof_path)
        physical=occurrences(xyz,uv,faces); valid=np.zeros(len(faces),dtype=bool); traversals=[]
        for fs in physical:
            points=triangles[fs]; finite=bool(np.isfinite(points).all() and (points>=0).all() and (points<=1).all())
            valid[fs]=finite
            traversals.append(dict(faces=fs,uvMin=points.min(axis=(0,1)).tolist(),uvMax=points.max(axis=(0,1)).tolist(),insideSingleNativeAtlas=finite))
        repeat=np.zeros(len(faces),dtype=bool)
        lo,hi=triangles.min(axis=1),triangles.max(axis=1)
        for unit in units:
            if unit['sourceDigest']==source['sourceDigest']:
                # Bounding overlap deliberately excludes a superset; never
                # admits an unmeasured genuine-repeat receiver through this test.
                repeat |= np.all(hi>=unit['min'],axis=1)&np.all(lo<=unit['max'],axis=1)
        flags=[];groups=collections.defaultdict(list)
        for index,fs in enumerate(read_domains(domain_path)):
            ids=list(fs)
            if index in window_domains:
                assert all(f in window_proof['sourceLocalFaces'] for f in ids)
                reason='non-tiling-native-windows-local-yz'
            elif sources[source['sourceCid']]['semantics'] is None: reason='unknown-native-semantics'
            elif repeat[ids].any(): reason='density-known-repeat-or-overlap'
            elif not valid[ids].all(): reason='unknown-atlas-boundary-traversal'
            else: reason='non-tiling-native-uv0-traversal'
            flags.append(int(reason in ('non-tiling-native-uv0-traversal','non-tiling-native-windows-local-yz')));groups[reason].append(index)
        if name=='LiftBridge04 Mesh' and mi==0:
            assert flags[3]==0 and flags[28]==0, 'Known mixed/overlapping profiles must retain density handling'
        encoded.extend([len(flags),*flags]);totals.update({k:len(v) for k,v in groups.items()})
        records.append(dict(**source,lod=2 if '_LOD2' in name else 1 if '_LOD1' in name else 0,materialSlot=mi,nativeSha256=sha(native_path),domainSha256=sha(domain_path),domainCount=len(flags),classifications=dict(groups),physicalOccurrences=traversals,nonTilingAxes=['U','V']))
        if window_proof:
            records[-1]['windowsLocalEvidence']=window_proof
            records[-1]['nonTilingAxes']=['UV0.y','UV0.z']
    out=BASE/'non-tiling';out.mkdir(exist_ok=True)
    report=dict(style='Lift04',schema=1,totals=dict(totals),sources=list(sources.values()),meshes=records,templateSha256=sha(templates_path),textures=graph['textures'],repeatEvidence=[dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in evidence_paths],repeatRegions=[{k:u[k] for k in ('id','sourceDigest','min','max')} for u in units],proof='Native Surface and template select identity UV0. Position-and-UV connected physical occurrences traverse the finite native atlas without sampler boundary crossings. Disconnected reuse and image motifs are not classified as UV tiling. Known repeat-region overlaps remain density-controlled. Unsupported native semantics remain unknown.',localization='No user-visible text or localization behavior changed.',acceptance='Offline classification; native rendering and saved-owner closure remain pending.')
    (out/'classification.json').write_text(json.dumps(report,separators=(',',':'))+'\n')
    report['windowsProof']='Original unchanged Surface 5a7696889d52714b56ac38b90d16c2de uses template 2 Windows / shader 550 with no property overrides. Exact geometry-hashed local YZ domains retain complete original UV0 XYZW and COLOR; no planar XY reinterpretation. All eight domains are explicitly measured.'
    print(json.dumps(dict(totals=totals,sourceSemantics={k:bool(v['semantics']) for k,v in sources.items()})))
    if '--write' not in sys.argv: return
    resource=NUMBERS/'Lift04UvSampling.bin'; resource.write_bytes(write_bbn(encoded,[]))
    assert read_bbn(resource.read_bytes())[0].tolist()==encoded
    code=(ROOT/'src/BridgeBuilder/Bridges/Types/Draw01/Draw01UvSampling.cs').read_text().replace('Draw01','Lift04')
    start=code.index('        int index =');end=code.index('        if (index < 0)',start)
    code=code[:start]+'        int index = (render, mesh) switch\n        {\n'+''.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},\n' for i,r in enumerate(records))+'            _ => -1\n        };\n'+code[end:]
    code=code.replace('values[1] != 25',f'values[1] != {len(records)}')
    if '// Embedded numerical arrays: Lift04UvSampling' not in code:
        code=code.replace('using System;','// Embedded numerical arrays: Lift04UvSampling\nusing System;',1)
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04UvSampling.cs').write_text(code)
    report['resourceSha256']=sha(resource);report['resourceBytes']=resource.stat().st_size
    report['topologySha256']=sha(topology_path)
    (out/'classification.json').write_text(json.dumps(report,separators=(',',':'))+'\n')
    print(json.dumps(dict(resourceBytes=resource.stat().st_size,resourceSha256=sha(resource))))

if __name__=='__main__': main()
