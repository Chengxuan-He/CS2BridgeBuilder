"""Measure finite authored UV0 islands; keep repeat and unknown mappings at density."""
import ast
import collections
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
import numpy as np

from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays, write_bbn
STYLE = 'PedestrianDraw02'
BASE = ROOT/'artifacts/prepared-materials'/STYLE
NUM = ROOT/'assets/HardcodedNumbers'/STYLE

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def properties(raw):
    r = io.BytesIO(raw)
    version, template, vt = struct.unpack('<Hi?', r.read(7))
    assert version == 1
    if vt:
        r.read(276)
        marker = r.read(1)[0]
        assert marker in (0,255)
        if marker: r.read(16)
    def count():
        marker = r.read(1)[0]
        assert marker in (0,255)
        return struct.unpack('<i',r.read(4))[0] if marker else 0
    def text():
        n=shift=0
        while True:
            b=r.read(1)[0]; n|=(b&127)<<shift
            if b<128: break
            shift+=7
        return r.read(n).decode()
    out={}
    for kind, fmt in [('float','<f'),('int','<i'),('vector','<4f'),('color','<4f')]:
        out[kind]={}
        for _ in range(count()):
            key=text();out[kind][key]=struct.unpack(fmt,r.read(struct.calcsize(fmt)))
    return template,out

def overlaps(triangle, region):
    points=[tuple(p) for p in triangle]
    for axis, edge, sign in [(0,region[0],1),(1,region[1],1),(0,region[2],-1),(1,region[3],-1)]:
        output=[]
        for a,b in zip(points,points[1:]+points[:1]):
            ia=(a[axis]-edge)*sign>=0; ib=(b[axis]-edge)*sign>=0
            if ia: output.append(a)
            if ia!=ib:
                t=(edge-a[axis])/(b[axis]-a[axis])
                output.append(tuple(a[k]+t*(b[k]-a[k]) for k in range(2)))
        points=output
        if not points: return False
    return abs(math.fsum(a[0]*b[1]-a[1]*b[0] for a,b in zip(points,points[1:]+points[:1])))>0

def main():
    native_path=BASE/'source-consolidation/native-charts.npz'
    chart_path=BASE/'source-consolidation/native-charts.json'
    native=np.load(native_path)
    chart_report=json.loads(chart_path.read_text())
    mapping=json.loads((BASE/'current-source-map.json').read_text())
    manifest=json.loads((ROOT/'artifacts/agent-material-env/PedestrianDraw02/source-input/source-inputs.json').read_text())
    semantics=ROOT/'artifacts/prepared-materials/shared-nontiling/native-semantics'
    templates_path=semantics/'template-0.json'
    current=json.loads((semantics/'templates-current.json').read_text())
    verified=next(t for t in current if t['hash']==0)
    assert verified['verifiedAgainstCurrentResources']
    template=dict(hash=0,material=json.loads(templates_path.read_text()))
    assert template['material']['m_Shader']['m_PathID']==571
    window_template_path=semantics/'template-2.json'
    window_template=json.loads(window_template_path.read_text())
    assert window_template['m_Shader']['m_PathID']==550
    assert next(t for t in current if t['hash']==2)['verifiedAgainstCurrentResources']
    props=template['material']['m_SavedProperties']
    colors=dict(props['m_Colors']); floats=dict(props['m_Floats'])
    assert floats['_UVBase']==0
    assert list(colors['_UVMappingMask'].values())==[1,0,0,0]
    assert list(colors['_UV0Scale'].values())==[1,1,0,0]
    assert list(colors['_UV0Offset'].values())==[0,0,0,0]
    for name,env in props['m_TexEnvs']:
        if name in ('_BaseColorMap','_NormalMap','_MaskMap','DefaultPVTStack'):
            assert env['m_Scale']=={'x':1,'y':1} and env['m_Offset']=={'x':0,'y':0}
    sources={}
    for src in manifest['sources']:
        asset=next(a for a in manifest['assets'] if a['cid']==src['cid'])
        location=asset['locations'][0]
        with zipfile.ZipFile(location['archive']) as archive:
            raw=archive.read(location['entry'])
            assert archive.read(location['entry']+'.cid').decode().strip()==src['cid']
        assert hashlib.sha256(raw).hexdigest()==src['sha256']
        tid,overrides=properties(raw)
        assert tid==src['template']
        if tid==0:
            assert all(k=='_Snow' for group in overrides.values() for k in group)
        sources[src['cid']]=dict(src,descriptorOverrides=overrides,sourceLocation=location)
    # Reuse the actual source-topology validator without importing its writer.
    source=ROOT/'tools/PreparedMaterials/PedestrianDraw02/GenerateUvCharts.py'
    tree=ast.parse(source.read_text())
    selected=ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[])
    env=dict(np=np,math=math,defaultdict=collections.defaultdict)
    exec(compile(selected,str(source),'exec'),env)
    validate=env['boundaries']
    road_path=NUM/'Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.bin'
    region=list(numbers(road_path)['F32'][6:10])
    repeat_digest='5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219'
    records=[];encoded=[1,len(mapping)];totals=collections.Counter()
    for binding in mapping:
        record=next(r for r in chart_report['records'] if (r['name'],r['mesh'])==(binding['render'],binding['mesh']))
        src=sources[binding['sourceCid']]
        assert src['sha256']==binding['sourceDigest'] and record['sourceSurfaceCid']==src['cid']
        prefix=record['arrayPrefix']
        positions,uv,faces,face_chart=[native[prefix+'_'+k] for k in ('positions','uv','faces','face_chart')]
        chart_reasons={};proof=[]
        for chart in record['charts']:
            ci=chart['id'];tri=uv[faces[chart['faces']]].astype(float)
            reason='native-uv0-finite-authored-mapping'
            if src['template']!=0: reason='unknown-template-2-interior-sampling'
            elif not np.isfinite(tri).all() or np.any(tri<0) or np.any(tri>1): reason='unknown-outside-finite-uv0-atlas'
            else:
                loops,failure=validate(positions,uv,faces,chart['faces'])
                proof.append(dict(chart=ci,faces=chart['faces'],uvBounds=chart['bounds'],uvArea=chart['uvArea'],loops=loops,chartClippingLimitation=failure,
                    mappingProof='Every authored triangle is an affine UV0 interpolation within the finite native atlas. Shader 571 identity mapping introduces no procedural period. Shared, mirrored, overlapping and constant/line UVs preserve the source mapping; chart polygon validity is not a tiling predicate.'))
            chart_reasons[ci]=reason
        path=NUM/('UvDomain_'+binding['render'].replace(' ','_')+'_'+str(binding['mesh'])+'.bin')
        values=numbers(path);raw,_=decode_arrays(values['I32'],values['F32'])
        window=None;local_domains=set();admitted_windows=[]
        if src['template']==2:
            assert binding['mesh']==1 and src['cid']=='3b81df0257792cb8f7641e88a51c2d1e'
            assert all(not group for group in src['descriptorOverrides'].values())
            window_path=semantics.parent/'windows'/(STYLE+'-'+binding['render'].replace(' ','_')+'-1.json')
            window=json.loads(window_path.read_text())
            assert sha(Path(window['geometry']))==window['geometrySha256']
            assert (window['style'],window['render'],window['mesh'])==(STYLE,binding['render'],binding['mesh'])
            full_uv=np.asarray(window['uv0'],dtype=np.float32);colors=np.asarray(window['colors'])
            assert full_uv.shape==(len(uv),4) and np.isfinite(full_uv).all() and np.isfinite(colors).all()
            assert np.array_equal(full_uv[:,:2],uv) and np.array_equal(window['indices'],faces)
            local=full_uv[faces,1:3]
            local_faces=np.flatnonzero(((local>=0)&(local<=1)).all(axis=(1,2)) &
                (full_uv[faces,3]==full_uv[faces,3][:,:1]).all(axis=1) &
                (colors[faces]==colors[faces][:,:1,:]).all(axis=(1,2))).tolist()
            assert local_faces==window['sourceLocalFaces']
            local_domains=set(window['sourceLocalDomains'])
        at=2;flags=[];groups=collections.defaultdict(list)
        for di in range(int(raw[1])):
            count=int(raw[at+3]);at+=4;members=raw[at:at+abs(count)];at+=abs(count)
            reasons={chart_reasons[int(face_chart[f])] for f in members}
            # Repeat membership belongs to this original domain's source faces.
            # A neighboring face in the same authored chart cannot veto retention.
            if src['sha256']==repeat_digest and any(overlaps(t,region) for t in uv[faces[members]].astype(float)):
                reasons.add('density-existing-road-repeat-overlap')
            flag=reasons=={'native-uv0-finite-authored-mapping'}
            reason='native-uv0-finite-authored-mapping' if flag else ';'.join(sorted(reasons-{'native-uv0-finite-authored-mapping'}))
            if window is not None:
                eligible=all(int(f) in local_faces for f in members)
                assert eligible==(di in local_domains)
                if eligible:
                    flag=True;reason='native-window-local-yz';admitted_windows.append(di)
            flags.append(int(flag))
            groups[reason].append(di)
        assert at==len(raw)
        encoded.extend([len(flags),*flags]);totals.update({k:len(v) for k,v in groups.items()})
        records.append(dict(**binding,lod=record['lod'],part=record['implementation'],domainCount=len(flags),domainSha256=sha(path),classifications=dict(groups),nativeIslands=proof,
            windowEvidence=None if window is None else dict(report=str(window_path.relative_to(ROOT)),sha256=sha(window_path),
                geometrySha256=window['geometrySha256'],sourceLocalDomains=admitted_windows,localAxes='UV0.YZ',
                preservedChannels=['UV0.XYZW','COLOR'],nativeTemplateSha256=sha(window_template_path),shader=550)))
    resource=NUM/(STYLE+'UvSampling.bin');resource.write_bytes(write_bbn(encoded,[]))
    assert list(numbers(resource)['I32'])==encoded
    switch='\n'.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},' for i,r in enumerate(records))
    known=' or '.join('"'+s+'"' for s in sorted({r['render'] for r in records}))
    code='''// Generated by tools/PreparedMaterials/PedestrianDraw02/BuildUvSampling.py.
// Embedded numerical arrays: PedestrianDraw02UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02UvSampling
{
    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
SWITCH
            _ => -1
        };
        if (index < 0)
        {
            if (render is KNOWN)
                throw new InvalidOperationException("PedestrianDraw02 UV sampling mesh mismatch.");
            return false;
        }
        var flags = Data.Items[index];
        if ((uint)domain >= (uint)flags.Length)
            throw new InvalidOperationException("PedestrianDraw02 UV sampling domain mismatch.");
        return flags[domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly bool[][] Items = Read();
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("PedestrianDraw02UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != 9)
                throw new InvalidOperationException("Invalid PedestrianDraw02 UV sampling schema.");
            var result = new bool[values[1]][];
            int at = 2;
            for (int mesh = 0; mesh < result.Length; mesh++)
            {
                if (at >= values.Length) throw new InvalidOperationException("Truncated PedestrianDraw02 UV sampling data.");
                int count = values[at++];
                if (count < 0 || count > values.Length - at)
                    throw new InvalidOperationException("Invalid PedestrianDraw02 UV sampling count.");
                var flags = result[mesh] = new bool[count];
                for (int d = 0; d < count; d++)
                {
                    int flag = values[at++];
                    if (flag != 0 && flag != 1)
                        throw new InvalidOperationException("Invalid PedestrianDraw02 UV sampling flag.");
                    flags[d] = flag == 1;
                }
            }
            if (at != values.Length) throw new InvalidOperationException("Invalid PedestrianDraw02 UV sampling length.");
            return result;
        }
    }
}
'''.replace('SWITCH',switch).replace('KNOWN',known)
    (ROOT/'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02UvSampling.cs').write_text(code)
    output=BASE/'non-tiling';output.mkdir(exist_ok=True)
    report=dict(style=STYLE,resourceBytes=resource.stat().st_size,resourceSha256=sha(resource),totals=dict(totals),
        sources=list(sources.values()),currentTemplateSource=verified,nativeTemplate=template,templateSha256=sha(templates_path),
        nativeSha256=sha(native_path),nativeChartSha256=sha(chart_path),topologyValidatorSha256=sha(source),
        genuineRepeatRegion=region,repeatResourceSha256=sha(road_path),nonTilingAxes=['U','V'],meshes=records,
        nativeSemanticsHashes={p.name:sha(p) for p in [semantics/'templates-current.json',semantics/'summary.json',semantics/'shader-571-parsed.json',semantics/'shader-550-parsed.json',semantics/'550-184.asm',semantics/'VTAtlassingInfo.cs',semantics/'VTDatabase.cs',semantics/'SurfaceAsset.cs']},
        rule='Identity native UV0 samples finite authored source mappings with no procedural tiling. UV reuse and polygon clipping ambiguity do not imply repeated sampling; exact original-domain road repeat overlaps and unknown shaders remain density mappings. Pixel periodicity is not used.',
        localization='No user-visible text or localization behavior changed; new strings are internal invariant diagnostics.',
        acceptance='Offline evidence only. Native rendering, complete owner footprint and package remain integration gates.')
    (output/'classification.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(totals=totals,bytes=resource.stat().st_size)))

if __name__=='__main__': main()




