using BridgeBuilder.Bridges;
using System.Text.Json;
using UnityEngine;
using Patch=BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

if(args.Length<1 || args.Length>2)throw new ArgumentException("Pass the Draw03 repeat inventory path and optional diagnostic report path.");
using var json=JsonDocument.Parse(File.ReadAllText(args[0]));
int keys=0,samples=0,reducedFaces=0,projectedSamples=0,rejectedCrossings=0;double worst=0,projectedWorst=0;
var knownKeys=json.RootElement.GetProperty("units").EnumerateArray().Select(u=>u.GetProperty("key").GetString()!).ToHashSet();
var sourceDigest=json.RootElement.GetProperty("units")[0].GetProperty("sourceDigest").GetString()!;
// Geometry validation runs through NativeEntry in Unity: determinant is a native call.
foreach(var unit in json.RootElement.GetProperty("units").EnumerateArray())
{
    Vector2 Pair(string name){var v=unit.GetProperty(name);return new Vector2(v[0].GetSingle(),v[1].GetSingle());}
    var key=MovableRepeatKey.For(unit.GetProperty("sourceDigest").GetString()!,Pair("min"),Pair("max"),
        unit.GetProperty("cutout").GetBoolean(),null,false);
    if(key!=unit.GetProperty("key").GetString())throw new InvalidDataException("Offline key differs from production: "+key);
    keys++;
}
var f=Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32;
var laneMin=new Vector2(f[1]/f[2],f[3]/f[4]);var laneMax=new Vector2(f[5]/f[6],f[7]/f[8]);
float gapHeight=f[9]/f[10];float gapMin=laneMin.y-gapHeight*f[11];
var cases=new[]{
    (new[]{-7.5f,-4.5f,-1.5f,1.5f,4.5f,7.5f},new Vector2(-9,9)),
    (new[]{-7.5f,-4.5f,-1.5f,5.5f,8.5f,11.5f},new Vector2(-9,13)),
    (new[]{-4f,-.5f,3.1f},new Vector2(-8,7)),
    (new[]{-1.5f,1.5f},new Vector2(-25,25))};
foreach(var (centers,span) in cases)
{
    var patches=Enumerable.Range(0,4).Select(_=>new Patch(laneMin,laneMax,new Vector4(1,0,0,0),new Vector4(0,0,1,0),repeat:true)).ToArray();
    var output=Draw03RoadWear.Apply(patches,centers,Matrix4x4.identity,span);
    var bands=output[0].Bands ?? throw new Exception("Production wear bands absent.");
    if(output[2].Bands!=null || output[3].Bands!=null)throw new Exception("Sidewalk was assigned wear bands.");
    float previous=0;
    foreach(var band in bands)
    {
        if(band.Start!=previous || band.End<=band.Start)throw new Exception("Noncontiguous production bands.");
        previous=band.End;
        bool gap=band.Min.y==gapMin;
        float lo=gap?gapMin:laneMin.y,hi=gap?gapMin+gapHeight:laneMax.y;
        float scale=(band.Max.y-band.Min.y)/(hi-lo),offset=(band.Min.y-lo)/(hi-lo);
        for(int i=1;i<258;i++)
        {
            float q=i/258f;
            float old=band.Min.y+Frac(q*band.RepeatY)*(band.Max.y-band.Min.y);
            float projected=gap?q*band.RepeatY:offset+q*scale;
            float atomic=lo+Frac(projected)*(hi-lo);
            worst=Math.Max(worst,Math.Abs((double)old-atomic)*4096);samples++;
        }
    }
    if(previous!=1)throw new Exception("Production bands do not span the unit.");
    foreach(var shift in new[]{-1f,0f,1f})
    {
        // A general three-axis projection exercises affine conversion independently
        // of the selected road's alignment and world origin.
        var original=output[0];
        var projection=new Vector4(.02f,.04f,-.03f,-.7f);
        var patch=new Patch(original.Min,original.Max,original.U,projection,
            original.Surface,true,bands,true);
        Vector3 Point(float v,float y,float z)=>new((v-projection.w-y*projection.y-z*projection.z)/projection.x,y,z);
        foreach(var band in bands)
        {
            float extent=band.End-band.Start;
            var a=Point(shift+band.Start+extent*.2f,1,2);
            var b=Point(shift+band.Start+extent*.4f,3,1);
            var c=Point(shift+band.Start+extent*.8f,2,4);
            if(!Draw03RoadWearUnits.TryReduce(patch,a,b,c,out var reduced))throw new Exception("Single-band face was not reduced.");
            if(reduced.Bands!=null || reduced.BandRows || !reduced.Repeat || !reduced.Project || reduced.U!=patch.U || !ReferenceEquals(reduced.Surface,patch.Surface))
                throw new Exception("Reduction changed unrelated patch properties.");
            foreach(bool cutout in new[]{false,true})
                if(!knownKeys.Contains(MovableRepeatKey.For(sourceDigest,reduced.Min,reduced.Max,cutout,reduced.Bands,reduced.BandRows)))
                    throw new Exception("Reduced face does not resolve an existing prepared unit.");
            reducedFaces++;
            for(int ia=0;ia<=8;ia++)for(int ib=0;ib<=8-ia;ib++)
            {
                var position=a*(ia/8f)+b*(ib/8f)+c*((8-ia-ib)/8f);
                float global=Vector3.Dot(position,patch.V)+patch.V.w;
                float local=(Frac(global)-band.Start)/extent;
                float expected=band.Min.y+Frac(local*band.RepeatY)*(band.Max.y-band.Min.y);
                float actual=reduced.Min.y+Frac(Vector3.Dot(position,reduced.V)+reduced.V.w)*(reduced.Max.y-reduced.Min.y);
                projectedWorst=Math.Max(projectedWorst,Math.Abs((double)expected-actual)*4096);projectedSamples++;
            }
        }
        var left=bands[0];var right=bands[1];
        var leftPoint=Point(shift+(left.Start+left.End)*.5f,1,2);
        var rightPoint=Point(shift+(right.Start+right.End)*.5f,3,1);
        if(Draw03RoadWearUnits.TryReduce(patch,leftPoint,rightPoint,rightPoint,out _))throw new Exception("Cross-band face was reduced.");
        rejectedCrossings++;
        if(Draw03RoadWearUnits.TryReduce(patch,Point(shift+.9f,0,0),Point(shift+1.1f,0,0),Point(shift+1.2f,0,0),out _))
            throw new Exception("Cross-period face was reduced.");
        rejectedCrossings++;
    }
}
if(worst>0.001)throw new Exception("Atomic UV conversion changes source samples: "+worst);
if(projectedWorst>0.01)throw new Exception("Affine face reduction changes source samples: "+projectedWorst);
var untouched=new[]{new Patch(laneMin,laneMax)};
if(!ReferenceEquals(untouched,Draw03RoadWear.Apply(untouched,Array.Empty<float>(),Matrix4x4.identity,default)))throw new Exception("No-lane behavior changed.");
Console.WriteLine($"PASS Draw03 production wear: {keys} offline keys match production; {samples} float32 samples; worst source pixel difference {worst:R}; original sidewalk and no-lane paths retained.");
Console.WriteLine($"PASS Draw03 triangle reduction: {reducedFaces} single-band triangles; {projectedSamples} barycentric samples; {rejectedCrossings} cross-band/period triangles retained; worst source pixel difference {projectedWorst:R}; no topology mutation.");
var growth=new List<object>();
foreach(float halfSpan in new[]{10f,20f,40f})
{
    var patches=Enumerable.Range(0,4).Select(_=>new Patch(laneMin,laneMax,new Vector4(1,0,0,0),new Vector4(0,0,1,0),repeat:true)).ToArray();
    var patch=Draw03RoadWear.Apply(patches,new[]{-1.5f,1.5f},Matrix4x4.identity,new Vector2(-halfSpan,halfSpan))[0];
    // Mirror the current common repeat-page dimension formula, without baking.
    var bands=patch.Bands!;
    int width=4*(int)MathF.Ceiling(bands.Max(b=>(b.Max.x-b.Min.x)*4096)/4);
    int height=4*(int)MathF.Ceiling(bands.Max(b=>(b.Max.y-b.Min.y)*b.RepeatY*4096/(b.End-b.Start))/4);
    string dynamicKey=MovableRepeatKey.For(sourceDigest,patch.Min,patch.Max,false,bands,patch.BandRows);
    bool prepared=knownKeys.Contains(dynamicKey);
    growth.Add(new {roadSpanMetres=halfSpan*2,laneCenters=new[]{-1.5f,1.5f},width,height,bandCount=bands.Length,
        gapRepetitions=bands.Where(b=>b.Min.y==gapMin).Select(b=>b.RepeatY).ToArray(),
        dynamicKey,preparedLookupMatch=prepared});
    Console.WriteLine($"REFERENCE unpartitioned band page: fixed 2 lanes, span={halfSpan*2}m -> {width}x{height}; prepared={prepared}.");
}
if(args.Length==2)
{
    var parent=Path.GetDirectoryName(Path.GetFullPath(args[1]))!;Directory.CreateDirectory(parent);
    File.WriteAllText(args[1],JsonSerializer.Serialize(new {scope="Production-linked Draw03 arithmetic and source key checks; no generated mesh or visual acceptance",keys,samples,worstSourcePixelError=worst,
        reducedFaces,projectedSamples,rejectedCrossings,projectedWorstSourcePixelError=projectedWorst,
        unpartitionedBandWidthGrowth=growth},new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);
}
static float Frac(float x)=>x-MathF.Floor(x);
