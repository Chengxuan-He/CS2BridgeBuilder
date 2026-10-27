using System.Reflection;
using System.Text;
using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

bool probeParentSamplers=args.Contains("--probe-parent-samplers");
args=args.Where(a=>a!="--probe-parent-samplers").ToArray();
var managed=Path.GetFullPath(args[1]);

AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
{
    var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
    return File.Exists(path)?Assembly.LoadFrom(path):null;
};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
const BindingFlags members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,members)!.GetValue(value)!;
double[] Metric(Vector3[] p,double[][] uv)
{
    var e=Enumerable.Range(0,3).Select(i=>(double)p[1][i]-p[0][i]).ToArray();
    var f=Enumerable.Range(0,3).Select(i=>(double)p[2][i]-p[0][i]).ToArray();
    double len=Math.Sqrt(e.Sum(x=>x*x));
    var cross=new[]{e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]};
    double y=Math.Sqrt(cross.Sum(x=>x*x))/len,x=e.Zip(f,(a,b)=>a*b).Sum()/len;
    if(!(len>0) || !(y>0))return Array.Empty<double>();
    double ux=(uv[1][0]-uv[0][0])/len,vx=(uv[1][1]-uv[0][1])/len;
    double uy=(uv[2][0]-uv[0][0]-ux*x)/y,vy=(uv[2][1]-uv[0][1]-vx*x)/y;
    return new[]{ux*ux+uy*uy,ux*vx+uy*vy,vx*vx+vy*vy};
}
using var reader=new BinaryReader(File.OpenRead(args[2]));
if(reader.ReadInt32()!=2)throw new Exception("Unsupported fixture version.");
int count=reader.ReadInt32(),failures=0,evaluatedInputs=0;
var reports=new List<object>();
using var compressed=new System.IO.Compression.GZipStream(File.Create(args[3]+".receivers.jsonl.gz"),System.IO.Compression.CompressionLevel.Fastest);
using var receivers=new StreamWriter(compressed);
// Authoring input: only immutable samplers reached by real geometry plans.
// Constructed fragments retain separate physical charts, sharing their parent sampler.
var preparationUnits = new Dictionary<string, object>();
for(int ci=0;ci<count;ci++)
{
    string Text()=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    string name=Text(),implementation=Text();
    int family=reader.ReadInt32(),lod=reader.ReadInt32(),mesh=reader.ReadInt32(),vertices=reader.ReadInt32(),indexCount=reader.ReadInt32();
    var source=new Vector3[vertices];var inputUv=new Vector2[vertices];var indices=new int[indexCount];
    for(int i=0;i<vertices;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
    for(int i=0;i<vertices;i++)inputUv[i]=new(reader.ReadSingle(),reader.ReadSingle());
    for(int i=0;i<indices.Length;i++)indices[i]=reader.ReadInt32();
    if(!name.StartsWith("DrawBridge03",StringComparison.Ordinal))continue;
    evaluatedInputs++;
    var selector=implementation.Split('#');
    var method=mod.GetType("BridgeBuilder.Bridges."+selector[0])?.GetMethod(selector.Length==2?selector[1]:"Create",BindingFlags.Static|BindingFlags.NonPublic);
    var domains=MovableUvDomains.For(name,mesh)??throw new Exception("Missing measured recipe "+name);
    var scenarios = new List<(float Delta, float Upper, float Lower, float Left, float Right, string Label)>();
    foreach(float d in new[]{0f,2f,8f})scenarios.Add((d,d,d,3.5f,3.5f,"symmetric"));
    if (implementation is "Lift03DetailedStructure" or "Lift03FarSupports#CreateSupplement")
        foreach(var widths in new[]{(0f,8f),(8f,0f),(2f,8f),(8f,2f),(-1.25f,24f),(24f,-1.25f)})
            scenarios.Add((Math.Max(widths.Item1,widths.Item2),widths.Item1,widths.Item2,3.5f,3.5f,"independent-decks"));
    bool selectedLayout = mesh==0 && lod<2 && (implementation is "Draw02MovingShell" or "Draw03DetailedStructure" or "Draw01RoadReplacement#CreateMainMeshPlan" or "Draw01UndersideSupplement");
    if(false && selectedLayout)
        foreach(float d in new[]{0f,2f,8f})
        foreach(var widths in new[]{(0f,0f),(2f,0f),(0f,2f),(1.5f,4.5f),(4.5f,1.5f)})
            scenarios.Add((d,d,d,widths.Item1,widths.Item2,"selected-sidewalks"));
    foreach(var scenario in scenarios)
    {
        if(args.Length>4 && (scenario.Delta!=(args.Length>5?float.Parse(args[5],System.Globalization.CultureInfo.InvariantCulture):8) || scenario.Label!="symmetric"))continue;
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
        int tested=0,failed=0,degenerate=0;double worst=0;
        int parentTested=0,parentFailed=0,parentPhaseTested=0,parentPhaseFailed=0;double parentWorst=0,parentPhaseWorst=0;
        var parentStoredUvs=new List<Vector2>();
        var units=new HashSet<(bool,Vector4,Vector2,Vector2,Vector2)>();
        var packedUnits=new Dictionary<(bool,Vector4,Vector2,Vector2,Vector2),MovableStructurePacking.Unit>();
        var chartUnits=new Dictionary<((bool,Vector4,Vector2,Vector2,Vector2),int,int,int),MovableStructurePacking.Unit>();
        var chartPreparation=new Dictionary<MovableStructurePacking.Unit,object>();
        var directChecks=new List<(MovableStructurePacking.Unit Unit,Vector3[] Source,Vector3[] Target,Vector2[] Uv,Vector2[] Local)>();
        for(int domainIndex=0;domainIndex<domains.Items.Length;domainIndex++)
        {
            var domain=domains.Items[domainIndex];
            if(!domain.Faces.ToArray().Any(f=>!removed[f] || fragments.ContainsKey(f)))continue;
            var frame=new MovableUvFrame(source[domain.A],source[domain.B],source[domain.C],uv[domain.A],uv[domain.B],uv[domain.C],
                positions[domain.A],positions[domain.B],positions[domain.C]);
            int[] Face(int f)=>fragments.TryGetValue(f,out var fragment)?fragment:removed[f]?Array.Empty<int>():indices.Skip(f*3).Take(3).ToArray();
            bool changed=frame.HasStretch || domain.Faces.ToArray().Where(f=>!fragments.ContainsKey(f)).SelectMany(Face).Distinct().Any(i=>(frame.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f);
            foreach(int face in domain.Faces)
            {
                var faceIds=fragments.TryGetValue(face,out var fragment)?fragment:removed[face]?Array.Empty<int>():indices.Skip(face*3).Take(3).ToArray();
                for(int at=0;at<faceIds.Length;at+=3)
                {
                    var ids=faceIds.Skip(at).Take(3).ToArray();
                    if(ids.Distinct().Count()!=3){degenerate++;continue;}
                    bool isFragment=fragments.ContainsKey(face);
                    int pivot=isFragment?MovableUvFrame.TrianglePivot(sourcePositions[ids[0]],sourcePositions[ids[1]],sourcePositions[ids[2]]):0;
                    int anchorA=isFragment?ids[pivot]:domain.A,anchorB=isFragment?ids[(pivot+1)%3]:domain.B,anchorC=isFragment?ids[(pivot+2)%3]:domain.C;
                    MovableUvFrame localFrame;
                    try { localFrame=isFragment?new MovableUvFrame(sourcePositions[anchorA],sourcePositions[anchorB],sourcePositions[anchorC],
                        uv[anchorA],uv[anchorB],uv[anchorC],positions[anchorA],positions[anchorB],positions[anchorC]):frame; }
                    catch(ArgumentException)
                    {
                        Console.WriteLine($"Invalid frame: {name}; mesh={mesh}; delta={delta}; face={face}; fragment={isFragment}");
                        Console.WriteLine(JsonSerializer.Serialize(new{ids,source=ids.Select(i=>new[]{sourcePositions[i].x,sourcePositions[i].y,sourcePositions[i].z}),target=ids.Select(i=>new[]{positions[i].x,positions[i].y,positions[i].z})}));
                        throw;
                    }
                    bool localChanged=isFragment?localFrame.HasStretch || ids.Any(i=>(localFrame.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f):changed;
                    MovableParentUv.Result? parentMap=null;
                    MovableStructurePacking.Unit? parentUnit=null;
                    Vector2[]? parentCoordinates=null;
                    if(isFragment && localChanged)
                    {
                        parentMap=MovableParentUv.Map(ids.Select(i=>sourcePositions[i]).ToArray(),
                            ids.Select(i=>uv[i]).ToArray(),ids.Select(i=>positions[i]).ToArray(),
                            new[]{source[domain.A],source[domain.B],source[domain.C]},
                            new[]{uv[domain.A],uv[domain.B],uv[domain.C]},domain.Triangle,domain.Bounds);
                        parentUnit=new MovableStructurePacking.Unit{Triangle=domain.Triangle,Bounds=domain.Bounds,
                            A=domain.Triangle?uv[domain.A]:default,B=domain.Triangle?uv[domain.B]:default,C=domain.Triangle?uv[domain.C]:default};
                        parentCoordinates=parentMap.Select(parentUnit);
                    }
                    if(probeParentSamplers && isFragment && localChanged)
                    {
                        var parentPoints=parentMap!.Points;
                        parentStoredUvs.AddRange(parentPoints);
                        // Compare the continuous source phase, including the removed
                        // integer repeat offset. Vertex modulo would pass at corners
                        // but fail these interior interpolation probes.
                        foreach(var weights in new[]{new[]{1d,0,0},new[]{0d,1,0},new[]{0d,0,1},new[]{.173,.317,.51},new[]{.793,.107,.1}})
                        {
                            double dx=parentMap.OffsetX,dy=parentMap.OffsetY;
                            for(int corner=0;corner<3;corner++)
                            {
                                dx+=weights[corner]*(parentPoints[corner].x-parentMap.Exact[corner][0]);
                                dy+=weights[corner]*(parentPoints[corner].y-parentMap.Exact[corner][1]);
                            }
                            double ex=domain.Triangle?((double)uv[domain.B].x-uv[domain.A].x)*dx+((double)uv[domain.C].x-uv[domain.A].x)*dy:((double)domain.Bounds.z-domain.Bounds.x)*dx;
                            double ey=domain.Triangle?((double)uv[domain.B].y-uv[domain.A].y)*dx+((double)uv[domain.C].y-uv[domain.A].y)*dy:((double)domain.Bounds.w-domain.Bounds.y)*dy;
                            double phaseError=4096*Math.Max(Math.Abs(ex),Math.Abs(ey));
                            parentPhaseTested++;parentPhaseWorst=Math.Max(parentPhaseWorst,phaseError);
                            if(!double.IsFinite(phaseError) || phaseError>.01)parentPhaseFailed++;
                        }
                        var parentMapped=parentPoints.Select(q=>Enumerable.Range(0,2).Select(axis=>domain.Triangle
                            ? (double)uv[domain.A][axis]+((double)uv[domain.B][axis]-uv[domain.A][axis])*q.x+((double)uv[domain.C][axis]-uv[domain.A][axis])*q.y
                            : (double)domain.Bounds[axis]+((double)domain.Bounds[axis+2]-domain.Bounds[axis])*q[axis]).ToArray()).ToArray();
                        var before=Metric(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>new[]{(double)uv[i].x,uv[i].y}).ToArray());
                        var after=Metric(ids.Select(i=>positions[i]).ToArray(),parentMapped);
                        if(before.Length!=0 && after.Length!=0 && before.Any(x=>x!=0))
                        {
                            double parentError=Math.Sqrt(before.Zip(after,(a,b)=>(a-b)*(a-b)).Sum()/before.Sum(x=>x*x));
                            parentTested++;parentWorst=Math.Max(parentWorst,parentError);
                            if(!double.IsFinite(parentError) || parentError>.01)
                            {
                                if(parentFailed<2)
                                {
                                    Console.WriteLine($"PROBE parent sampler density violation: {name}; mesh={mesh}; delta={delta}; face={face}; triangle={domain.Triangle}; error={parentError:R}");
                                    Console.WriteLine(JsonSerializer.Serialize(new{before,after,
                                        parentPoints=parentPoints.Select(q=>new[]{q.x,q.y}),
                                        source=ids.Select(i=>new[]{sourcePositions[i].x,sourcePositions[i].y,sourcePositions[i].z}),
                                        target=ids.Select(i=>new[]{positions[i].x,positions[i].y,positions[i].z}),
                                        uv=ids.Select(i=>new[]{uv[i].x,uv[i].y}),
                                        parentUv=new[]{uv[domain.A],uv[domain.B],uv[domain.C]}.Select(q=>new[]{q.x,q.y}),
                                        bounds=new[]{domain.Bounds.x,domain.Bounds.y,domain.Bounds.z,domain.Bounds.w}}));
                                }
                                parentFailed++;
                            }
                        }
                    }
                    if(localChanged)
                    {
                        var key=domain.Triangle?(true,default(Vector4),uv[domain.A],uv[domain.B],uv[domain.C]):(false,domain.Bounds,default(Vector2),default(Vector2),default(Vector2));
                        var prepared = new { render = name, implementation, mesh, fragment = false,
                            triangle = key.Item1, bounds = new[] { key.Item2.x, key.Item2.y, key.Item2.z, key.Item2.w },
                            a = new[] { key.Item3.x, key.Item3.y }, b = new[] { key.Item4.x, key.Item4.y }, c = new[] { key.Item5.x, key.Item5.y } };
                        units.Add(key);
                        var q=parentCoordinates??ids.Select(i=>key.Item1?localFrame.TriangleUnit(positions[i]):new Vector2(
                            (localFrame.Map(sourcePositions[i],positions[i],uv[i]).x-domain.Bounds.x)/(domain.Bounds.z-domain.Bounds.x),
                            (localFrame.Map(sourcePositions[i],positions[i],uv[i]).y-domain.Bounds.y)/(domain.Bounds.w-domain.Bounds.y))).ToArray();
                        if(!packedUnits.TryGetValue(key,out var unit))packedUnits.Add(key,unit=new MovableStructurePacking.Unit{Id=packedUnits.Count,Triangle=key.Item1,Bounds=key.Item2,A=key.Item3,B=key.Item4,C=key.Item5,Min=q[0],Max=q[0]});
                        foreach(var value in q){unit.Min=Vector2.Min(unit.Min,value);unit.Max=Vector2.Max(unit.Max,value);unit.Points.Add(value);}
                        var chartKey=(key,anchorA,anchorB,anchorC);
                        if(!chartUnits.TryGetValue(chartKey,out var chart))chartUnits.Add(chartKey,chart=new MovableStructurePacking.Unit{Id=chartUnits.Count,Triangle=key.Item1,Bounds=key.Item2,A=key.Item3,B=key.Item4,C=key.Item5,Min=q[0],Max=q[0]});
                        chart.RepeatOnly|=parentUnit?.RepeatOnly??false;
                        chartPreparation[chart]=prepared;
                        foreach(var value in q){chart.Min=Vector2.Min(chart.Min,value);chart.Max=Vector2.Max(chart.Max,value);chart.Points.Add(value);}
                        directChecks.Add((chart,ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>positions[i]).ToArray(),ids.Select(i=>uv[i]).ToArray(),q));
                    }
                    var mapped=ids.Select(i=>
                    {
                        if(!localChanged)return new[]{(double)uv[i].x,uv[i].y};
                        if(parentCoordinates!=null)
                        {
                            var point=parentCoordinates[Array.IndexOf(ids,i)];
                            return Enumerable.Range(0,2).Select(axis=>domain.Triangle
                                ? (double)uv[domain.A][axis]+point.x*((double)uv[domain.B][axis]-uv[domain.A][axis])+point.y*((double)uv[domain.C][axis]-uv[domain.A][axis])
                                : domain.Bounds[axis]+(double)point[axis]*(domain.Bounds[axis+2]-domain.Bounds[axis])).ToArray();
                        }
                        if(!domain.Triangle && !isFragment)
                        {var value=localFrame.Map(sourcePositions[i],positions[i],uv[i]);return new[]{(double)value.x,value.y};}
                        var q=localFrame.TriangleUnit(positions[i]);
                        // The output stores local unit UVs, not re-expanded atlas
                        // coordinates. Evaluate its texture Jacobian in double to
                        // avoid introducing an atlas quantization absent at runtime.
                        return Enumerable.Range(0,2).Select(axis=>(double)uv[anchorA][axis]+
                            q.x*((double)uv[anchorB][axis]-uv[anchorA][axis])+q.y*((double)uv[anchorC][axis]-uv[anchorA][axis])).ToArray();
                    }).ToArray();
                    if(localChanged)receivers.WriteLine(JsonSerializer.Serialize(new{name,mesh,delta,domain=domainIndex,face,fragment=isFragment,mapped}));
                    var a=Metric(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>new[]{(double)uv[i].x,uv[i].y}).ToArray());
                    var b=Metric(ids.Select(i=>positions[i]).ToArray(),mapped);
                    if(a.Length==0 || b.Length==0){degenerate++;continue;}
                    double norm=Math.Sqrt(a.Sum(x=>x*x));if(norm==0)continue;
                    double error=Math.Sqrt(a.Zip(b,(x,y)=>(x-y)*(x-y)).Sum())/norm;
                    tested++;worst=Math.Max(worst,error);
                    if(!double.IsFinite(error) || error>.01)
                    {
                        if(failed<3)
                        {
                            Console.WriteLine($"FAIL {name} delta={delta} face={face} fragment={isFragment} changed={localChanged} triangleUnit={domain.Triangle} error={error:R}");
                            Console.WriteLine(JsonSerializer.Serialize(new{ids,source=ids.Select(i=>new[]{sourcePositions[i].x,sourcePositions[i].y,sourcePositions[i].z}),target=ids.Select(i=>new[]{positions[i].x,positions[i].y,positions[i].z}),uv=ids.Select(i=>new[]{uv[i].x,uv[i].y}),unit=ids.Select(i=>localFrame.TriangleUnit(positions[i])).Select(q=>new[]{q.x,q.y}),mapped,a,b}));
                        }
                        failed++;
                    }
                }
            }
        }
        reports.Add(new{name,implementation,mesh,lod,delta,tested,failed,degenerate,worst});
        failures+=failed;
        Console.WriteLine($"{name}: delta={delta}; tested={tested}; failed={failed}");
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{evaluatedInputs,failures,reports},new JsonSerializerOptions{WriteIndented=true}));
if(failures>0)throw new Exception("Actual receiver density checks failed.");
