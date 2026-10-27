"""Create a private Lift03 observer; never changes production plan or UV behavior."""
from pathlib import Path

root=Path(__file__).resolve().parents[3]
original=root/'tools/MovablePlanUvCheck'
target=root/'artifacts/agent-material-env/Lift03/rigid-probe'
target.mkdir(parents=True,exist_ok=True)
for p in original.glob('*.cs'):
    code=p.read_text()
    if p.name=='Program.cs':
        code=code.replace('var preparationUnits = new Dictionary<string, object>();',
            'var preparationUnits = new Dictionary<string, object>();\nvar rigidObservations=new Dictionary<string,RigidObservation>();\nvar rigidityClassifications=new List<object>();')
        code=code.replace('if(args.Length>4 && name!=args[4])continue;',
            'if(!implementation.StartsWith("Lift03",StringComparison.Ordinal))continue;\n    if(args.Length>4 && name!=args[4])continue;')
        code=code.replace('Vector2[] Uv,Vector2[] Local)>();', 'Vector2[] Uv,Vector2[] Local,int Face,int[] Ids,bool Fragment,bool Stretch)>();')
        code=code.replace('ids.Select(i=>uv[i]).ToArray(),q));', 'ids.Select(i=>uv[i]).ToArray(),q,face,ids,isFragment,localFrame.HasStretch));')
        code=code.replace('var directChecks=new List', 'long allFaces=0,allExactRigid=0,unchangedExactRigid=0,changedExactRigid=0;\n        var directChecks=new List')
        code=code.replace('                    if(localChanged)\n', '''                    bool exactFaceRigid=Enumerable.Range(0,3).All(axis=>
                        (double)positions[ids[0]][axis]-sourcePositions[ids[0]][axis]==(double)positions[ids[1]][axis]-sourcePositions[ids[1]][axis] &&
                        (double)positions[ids[0]][axis]-sourcePositions[ids[0]][axis]==(double)positions[ids[2]][axis]-sourcePositions[ids[2]][axis]);
                    allFaces++;if(exactFaceRigid){allExactRigid++;if(localChanged)changedExactRigid++;else unchangedExactRigid++;}
                    if(localChanged)
''')
        needle='int originalReuses=chartUnits.Values.Count(u=>u.CanUseSource),originalReuseFailures=0;'
        assert needle in code
        code=code.replace(needle,'''rigidityClassifications.Add(new{name,implementation,lod,mesh,family,delta,upper=scenario.Upper,lower=scenario.Lower,scenario=scenario.Label,
            allFaces,allExactRigid,unchangedExactRigid,changedExactRigid,
            originalSourceChecks=directChecks.Count(c=>c.Unit.CanUseSource),retileChecks=directChecks.Count(c=>!c.Unit.CanUseSource)});
        foreach(var pair in chartPreparation.Where(p=>!p.Key.CanUseSource)) {
            string signature=JsonSerializer.Serialize(pair.Value);
            if(!rigidObservations.TryGetValue(signature,out var observation))
                rigidObservations.Add(signature,observation=new RigidObservation{Recipe=pair.Value});
            foreach(var check in directChecks.Where(c=>ReferenceEquals(c.Unit,pair.Key))) {
                bool rigid=Enumerable.Range(0,3).All(axis=>
                    (double)check.Target[0][axis]-check.Source[0][axis]==(double)check.Target[1][axis]-check.Source[1][axis] &&
                    (double)check.Target[0][axis]-check.Source[0][axis]==(double)check.Target[2][axis]-check.Source[2][axis]);
                bool raw=check.Ids.All(i=>i<inputUv.Length && BitConverter.SingleToInt32Bits(uv[i].x)==BitConverter.SingleToInt32Bits(inputUv[i].x) && BitConverter.SingleToInt32Bits(uv[i].y)==BitConverter.SingleToInt32Bits(inputUv[i].y));
                bool same=check.Local.Select(check.Unit.SourceUv).Zip(check.Uv,(a,b)=>BitConverter.SingleToInt32Bits(a.x)==BitConverter.SingleToInt32Bits(b.x)&&BitConverter.SingleToInt32Bits(a.y)==BitConverter.SingleToInt32Bits(b.y)).All(x=>x);
                bool contained=check.Unit.Points.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&p.x>=0&&p.y>=0&&(check.Unit.Triangle?(double)p.x+p.y<=1:p.x<=1&&p.y<=1));
                observation.Faces++;if(rigid)observation.ExactRigid++;else observation.NonRigid++;
                if(rigid&&raw)observation.OriginalRawEligible++;
                if(rigid&&raw&&same)observation.RawAndReprojectedEqual++;
                if(rigid&&raw&&!same)observation.RawButReprojectedDiffer++;
                if(observation.Examples.Count<1 && (rigid&&raw || implementation=="Lift03ColumnGeometry" || pair.Key.A.x==0.3100586f && pair.Key.A.y==0.23828125f))
                    observation.Examples.Add(new{name,implementation,lod,mesh,family,delta,upper=scenario.Upper,lower=scenario.Lower,scenario=scenario.Label,
                        face=check.Face,ids=check.Ids,fragment=check.Fragment,exactRigid=rigid,originalRawUv=raw,frameHasStretch=check.Stretch,contained,pair.Key.RepeatOnly,reprojectedRawEqual=same,
                        increments=Enumerable.Range(0,3).Select(i=>Enumerable.Range(0,3).Select(axis=>(double)check.Target[i][axis]-check.Source[i][axis]).ToArray()),
                        source=check.Source.Select(p=>new[]{p.x,p.y,p.z}),target=check.Target.Select(p=>new[]{p.x,p.y,p.z}),
                        rawUv=check.Uv.Select(p=>new[]{p.x,p.y}),local=check.Local.Select(p=>new[]{p.x,p.y}),
                        reconstructed=check.Local.Select(check.Unit.SourceUv).Select(p=>new[]{p.x,p.y})});
            }
        }
        '''+needle)
        code=code.replace('if(failures>0)throw new Exception',
            'File.WriteAllText(args[3]+".rigid.json",JsonSerializer.Serialize(rigidObservations.Values));\nFile.WriteAllText(args[3]+".classifications.json",JsonSerializer.Serialize(rigidityClassifications));\nif(failures>0)throw new Exception')
    (target/p.name).write_text(code)
(target/'RigidObservation.cs').write_text('''internal sealed class RigidObservation {
 public object Recipe {get;set;}=null!;
 public long Faces {get;set;} public long ExactRigid {get;set;} public long NonRigid {get;set;}
 public long OriginalRawEligible {get;set;} public long RawAndReprojectedEqual {get;set;} public long RawButReprojectedDiffer {get;set;}
 public List<object> Examples {get;set;}=new();
}
''')
project=(original/'MovablePlanUvCheck.csproj').read_text().replace('../../src/',root.as_posix()+'/src/')
project=project.replace('</Project>',f'<Import Project="{root.as_posix()}/tools/Directory.Build.targets" /></Project>')
(target/'Lift03RigidProbe.csproj').write_text(project)
print(target)
