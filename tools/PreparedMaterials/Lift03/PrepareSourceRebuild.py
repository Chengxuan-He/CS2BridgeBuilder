"""Prepare freshly verified Lift03 native source bindings, without legacy baked products."""
import argparse,hashlib,json,re,zipfile,struct
from pathlib import Path
from ExportRecipes import extract,sampler_key
ROOT=Path(__file__).resolve().parents[3]
def digest(data):return hashlib.sha256(data).hexdigest()
def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--game-data',type=Path,required=True)
    ap.add_argument('--output',type=Path,required=True)
    args=ap.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    old=json.loads((ROOT/'artifacts/prepared-materials/Lift03/shared-source-map/mapping.json').read_text())
    uses=[]
    for name in ('Lift03SourceRenders.cs','Lift03PillarSources.cs'):
        s=(ROOT/'src/BridgeBuilder/Bridges/Types/Lift03'/name).read_text()
        for render,geo,surfaces in re.findall(r'Create\("([^\"]+)", "([a-f0-9]{32})", new\[\] \{ ([^}]+) \}',s):
            for slot,cid in enumerate(re.findall('"([a-f0-9]{32})"',surfaces)):
                uses.append(dict(render=render,meshIndex=slot,geometryCid=geo,surfaceCid=cid,mode='original-native-surface'))
    wanted={x['surfaceCid'] for x in uses};found={}
    for family in ('Game','BridgesAndPorts'):
        for path in (args.game_data/'Content'/family).glob('Blob*.cok'):
            with zipfile.ZipFile(path) as z:
                for name in z.namelist():
                    if not name.endswith('.Surface.cid'):continue
                    cid=z.read(name).decode().strip()
                    if cid not in wanted:continue
                    data=z.read(name[:-4]);row=dict(cid=cid,builtin=True,archive=str(path),entry=name[:-4],bytes=len(data),sha256=digest(data),template=struct.unpack_from('<i',data,2)[0],isVT=bool(data[6]))
                    if cid in found:assert found[cid]['sha256']==row['sha256']
                    found[cid]=row
    assert set(found)==wanted
    for cid,row in old['sources'].items():assert found[cid]['sha256']==row['sourceDigest']
    textures={}
    for row in old['textures'].values():
        cid=row['nativeCid']
        if cid in textures:continue
        path=Path(row['nativeArchive'])
        with zipfile.ZipFile(path) as z:
            data=z.read(row['nativeEntry']);sidecar=z.read(row['nativeEntry']+'.cid').decode().strip()
        assert sidecar==cid and digest(data)==row['nativePayloadSha256']
        textures[cid]=dict(cid=cid,builtin=True,archive=str(path),entry=row['nativeEntry'],bytes=len(data),sha256=digest(data),treatment='native-reference-with-complete-original-mip-graph')
    roads,evidence=extract(ROOT)
    main='d5708265307c0c422270f918fb8ca8e8'
    for road in roads:
        road.update(sourceCid=main,sourceDigest=found[main]['sha256'],key=sampler_key(found[main]['sha256'],road['min'],road['max'],road['cutout']),storage='sampling-metadata-only-no-cropped-texture')
    config=dict(schema='Lift03/SourceRebuildInput/1',style='Lift03',sources=list(found.values()),nativeTexturePayloads=list(textures.values()),structuralBindings=uses,roadSamplers=roads,
        immutableCharts=dict(resource='assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03UvCharts_cs.bin',sha256=digest((ROOT/'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03UvCharts_cs.bin').read_bytes())),
        evidence=evidence,oldProductStores=[],oldGeneratedAssets=[],scope='Source inputs for the common source-level producer; not a substituted runtime renderer',
        requiredProducerOperations=['Keep compatible native Surface/VT references unchanged','Retain two road source regions as sampling metadata sharing the same source payload','Prepare only necessary native descriptor conversions','Use one fresh explicit transparent zero when native descriptor requires a control binding'],
        unresolved=['Native region-local repetition for both road regions','Full non-contained structural receiver native sampling','Shared source-level producer schema and native end-to-end validation'])
    (args.output/'sources.json').write_text(json.dumps(dict(style='Lift03',sources=list(found.values())),indent=2)+'\n')
    (args.output/'source-input.json').write_text(json.dumps(config,indent=2)+'\n')
    result=dict(status='PASS native input bytes verified',surfaceSources=len(found),structuralSurfaceSources=len(old['sources']),glassSources=len(found)-len(old['sources']),nativeTexturePayloads=len(textures),nativeTextureBytes=sum(x['bytes'] for x in textures.values()),newPackagedTextureFiles=0,newDescriptorFiles=0,roadSamplingRecords=len(roads),uniqueRoadRegions=len({tuple(u['min']+u['max']) for u in roads}),legacyProductsRead=0,formalAssetsWritten=False)
    (args.output/'input-validation.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
if __name__=='__main__':main()
