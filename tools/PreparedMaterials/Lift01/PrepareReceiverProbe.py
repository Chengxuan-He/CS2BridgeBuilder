"""Prepare private actual-plan receiver validation without building the mod."""
from pathlib import Path
import hashlib
import shutil

ROOT=Path(__file__).resolve().parents[3]
ENV=ROOT/'artifacts/agent-material-env/Lift01/chart-receiver'


def main():
    ENV.mkdir(parents=True,exist_ok=True)
    original=(ROOT/'tools/MovablePlanUvCheck/Program.cs').read_text()
    prefix='using System.Reflection;\nusing System.Text;\nusing System.Text.Json;\nusing BridgeBuilder.Bridges;\nusing UnityEngine;\n'
    prefix+=original[original.index('var managed='):original.index('        int tested=0,failed=0,degenerate=0;')]
    prefix=prefix.replace('var nativeCodec=new NativeUvCodec(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll"));','')
    prefix=prefix.replace('if(args.Length>4 && name!=args[4])continue;','if(!name.StartsWith("LiftBridge01",StringComparison.Ordinal))continue;')
    suffix=r'''
        int tested=0,contained=0,sourceOutside=0,empty=0,changedReceivers=0,changedContained=0;var examples=new List<object>();var sourceExamples=new List<object>();
        for(int di=0;di<domains.Items.Length;di++)
        {
            var domain=domains.Items[di];var charts=Lift01UvCharts.For(name,mesh,di);
            if(!domain.Faces.ToArray().Any(f=>!removed[f]||fragments.ContainsKey(f)))continue;
            var frame=new MovableUvFrame(source[domain.A],source[domain.B],source[domain.C],uv[domain.A],uv[domain.B],uv[domain.C],positions[domain.A],positions[domain.B],positions[domain.C]);
            foreach(int face in domain.Faces)
            {
                var ids0=fragments.TryGetValue(face,out var fragment)?fragment:removed[face]?Array.Empty<int>():indices.Skip(face*3).Take(3).ToArray();
                for(int at=0;at<ids0.Length;at+=3)
                {
                    var ids=ids0.Skip(at).Take(3).ToArray();if(ids.Distinct().Count()!=3)continue;
                    double[][] target;
                    if(fragments.ContainsKey(face))
                    {
                        var exact=MovableParentUv.Map(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>uv[i]).ToArray(),ids.Select(i=>positions[i]).ToArray(),new[]{source[domain.A],source[domain.B],source[domain.C]},new[]{uv[domain.A],uv[domain.B],uv[domain.C]},domain.Triangle,domain.Bounds).Exact;
                        target=exact.Select(q=>new[]{domain.Triangle?(double)uv[domain.A].x+((double)uv[domain.B].x-uv[domain.A].x)*q[0]+((double)uv[domain.C].x-uv[domain.A].x)*q[1]:domain.Bounds.x+((double)domain.Bounds.z-domain.Bounds.x)*q[0],domain.Triangle?(double)uv[domain.A].y+((double)uv[domain.B].y-uv[domain.A].y)*q[0]+((double)uv[domain.C].y-uv[domain.A].y)*q[1]:domain.Bounds.y+((double)domain.Bounds.w-domain.Bounds.y)*q[1]}).ToArray();
                    }
                    else target=ids.Select(i=>{var q=frame.Map(sourcePositions[i],positions[i],uv[i]);return new[]{(double)q.x,q.y};}).ToArray();
                    var src=ids.Select(i=>new[]{(double)uv[i].x,uv[i].y}).ToArray();
                    bool changed=Enumerable.Range(0,3).Any(i=>Math.Abs(src[i][0]-target[i][0])>1e-10||Math.Abs(src[i][1]-target[i][1])>1e-10);
                    bool accepted=charts.Any(chart=>chart.Contains(target));
                    tested++;if(charts.Length==0)empty++;if(accepted)contained++;
                    if(changed){changedReceivers++;if(accepted)changedContained++;}
                    if(!charts.Any(chart=>chart.Contains(src)))
                    {
                        sourceOutside++;
                        if(sourceExamples.Count<4)sourceExamples.Add(new{domain=di,face,fragment=fragments.ContainsKey(face),src,chartCount=charts.Length,loops=charts.Select(c=>c.Loops)});
                    }
                    if(!accepted&&changed&&examples.Count<4)examples.Add(new{domain=di,face,source=src,target,chartCount=charts.Length});
                }
            }
        }
        reports.Add(new{name,implementation,family,lod,mesh,delta,tested,contained,sourceOutside,empty,changedReceivers,changedContained,examples,sourceExamples});
        Console.WriteLine($"{name} mesh={mesh} delta={delta}: contained={contained}/{tested}; changed={changedContained}/{changedReceivers}; sourceOutside={sourceOutside}; empty={empty}");
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{evaluatedInputs,oracle="Independent complete triangle boundary/hole containment, no native shader",reports},new JsonSerializerOptions{WriteIndented=true}));
'''
    (ENV/'Program.cs').write_text(prefix+suffix)
    shutil.copyfile(ROOT/'tools/PreparedMaterials/Lift01/ReceiverChart.cs',ENV/'ReceiverChart.cs')
    files=['Bridges/HardcodedNumbers.cs','Bridges/Common/Movable/MovableUvFrame.cs','Bridges/Common/Movable/MovableParentUv.cs',
           'Bridges/Common/Movable/MovableStructurePacking.cs','Bridges/Common/Movable/MovableTextureFootprint.cs','Bridges/Common/Movable/MovableUvDomains.cs',
           'Bridges/Common/Movable/MovableUvCatalogue.g.cs','Bridges/Types/Lift01/Lift01UvCharts.cs']
    for file in files:shutil.copyfile(ROOT/'src/BridgeBuilder'/file,ENV/Path(file).name)
    shutil.copyfile(ROOT/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll',ENV/'BridgeBuilder.dll')
    (ENV/'Lift01ChartReceiver.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include="UnityEngine.CoreModule"><HintPath>D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed/UnityEngine.CoreModule.dll</HintPath></Reference><EmbeddedResource Include="'''+str(ROOT/'assets/HardcodedNumbers/Lift01/*.bin')+'''" LogicalName="BridgeBuilder.Numbers.%(Filename)%(Extension)" WithCulture="false" /></ItemGroup></Project>''')
    print('Prepared private Lift01 receiver probe; mod SHA256='+hashlib.sha256((ENV/'BridgeBuilder.dll').read_bytes()).hexdigest())


if __name__=='__main__':main()
