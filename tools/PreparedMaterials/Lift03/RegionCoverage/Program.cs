using System.Reflection;
using System.Text;
using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;
var managed=Path.GetFullPath(args[1]);

AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
{
    var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
    return File.Exists(path)?Assembly.LoadFrom(path):null;
};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
const BindingFlags members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,members)!.GetValue(value)!;
using var reader=new BinaryReader(File.OpenRead(args[2]));
if(reader.ReadInt32()!=2)throw new Exception("Unsupported fixture version.");
int count=reader.ReadInt32(),evaluatedInputs=0;
var reports=new List<object>();
var domainAudit=new Dictionary<string,DomainAudit>();
var capabilityCounts=new Dictionary<string,int[]>();
var representatives=new Dictionary<string,object>();
using var inputs=JsonDocument.Parse(File.ReadAllText(args[4]));
var hashes=inputs.RootElement.GetProperty("sources").EnumerateArray().ToDictionary(x=>x.GetProperty("cid").GetString()!,x=>x.GetProperty("sha256").GetString()!);
var sourceLookup=inputs.RootElement.GetProperty("structuralBindings").EnumerateArray().ToDictionary(x=>(x.GetProperty("render").GetString()!,x.GetProperty("meshIndex").GetInt32()),x=>x.GetProperty("surfaceCid").GetString()!);
// Authoring input: only immutable samplers reached by real geometry plans.
// Constructed fragments retain separate physical charts, sharing their parent sampler.

for(int ci=0;ci<count;ci++)
{
    string Text()=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    string name=Text(),implementation=Text();
    int family=reader.ReadInt32(),lod=reader.ReadInt32(),mesh=reader.ReadInt32(),vertices=reader.ReadInt32(),indexCount=reader.ReadInt32();
    var source=new Vector3[vertices];var inputUv=new Vector2[vertices];var indices=new int[indexCount];
    for(int i=0;i<vertices;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
    for(int i=0;i<vertices;i++)inputUv[i]=new(reader.ReadSingle(),reader.ReadSingle());
    for(int i=0;i<indices.Length;i++)indices[i]=reader.ReadInt32();
    if(!implementation.StartsWith("Lift03"))continue;
    evaluatedInputs++;
    var selector=implementation.Split('#');
    var method=mod.GetType("BridgeBuilder.Bridges."+selector[0])?.GetMethod(selector.Length==2?selector[1]:"Create",BindingFlags.Static|BindingFlags.NonPublic);
    var domains=MovableUvDomains.For(name,mesh)??throw new Exception("Missing measured recipe "+name);
    var scenarios = new List<(float Delta, float Upper, float Lower, float Left, float Right, string Label)>();
    foreach(float d in new[]{-1.25f,-.25f,0f,2f,8f,24f})scenarios.Add((d,d,d,3.5f,3.5f,"symmetric"));
    if (implementation is "Lift03DetailedStructure" or "Lift03FarSupports#CreateSupplement")
        foreach(var widths in new[]{(0f,8f),(8f,0f),(2f,8f),(8f,2f),(-1.25f,24f),(24f,-1.25f)})
            scenarios.Add((Math.Max(widths.Item1,widths.Item2),widths.Item1,widths.Item2,3.5f,3.5f,"independent-decks"));
    bool selectedLayout = mesh==0 && lod<2 && (implementation is "Draw02MovingShell" or "Draw03DetailedStructure" or "Draw01RoadReplacement#CreateMainMeshPlan" or "Draw01UndersideSupplement");
    if(selectedLayout)
        foreach(float d in new[]{0f,2f,8f})
        foreach(var widths in new[]{(0f,0f),(2f,0f),(0f,2f),(1.5f,4.5f),(4.5f,1.5f)})
            scenarios.Add((d,d,d,widths.Item1,widths.Item2,"selected-sidewalks"));
    foreach(var scenario in scenarios)
    {

        float delta=scenario.Delta;
        object Layout(Type type)
        {
            var layout=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
            var left=scenario.Left>0?new Vector2(-6-delta-scenario.Left,-6-delta):Vector2.zero;
            var right=scenario.Right>0?new Vector2(6+delta,6+delta+scenario.Right):Vector2.zero;
            if(type.Name=="Draw01UndersideLayout")type.GetField("Spans",members)!.SetValue(layout,new[]{new Vector2(-6-delta,6+delta),left,right});
            else
            {
                type.GetField("LeftSidewalk",members)!.SetValue(layout,left);
                type.GetField("RightSidewalk",members)!.SetValue(layout,right);
            }
            return layout;
        }
        object Argument(ParameterInfo p)
        {
            if(p.ParameterType==typeof(Vector3[]))return (Vector3[])source.Clone();
            if(p.ParameterType==typeof(int[]))return indices;
            if(p.ParameterType==typeof(int[][]))return new[]{indices};
            if(p.ParameterType==typeof(float))return p.Name=="upperDelta"?scenario.Upper:p.Name=="lowerDelta"?scenario.Lower:delta;
            if(p.Name=="lod")return lod;
            if(p.Name=="mesh")return mesh;
            if(p.Name is "family" or "part")return family;
            if(p.Name=="footing")return family==1;
            if(p.Name=="column")return family==0;
            if(p.Name=="underside")
            {
                // Explicit selected-road fixture: 12 m carriageway, two 3.5 m
                // sidewalks, widened equally. No game Prefab constructor is used.
                return Layout(p.ParameterType);
            }
            throw new Exception("Unsupported plan argument "+implementation+"."+p.Name);
        }
        object plan;
        try
        {
        if(implementation is "Identity" or "Draw01Glass" or "Draw01UndersideSupplement")
        {
            var pos=(Vector3[])source.Clone();
            if(implementation=="Draw01Glass")mod.GetType("BridgeBuilder.Bridges.Draw01FixedWideningData")!.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{lod,mesh,delta,pos});
            var type=mod.GetType("BridgeBuilder.Bridges.MovableStructureModel+Plan")!;
            var mask=new bool[indices.Length/3];object? corrections=null;
            if(implementation=="Draw01UndersideSupplement")
            {
                Array.Fill(mask,true);
                var apply=mod.GetType("BridgeBuilder.Bridges.Draw01UndersideData")!.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic)!;
                apply.Invoke(null,new object[]{lod,Layout(apply.GetParameters()[1].ParameterType),pos,mask});
                corrections=mod.GetType("BridgeBuilder.Bridges.Draw01UndersideUv")!.GetMethod("ForLod",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{lod});
            }
            plan=type.GetConstructors(members).Single().Invoke(new object?[]{source,pos,new[]{indices},new[]{mask},null,null,null,null,false,corrections});
        }
        else plan=(method??throw new Exception("Missing plan method "+implementation)).Invoke(null,method.GetParameters().Select(Argument).ToArray())!;
        if(implementation=="Lift01FarTowerCore")plan=mod.GetType("BridgeBuilder.Bridges.Lift01FarEndLips")!.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new[]{plan,(object)delta})!;
        if(implementation=="Draw02MovingShell")
        {
            plan=mod.GetType("BridgeBuilder.Bridges.Draw02FixedCurbs")!.GetMethod("Remove",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new[]{(object)lod,plan})!;
            var apply=mod.GetType("BridgeBuilder.Bridges.Draw02FixedSidewalks")!.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic)!;
            plan=apply.Invoke(null,new[]{(object)lod,plan,Layout(apply.GetParameters()[2].ParameterType)})!;
        }
        if(implementation=="Draw03DetailedStructure")
        {
            var apply=mod.GetType("BridgeBuilder.Bridges.Draw03JointLayout")!.GetMethod("Apply",BindingFlags.Static|BindingFlags.NonPublic)!;
            plan=apply.Invoke(null,new[]{(object)lod,plan,Layout(apply.GetParameters()[2].ParameterType),delta})!;
        }
        }
        catch(TargetInvocationException ex) when (delta<0 && ex.InnerException is InvalidOperationException invalid &&
            (invalid.Message=="Draw02 selected width collapses or reverses its recorded central linkage span." ||
             invalid.Message=="PedestrianDraw01 A-frame layout or span is invalid."))
        {
            reports.Add(new{name,implementation,lod,mesh,delta,rejected=true,reason=ex.InnerException.Message});
            Console.WriteLine($"REJECT {name}: delta={delta}; {ex.InnerException.Message}");
            continue;
        }
        var positions=Field<Vector3[]>(plan,"Positions");
        var sourcePositions=new Vector3[positions.Length];Array.Copy(source,sourcePositions,source.Length);
        var uv=new Vector2[positions.Length];Array.Copy(inputUv,uv,inputUv.Length);
        foreach(var correction in Field<KeyValuePair<int,Vector2>[]>(plan,"UvCorrections"))uv[correction.Key]=correction.Value;
        var construction=plan.GetType().GetMethod("Construction",members)!;
        for(int i=source.Length;i<positions.Length;i++)
        {
            var v=construction.Invoke(plan,new object[]{i-source.Length})!;
            int a=Field<int>(v,"A"),b=Field<int>(v,"B"),c=Field<int>(v,"C");
            float wb=Field<float>(v,"WeightB"),wc=Field<float>(v,"WeightC");
            sourcePositions[i]=source[a]+(source[b]-source[a])*wb+(source[c]-source[a])*wc;
            uv[i]=uv[a]+(uv[b]-uv[a])*wb+(uv[c]-uv[a])*wc;
        }
        var removed=Field<bool[][]>(plan,"RemoveTriangles")[0];
        var fragments=new Dictionary<int,int[]>();
        foreach(var fragment in Field<Array>(plan,"Fragments"))fragments.Add(Field<int>(fragment!,"Triangle"),Field<int[]>(fragment!,"Indices"));

        var units=new List<MovableStructurePacking.Unit>();
        var unitVertices=new Dictionary<MovableStructurePacking.Unit,int[]>();
        string nativeCid=sourceLookup[(name,mesh)];
        var activeAudit=new DomainAudit[domains.Items.Length];
        for(int di=0;di<domains.Items.Length;di++)
        {
            var id=name+"|"+mesh+"|"+di;
            if(!domainAudit.TryGetValue(id,out var entry))
            {
                var charts=Lift03UvCharts.For(name,mesh,di);
                entry=new DomainAudit{id=id,sourceDigest=hashes[nativeCid],render=name,mesh=mesh,lod=lod,domain=di,chartCount=charts.Length};
                domainAudit.Add(id,entry);
            }
            activeAudit[di]=entry;
        }
        int retainedTriangles=0,unchangedTriangles=0;
        for(int di=0;di<domains.Items.Length;di++)
        {
            var d=domains.Items[di];
            var charts=Lift03UvCharts.For(name,mesh,di);
            int[] Face(int f)=>fragments.TryGetValue(f,out var fragment)?fragment:removed[f]?Array.Empty<int>():indices.Skip(f*3).Take(3).ToArray();
            var domainVertices=d.Faces.ToArray().Where(f=>!fragments.ContainsKey(f)).SelectMany(Face).Distinct().ToArray();
            var frame=new MovableUvFrame(sourcePositions[d.A],sourcePositions[d.B],sourcePositions[d.C],uv[d.A],uv[d.B],uv[d.C],positions[d.A],positions[d.B],positions[d.C]);
            bool changed=domainVertices.Length>0&&(frame.HasStretch||domainVertices.Any(i=>(frame.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f));
            MovableStructurePacking.Unit Unit()=>new(){Id=di,Triangle=d.Triangle,Bounds=d.Bounds,Charts=charts,A=d.Triangle?uv[d.A]:default,B=d.Triangle?uv[d.B]:default,C=d.Triangle?uv[d.C]:default};
            var aggregate=Unit();
            var aggregateVertices=new List<int>();
            foreach(int face in d.Faces)
            {
                var faceIds=Face(face);
                for(int at=0;at<faceIds.Length;at+=3)
                {
                    retainedTriangles++;activeAudit[di].receivers++;
                    var ids=faceIds.Skip(at).Take(3).ToArray();
                    if(fragments.ContainsKey(face))
                    {
                        if(ids.Distinct().Count()!=3){unchangedTriangles++;activeAudit[di].original++;continue;}
                        int pivot=MovableUvFrame.TrianglePivot(sourcePositions[ids[0]],sourcePositions[ids[1]],sourcePositions[ids[2]]);
                        int a=ids[pivot],b=ids[(pivot+1)%3],c=ids[(pivot+2)%3];
                        var local=new MovableUvFrame(sourcePositions[a],sourcePositions[b],sourcePositions[c],uv[a],uv[b],uv[c],positions[a],positions[b],positions[c]);
                        if(!local.HasStretch&&!ids.Any(i=>(local.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f)){unchangedTriangles++;activeAudit[di].original++;continue;}
                        var unit=Unit();
                        var projection=MovableParentUv.Map(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>uv[i]).ToArray(),ids.Select(i=>positions[i]).ToArray(),new[]{sourcePositions[d.A],sourcePositions[d.B],sourcePositions[d.C]},new[]{uv[d.A],uv[d.B],uv[d.C]},d.Triangle,d.Bounds);
                        var points=projection.Select(unit);unit.Points.AddRange(points);units.Add(unit);unitVertices.Add(unit,ids);
                    }
                    else
                    {
                        if(!changed){unchangedTriangles++;activeAudit[di].original++;continue;}
                        aggregateVertices.AddRange(ids);
                        foreach(int i in ids)
                        {
                            var value=frame.Map(sourcePositions[i],positions[i],uv[i]);
                            aggregate.Points.Add(d.Triangle?frame.TriangleUnit(positions[i]):new Vector2((value.x-d.Bounds.x)/(d.Bounds.z-d.Bounds.x),(value.y-d.Bounds.y)/(d.Bounds.w-d.Bounds.y)));
                        }
                    }
                }
            }
            if(aggregate.Points.Count>0){units.Add(aggregate);unitVertices.Add(aggregate,aggregateVertices.ToArray());}
        }
        if(!sourceLookup.TryGetValue((name,mesh),out var sourceCid))throw new Exception("Missing native source identity "+name+"/"+mesh);
        var regions=MovableRepeatUnits.Regions("Lift03",hashes[sourceCid]);
        int sourceUnits=0,sourceTriangles=0,repeatUnits=0,repeatTriangles=0,remainingUnits=0,remainingTriangles=0,containedDomains=0;
        var remainingDomainIds=new HashSet<int>();
        foreach(var unit in units)
        {
            if(unit.CanUseSource){sourceUnits++;sourceTriangles+=unit.Points.Count/3;activeAudit[unit.Id].original+=unit.Points.Count/3;continue;}
            if(regions.Any(r=>r.ContainsDomain(unit)))containedDomains++;
            var mapping=regions.Select(r=>MovableRepeatUnits.RegionMapping.Create(r,unit)).FirstOrDefault(x=>x!=null);
            if(mapping!=null){repeatUnits++;repeatTriangles+=unit.Points.Count/3;activeAudit[unit.Id].repeat+=unit.Points.Count/3;}
            else{remainingUnits++;remainingTriangles+=unit.Points.Count/3;remainingDomainIds.Add(unit.Id);activeAudit[unit.Id].remaining+=unit.Points.Count/3;
                if(unit.RepeatOnly)activeAudit[unit.Id].conditionedPhase++;
                if(unit.Charts.Length==0)activeAudit[unit.Id].unavailableChart++;
                else if(unit.Points.Chunk(3).All(t=>unit.Charts.Any(c=>c.ContainsTriangle(unit.SourceUv(t[0]),unit.SourceUv(t[1]),unit.SourceUv(t[2])))))activeAudit[unit.Id].gradientFailure++;
                else activeAudit[unit.Id].outsideAuthoredChart++;
                var vertexIds=unitVertices[unit];
                var shift=positions[vertexIds[0]]-sourcePositions[vertexIds[0]];
                var exactShift=Enumerable.Range(0,3).Select(axis=>(double)positions[vertexIds[0]][axis]-sourcePositions[vertexIds[0]][axis]).ToArray();
                bool rigid=vertexIds.All(i=>Enumerable.Range(0,3).All(axis=>(double)positions[i][axis]-sourcePositions[i][axis]==exactShift[axis]));
                bool contained=unit.Charts.Length!=0&&unit.Points.Chunk(3).All(t=>unit.Charts.Any(c=>c.ContainsTriangle(unit.SourceUv(t[0]),unit.SourceUv(t[1]),unit.SourceUv(t[2]))));
                string capability=rigid?"exact-rigid-raw-uv-route":unit.Charts.Length==0?"reliable-authored-part-boundary":contained?"float32-gradient-preserving-native-mapping":"same-pattern-coverage-or-nonperiodic-atlas-mapping";
                if(!capabilityCounts.TryGetValue(capability,out var tally))capabilityCounts.Add(capability,tally=new int[2]);
                tally[0]++;tally[1]+=unit.Points.Count/3;
                string representativeKey=capability+"/"+sourceCid;
                if(!representatives.ContainsKey(representativeKey))
                {
                    double[] V2(Vector2 v)=>new[]{(double)v.x,v.y};
                    double[] V3(Vector3 v)=>new[]{(double)v.x,v.y,v.z};
                    representatives.Add(representativeKey,new{capability,name,implementation,family,lod,mesh,domain=unit.Id,delta,upper=scenario.Upper,lower=scenario.Lower,scenario=scenario.Label,sourceCid,sourceDigest=hashes[sourceCid],rigid,exactShift,shift=V3(shift),unit.Triangle,bounds=new[]{(double)unit.Bounds.x,unit.Bounds.y,unit.Bounds.z,unit.Bounds.w},a=V2(unit.A),b=V2(unit.B),c=V2(unit.C),chartCount=unit.Charts.Length,unit.RepeatOnly,vertexIds,sourcePositions=vertexIds.Select(i=>V3(sourcePositions[i])).ToArray(),targetPositions=vertexIds.Select(i=>V3(positions[i])).ToArray(),originalUv=vertexIds.Select(i=>V2(uv[i])).ToArray(),points=unit.Points.Select(V2).ToArray(),unit.ExactPoints,exactCoordinates=unit.ExactCoordinates?.Select(p=>new{point=V2(p.Key),exact=p.Value}).ToArray(),exactSourceUvs=unit.ExactSourceUvs?.Select(p=>new{point=V2(p.Key),source=V2(p.Value)}).ToArray()});
                }
            }
        }
        reports.Add(new{name,implementation,family,lod,mesh,delta,upper=scenario.Upper,lower=scenario.Lower,scenario=scenario.Label,sourceCid,retainedTriangles,unchangedTriangles,sourceUnits,sourceTriangles,repeatUnits,repeatTriangles,remainingUnits,remainingTriangles,containedDomains,remainingDomainIds=remainingDomainIds.Order().ToArray()});
        Console.WriteLine($"{name} {delta}: original={unchangedTriangles+sourceTriangles}; repeat={repeatTriangles}; remaining={remainingTriangles}");
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{style="Lift03",evaluatedInputs,geometryAssemblySha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(args[0]))),reports},new JsonSerializerOptions{WriteIndented=true}));

File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[3])!,"original-domain-receivers.json"),JsonSerializer.Serialize(new{schema="Lift03/OriginalDomainReceivers/1",style="Lift03",finiteScenarios=reports.Count,domains=domainAudit.Values},new JsonSerializerOptions{IncludeFields=true}));
File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[3])!,"capability-reproducers.json"),JsonSerializer.Serialize(new{style="Lift03",counts=capabilityCounts.ToDictionary(p=>p.Key,p=>new{units=p.Value[0],triangles=p.Value[1]}),representatives=representatives.Values},new JsonSerializerOptions{WriteIndented=true}));
sealed class DomainAudit
{
 public string id="",sourceDigest="",render="";
 public int mesh,lod,domain,chartCount,receivers,original,repeat,remaining,conditionedPhase,unavailableChart,gradientFailure,outsideAuthoredChart;
}
