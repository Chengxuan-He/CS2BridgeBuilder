"""Check measured Lift01 region eligibility on actual plan receivers, without baking."""
from pathlib import Path
import shutil
import hashlib
import json

ROOT = Path(__file__).resolve().parents[3]


def main():
    old = ROOT / 'artifacts/agent-material-env/Lift01/chart-receiver'
    env = ROOT / 'artifacts/agent-material-env/Lift01/region-receiver'
    env.mkdir(parents=True, exist_ok=True)
    code = (old / 'Program.cs').read_text()
    code = code[:code.index('        int tested=0,contained=0')]
    graph = json.loads((ROOT / 'artifacts/prepared-materials/Lift01/rebuild-native-v1/config/source-graph.json').read_text())
    digests = {s['cid']: s['sha256'] for s in graph['sources']}
    usages = {}
    for use in graph['usages']:
        assert hashlib.sha256((ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01' / use['code']).read_bytes()).hexdigest() == use['codeSha256']
        usages[use['render'] + '|' + str(use['slot'])] = digests[use['sourceCid']]
    (env / 'source-map.json').write_text(json.dumps(usages))
    code = code.replace('var reports=new List<object>();', 'var reports=new List<object>();\nvar sourceMap=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0])!,"source-map.json")))!;')
    code += '''
        int domainsTested=0,domainEligible=0,receivers=0,eligibleReceivers=0;
        string digest=sourceMap.TryGetValue(name+"|"+mesh,out var known)?known:"no-source";
        var regions=MovableRepeatUnits.Regions("Lift01",digest);
        if((regions.Count==1)!=(digest=="da154acf2452319660b3729ff4ebc7f181fbf6524fdf0c3b8411b47d4990bbf4")||MovableRepeatUnits.Regions("Lift01","wrong-source").Count!=0)throw new Exception("Exact source identity gate");
        foreach(var domain in domains.Items)
        {
            int live=domain.Faces.ToArray().Sum(face=>fragments.TryGetValue(face,out var fragment)?fragment.Length/3:removed[face]?0:1);
            if(live==0)continue;
            domainsTested++;receivers+=live;
            var unit=new MovableStructurePacking.Unit{Triangle=domain.Triangle,Bounds=domain.Bounds,A=uv[domain.A],B=uv[domain.B],C=uv[domain.C]};
            if(regions.Any(region=>region.ContainsDomain(unit))){domainEligible++;eligibleReceivers+=live;}
        }
        reports.Add(new{name,mesh,lod,delta,digest,sourceEligible=regions.Count>0,domainsTested,domainEligible,receivers,eligibleReceivers});
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{evaluatedInputs,method="Production Region.ContainsDomain on actual plan domains; zero eligibility makes further phase/gradient checks unreachable",reports},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS actual Lift01 region eligibility: "+reports.Count+" scenarios");
'''
    (env / 'Program.cs').write_text(code)
    for name in ('MovableUvDomains.cs', 'MovableUvCatalogue.g.cs', 'HardcodedNumbers.cs'):
        shutil.copyfile(old / name, env / name)
    for name in ('MovableRepeatUnits.cs', 'MovableRepeatRegions.cs'):
        shutil.copyfile(ROOT / 'src/BridgeBuilder/Bridges/Common/Movable' / name, env / name)
    shutil.copyfile(ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01RepeatUnits.cs', env / 'Lift01RepeatUnits.cs')
    shutil.copyfile(ROOT / 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll', env / 'BridgeBuilder.dll')
    (env / 'MinimalUnit.cs').write_text('''
using UnityEngine;
namespace BridgeBuilder.Bridges;
internal static class MovableStructurePacking {internal sealed class Unit {
 internal bool Triangle,RepeatOnly; internal Vector4 Bounds;internal Vector2 A,B,C;
 internal Dictionary<Vector2,double[]>? ExactCoordinates;internal List<Vector2> Points=new();}}
internal static class Numeric_Types_Lift01_Lift01RoadAppearance_cs {
 internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift01_Lift01RoadAppearance_cs");}
''')
    shutil.copyfile(old / 'Lift01ChartReceiver.csproj', env / 'Lift01RegionReceiver.csproj')
    print(env)


if __name__ == '__main__':
    main()
