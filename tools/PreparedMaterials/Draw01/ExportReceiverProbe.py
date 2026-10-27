"""Create an isolated Draw01 receiver observer from the production plan checker."""
from pathlib import Path
import hashlib
import json
root=Path(__file__).resolve().parents[3]
original=root/'tools/MovablePlanUvCheck'
target=root/'artifacts/agent-material-env/Draw01/receiver-probe'
target.mkdir(exist_ok=True)
inputs=[]
def replace_once(code, old, new):
    assert code.count(old)==1, f'Expected one source marker: {old}'
    return code.replace(old,new)
for p in original.glob('*.cs'):
    code=p.read_text()
    inputs.append(dict(path=str(p.relative_to(root)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    if p.name=='Program.cs':
        code=replace_once(code,'var preparationUnits = new Dictionary<string, object>();',
            'var preparationUnits = new Dictionary<string, object>();\nvar receiverObservations = new List<object>();')
        code=replace_once(code,'if(args.Length>4 && name!=args[4])continue;',
            'if(!(name.StartsWith("DrawBridge01",StringComparison.Ordinal) || implementation.StartsWith("Draw01",StringComparison.Ordinal)))continue;\n    if(args.Length>4 && name!=args[4])continue;')
        code=replace_once(code,'foreach(var domain in domains.Items)',
            'foreach(var (domain, domainIndex) in domains.Items.Select((domain, index)=>(domain, index)))')
        needle='chartPreparation[chart]=prepared;'
        assert needle in code
        code=replace_once(code,needle,needle+'''
            receiverObservations.Add(new{name,implementation,mesh,lod,family,delta,scenario=scenario.Label,left=scenario.Left,right=scenario.Right,
                domain=domainIndex,sourceFace=face,fragment=isFragment,fragmentTriangle=at/3,
                chart.Triangle,RepeatOnly=parentUnit?.RepeatOnly??false,
                a=new[]{chart.A.x,chart.A.y},b=new[]{chart.B.x,chart.B.y},c=new[]{chart.C.x,chart.C.y},
                bounds=new[]{chart.Bounds.x,chart.Bounds.y,chart.Bounds.z,chart.Bounds.w},
                points=q.Select(point=>new[]{point.x,point.y}).ToArray()});
        ''')
        code=replace_once(code,'if(failures>0)throw new Exception',
            'File.WriteAllText(args[3]+".receivers.json",JsonSerializer.Serialize(receiverObservations));\nif(failures>0)throw new Exception')
    (target/p.name).write_text(code)
project=(original/'MovablePlanUvCheck.csproj').read_text().replace('../../src/',root.as_posix()+'/src/')
project=project.replace('</Project>',f'<Import Project="{root.as_posix()}/tools/Directory.Build.targets" /></Project>')
(target/'Draw01ReceiverProbe.csproj').write_text(project)
report=root/'artifacts/prepared-materials/Draw01/receiver-provenance-source.json'
report.write_text(json.dumps(dict(inputs=inputs,observerSha256=hashlib.sha256((target/'Program.cs').read_bytes()).hexdigest(),
    generatorSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    scope='Copied production plan checker with diagnostic-only per-triangle parent-domain provenance.'),indent=2)+'\n')
print(target)
