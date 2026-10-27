"""Measure Draw03 source UV traversals and emit the immutable sampling catalogue.

No receiver failures or old NONREPEATING labels are classification inputs.
"""
from pathlib import Path
import argparse, collections, hashlib, json, sys, zipfile, struct
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/prepared-materials/Draw03/non-tiling'
sys.path[:0] = [str(ROOT/'tools/GeometryMetaprogram'), str(ROOT/'tools/PreparedMaterials/Draw03')]
from MovableUvDomainFormat import decode_arrays, read_bbn, write_bbn
from PrepareRecipes import numbers

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def ranges(values):
    result=[]
    for v in sorted(values):
        if result and result[-1][1]+1==v: result[-1][1]=v
        else: result.append([v,v])
    return result

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--library',type=Path,required=True)
    parser.add_argument('--publish',action='store_true')
    args=parser.parse_args()
    base=ROOT/'artifacts/prepared-materials/Draw03'
    manifest=base/'native-charts/manifest.json'
    sources=base/'rebuild-native-v1/native-rebuild.json'
    candidate=base/'repeat-units-v2/candidate-manifest.json'
    source_inventory=json.loads(sources.read_text())
    source_map={s['cid']:s for s in source_inventory['sourceSurfaces']}
    library={s['hash']:s for s in json.loads(args.library.read_text())}
    inputs={str(p):sha(p) for p in (manifest,sources,candidate,args.library)}
    semantics=ROOT/'artifacts/prepared-materials/shared-nontiling/native-semantics'
    for name in ('templates-current.json','summary.json','VTAtlassingInfo.cs','VTDatabase.cs'):
        p=semantics/name;inputs[str(p.relative_to(ROOT))]=sha(p)
    for name in ('571-2701.asm','581-133.asm','581-106.asm'):
        p=args.library.parent/'shader-programs'/name;inputs[str(p)]=sha(p)
    payloads={p['cid']:p for p in source_inventory['nativePayloads']}
    mask_samples={};mask_report={}
    for p in payloads.values():
        with zipfile.ZipFile(p['archive']) as archive:
            raw=archive.read(p['entry']);assert hashlib.sha256(raw).hexdigest()==p['sha256']
        if p['cid'] in ('57e96ebbfe702a5d07d2beacec3afe26','d8ebc942d48f59fc525feb03622589ed'):
            assert p['metadata']['anisotropy']==16 and p['metadata']['filterMode']==2 and p['metadata']['wrapMode']==0
            v,w,h,depth=struct.unpack_from('<4H',raw);assert v==4 and raw[9]==109 and depth==1
            at=14;sample=[];details=[]
            for mip in range(raw[8]):
                size=((w+3)//4)*((h+3)//4)*16
                pixels=np.asarray(Image.frombytes('RGBA',(w,h),raw[at:at+size],'bcn',(7,'BC7')))
                y,x=np.nonzero(pixels[:,:,2]);points=(np.column_stack((x,y))+.5)/[w,h]
                sample.append((points,np.asarray([17/w,17/h])))
                details.append(dict(mip=mip,width=w,height=h,encodedSha256=hashlib.sha256(raw[at:at+size]).hexdigest(),decodedSha256=hashlib.sha256(pixels.tobytes()).hexdigest(),blueMax=int(pixels[:,:,2].max()),nonzeroTexels=np.column_stack((x,y)).tolist()))
                at+=size;w=max(1,w//2);h=max(1,h//2)
            assert at==len(raw);mask_samples[p['cid']]=sample;mask_report[p['cid']]=details
    regions=json.loads(candidate.read_text())['candidates']
    main_source=source_map['792a855e9d0c5c34d2c41d48b062139d']['descriptor']['textureProperties']
    truss_path=ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03Truss_g_cs.bin'
    truss=numbers(truss_path)['I32'];inputs[str(truss_path)]=sha(truss_path)
    rows=[]; arrays=[]; totals=collections.Counter(); verified={}
    for mesh in json.loads(manifest.read_text())['meshes']:
        render,mi=mesh['render'],mesh['mesh'];s=source_map[mesh['surfaceCid']];desc=s['descriptor']
        if s['cid'] not in verified:
            with zipfile.ZipFile(s['archive']) as archive:
                assert hashlib.sha256(archive.read(s['entry'])).hexdigest()==s['sha256']
            prop=library[desc['template']]['material']['m_SavedProperties']
            assert dict(prop['m_Floats'])['_UVBase']==0
            colors=dict(prop['m_Colors'])
            assert colors['_UVMappingMask']==dict(r=1.,g=0.,b=0.,a=0.)
            if desc['template'] in (0,1):
                assert colors['_UV0Scale']==dict(r=1.,g=1.,b=0.,a=0.)
                assert colors['_UV0Offset']==dict(r=0.,g=0.,b=0.,a=0.)
            assert not desc['properties']['vectors'] and not desc['properties']['colors']
            for key,env in prop['m_TexEnvs']:
                if key in desc['textureProperties']:
                    assert env['m_Scale']==dict(x=1.,y=1.) and env['m_Offset']==dict(x=0.,y=0.)
            verified[s['cid']]=dict(sourceDigest=s['sha256'],template=desc['template'],textures=desc['textureProperties'],stacks=desc['stacks'],nativeUv0=True,scale=[1,1],offset=[0,0],virtualTextures=desc['virtualTextures'],nativePayloadMetadata={cid:payloads[cid]['metadata'] for cid in desc['dependencies'] if cid in payloads})
        native=ROOT/'artifacts/uv-density/native'/(render+'.npz')
        if not native.exists(): native=ROOT/'artifacts/uv-density/native-additional'/(render+'.npz')
        assert sha(native)==mesh['sourceNpZSha256']
        d=np.load(native);uv=d[f'mesh{mi}_uv0'][:,:2].astype(float);faces=d[f'mesh{mi}_faces'];positions=d[f'mesh{mi}_positions']
        window_domains=set()
        if desc['template']==2:
            wp=ROOT/'artifacts/prepared-materials/shared-nontiling/windows'/('Draw03-'+render.replace(' ','_')+'-'+str(mi)+'.json')
            wd=json.loads(wp.read_text());assert wd['render']==render and wd['mesh']==mi
            assert sha(Path(wd['geometry']))==wd['geometrySha256']
            assert np.array_equal(faces,np.asarray(wd['indices']))
            inputs[str(wp.relative_to(ROOT))]=sha(wp);window_domains=set(wd['sourceLocalDomains'])
        cp=base/'native-charts'/mesh['file'];assert sha(cp)==mesh['sha256']
        chart=numbers(cp)['I32'];nf,nc=chart[1:3];fc=np.asarray(chart[3:3+nf]);assert nf==len(faces)
        rp=ROOT/'assets/HardcodedNumbers/Draw03'/('UvDomain_'+render.replace(' ','_')+'_'+str(mi)+'.bin')
        ri,_=decode_arrays(*read_bbn(rp.read_bytes()))
        for p in (native,cp,rp):inputs[str(p.relative_to(ROOT))]=sha(p)
        # Check complete physical charts, not receiver bounding rectangles. The
        # recorded chart graph joins reversed source edges only at identical UVs.
        low=np.full((nc,2),np.inf);high=np.full((nc,2),-np.inf)
        np.minimum.at(low,fc,uv[faces].min(1));np.maximum.at(high,fc,uv[faces].max(1))
        tile=np.floor(low)
        chart_ok=np.isfinite(low).all(1)&np.isfinite(high).all(1)&(high<=tile+1).all(1)
        # Non-VT network sources use a single negatively shifted atlas tile.
        # Confirm actual native Repeat headers, not merely the sign of UVs.
        if np.any(tile<0):
            assert desc['template'] in (17,33) and not desc['virtualTextures']
            for key in ('_BaseColorMap','_MaskMap','_NormalMap'):
                assert payloads[desc['textureProperties'][key]]['metadata']['wrapMode']==0
        # Whole-texture periodic seams at coincident source vertices indicate an
        # actual traversal reset, unlike reuse by disconnected physical parts.
        at_position=collections.defaultdict(list)
        used=np.unique(faces)
        for v in used:at_position[tuple(positions[v])].append(int(v))
        seam_vertices=set()
        for vs in at_position.values():
            if len(vs)<2:continue
            coords=np.unique(uv[vs],axis=0)
            for i in range(len(coords)):
                delta=coords[i+1:]-coords[i]
                if len(delta) and np.any(np.all(delta==np.rint(delta),axis=1)&np.any(delta!=0,axis=1)):
                    seam_vertices.update(vs);break
        seam_charts=np.unique(fc[np.isin(faces,list(seam_vertices)).any(1)]) if seam_vertices else []
        chart_ok[seam_charts]=False
        mask=None
        if render in ('DrawBridge03 Mesh','DrawBridge03_LOD1 Mesh') and mi==0:
            offset=4 if render=='DrawBridge03 Mesh' else 36;mask=np.zeros(len(uv),bool)
            for a,b in np.asarray(truss[offset:offset+32]).reshape(-1,2):mask[a:b+1]=True
        counts=collections.Counter(); flags=[];reasons=collections.defaultdict(list);at=2
        worldspace_bad=np.zeros(len(faces),bool)
        if desc['template'] in (17,33):
            assert np.all(uv[:,1]<0) and np.all(uv[:,1]>-1)
            mapped=uv.copy();mapped[:,1]-=np.floor(mapped[:,1])
            tlo=mapped[faces].min(1);thi=mapped[faces].max(1)
            for points,pad in mask_samples[desc['textureProperties']['_MaskMap']]:
                for point in points:
                    for x in (-1,0,1):
                        for y in (-1,0,1):
                            p=point+[x,y]
                            worldspace_bad |= np.all(thi+pad>=p,axis=1)&np.all(tlo-pad<=p,axis=1)
        same_atlas=desc['textureProperties'].get('_BaseColorMap')==main_source.get('_BaseColorMap')
        for domain in range(int(ri[1])):
            count=abs(int(ri[at+3]));ids=ri[at+4:at+4+count];at+=4+count
            coords=uv[faces[ids]];lo=coords.min((0,1));hi=coords.max((0,1))
            reason='authored-uv0-chart'
            if desc['template']==2:reason='window-source-local' if domain in window_domains else 'window-interior-unresolved'
            elif desc['template'] in (17,33):
                reason='network-conversion-unresolved'
                counts['conditional17TexelSupportCandidates']+=int(not np.any(worldspace_bad[ids]))
            elif not np.all(chart_ok[fc[ids]]):reason='outside-allocation-or-periodic-seam'
            elif same_atlas and any(np.all(hi>=r['region']['min']) and np.all(lo<=r['region']['max']) for r in regions):reason='known-road-repeat-region'
            approved=reason in ('authored-uv0-chart','window-source-local');flags.append(int(approved));reasons[reason].append(domain);counts[reason]+=1
            if mask is not None and mask[faces[ids]].all():counts['trussDomains']+=1;counts['trussApproved']+=approved
        assert at==len(ri)
        arrays.append(flags);totals.update(counts);totals['domains']+=len(flags)
        rows.append(dict(render=render,mesh=mi,surfaceCid=s['cid'],geometryCid=mesh['geometryCid'],domains=len(flags),counts=dict(counts),nonTilingAxes='UV0 U and V; native worldspace layers keep their original physical mapping',chartCount=nc,sourceUvMinimum=uv.min(0).tolist(),sourceUvMaximum=uv.max(0).tolist(),authoredTileOffsets=np.unique(tile,axis=0).astype(int).tolist(),periodicSeamCharts=len(seam_charts),domainRanges={k:ranges(v) for k,v in reasons.items()}))
    raw=write_bbn([1,len(arrays),*[v for a in arrays for v in [len(a),*a]]],[])
    (OUT/'UvSampling.bin').write_bytes(raw)
    report=dict(schema='Draw03NonTilingUvMapping/1',summary=dict(totals),meshes=rows,sources=verified,inputs=inputs,networkMaskMipEvidence=mask_report,networkFilterSupport='Conservative per-source-triangle AABB expanded by 17 texels each side at every original mip, including all neighbouring Repeat tiles; native max anisotropy16 plus bilinear support. Any overlap stays false. Decode rows preserve native payload order.',resourceSha256=hashlib.sha256(raw).hexdigest(),resourceBytes=len(raw),legacyNonRepeatingLabelsUsed=False)
    (OUT/'evidence.json').write_text(json.dumps(report,indent=2)+'\n')
    if args.publish:
        (ROOT/'assets/HardcodedNumbers/Draw03/Draw03UvSampling.bin').write_bytes(raw)
        switches='\n'.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},' for i,r in enumerate(rows))
        code='''// Generated by tools/PreparedMaterials/Draw03/GenerateUvSampling.py.
// Embedded numerical arrays: Draw03UvSampling
// Embedded numerical arrays: Draw03UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class Draw03UvSampling
{
    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
SWITCHES
            _ => -1
        };
        if (index < 0) return false;
        var values = Data.Items[index];
        if ((uint)domain >= (uint)values.Length) throw new ArgumentOutOfRangeException(nameof(domain));
        return values[domain];
    }
    private static class Data
    {
        internal static readonly bool[][] Items = Read();
        static Data() { }
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("Draw03UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != MESHCOUNT)
                throw new InvalidOperationException("Invalid Draw03 UV sampling resource.");
            var result = new bool[values[1]][];
            int at = 2;
            for (int m = 0; m < result.Length; m++)
            {
                if (at >= values.Length) throw new InvalidOperationException("Truncated Draw03 UV sampling resource.");
                int count = values[at++];
                if (count < 0 || count > values.Length - at) throw new InvalidOperationException("Invalid Draw03 UV sampling count.");
                var row = result[m] = new bool[count];
                for (int d = 0; d < count; d++)
                {
                    int flag = values[at++];
                    if ((uint)flag > 1u) throw new InvalidOperationException("Invalid Draw03 UV sampling flag.");
                    row[d] = flag == 1;
                }
            }
            if (at != values.Length) throw new InvalidOperationException("Trailing Draw03 UV sampling data.");
            return result;
        }
    }
}
'''.replace('SWITCHES',switches).replace('MESHCOUNT',str(len(rows)))
        (ROOT/'src/BridgeBuilder/Bridges/Types/Draw03/Draw03UvSampling.cs').write_text(code)
    print(json.dumps(report['summary']))

if __name__=='__main__':main()
