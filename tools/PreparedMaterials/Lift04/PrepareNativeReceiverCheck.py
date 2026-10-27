"""Source-link the actual shared native predicate in the isolated Lift04 check."""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[3]
target=root/'artifacts/agent-material-env/Lift04/native-receiver-check'
target.mkdir(parents=True,exist_ok=True)
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk')
props=ET.SubElement(project,'PropertyGroup')
for name,value in dict(OutputType='Exe',TargetFramework='net8.0',ImplicitUsings='enable',Nullable='enable').items(): ET.SubElement(props,name).text=value
group=ET.SubElement(project,'ItemGroup')
reference=ET.SubElement(group,'Reference',Include='UnityEngine.CoreModule')
ET.SubElement(reference,'HintPath').text='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed/UnityEngine.CoreModule.dll'
for filename in ('MovableUvChart','MovableUvFrame','MovableParentUv','MovableTextureFootprint','MovableStructurePacking','MovableChartPartition'):
    ET.SubElement(group,'Compile',Include=str(root/'src/BridgeBuilder/Bridges/Common/Movable'/(filename+'.cs')))
for filename in ('src/BridgeBuilder/Bridges/HardcodedNumbers.cs','src/BridgeBuilder/Bridges/Types/Lift04/Lift04UvCharts.cs','tools/PreparedMaterials/Lift04/NativeReceiverCheck.cs'):
    ET.SubElement(group,'Compile',Include=str(root/filename))
ET.SubElement(group,'EmbeddedResource',Include=str(root/'assets/HardcodedNumbers/Lift04/Lift04UvCharts.bin'),LogicalName='BridgeBuilder.Numbers.Lift04UvCharts.bin',WithCulture='false')
ET.ElementTree(project).write(target/'NativeReceiverCheck.csproj',encoding='unicode')
print(target/'NativeReceiverCheck.csproj')
