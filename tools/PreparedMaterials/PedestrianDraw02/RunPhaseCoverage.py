"""Run production geometry plans with the PedestrianDraw02 source-period selector."""
import argparse
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('repository', type=Path)
p.add_argument('managed', type=Path)
p.add_argument('fixture', type=Path)
a = p.parse_args()
root = a.repository.resolve()
env = root / 'artifacts/agent-material-env/PedestrianDraw02/phase-coverage'
env.mkdir(parents=True, exist_ok=True)
source = root / 'tools/MovablePlanUvCheck'
code = (source / 'Program.cs').read_text(encoding='utf-8-sig')
code = code.replace('var preparationUnits = new Dictionary<string, object>();',
                    'var preparationUnits = new Dictionary<string, object>();\nvar sourcePeriods=new List<object>();')
code = code.replace('if(args.Length>4 && name!=args[4])continue;',
                    'if(!name.StartsWith("PedestrianDrawBridge02",StringComparison.Ordinal))continue;\n    if(args.Length>4 && name!=args[4])continue;')
anchor = 'int originalReuses=chartUnits.Values.Count(u=>u.CanUseSource),originalReuseFailures=0;'
assert code.count(anchor) == 1
code = code.replace(anchor, anchor + '''
        int periodReuses=0, periodFaces=0, periodFailures=0, conditionedPeriodReuses=0;
        double periodWorst=0;
        foreach(var chart in chartUnits.Values.Where(u=>!u.CanUseSource))
        {
            if(!PedestrianDraw02NativeSampling.TrySourcePeriod(chart,out var native,out var phase))continue;
            periodReuses++; if(chart.RepeatOnly)conditionedPeriodReuses++;
            foreach(var check in directChecks.Where(c=>ReferenceEquals(c.Unit,chart)))
            {
                periodFaces++;
                var output=check.Local.Select(q=>native.SourceUv(q-phase)).ToArray();
                var before=Metric(check.Source,check.Uv.Select(q=>new[]{(double)q.x,q.y}).ToArray());
                var after=Metric(check.Target,output.Select(q=>new[]{(double)q.x,q.y}).ToArray());
                if(before.Length==0 || after.Length==0 || before.All(x=>x==0))continue;
                double error=Math.Sqrt(before.Zip(after,(x,y)=>(x-y)*(x-y)).Sum()/before.Sum(x=>x*x));
                periodWorst=Math.Max(periodWorst,error);
                if(!double.IsFinite(error) || error>.01)periodFailures++;
            }
        }
        sourcePeriods.Add(new{name,lod,mesh,delta,charts=chartUnits.Count,originalReuses,periodReuses,periodFaces,periodFailures,conditionedPeriodReuses,periodWorst});
''')
code += '\nFile.WriteAllText(args[3]+".source-periods.json",JsonSerializer.Serialize(sourcePeriods,new JsonSerializerOptions{WriteIndented=true}));\n'
(env / 'Program.cs').write_text(code, encoding='utf-8')
project = ET.parse(source / 'MovablePlanUvCheck.csproj')
for node in project.findall('.//Compile'):
    node.set('Include', str((source / node.get('Include')).resolve()))
group = ET.SubElement(project.getroot(), 'ItemGroup')
for name in ('NativeUvCodec.cs', 'RoadUvChecks.cs', 'RoadPartitionChecks.cs'):
    ET.SubElement(group, 'Compile', Include=str(source / name))
ET.SubElement(group, 'Compile', Include=str(root / 'tools/PreparedMaterials/PedestrianDraw02/PedestrianDraw02NativeSampling.cs'))
ET.SubElement(project.getroot(), 'Import', Project=str(root / 'tools/Directory.Build.targets'))
project.write(env / 'Coverage.csproj', encoding='utf-8')
report = root / 'artifacts/prepared-materials/PedestrianDraw02/source-consolidation/production-phase-coverage.json'
report.parent.mkdir(parents=True, exist_ok=True)
with report.with_suffix('.log').open('w', encoding='utf-8') as log:
    result = subprocess.run(['dotnet','run','--project',str(env/'Coverage.csproj'),'-c','Release',
        '-p:GameManagedDir='+str(a.managed.resolve()),'--',str(root/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll'),
        str(a.managed.resolve()),str(a.fixture.resolve()),str(report),'--probe-parent-samplers'],stdout=log,stderr=subprocess.STDOUT)
if result.returncode:
    raise SystemExit('Plan coverage failed; inspect '+str(report.with_suffix('.log')))
print(report)
