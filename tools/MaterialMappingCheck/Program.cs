using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using G = BridgeBuilder.Bridges.MovableRoadGeometry;
using Kind = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

// Regression: preparing a non-object receiver used to erase its evaluated native
// UV and color before the material path could choose original-source retention.
G.Vertex Vertex(float x) => new(new Vector3(x, 2, 3), Vector3.up,
    new Vector4(1, 0, 0, -1), new Vector4(x, -.25f, 7, 9), new Color(.1f, .2f, .3f, .4f));
var faces = new[]
{
    new G.BoundTriangle(new G.Triangle(Vertex(1), Vertex(2), Vertex(3), 0, 13, 27), 4),
    new G.BoundTriangle(new G.Triangle(Vertex(-2), Vertex(0), Vertex(4), 1, 14, 29), 5)
};
foreach (var flags in new[] { new[] { true, false }, new[] { false, true }, new[] { true, true } })
{
    var output = MovableRoadTemplate.Prepare(faces, flags);
    if (output.Length != faces.Length) throw new Exception("Receiver count changed.");
    for (var i = 0; i < output.Length; i++)
        if (!output[i].Equals(faces[i].Geometry))
            throw new Exception("Preparing the receiver erased source attributes or provenance.");
}

// Distinct finite far-LOD projection and domain support are carried together.
var near = new SurfaceAsset(); var far = new SurfaceAsset();
var nearPatch = new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one,
    new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0));
var farPatch = new MovableExistingRoadSurface.Patch(new Vector2(.2f, .3f), new Vector2(.4f, .5f),
    new Vector4(.01f, 0, 0, .2f), new Vector4(0, 0, .001f, .3f), far, sourceUv: true);
var partition = new G.Partition(0);
var levels = new[]
{
    new MovableRoadAppearance.Level(near, new[] { nearPatch }, new[] { Kind.Asphalt }),
    new MovableRoadAppearance.Level(far, new[] { farPatch }, new[] { Kind.Pathway }, partition, partition)
};
if (!ReferenceEquals(levels[1].Native, far) || !levels[1].Patches[0].SourceUv ||
    levels[1].SupportedTypes[0] != Kind.Pathway || !ReferenceEquals(levels[1].Sidewalk, partition) ||
    levels[0].Patches[0].SourceUv)
    throw new Exception("Per-LOD appearance state was conflated.");
try
{
    _ = new MovableRoadAppearance.Level(near, new[] { nearPatch }, Array.Empty<Kind>());
    throw new Exception("Mismatched support domains were accepted.");
}
catch (ArgumentException) { }
Console.WriteLine("PASS material mapping: receiver UV/color/provenance retained for both conversion paths; distinct LOD source, mapping, support and partition retained.");

var ballast = MovableRoadLayerIdentity.Worldspace("a3522ead5e61e02569f0884fb4607eec", true,
    "18a5b9c996ce7c551bc422cd96a324ad", "346c601cafab351569d7d0a4fcac777c", "646901d94c691865ba8a1a647f57f7c3", "");
if (ballast != Kind.Ballast) throw new Exception("Source gravel identity became a destination-specific geometry role.");
var tram=MovableRoadLayerIdentity.Worldspace("1d7dad9f64c9fe55a809f1080c39fb92",true,
    "b00176216b0ce36579bce6fc675cfb77","9f446790360787756b0e5909db12e200","375b871b66aaf675dacef141eb2a9810","");
Kind Tram(float maxU)=>MovableRoadLayerIdentity.WholeReceiver("1d7dad9f64c9fe55a809f1080c39fb92",true,
    "9a6506aaede935752b5c4c0362c95f6e","a142c18b42577de52815d1afd054660b","37ceea0e8c2bd24599e237236e6ae338",tram,0,maxU);
if(tram!=Kind.Asphalt || Tram(.249755859375f)!=Kind.Asphalt || Tram(.5f)!=Kind.None)
    throw new Exception("Tram receiver source type was guessed from lane role or extended beyond its observed footprint.");
Kind WholePath(bool builtin=true,float maxU=.5f,string normal="1e09bb5978cb13953988206f636cc6f5") =>
    MovableRoadLayerIdentity.WholeReceiver("16be9bdc3ead50651a57c398efa6ed2e",builtin,
        "ecdec3f86d6792856b92044582832fb9",normal,"1c50af8d69f824751a0e29d32c2ea92c",Kind.Pathway,0,maxU);
if (WholePath()!=Kind.Pathway || WholePath(false)!=Kind.None || WholePath(maxU:.6f)!=Kind.None ||
    WholePath(normal:"unknown")!=Kind.None)
    throw new Exception("Complete appearance identity or native footprint gate failed.");
if (MovableRoadLayerIdentity.WholeReceiver("a3522ead5e61e02569f0884fb4607eec",true,
    "6bc6397af4c4779558d46a66beaf6f64","42cf28936b3ef6d5f8bfa78a0eba2d86","fe820b176007ab650be5168cae1b46a6",
    ballast,0,1)!=Kind.Ballast) throw new Exception("Verified complete ballast receiver was treated as unsupported base content.");
var unit = new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new Vector4(1,0,0,0),
    new Vector4(0,0,.1f,.25f),far,preparedUnit:true,repeatU:true);
if (!unit.PreparedUnit || !unit.RepeatU || unit.RepeatV) throw new Exception("Offline unit axes lost.");
var preparedVertex=Vertex(8);
var preparedMapped=MovableRoadProjection.Prepared(preparedVertex,unit);
if(preparedMapped.Position!=preparedVertex.Position || preparedMapped.Normal!=preparedVertex.Normal ||
   preparedMapped.Color!=preparedVertex.Color || preparedMapped.Tangent!=preparedVertex.Tangent ||
   preparedMapped.UV!=new Vector4(8,.55f,7,9))
    throw new Exception("Prepared mapping lost source attributes or wrapped continuous U.");
var preparedProjected=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,
    new(1,0,0,0),new(0,0,.1f,.25f),far,preparedUnit:true,repeatU:true,projectTangent:true);
var projectedUnit=MovableRoadProjection.Prepared(preparedVertex,preparedProjected);
if(projectedUnit.Tangent!=new Vector4(1,0,0,-1) || projectedUnit.Normal!=preparedVertex.Normal)
    throw new Exception("Prepared projected tangent failed.");
try {
    _=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new(1,0,0,0),new(0,0,1,0),far,
       preparedUnit:true,repeatU:true,projectTangent:true,tangent:Vector4.one);
    throw new Exception("Conflicting prepared tangent modes accepted.");
} catch(ArgumentException) { }
try {
    _=MovableRoadProjection.Prepared(new G.Vertex(new Vector3(0,0,20),Vector3.up,Vector4.one,Vector4.zero,Color.white),unit);
    throw new Exception("Finite prepared axis escaped its guard.");
} catch(InvalidOperationException) { }
foreach(float bad in new[]{float.NaN,float.PositiveInfinity})
{
    try {
        var invalid=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new(bad,0,0,0),new(0,0,0,.5f),far,preparedUnit:true,repeatU:true);
        _=MovableRoadProjection.Prepared(preparedVertex,invalid);
        throw new Exception("Non-finite repeated coordinate accepted.");
    } catch(InvalidOperationException) { }
    try {
        var invalid=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new(1,0,0,0),new(0,0,0,.5f),far,preparedUnit:true,repeatU:true,tangent:new(bad,0,0,1));
        _=MovableRoadProjection.Prepared(preparedVertex,invalid);
        throw new Exception("Non-finite explicit frame accepted.");
    } catch(InvalidOperationException) { }
}
try {
    var guarded=new MovableExistingRoadSurface.Patch(new(0,.4f),new(1,.6f),new(1,0,0,0),new(0,0,0,.7f),far,preparedUnit:true,repeatU:true);
    _=MovableRoadProjection.Prepared(preparedVertex,guarded);
    throw new Exception("Finite coordinate inside texture but outside measured content accepted.");
} catch(InvalidOperationException) { }
Console.WriteLine("PASS prepared projection: continuous repeated UV, guarded finite axis, raw receiver attributes and explicit tangent modes.");
Console.WriteLine("PASS source appearance: exact six-channel identity and Pathway footprint; Ballast is independent of bridge role; original B=254 residual belongs to the same supported whole appearance; offline unit axes retained.");
if(args.Length>=1)
{
    using var evidence=System.Text.Json.JsonDocument.Parse(File.ReadAllText(args[0]));
    var root=evidence.RootElement;
    var ids=root.GetProperty("textureCids");
    string Id(string property)=>ids.GetProperty(property).GetString()!;
    var type=MovableRoadLayerIdentity.Worldspace(root.GetProperty("surface").GetString()!,true,
        Id("_WorldspaceAlbedo"),Id("_WorldspaceNormalMap"),Id("_WorldspaceMaskMap"),"");
    var full=MovableRoadLayerIdentity.WholeReceiver(root.GetProperty("surface").GetString()!,true,
        Id("_BaseColorMap"),Id("_NormalMap"),Id("_MaskMap"),type,0,1);
    if(type!=Kind.Ballast || full!=Kind.Ballast ||
        root.GetProperty("mips")[0].GetProperty("blueHistogram").GetProperty("254").GetInt32()!=1269)
        throw new Exception("Real BC7 B=254 source evidence did not select complete supported Ballast.");
    Console.WriteLine("PASS measured six-channel Subway source with 1269 B=254 texels: complete Ballast dispatch, independent of pure-worldspace threshold.");
}
// The approved road path cuts only original-atlas wrap planes; attributes and provenance survive.
G.Vertex WrapVertex(float x,float z) => new(new Vector3(x,0,z),Vector3.up,
    new Vector4(-1,0,0,-1),new Vector4(0,0,7,9),new Color(x*.01f+.5f,z*.02f+.5f,.3f,.7f));
foreach(bool uRepeat in new[]{false,true}) foreach(bool vRepeat in new[]{false,true})
{
    if(!uRepeat&&!vRepeat)continue;
    foreach(float width in new[]{.25f,1f,2.5f,7f}) foreach(float phase in new[]{-.25f,0f,.5f})
    {
        var triangle=new G.Triangle(WrapVertex(0,0),WrapVertex(width,0),WrapVertex(0,width),3,17,29);
        var patch=new MovableExistingRoadSurface.Patch(new(.25f,.125f),new(.5f,.625f),
            new(.25f,0,0,.25f+phase*.25f),new(0,0,.5f,.125f+phase*.5f),near,
            sourceUv:true,repeatU:uRepeat,repeatV:vRepeat,sourceAtlasRepeat:true);
        var output=MovableRoadAtlasWrap.Apply(triangle,patch);
        double area=0;
        foreach(var t in output)
        {
            if(t.Material!=3||t.Occurrence!=17||t.SourceTriangle!=29)throw new Exception("Wrap lost provenance.");
            var cross=Vector3.Cross(t.B.Position-t.A.Position,t.C.Position-t.A.Position);
            if(cross.y>=0)throw new Exception("Wrap changed source winding.");
            area-=cross.y*.5;
            foreach(var v in new[]{t.A,t.B,t.C})
            {
                if(v.Normal!=Vector3.up||v.Tangent!=new Vector4(-1,0,0,-1)||v.UV.z!=7||v.UV.w!=9||
                    Math.Abs(v.Color.r-(v.Position.x*.01f+.5f))>1e-6||Math.Abs(v.Color.g-(v.Position.z*.02f+.5f))>1e-6||v.Color.a!=.7f)
                    throw new Exception("Wrap changed original interpolated attributes.");
                if(uRepeat&&(v.UV.x<.25f||v.UV.x>.5f)||vRepeat&&(v.UV.y<.125f||v.UV.y>.625f))
                    throw new Exception("Wrap escaped source region.");
                if(!uRepeat&&Math.Abs(v.UV.x-(.25f+phase*.25f+v.Position.x*.25f))>1e-6||
                   !vRepeat&&Math.Abs(v.UV.y-(.125f+phase*.5f+v.Position.z*.5f))>1e-6)
                    throw new Exception("Wrap changed finite-axis projection.");
            }
        }
        if(Math.Abs(area-width*width*.5)>1e-4)throw new Exception("Wrap lost physical coverage.");
        if(width==.25f&&phase==0&&output.Length!=1)throw new Exception("Wrap added an unnecessary cut.");
    }
}
Console.WriteLine("PASS original-atlas wrap: 36 phase/axis/span cases, coverage, winding, finite axes and source attributes/provenance.");
var seamPatch=new MovableExistingRoadSurface.Patch(new(.25f,.125f),new(.5f,.625f),
    new(.25f,0,0,.25f),new(0,0,.5f,.125f),near,sourceUv:true,repeatU:true,
    tangent:new Vector4(1,0,0,1),sourceAtlasRepeat:true);
var left=MovableRoadAtlasWrap.Apply(new G.Triangle(WrapVertex(0,0),WrapVertex(3,0),WrapVertex(0,1),0,1,1),seamPatch);
var right=MovableRoadAtlasWrap.Apply(new G.Triangle(WrapVertex(3,1),WrapVertex(0,1),WrapVertex(3,0),0,1,2),seamPatch);
foreach(float x in new[]{1f,2f})
{
    var a=left.SelectMany(t=>new[]{t.A,t.B,t.C}).Where(v=>v.Position.x==x&&v.Position.z!=0).ToArray();
    var b=right.SelectMany(t=>new[]{t.A,t.B,t.C}).Where(v=>v.Position.x==x&&v.Position.z!=1).ToArray();
    if(a.Length==0||b.Length==0||a.Any(v=>!b.Any(w=>w.Position.Equals(v.Position))))
        throw new Exception("Opposite shared-edge directions produced different seam positions.");
    if(!a.Any(v=>v.UV.x==.25f)||!a.Any(v=>v.UV.x==.5f)||a.Any(v=>v.Tangent!=new Vector4(1,0,0,1)))
        throw new Exception("Seam twins or evidenced tangent frame were lost.");
}
Console.WriteLine("PASS atlas seams: bit-equal reversed shared edges, dual UV endpoints and explicit original tangent frame.");
var tilted=new G.Vertex(new(0,0,0),new Vector3(0,1,1).normalized,new(0,1,0,1),new(0,0,7,9),Color.white);
var projectedPatch=new MovableExistingRoadSurface.Patch(new(.25f,.125f),new(.5f,.625f),
    new(.25f,0,0,.25f),new(0,0,.5f,.125f),near,sourceUv:true,repeatU:true,sourceAtlasRepeat:true,projectTangent:true);
var projected=MovableRoadAtlasWrap.Apply(new G.Triangle(tilted,WrapVertex(1,0),WrapVertex(0,1),0,1,1),projectedPatch);
var origin=projected.SelectMany(t=>new[]{t.A,t.B,t.C}).First(v=>v.Position==Vector3.zero);
if(origin.Normal!=tilted.Normal||origin.Tangent!=new Vector4(1,0,0,-1))throw new Exception("Explicit projected tangent changed receiver normal or frame.");
try {
 _=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new(1,0,0,0),new(0,0,1,0),near,
     sourceUv:true,repeatU:true,sourceAtlasRepeat:true,projectTangent:true,tangent:Vector4.one);
 throw new Exception("Conflicting tangent modes were accepted.");
} catch(ArgumentException) { }
Console.WriteLine("PASS opt-in projected tangent: actual receiver normal retained; conflicting constant override rejected.");
var finiteProjected=new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one,new(1,0,0,0),new(0,0,1,0),near,
    sourceUv:true,projectTangent:true);
if(finiteProjected.SourceAtlasRepeat || !finiteProjected.ProjectTangent ||
   MovableRoadAtlasWrap.ProjectedTangent(tilted.Normal,finiteProjected.U,finiteProjected.V)!=new Vector4(1,0,0,-1))
    throw new Exception("Finite source-atlas projection mode is unavailable or differs from wrap mode.");
Console.WriteLine("PASS finite SourceUv explicit projected tangent shares the tested wrap formula.");
// A native Float32 dot-product boundary interval is not a geometric area threshold.
var roundP=new Vector4(.25f,0,0,.25f);
var roundNear=MovableRoadAtlasWrap.Coordinate(roundP,new Vector3(1.0000001f,0,0),.25f,.5f,true);
var roundFar=MovableRoadAtlasWrap.Coordinate(roundP,new Vector3(1.00001f,0,0),.25f,.5f,true);
if(roundNear!=1||!(roundFar>1))throw new Exception("Float32 wrap boundary interval lost its genuine cross-seam negative control.");
var narrow=new G.Triangle(WrapVertex(.99999f,0),WrapVertex(1.00001f,0),WrapVertex(.99999f,1),0,1,1);
var retainedNarrow=MovableRoadAtlasWrap.Apply(narrow,seamPatch);
if(retainedNarrow.Length<2)throw new Exception("A genuinely narrow cross-seam face was removed by a geometric epsilon.");
Console.WriteLine("PASS Float32 wrap-boundary interval: representational boundary canonicalized, genuine narrow crossing retained.");
if(args.Length>1)
{
 using var repro=System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(args[1]));var data=repro.RootElement;
 Vector3 Read3(System.Text.Json.JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle(),e.GetProperty("z").GetSingle());
 Vector4 Read4(System.Text.Json.JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle(),e.GetProperty("z").GetSingle(),e.GetProperty("w").GetSingle());
 Vector2 Read2(System.Text.Json.JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle());
 G.Vertex ReadVertex(System.Text.Json.JsonElement e) {var c=e.GetProperty("color");return new(Read3(e.GetProperty("position")),Read3(e.GetProperty("normal")),Read4(e.GetProperty("tangent")),Read4(e.GetProperty("uv")),new(c.GetProperty("r").GetSingle(),c.GetProperty("g").GetSingle(),c.GetProperty("b").GetSingle(),c.GetProperty("a").GetSingle()));}
 var vertices=data.GetProperty("triangle").EnumerateArray().Select(ReadVertex).ToArray();
 var input=new G.Triangle(vertices[0],vertices[1],vertices[2],0,13,101);
 var patch=new MovableExistingRoadSurface.Patch(Read2(data.GetProperty("min")),Read2(data.GetProperty("max")),
   Read4(data.GetProperty("u")),Read4(data.GetProperty("v")),near,sourceUv:true,
   repeatU:data.GetProperty("repeatU").GetBoolean(),repeatV:data.GetProperty("repeatV").GetBoolean(),
   tangent:Read4(data.GetProperty("tangent")),sourceAtlasRepeat:true);
 var output=MovableRoadAtlasWrap.Apply(input,patch);
 if(output.Length!=1)throw new Exception($"Real Lift04 identity boundary still creates {output.Length} fragments.");
 var actual=new[]{output[0].A,output[0].B,output[0].C};
 for(int i=0;i<3;i++)if(!actual[i].Position.Equals(vertices[i].Position)||!actual[i].Color.Equals(vertices[i].Color)||actual[i].UV.z!=7||actual[i].UV.w!=9)
   throw new Exception("Lift04 boundary repair changed original geometry or untouched attributes.");
 Console.WriteLine("PASS real Lift04 candidate05 domain97: one unchanged physical triangle, no 1.7347e-18 wrap slivers, original colors/UVzw retained.");
}
var seamFrame=Matrix4x4.identity;seamFrame[2,0]=1;seamFrame[2,2]=0;seamFrame[2,3]=-1;
if(G.SeamDistance(seamFrame,new Vector3(1.0000001f,0,0))!=0 || G.SeamDistance(seamFrame,new Vector3(1.00001f,0,0))<=0 || G.SeamDistance(seamFrame,new Vector3(.99999f,0,0))>=0)
 throw new Exception("Physical partition rounding interval removed a real side or failed a coplanar point.");
Console.WriteLine("PASS physical partition plane: Float32 cancellation interval, true positive/negative side controls.");

var gapPlane=Matrix4x4.identity;gapPlane[2,3]=80;
var thinCell=new[]{WrapVertex(11.99997615814209f,-80),WrapVertex(12,-80),WrapVertex(11.999977111816406f,-80.00001525878906f)};
var thinSides=G.SeamDistances(gapPlane,thinCell);
if(!thinSides.Any(d=>d<0)||thinSides.Any(d=>d>0))throw new Exception("A genuine narrow wrap cell became coplanar with its gap boundary.");
Console.WriteLine("PASS actual Lift04 wide-wrap child: fully uncertain thin polygon retains its genuine negative side; no area epsilon.");
if(args.Length>2)AtlasWrapReproChecks.Run(args[2],near);


// Content windows are not the whole offline-authored finite guard domain.
var contentOnly=new MovableExistingRoadSurface.Patch(new(.25f,0),new(.75f,1),
    new(0,0,0,.2499992f),new(0,0,1,0),far,preparedUnit:true,repeatV:true);
var declaredGuard=contentOnly.WithFiniteSampling(new(.125f,0),new(.875f,1));
var guardPoint=Vertex(0);
var inGuard=MovableRoadProjection.Prepared(guardPoint,declaredGuard);
if(inGuard.UV.x!=.2499992f||inGuard.Color!=guardPoint.Color||inGuard.UV.z!=guardPoint.UV.z)
    throw new Exception("Explicit finite guard altered the original coordinate or attributes.");
try{MovableRoadProjection.Prepared(guardPoint,contentOnly);throw new Exception("Undeclared guard accepted.");}
catch(InvalidOperationException){}
var outsideGuard=new MovableExistingRoadSurface.Patch(new(.25f,0),new(.75f,1),
    new(0,0,0,.1f),new(0,0,1,0),far,preparedUnit:true,repeatV:true).WithFiniteSampling(new(.125f,0),new(.875f,1));
try{MovableRoadProjection.Prepared(guardPoint,outsideGuard);throw new Exception("Outside authored sampling domain accepted.");}
catch(InvalidOperationException){}
foreach(var invalidBounds in new[]{(new Vector2(-.1f,0),Vector2.one),(new Vector2(.3f,0),Vector2.one),(Vector2.zero,new Vector2(float.NaN,1))})
{
    try{contentOnly.WithFiniteSampling(invalidBounds.Item1,invalidBounds.Item2);throw new Exception("Invalid guard declaration accepted.");}
    catch(ArgumentException){}
}
var witnessFile=Path.Combine("artifacts","material-remap","Lift05","road-continuous-candidate","results","finite-guard-complete-inputs.json");
if(File.Exists(witnessFile))
{
    using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(witnessFile));int count=0;
    foreach(var row in document.RootElement.GetProperty("witnesses").EnumerateArray())
    {
        Vector4 Q(string key){var q=row.GetProperty(key);return new(q.GetProperty("x").GetSingle(),q.GetProperty("y").GetSingle(),q.GetProperty("z").GetSingle(),q.GetProperty("w").GetSingle());}
        Vector2 B(string key){var q=row.GetProperty(key);return new(q.GetProperty("x").GetSingle(),q.GetProperty("y").GetSingle());}
        var position=row.GetProperty("position");var point=new G.Vertex(new(position.GetProperty("x").GetSingle(),position.GetProperty("y").GetSingle(),position.GetProperty("z").GetSingle()),Vector3.up,Vector4.one,new(0,0,7,9),Color.gray);
        var patch=new MovableExistingRoadSurface.Patch(B("min"),B("max"),Q("U"),Q("V"),far,preparedUnit:true,
            repeatU:row.GetProperty("repeatU").GetBoolean(),repeatV:row.GetProperty("repeatV").GetBoolean()).WithFiniteSampling(Vector2.zero,Vector2.one);
        var actual=MovableRoadProjection.Prepared(point,patch);var expected=new Vector4(point.Position.x,point.Position.y,point.Position.z,1);
        if(actual.UV.x!=Vector4.Dot(expected,patch.U)||actual.UV.y!=Vector4.Dot(expected,patch.V)||actual.UV.z!=7||actual.UV.w!=9)
            throw new Exception("Actual finite guard witness was clamped or changed.");
        count++;
    }
    Console.WriteLine($"PASS explicit offline finite sampling domain: {count} actual Lift05 witnesses retain exact evaluated UV; undeclared/outside domain and invalid-bound negative controls.");
}

try{new MovableExistingRoadSurface.Patch(Vector2.zero,Vector2.one).WithFiniteSampling(Vector2.zero,Vector2.one);throw new Exception("Non-prepared material declared offline guards.");}
catch(ArgumentException){}
try{new MovableExistingRoadSurface.Patch(new(float.NaN,0),Vector2.one,Vector4.one,Vector4.one,far,preparedUnit:true).WithFiniteSampling(Vector2.zero,Vector2.one);throw new Exception("Non-finite content window accepted.");}
catch(ArgumentException){}
Console.WriteLine("PASS offline guard declarations reject non-prepared surfaces and non-finite content windows.");
