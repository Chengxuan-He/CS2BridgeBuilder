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
int count=reader.ReadInt32(),evaluatedInputs=0;
var reports=new List<object>();
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

        int changedCount=0,unchangedCount=0,contained=0,sourcePreviously=0,newlyContained=0,noChart=0,densityFailed=0,publicAccepted=0;double worst=0;
        for(int di=0;di<domains.Items.Length;di++)
        {
            var domain=domains.Items[di];
            var charts=Lift03UvCharts.For(name,mesh,di);
            if(!ReferenceEquals(charts,Lift03UvCharts.For(name,mesh,di)))throw new Exception("Unshared chart set");
            if(!domain.Faces.ToArray().Any(f=>!removed[f] || fragments.ContainsKey(f)))continue;
            var frame=new MovableUvFrame(source[domain.A],source[domain.B],source[domain.C],uv[domain.A],uv[domain.B],uv[domain.C],positions[domain.A],positions[domain.B],positions[domain.C]);
            int[] Face(int f)=>fragments.TryGetValue(f,out var fragment)?fragment:removed[f]?Array.Empty<int>():indices.Skip(f*3).Take(3).ToArray();
            bool changed=frame.HasStretch || domain.Faces.ToArray().Where(f=>!fragments.ContainsKey(f)).SelectMany(Face).Distinct().Any(i=>(frame.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f);
            foreach(int face in domain.Faces)
            {
                var faceIds=Face(face);
                for(int at=0;at<faceIds.Length;at+=3)
                {
                    var ids=faceIds.Skip(at).Take(3).ToArray();if(ids.Distinct().Count()!=3)continue;
                    bool fragment=fragments.ContainsKey(face);
                    int pivot=fragment?MovableUvFrame.TrianglePivot(sourcePositions[ids[0]],sourcePositions[ids[1]],sourcePositions[ids[2]]):0;
                    int aa=fragment?ids[pivot]:domain.A,bb=fragment?ids[(pivot+1)%3]:domain.B,cc=fragment?ids[(pivot+2)%3]:domain.C;
                    var local=fragment?new MovableUvFrame(sourcePositions[aa],sourcePositions[bb],sourcePositions[cc],uv[aa],uv[bb],uv[cc],positions[aa],positions[bb],positions[cc]):frame;
                    bool localChanged=fragment?local.HasStretch || ids.Any(i=>(local.Map(sourcePositions[i],positions[i],uv[i])-uv[i]).sqrMagnitude>1e-12f):changed;
                    if(!localChanged){unchangedCount++;continue;}
                    changedCount++;
                    var unit=new MovableStructurePacking.Unit{Triangle=domain.Triangle,Bounds=domain.Bounds,A=uv[domain.A],B=uv[domain.B],C=uv[domain.C]};
                    Vector2[] q;
                    if(fragment)
                    {
                        var pm=MovableParentUv.Map(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>uv[i]).ToArray(),ids.Select(i=>positions[i]).ToArray(),new[]{source[domain.A],source[domain.B],source[domain.C]},new[]{uv[domain.A],uv[domain.B],uv[domain.C]},domain.Triangle,domain.Bounds);
                        q=pm.Exact.Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();
                    }
                    else q=ids.Select(i=>domain.Triangle?local.TriangleUnit(positions[i]):new Vector2((local.Map(sourcePositions[i],positions[i],uv[i]).x-domain.Bounds.x)/(domain.Bounds.z-domain.Bounds.x),(local.Map(sourcePositions[i],positions[i],uv[i]).y-domain.Bounds.y)/(domain.Bounds.w-domain.Bounds.y))).ToArray();
                    unit.Points.AddRange(q);bool previous=unit.CanUseSource;if(previous)sourcePreviously++;
                    var atlas=q.Select(unit.SourceUv).ToArray();
                    if(charts.Length==0){noChart++;continue;}
                    if(!charts.Any(chart=>Contains(chart,atlas)))continue;
                    contained++;if(!previous)newlyContained++;
                    unit.Charts=charts;if(unit.CanUseSource)publicAccepted++;
                    var a=Metric(ids.Select(i=>sourcePositions[i]).ToArray(),ids.Select(i=>new[]{(double)uv[i].x,uv[i].y}).ToArray());
                    var b=Metric(ids.Select(i=>positions[i]).ToArray(),atlas.Select(v=>new[]{(double)v.x,v.y}).ToArray());
                    if(a.Length==0||b.Length==0||a.All(v=>v==0))continue;
                    double error=Math.Sqrt(a.Zip(b,(x,y)=>(x-y)*(x-y)).Sum()/a.Sum(v=>v*v));worst=Math.Max(worst,error);
                    if(!double.IsFinite(error)||error>.01)densityFailed++;
                }
            }
        }
        reports.Add(new{name,implementation,family,lod,mesh,delta,upper=scenario.Upper,lower=scenario.Lower,scenario=scenario.Label,changedCount,unchangedCount,contained,sourcePreviously,newlyContained,noChart,publicAccepted,densityFailed,worst});
        Console.WriteLine($"{name} {delta}: changed={changedCount}; chart-contained={contained}; added={newlyContained}; densityFailures={densityFailed}");
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{style="Lift03",evaluatedInputs,publicChartImplementation=typeof(MovableUvChart).FullName,reports},new JsonSerializerOptions{WriteIndented=true}));

bool Contains(MovableUvChart chart,Vector2[] triangle)=>chart.ContainsTriangle(triangle[0],triangle[1],triangle[2]);
