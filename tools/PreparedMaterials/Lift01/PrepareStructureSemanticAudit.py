"""Prepare a single exhaustive finite-width Lift01 production-domain audit."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
ENV = ROOT / 'artifacts/agent-material-env/Lift01/structure-semantic'


def main():
    ENV.mkdir(parents=True, exist_ok=True)
    prior = ROOT / 'artifacts/agent-material-env/Lift01/chart-receiver'
    code = (prior / 'Program.cs').read_text()
    code = code[:code.index('        int tested=0,contained=0')]
    code = code.replace('var reports=new List<object>();', '''var reports=new List<object>();
var all=new Dictionary<string,State>();
var seen=new HashSet<string>();
var sourceMap=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0])!,"source-map.json")))!;''')
    code = code.replace('    evaluatedInputs++;', '    if(!seen.Add(name+"|"+mesh))continue;\n    evaluatedInputs++;')
    code += r'''
        string digest=sourceMap[name+"|"+mesh];
        var regions=MovableRepeatUnits.Regions("Lift01",digest);
        for(int di=0;di<domains.Items.Length;di++)
        {
            var d=domains.Items[di];string id=name+"|"+mesh+"|"+di;
            if(!all.TryGetValue(id,out var state))all.Add(id,state=new State{Id=id,SourceDigest=digest});
            var charts=Lift01UvCharts.For(name,mesh,di);
            MovableStructurePacking.Unit Unit()=>new(){Triangle=d.Triangle,Bounds=d.Bounds,Charts=charts,
                A=d.Triangle?uv[d.A]:default,B=d.Triangle?uv[d.B]:default,C=d.Triangle?uv[d.C]:default};
            int[] Face(int face)=>fragments.TryGetValue(face,out var frag)?frag:removed[face]?Array.Empty<int>():new[]{indices[face*3],indices[face*3+1],indices[face*3+2]};
            bool Changed(int a,int b,int c,int[] ids)
            {
                var f=new MovableUvFrame(sourcePositions[a],sourcePositions[b],sourcePositions[c],uv[a],uv[b],uv[c],positions[a],positions[b],positions[c]);
                return f.HasStretch||ids.Any(i=>(f.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f);
            }
            void Record(MovableStructurePacking.Unit unit,int receivers,int face)
            {
                state.Changed+=receivers;
                if(unit.CanUseSource){state.Original+=receivers;return;}
                if(regions.Any(r=>MovableRepeatUnits.RegionMapping.Create(r,unit)!=null)){state.Repeated+=receivers;return;}
                state.Missing+=receivers;
                bool outside=false;
                for(int at=0;at<unit.Points.Count;at+=3)
                    if(!charts.Any(c=>c.ContainsTriangle(unit.SourceUv(unit.Points[at]),unit.SourceUv(unit.Points[at+1]),unit.SourceUv(unit.Points[at+2]))))outside=true;
                if(outside)state.Outside+=receivers;else state.Numeric+=receivers;
                state.Example??=new{delta,face,triangle=d.Triangle,source=new[]{uv[d.A],uv[d.B],uv[d.C]}.Select(q=>new[]{q.x,q.y}),
                    bounds=new[]{d.Bounds.x,d.Bounds.y,d.Bounds.z,d.Bounds.w},repeatOnly=unit.RepeatOnly,
                    target=unit.Points.Take(3).Select(q=>unit.SourceUv(q)).Select(q=>new[]{q.x,q.y})};
            }
            var rigid=new List<int>();int first=-1;
            foreach(int face in d.Faces)
            {
                var ids=Face(face);state.Receivers+=ids.Length/3;
                if(!fragments.ContainsKey(face)){rigid.AddRange(ids);if(ids.Length>0&&first<0)first=face;continue;}
                for(int at=0;at<ids.Length;at+=3)
                {
                    var corners=ids.Skip(at).Take(3).ToArray();
                    if(corners.Distinct().Count()!=3){state.Original++;continue;}
                    int pivot=MovableUvFrame.TrianglePivot(sourcePositions[corners[0]],sourcePositions[corners[1]],sourcePositions[corners[2]]);
                    if(!Changed(corners[pivot],corners[(pivot+1)%3],corners[(pivot+2)%3],corners)){state.Original++;continue;}
                    var unit=Unit();
                    var result=MovableParentUv.Map(corners.Select(i=>sourcePositions[i]).ToArray(),corners.Select(i=>uv[i]).ToArray(),corners.Select(i=>positions[i]).ToArray(),
                        new[]{sourcePositions[d.A],sourcePositions[d.B],sourcePositions[d.C]},new[]{uv[d.A],uv[d.B],uv[d.C]},d.Triangle,d.Bounds);
                    unit.Points.AddRange(result.Select(unit));Record(unit,1,face);
                }
            }
            if(rigid.Count==0)continue;
            if(!Changed(d.A,d.B,d.C,rigid.ToArray())){state.Original+=rigid.Count/3;continue;}
            var frame=new MovableUvFrame(sourcePositions[d.A],sourcePositions[d.B],sourcePositions[d.C],uv[d.A],uv[d.B],uv[d.C],positions[d.A],positions[d.B],positions[d.C]);
            var whole=Unit();
            foreach(int i in rigid)
            {
                var q=frame.Map(sourcePositions[i],positions[i],uv[i]);
                whole.Points.Add(d.Triangle?frame.TriangleUnit(positions[i]):new Vector2((q.x-d.Bounds.x)/(d.Bounds.z-d.Bounds.x),(q.y-d.Bounds.y)/(d.Bounds.w-d.Bounds.y)));
            }
            Record(whole,rigid.Count/3,first);
        }
        Console.WriteLine(name+" mesh="+mesh+" delta="+delta);
    }
}
if(all.Values.Any(s=>s.Receivers!=s.Original+s.Repeated+s.Missing))throw new Exception("Incomplete receiver disposition.");
File.WriteAllText(args[3],JsonSerializer.Serialize(new{evaluatedInputs,widthDeltas=new[]{-1.25,-.25,0,2,8,24},domains=all.Values},new JsonSerializerOptions{IncludeFields=true}));
Console.WriteLine("PASS complete Lift01 original-domain inventory: "+all.Count);
sealed class State {public string Id="",SourceDigest="";public long Receivers,Changed,Original,Repeated,Missing,Outside,Numeric;public object? Example;}
'''
    (ENV / 'Program.cs').write_text(code)
    common = ROOT / 'src/BridgeBuilder/Bridges/Common/Movable'
    for name in ('MovableUvFrame.cs','MovableParentUv.cs','MovableStructurePacking.cs','MovableTextureFootprint.cs','MovableUvDomains.cs','MovableUvCatalogue.g.cs','MovableUvChart.cs','MovableRepeatUnits.cs','MovableRepeatRegions.cs'):
        shutil.copyfile(common / name, ENV / name)
    for name in ('Lift01UvCharts.cs','Lift01RepeatUnits.cs'):
        shutil.copyfile(ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01' / name, ENV / name)
    shutil.copyfile(ROOT / 'src/BridgeBuilder/Bridges/HardcodedNumbers.cs', ENV / 'HardcodedNumbers.cs')
    shutil.copyfile(ROOT / 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll', ENV / 'BridgeBuilder.dll')
    shutil.copyfile(prior / 'Lift01ChartReceiver.csproj', ENV / 'Audit.csproj')
    (ENV/'Numbers.cs').write_text('namespace BridgeBuilder.Bridges; internal static class Numeric_Types_Lift01_Lift01RoadAppearance_cs {internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift01_Lift01RoadAppearance_cs");}')
    graph=json.loads((ROOT/'artifacts/prepared-materials/Lift01/rebuild-native-v1/config/source-graph.json').read_text())
    digests={s['cid']:s['sha256'] for s in graph['sources']}
    mapping={u['render']+'|'+str(u['slot']):digests[u['sourceCid']] for u in graph['usages']}
    (ENV/'source-map.json').write_text(json.dumps(mapping))
    (ENV/'inputs.json').write_text(json.dumps({str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [ROOT/'artifacts/uv-density/layout-plans.bin',*ENV.glob('*.cs'),ENV/'BridgeBuilder.dll']},indent=2))
    print(ENV)


if __name__=='__main__':main()
