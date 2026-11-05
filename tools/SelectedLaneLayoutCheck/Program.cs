using BridgeBuilder.Bridges;
using Kind = BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind;
using Lane = BridgeBuilder.Bridges.MovableRoadLaneLayout.Lane;

Lane L(float left, float right, Kind kind, float start = -80, float end = 80)
    => new((left+right)/2, right-left, left, right, start, end, kind, 0);
void Check(Lane[] lanes, float[] expected, float[] low, float z = 0)
{
    var result = MovableRoadLaneLayout.BoundariesAt(lanes,z);
    if (!result.Select(b=>b.X).SequenceEqual(expected) ||
        !result.Where(b=>b.CarCar).Select(b=>b.X).SequenceEqual(low))
        throw new Exception("Native-width boundary classification changed.");
}
// Unequal native widths: the shared edge is 0, not the centre midpoint -.25.
Check(new[]{L(-3,0,Kind.Car),L(0,2,Kind.Car)},new[]{-3f,0,2},new[]{0f});
// Median/no-lane gap: both car-facing edges remain high, no midpoint railing.
Check(new[]{L(-4,-1,Kind.Car),L(1,4,Kind.Car)},new[]{-4f,-1,1,4},Array.Empty<float>());
// Bicycle/pedestrian/parking/track regions are not car-car adjacency.
foreach(var type in new[]{Kind.Bicycle,Kind.Pedestrian,Kind.Parking,Kind.Track,Kind.Other})
    Check(new[]{L(-3,0,Kind.Car),L(0,2,type),L(2,5,Kind.Car)},new[]{-3f,0,2,5},Array.Empty<float>());
// Duplicate longitudinal occurrences do not duplicate a boundary.
Check(new[]{L(-3,0,Kind.Car),L(-3,0,Kind.Car),L(0,3,Kind.Car)},new[]{-3f,0,3},new[]{0f});
// A lane that does not cover this station cannot create adjacency.
Check(new[]{L(-3,0,Kind.Car),L(0,3,Kind.Car,10,80)},new[]{-3f,0},Array.Empty<float>());
Check(new[]{L(-3,0,Kind.Car)},Array.Empty<float>(),Array.Empty<float>(),100);
Check(new[]{L(-3,0,Kind.Pedestrian)},Array.Empty<float>(),Array.Empty<float>());
// Tiny positive gaps must not be swallowed by an arbitrary adjacency tolerance.
Check(new[]{L(-3,0,Kind.Car),L(float.Epsilon,3,Kind.Car)},new[]{-3f,0,float.Epsilon,3},Array.Empty<float>());
// Overlapping lanes must not manufacture a low railing through an occupied lane.
Check(new[]{L(-3,1,Kind.Car),L(0,3,Kind.Car)},new[]{-3f,3},Array.Empty<float>());
// The native curve parameter is not linear physical Z: z=8*t^3.
var taper = new Lane(0,0,0,0,0,0,0,8,3,3,5,1,Kind.Car,8);
if(taper.WidthAt(1)!=4 || taper.LeftAt(1)!=-2 || taper.RightAt(1)!=2 ||
    taper.WidthAt(0)!=3 || taper.WidthAt(8)!=5 || taper.ConstantCrossSection)
    throw new Exception("Variable native endpoint widths lost their Bezier parameter.");
Check(new[]{taper},new[]{-2f,2},Array.Empty<float>(),1);
var reverse = new Lane(0,8,0,0,0,0,0,0,3,5,3,-1,Kind.Car,8);
if(reverse.WidthAt(1)!=4 || reverse.LeftAt(1)!=-2 || reverse.StartWidth!=3 || reverse.EndWidth!=5)
    throw new Exception("Reversed lane endpoint widths or transformed lateral sign changed.");
Console.WriteLine("PASS source-linked native-width adjacency: unequal widths, non-lane median gap, five non-car classes, duplicates, longitudinal coverage, no-car, positive gap and overlap.");

if(args.Length==2)
{
    AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
    {
        var path=Path.Combine(args[1],new System.Reflection.AssemblyName(e.Name).Name+".dll");
        return File.Exists(path)?System.Reflection.Assembly.LoadFrom(path):null;
    };
    var assembly=System.Reflection.Assembly.LoadFrom(Path.GetFullPath(args[0]));
    const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|
        System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
    var level=assembly.GetType("BridgeBuilder.Bridges.MovableRoadAssets+Level",true)!;
    var supplement=assembly.GetType("BridgeBuilder.Bridges.MovableRoadAssets+Supplement",true)!;
    var supplementConstructor=supplement.GetConstructors(flags).Single();
    var first=supplementConstructor.Invoke(new object?[]{null,0,null,null});
    var second=supplementConstructor.Invoke(new object?[]{null,1,null,"first-physical-occurrence"});
    var third=supplementConstructor.Invoke(new object?[]{null,2,null,"second-physical-occurrence"});
    var constructor=level.GetConstructors(flags).Single(c=>c.GetParameters().Any(p=>p.Name=="triangles"));
    foreach(var withLegacy in new[]{false,true}) foreach(var count in new[]{0,2})
    {
        var extras=Array.CreateInstance(supplement,count);
        if(count!=0){extras.SetValue(second,0);extras.SetValue(third,1);}
        var parameters=constructor.GetParameters().Select(p=>p.Name switch
        {
            "triangles"=>Array.CreateInstance(p.ParameterType.GetGenericArguments()[0],0),
            "surfaces"=>Array.CreateInstance(p.ParameterType.GetElementType()!,0),
            "skeleton"=>System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(p.ParameterType),
            "extraStructure"=>withLegacy?first:null,
            "additionalStructures"=>extras,
            _=>p.HasDefaultValue?p.DefaultValue:p.ParameterType.IsValueType?Activator.CreateInstance(p.ParameterType):null
        }).ToArray();
        var instance=constructor.Invoke(parameters);
        if(count!=0)extras.SetValue(first,0); // Constructor must snapshot the caller's collection.
        var observed=((System.Collections.IEnumerable)level.GetProperty("StructuralSupplements",flags)!.GetValue(instance)!).Cast<object>().ToArray();
        var expected=(withLegacy?new[]{first}:Array.Empty<object>()).Concat(count==0?Array.Empty<object>():new[]{second,third}).ToArray();
        if(!observed.SequenceEqual(expected))throw new Exception("Actual Level lost/reordered supplements or retained a mutable caller array.");
    }
    Console.WriteLine("PASS actual compiled Level constructors: no/legacy/multiple/combined supplements retain order and snapshot input collections. This is not a geometry/skin upload test.");
}
