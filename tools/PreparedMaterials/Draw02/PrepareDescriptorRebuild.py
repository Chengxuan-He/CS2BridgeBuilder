"""Create isolated Draw02 descriptor-only authors from verified native sources."""
import hashlib
import json
from pathlib import Path
import sys


def main(repo):
    work=repo/'artifacts/agent-material-env/Draw02/rebuild-native-v1'
    evidence=repo/'artifacts/prepared-materials/Draw02/rebuild-native-v1'
    evidence.mkdir(parents=True,exist_ok=True)
    target=work/'authoring';target.mkdir(parents=True,exist_ok=True)
    styles='new[] { "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" }'
    hashes=[]
    for name in ('Cable','Road','Lanes'):
        source=repo/'tools'/('Prepare'+name)/'Program.cs';text=source.read_text();assert styles in text
        text=text.replace(styles,'new[] { "Draw02" }')
        if name=='Cable':
            old='Path.Combine(Path.GetFullPath(args[2]), style, "Cable", file.Key)'
            assert old in text
            text=text.replace(old,'Path.Combine(Path.GetFullPath(args[2]), style, file.Key == "ZeroControl.Texture" ? "Shared" : "Cable", file.Key == "ZeroControl.Texture" ? file.Value.Cid + ".Texture" : file.Key)')
        if name=='Road':
            text=text.replace('Path.Combine(Path.GetFullPath(args[1]), "Draw01/Cable/ZeroControl.Texture")','Directory.GetFiles(Path.Combine(Path.GetFullPath(args[1]), "Draw02/Shared"), "*.Texture").Single()')
            text=text.replace('files.Add("ZeroControl.Texture", (controlCid, controlBytes));','// Reference the one freshly authored Shared control; do not copy it again.')
        folder=target/name;folder.mkdir(exist_ok=True)
        (folder/'Program.cs').write_text(text)
        reference=''
        if name=='Lanes':
            reference='<ItemGroup><Compile Include="'+str(repo/'src/BridgeBuilder/Runtime/BridgeSurfaceReferences.cs')+'" Link="BridgeSurfaceReferences.cs" /></ItemGroup>'
        (folder/(name+'.csproj')).write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>'+reference+'</Project>')
        hashes.append(dict(category=name,source=str(source),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),privateSha256=hashlib.sha256((folder/'Program.cs').read_bytes()).hexdigest()))
    input_dir=work/'input';input_dir.mkdir(exist_ok=True)
    structure=json.loads((repo/'artifacts/prepared-materials/Draw02/shared-source-migration/source-sampler-map.json').read_text())
    repeat=json.loads((repo/'artifacts/prepared-materials/Draw02/repeat-inventory.json').read_text())
    plan=dict(style='Draw02',schema='BridgeBuilder/Draw02NativeRebuildPlan/1',
        sourceBindings=structure['sources'],nativeSourcePayloads=structure['sourcePayloads'],channels=structure['textureChannels'],
        structuralSamplers=structure['samplers'],roadSamplers=repeat['units'],
        output=str(work/'output'),oldProductsAllowed=False,
        descriptorOnlyCategories=['Cable','Road','Lanes'],
        pendingCategories=['Paint native full-mip-compatible descriptor conversion','Repeat one source plus persisted region/band sampling','Structure out-of-chart source sampling'],
        counts=dict(structuralSurfaces=len(structure['sources']),builtinBackingPayloads=len(structure['sourcePayloads']),channelIdentities=len(structure['textureChannels']),structuralSamplerRecords=len(structure['samplers']),roadSamplerRecords=len(repeat['units']),newStructureTextureCopies=0))
    (input_dir/'source-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
    (evidence/'source-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
    (evidence/'authoring-sources.json').write_text(json.dumps(hashes,indent=2)+'\n')
    print(json.dumps(plan['counts']))


if __name__=='__main__':main(Path(sys.argv[1]).resolve())
