using BridgeBuilder.Bridges;
using UnityEngine;

// Compute the UV gradient Gram matrix independently from physical edge lengths.
// Equal determinant/area alone would miss reciprocal stretch in the two axes.
double[] Metric(Vector3[] p, Vector2[] uv)
{
    var e=Enumerable.Range(0,3).Select(i=>(double)p[1][i]-p[0][i]).ToArray();
    var f=Enumerable.Range(0,3).Select(i=>(double)p[2][i]-p[0][i]).ToArray();
    double g00=e.Sum(x=>x*x),g01=e.Zip(f,(x,y)=>x*y).Sum(),g11=f.Sum(x=>x*x);
    var cross=new[]{e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]};
    double det=cross.Sum(x=>x*x);
    var u=(x:(double)uv[1].x-uv[0].x,y:(double)uv[1].y-uv[0].y);
    var v=(x:(double)uv[2].x-uv[0].x,y:(double)uv[2].y-uv[0].y);
    double H(double a,double b,double c,double d)=>(a*c*g11-(a*d+b*c)*g01+b*d*g00)/det;
    return new[]{H(u.x,v.x,u.x,v.x),H(u.x,v.x,u.y,v.y),H(u.y,v.y,u.y,v.y)};
}
void EqualMetric(Vector3[] source,Vector2[] sourceUv,Vector3[] target,Vector2[] targetUv)
{
    var a=Metric(source,sourceUv);var b=Metric(target,targetUv);
    for(int i=0;i<3;i++)
        if(Math.Abs(a[i]-b[i])>2e-5*Math.Max(1e-5,Math.Abs(a[i])))
            throw new Exception($"Native density changed in tensor entry {i}: {a[i]} -> {b[i]}");
}
var source=new[]{new Vector3(2,3,4),new Vector3(5,4,7),new Vector3(1,8,6)};
var uv=new[]{new Vector2(.23f,.17f),new Vector2(.67f,.31f),new Vector2(.37f,.89f)};
var deformations=new Func<Vector3,Vector3>[] {
    p=>p,
    p=>p+new Vector3(15,-8,7),
    p=>new Vector3(p.z,p.x,p.y),
    p=>new Vector3(p.x*4,p.y,p.z),
    p=>new Vector3(p.x*.25f,p.y,p.z),
    p=>new Vector3(p.x*2,p.y*.5f,p.z),
    p=>new Vector3(p.x+p.z*.75f,p.y*1.7f-p.x*.2f,p.z*.6f),
    p=>new Vector3(-p.x*2,p.y,p.z*3)
};
foreach(var deform in deformations)
{
    var target=source.Select(deform).ToArray();
    var frame=new MovableUvFrame(source[0],source[1],source[2],uv[0],uv[1],uv[2],target[0],target[1],target[2]);
    var mapped=Enumerable.Range(0,3).Select(i=>frame.Map(source[i],target[i],uv[i])).ToArray();
    EqualMetric(source,uv,target,mapped);
    var sourceNormal=Vector3.Cross(source[1]-source[0],source[2]-source[0]).normalized;
    var targetNormal=Vector3.Cross(target[1]-target[0],target[2]-target[0]).normalized;
    if((frame.Rotate(sourceNormal)-targetNormal).sqrMagnitude>1e-10f)
        throw new Exception("UV-frame rotation changed oriented normal/reflection semantics.");
    if(mapped[0]!=uv[0])throw new Exception("Measured anchor phase changed.");
}
var identity=new MovableUvFrame(source[0],source[1],source[2],uv[0],uv[1],uv[2],source[0],source[1],source[2]);
var residual=new Vector2(.123456f,.987654f);
if(identity.Map(source[0]+(source[1]-source[0])*.3f,source[0]+(source[1]-source[0])*.3f,residual)!=residual)
    throw new Exception("Identity mapping did not retain native UV residuals.");
try { _=new MovableUvFrame(source[0],source[0],source[0],uv[0],uv[1],uv[2],source[0],source[1],source[2]);throw new Exception("Degenerate source accepted"); }
catch(ArgumentException) { }
try { _=new MovableUvFrame(source[0],source[1],source[2],uv[0],uv[1],uv[2],source[0],source[0],source[0]);throw new Exception("Degenerate target accepted"); }
catch(ArgumentException) { }
Console.WriteLine("PASS native UV density tensor for rigid motion, widening, narrowing, reciprocal stretch, XYZ shear and reflection; native residuals and degenerate rejection.");

// Draw01 measured main-mesh domain102 anchors304/305/306: horizontal strip,
// source U along Z and V along X. A diagonal first edge must not rotate either.
var strip=new[]{new Vector3(0,7.233154296875f,.30175018310546875f),new Vector3(-3.20867919921875f,7.233154296875f,.43000030517578125f),new Vector3(-3.20867919921875f,7.233154296875f,.30175018310546875f)};
foreach(float scale in new[]{.25f,.75f,1f,2f,4f})
{
    var changed=strip.Select(p=>new Vector3(p.x*scale,p.y,p.z)).ToArray();
    var frame=new MovableUvFrame(strip[0],strip[1],strip[2],Vector2.zero,Vector2.one,Vector2.right,changed[0],changed[1],changed[2]);
    foreach(var axis in new[]{Vector3.forward,Vector3.right,Vector3.up})
        if((frame.Rotate(axis)-axis).sqrMagnitude>1e-12f)throw new Exception("Draw01 domain102 pure X scaling rotated a source U/V/normal axis.");
}
Console.WriteLine("PASS Draw01 domain102 measured anchors: Rotate preserves Z/X/Y under pure positive X scaling.");

if(args.Length==2&&args[0]=="--flange")
{
    using var evidence=System.Text.Json.JsonDocument.Parse(File.ReadAllText(args[1]));
    var row=evidence.RootElement.GetProperty("results")[0];
    Vector3 P(System.Text.Json.JsonElement value)=>new(value[0].GetSingle(),value[1].GetSingle(),value[2].GetSingle());
    Vector2 Q(System.Text.Json.JsonElement value)=>new(value[0].GetSingle(),value[1].GetSingle());
    var vertices=row.GetProperty("vertices").EnumerateArray().ToArray();
    var original=vertices.Select(v=>P(v.GetProperty("source"))).ToArray();
    var changed=vertices.Select(v=>P(v.GetProperty("target"))).ToArray();
    var native=vertices.Select(v=>Q(v.GetProperty("sourceUv"))).ToArray();
    var measured=new MovableUvFrame(original[0],original[1],original[2],native[0],native[1],native[2],changed[0],changed[1],changed[2]);
    var actual=Enumerable.Range(0,vertices.Length).Select(i=>measured.Map(original[i],changed[i],native[i])).ToArray();
    // Native U varies solely with unchanged Y on this measured XY flange.
    // Widening X must not rotate its profile into the width direction.
    for(int i=0;i<actual.Length;i++)if(actual[i].x!=native[i].x)
        throw new Exception($"Measured Lift04 flange U drift at vertex {i}: {native[i].x:R} -> {actual[i].x:R}");
    var tangent=measured.Rotate(Vector3.up);
    if((tangent-Vector3.up).sqrMagnitude>1e-12f)throw new Exception("Pure X scaling rotated the original vertical tangent.");
    EqualMetric(original.Take(3).ToArray(),native.Take(3).ToArray(),changed.Take(3).ToArray(),actual.Take(3).ToArray());
    var bounds=row.GetProperty("bounds");var rectangle=new Vector4(bounds[0].GetSingle(),bounds[1].GetSingle(),bounds[2].GetSingle(),bounds[3].GetSingle());
    var parent=MovableParentUv.Map(original.Take(3).ToArray(),native.Take(3).ToArray(),changed.Take(3).ToArray(),original.Take(3).ToArray(),native.Take(3).ToArray(),false,rectangle);
    var parentUvs=parent.Exact.Select(q=>new Vector2((float)(rectangle.x+((double)rectangle.z-rectangle.x)*q[0]),(float)(rectangle.y+((double)rectangle.w-rectangle.y)*q[1]))).ToArray();
    for(int i=0;i<3;i++)if(parentUvs[i].x!=native[i].x)throw new Exception("Constructed parent mapping rotated the measured nonperiodic U profile.");
    EqualMetric(original.Take(3).ToArray(),native.Take(3).ToArray(),changed.Take(3).ToArray(),parentUvs);
    Console.WriteLine("PASS measured Lift04 flange fixed U profile, source orientation and full density tensor.");
    args=Array.Empty<string>();
}

var repeat=TextureWrapMode.Repeat;
float Period(params MovableTextureFootprint.Cycle[] cycles)=>MovableTextureFootprint.Period(cycles,84,64);
if(Period(new(1d/16,4096,repeat),new(1d/8,2048,repeat))!=16 ||
   Period(new(1d/16,4096,TextureWrapMode.Mirror),new(1d/8,2048,repeat))!=32 ||
   Period(new MovableTextureFootprint.Cycle(1d/16,4096,TextureWrapMode.Clamp))!=0 ||
   Period(new(1d/16,4096,repeat),new(1d/8+.00001,2048,repeat))!=0 ||
   Period(new(0,4096,TextureWrapMode.Clamp),new(1d/8,2048,repeat))!=8)
    throw new Exception("Native composite period failed clamp/mirror/common-period/drift checks.");
var footprint=new MovableTextureFootprint();
var derivatives=new[]{(new Vector2(128,0),new Vector2(0,512)),
    (new Vector2(400,100),new Vector2(300,200)),
    (new Vector2(17,42),new Vector2(91,-20))};
foreach(var pair in derivatives)footprint.Include(pair.Item1,pair.Item2);
foreach(var pair in derivatives)
{
    var x=pair.Item1/footprint.Density.x;var y=pair.Item2/footprint.Density.y;
    double a=x.sqrMagnitude,b=Vector2.Dot(x,y),d=y.sqrMagnitude;
    if((a+d+Math.Sqrt((a-d)*(a-d)+4*b*b))*.5>1.000001)
        throw new Exception("Native composite downsampled a channel or an oblique direction.");
}
Console.WriteLine("PASS composite channel footprints and common periods; clamp and accumulated phase drift reject unsafe repetition.");

foreach(var originalUv in new[]{uv,new[]{new Vector2(.2f,.3f),new Vector2(.8f,.3f),new Vector2(.4f,.3f)}})
foreach(var deform in deformations)
{
    var target=source.Select(deform).ToArray();
    var frame=new MovableUvFrame(source[0],source[1],source[2],originalUv[0],originalUv[1],originalUv[2],target[0],target[1],target[2]);
    var mapped=target.Select(p=>
    {
        var unit=frame.TriangleUnit(p);
        return originalUv[0]+unit.x*(originalUv[1]-originalUv[0])+unit.y*(originalUv[2]-originalUv[0]);
    }).ToArray();
    EqualMetric(source,originalUv,target,mapped);
    // The other half of a triangular repeating tile rotates the same source
    // triangle by 180 degrees; its complete density tensor is unchanged.
    EqualMetric(source,originalUv,target,mapped.Select(p=>-p).ToArray());
}
foreach(var points in new[]{uv,new[]{Vector2.zero,new Vector2(1,1),new Vector2(.9f,.91f)},new[]{Vector2.zero,Vector2.zero,new Vector2(.5f,0)}})
foreach(var dimensions in new[]{new Vector2Int(1024,4096),new Vector2Int(4096,512)})
{
    var size=MovableUvFrame.TrianglePixels(points[0],points[1],points[2],dimensions.x,dimensions.y);
    var u=Vector2.Scale(points[1]-points[0],new Vector2(dimensions.x,dimensions.y))/size.x;
    var v=Vector2.Scale(points[2]-points[0],new Vector2(dimensions.x,dimensions.y))/size.y;
    double a=Vector2.Dot(u,u),b=Vector2.Dot(u,v),c=Vector2.Dot(v,v);
    double maximum=(a+c+Math.Sqrt((a-c)*(a-c)+4*b*b))/2;
    if(maximum>1.000001 || size.x%4!=0 || size.y%4!=0)throw new Exception("Triangular source sampling loses diagonal resolution.");
}
Console.WriteLine("PASS triangular units preserve full/rank-one UV metrics and oblique-channel pixel footprints.");

if(args.Length!=0)
{
    using var reader=new BinaryReader(File.OpenRead(args[0]));
    if(reader.ReadInt32()!=1)throw new Exception("Unknown native frame fixture version.");
    int count=reader.ReadInt32(),failures=0;double worst=0;
    for(int row=0;row<count;row++)
    {
        int asset=reader.ReadInt32(),face=reader.ReadInt32();
        var p=Enumerable.Range(0,3).Select(_=>new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle())).ToArray();
        var q=Enumerable.Range(0,3).Select(_=>new Vector2(reader.ReadSingle(),reader.ReadSingle())).ToArray();
        foreach(float scale in new[]{.5f,2f,4f})
        {
            var target=p.Select(v=>new Vector3(v.x*scale,v.y,v.z)).ToArray();
            var frame=new MovableUvFrame(p[0],p[1],p[2],q[0],q[1],q[2],target[0],target[1],target[2]);
            var mapped=target.Select(v=>{var unit=frame.TriangleUnit(v);return q[0]+unit.x*(q[1]-q[0])+unit.y*(q[2]-q[0]);}).ToArray();
            var before=PreciseMetric(p,q);var after=PreciseMetric(target,mapped);
            double error=Math.Sqrt(before.Zip(after,(a,b)=>(a-b)*(a-b)).Sum()/before.Sum(a=>a*a));
            if(double.IsNaN(error) || error>.01)
            {
                failures++;
                if(failures<=12)Console.WriteLine($"FAIL native frame source={asset} face={face} scale={scale} error={error:R}");
            }
            worst=Math.Max(worst,error);
        }
    }
    Console.WriteLine($"Native float frames: faces={count}; transforms={count*3}; failures={failures}; max tensor error={worst:R}");
    if(failures!=0)throw new Exception("Native frame precision regressions remain.");
}

double[] PreciseMetric(Vector3[] positions,Vector2[] coordinates)
{
    var e=Enumerable.Range(0,3).Select(i=>(double)positions[1][i]-positions[0][i]).ToArray();
    var f=Enumerable.Range(0,3).Select(i=>(double)positions[2][i]-positions[0][i]).ToArray();
    double length=Math.Sqrt(e.Sum(v=>v*v));
    double x=e.Zip(f,(a,b)=>a*b).Sum()/length;
    var cross=new[]{e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]};
    double y=Math.Sqrt(cross.Sum(v=>v*v))/length;
    double ux=((double)coordinates[1].x-coordinates[0].x)/length;
    double vx=((double)coordinates[1].y-coordinates[0].y)/length;
    double uy=(((double)coordinates[2].x-coordinates[0].x)-ux*x)/y;
    double vy=(((double)coordinates[2].y-coordinates[0].y)-vx*x)/y;
    return new[]{ux*ux+uy*uy,ux*vx+uy*vy,vx*vx+vy*vy};
}
