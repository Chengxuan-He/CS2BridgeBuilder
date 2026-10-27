"""Prepare clean Lift05 source-level inputs exclusively from original game archives."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile
from ExportRecipes import recipes, reachable_regions
from MapSharedSampling import surface


def main():
    p=argparse.ArgumentParser()
    p.add_argument('repo',type=Path);p.add_argument('game',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();root=a.repo.resolve();out=a.output.resolve()
    assert not out.exists(), 'Use a clean source input directory'
    renders=(root/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05SourceRenders.cs').read_text(encoding='utf-8-sig')
    bindings=[]
    for name,geometry,materials in re.findall(r'Create\("([^"]+)", "([0-9a-f]{32})", new\[\] \{ ([^}]+) \}',renders):
        for slot,cid in enumerate(re.findall(r'"([0-9a-f]{32})"',materials)):
            bindings.append(dict(render=name,geometryCid=geometry,materialSlot=slot,surfaceCid=cid))
    assert len(bindings)==11
    ids={b['surfaceCid'] for b in bindings}
    paint_cid='4b5500b1b5c270890d99149f7f62e51a'
    ids.add(paint_cid)
    sources={}
    for family in ('Game','BridgesAndPorts'):
        for path in sorted((a.game/'Content'/family).glob('Blob*.cok')):
            with zipfile.ZipFile(path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid'):continue
                    cid=archive.read(entry).decode().strip()
                    if cid not in ids:continue
                    raw=archive.read(entry[:-4]);digest=hashlib.sha256(raw).hexdigest()
                    if cid in sources:
                        assert sources[cid]['sha256']==digest
                        continue
                    sources[cid]=dict(cid=cid,sha256=digest,ownership='builtin',
                        archive=str(path),entry=entry[:-4],bytes=len(raw),template=surface(raw)['template'],native=surface(raw),raw=raw)
    assert sources.keys()==ids
    patches,road_evidence=recipes(root)
    fixed,reachability=reachable_regions(root)
    # The existing extractor returns the actual selected branch membership, not
    # inferred road width or generated-bridge material references.
    out.mkdir(parents=True)
    for cid,item in sources.items():
        (out/(cid+'.Surface')).write_bytes(item.pop('raw'))
    manifest=dict(schema='Lift05/NativeMaterialRebuildInputs/1',style='Lift05',
        sources=[v for k,v in sources.items() if k!=paint_cid],paintSource=sources[paint_cid],bindings=bindings,
        sourceRenderSha256=hashlib.sha256(renders.encode('utf-8')).hexdigest(),
        roadPatches=patches,roadEvidence=road_evidence,reachableRegions=sorted(fixed),reachability=reachability,
        paintSourceCid=paint_cid,texturePayloadsCopied=0,oldPreparedResourcesRead=0,
        operations=[
            dict(kind='structure',action='retain original builtin Surface and complete texture graph; consume part-scoped UV charts',newPixelPayloads=0),
            dict(kind='fixed-road',action='native source-level region sampling or one approved whole-source conversion; no per-sampler cropped images'),
            dict(kind='paint',action='offline native descriptor retaining compatible original channel identities; any necessary conversion must retain every source mip'),
            dict(kind='constant-control',action='one transparent zero payload shared by descriptors that have no authored control')],
        pending='Common source-level producer and native region-sampling representation; no old per-triangle library fallback')
    (out/'native-inputs.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(dict(surfaceInputs=len(sources),sourceBindings=len(bindings),newTextures=0,oldResourcesRead=0),indent=2))


if __name__=='__main__':main()
