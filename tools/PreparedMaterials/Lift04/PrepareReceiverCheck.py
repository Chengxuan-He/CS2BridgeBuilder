"""Create an isolated instrumented copy of the actual plan audit for Lift04."""
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[3]
source = root/'tools/MovablePlanUvCheck'
target = root/'artifacts/agent-material-env/Lift04/receiver-check'
target.mkdir(parents=True, exist_ok=True)
for file in source.glob('*.cs'): shutil.copyfile(file, target/file.name)
code = (source/'Program.cs').read_text()
code = code.replace('var preparationUnits =', 'using var observations = new StreamWriter(args[3]+".receivers.ndjson");\nvar preparationUnits =', 1)
code = code.replace('if(args.Length>4 && name!=args[4])continue;', 'if(!name.StartsWith("LiftBridge04",StringComparison.Ordinal))continue;\n    if(args.Length>4 && name!=args[4])continue;', 1)
needle = 'foreach(var value in q){unit.Min=Vector2.Min(unit.Min,value);unit.Max=Vector2.Max(unit.Max,value);unit.Points.Add(value);}'
injection = '''observations.WriteLine(JsonSerializer.Serialize(new{name,mesh,lod,delta,domain=Array.IndexOf(domains.Items,domain),face,fragment=isFragment,repeatOnly=parentUnit?.RepeatOnly??false,triangle=key.Item1,bounds=new[]{key.Item2.x,key.Item2.y,key.Item2.z,key.Item2.w},a=new[]{key.Item3.x,key.Item3.y},b=new[]{key.Item4.x,key.Item4.y},c=new[]{key.Item5.x,key.Item5.y},points=q.Select(v=>new[]{v.x,v.y}),exact=parentMap?.Exact,hasExactCoordinates=parentUnit?.ExactCoordinates!=null,exactPoints=parentUnit?.ExactPoints,hasExactSourceUvs=parentUnit?.ExactSourceUvs!=null}));'''
assert code.count(needle) == 1
code = code.replace(needle, needle+'\n                        '+injection)
code = code.replace('parentCoordinates=parentMap.Select(parentUnit);','parentUnit.Charts=Lift04UvCharts.For(name,mesh,Array.IndexOf(domains.Items,domain));\n                        parentCoordinates=parentMap.Select(parentUnit);')
(target/'Program.cs').write_text(code)
tree = ET.parse(source/'MovablePlanUvCheck.csproj')
for item in tree.findall('.//Compile'):
    item.set('Include', str((source/item.attrib['Include']).resolve()))
group = ET.SubElement(tree.getroot(),'ItemGroup')
for file in ('HardcodedNumbers.cs','AffineGeometry.cs'):
    ET.SubElement(group,'Compile',Include=str(root/'src/BridgeBuilder/Bridges'/file),Link=file)
ET.SubElement(group,'Compile',Include=str(root/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04UvCharts.cs'))
ET.SubElement(group,'EmbeddedResource',Include=str(root/'assets/HardcodedNumbers/**/*.bin'),LogicalName='BridgeBuilder.Numbers.%(Filename)%(Extension)',WithCulture='false')
tree.write(target/'Lift04ReceiverCheck.csproj', encoding='unicode')
print(target/'Lift04ReceiverCheck.csproj')
