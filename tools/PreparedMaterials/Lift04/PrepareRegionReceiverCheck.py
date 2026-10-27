"""Source-link production region mapping and original BBN values in a Lift04 check."""
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[3]
target=root/'artifacts/agent-material-env/Lift04/region-receiver-check'
target.mkdir(parents=True,exist_ok=True)
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk')
props=ET.SubElement(project,'PropertyGroup')
for name,value in dict(OutputType='Exe',TargetFramework='net8.0',ImplicitUsings='enable',Nullable='enable').items():ET.SubElement(props,name).text=value
group=ET.SubElement(project,'ItemGroup')
reference=ET.SubElement(group,'Reference',Include='UnityEngine.CoreModule')
ET.SubElement(reference,'HintPath').text='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed/UnityEngine.CoreModule.dll'
for name in ('MovableUvChart','MovableUvFrame','MovableParentUv','MovableTextureFootprint','MovableStructurePacking','MovableChartPartition','MovableRepeatUnits','MovableRepeatRegions'):
    ET.SubElement(group,'Compile',Include=str(root/'src/BridgeBuilder/Bridges/Common/Movable'/(name+'.cs')))
for file in ('src/BridgeBuilder/Bridges/HardcodedNumbers.cs','src/BridgeBuilder/Bridges/Types/Lift04/Lift04UvCharts.cs','src/BridgeBuilder/Bridges/Types/Lift04/Lift04RepeatUnits.cs','tools/PreparedMaterials/Lift04/RegionReceiverCheck.cs'):
    ET.SubElement(group,'Compile',Include=str(root/file))
for name in ('Lift04UvCharts','Types_Lift04_Lift04RoadAppearance_cs','Types_Lift04_BeamRepeatRegions','Types_Lift04_StructuralProfileRegions'):
    ET.SubElement(group,'EmbeddedResource',Include=str(root/'assets/HardcodedNumbers/Lift04'/(name+'.bin')),LogicalName='BridgeBuilder.Numbers.'+name+'.bin',WithCulture='false')
source=(root/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RoadAppearance.cs').read_text()
numeric=source[source.index('internal static class Numeric_Types_Lift04_Lift04RoadAppearance_cs'):]
(target/'NativeNumbers.cs').write_text('namespace BridgeBuilder.Bridges;\n'+numeric)
ET.ElementTree(project).write(target/'RegionReceiverCheck.csproj',encoding='unicode')
print(target/'RegionReceiverCheck.csproj')
