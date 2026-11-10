from pathlib import Path
import subprocess,sys
root=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();out.mkdir(parents=True,exist_ok=True)
s=(root/'src/BridgeBuilder/Runtime/BridgePreviewRenderResources.cs').read_text(encoding='utf-8-sig')
def method(marker):
 start=s.index(marker);brace=s.index('{',start);i=brace+1;depth=1
 while depth:
  depth+= (s[i]=='{')-(s[i]=='}');i+=1
 return s[start:i]
a=method('    private static void SetKeyword(');b=method('    private static void ConfigurePreviewDepth(')
args='material,network,packed,prefab'+(',nativeLane' if 'bool nativeLane)' in b else '')
(out/'Policy.cs').write_text('using System;\nclass Policy {\n'+a+'\n'+b+'\n internal static void Run(Material material,bool network,bool packed,RenderPrefab prefab,bool nativeLane)=>ConfigurePreviewDepth('+args+');\n}\n')
(out/'Program.cs').write_bytes((Path(__file__).parent/'Program.cs').read_bytes())
(out/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
sys.exit(subprocess.call(['dotnet','run','--project',str(out/'Check.csproj')]))
