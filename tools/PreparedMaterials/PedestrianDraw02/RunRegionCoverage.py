"""Measure the real production Region gate on this bridge's finite plan matrix."""
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[3]
env = root / "artifacts/agent-material-env/PedestrianDraw02/region-coverage"
env.mkdir(parents=True, exist_ok=True)
source = root / "tools/MovablePlanUvCheck"
code = (source / "Program.cs").read_text(encoding="utf-8-sig")
code = code.replace("var preparationUnits = new Dictionary<string, object>();",
    "var preparationUnits = new Dictionary<string, object>();\nvar regionChecks=new List<object>();")
code = code.replace('if(args.Length>4 && name!=args[4])continue;',
    'if(!name.StartsWith("PedestrianDrawBridge02",StringComparison.Ordinal))continue;\n    if(args.Length>4 && name!=args[4])continue;')
anchor = 'int originalReuses=chartUnits.Values.Count(u=>u.CanUseSource),originalReuseFailures=0;'
assert code.count(anchor) == 1
code = code.replace(anchor, anchor + '''
        string digest=mesh==1||name=="PedestrianDrawBridge02Pillar_LOD2 Mesh"?"different-source":
            "5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219";
        var regions=MovableRepeatUnits.Regions("PedestrianDraw02",digest);
        if(MovableRepeatUnits.Regions("PedestrianDraw02","incorrect-source").Count!=0)throw new Exception("Source identity gate");
        var eligible=chartUnits.Values.Where(u=>regions.Any(r=>r.ContainsDomain(u))).ToHashSet();
        int mappings=eligible.Count(u=>regions.Any(r=>MovableRepeatUnits.RegionMapping.Create(r,u)!=null));
        regionChecks.Add(new{name,lod,mesh,delta,charts=chartUnits.Count,
            regions=regions.Count,eligibleDomains=eligible.Count,eligibleMappings=mappings,
            receivers=directChecks.Count,eligibleReceivers=directChecks.Count(c=>eligible.Contains(c.Unit))});
''')
code += '\nFile.WriteAllText(args[3]+".regions.json",JsonSerializer.Serialize(regionChecks,new JsonSerializerOptions{WriteIndented=true}));\n'
(env / "Program.cs").write_text(code)
project = ET.parse(source / "MovablePlanUvCheck.csproj")
for node in project.findall(".//Compile"):
    node.set("Include", str((source / node.get("Include")).resolve()))
group = ET.SubElement(project.getroot(), "ItemGroup")
for name in ("NativeUvCodec.cs", "RoadUvChecks.cs", "RoadPartitionChecks.cs"):
    ET.SubElement(group, "Compile", Include=str(source / name))
for name in ("MovableRepeatUnits.cs", "MovableRepeatRegions.cs"):
    ET.SubElement(group, "Compile", Include=str(root / "src/BridgeBuilder/Bridges/Common/Movable" / name))
ET.SubElement(group, "Compile", Include=str(root / "src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02RepeatUnits.cs"))
ET.SubElement(project.getroot(), "Import", Project=str(root / "tools/Directory.Build.targets"))
project.write(env / "Coverage.csproj", encoding="utf-8")
numeric = (root / "src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02RoadAppearance.cs").read_text()
numeric = numeric[numeric.index("internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs"):]
(env / "NativeNumbers.cs").write_text("namespace BridgeBuilder.Bridges;\n" + numeric)
report = root / "artifacts/prepared-materials/PedestrianDraw02/repeat-call-semantics/production-coverage.json"
managed = "D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"
with report.with_suffix(".log").open("w", encoding="utf-8") as log:
    result = subprocess.run(["dotnet", "run", "--project", str(env / "Coverage.csproj"), "-c", "Release",
        "-p:GameManagedDir="+managed, "--", str(root / "src/BridgeBuilder/bin/Release/BridgeBuilder.dll"),
        managed, str(root / "artifacts/uv-density/layout-plans.bin"), str(report), "--probe-parent-samplers"],
        stdout=log, stderr=subprocess.STDOUT)
if result.returncode:
    raise SystemExit("Region coverage failed: " + str(report.with_suffix(".log")))
print(str(report)+".regions.json")
