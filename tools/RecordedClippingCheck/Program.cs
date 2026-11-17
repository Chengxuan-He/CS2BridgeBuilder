using BridgeBuilder.Bridges;
using UnityEngine.Rendering;
using Unity.Mathematics;
int assertions=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);assertions++;}
var corners=new[]{0,1,2, 0,1,2, 0,1,2};var weights=new[]{1f,0,0, .5f,.5f,0, .25f,.25f,.5f};
byte[] Floats(params float[] v)=>v.SelectMany(BitConverter.GetBytes).ToArray();
var uv=RecordedVertexChannels.InterpolateScalars(VertexAttributeFormat.Float32,2,Floats(0,0, 8,0, 0,4),corners,weights);
var expected=new[]{0f,0,4,0,2,2};
for(int i=0;i<expected.Length;i++)Check(BitConverter.ToSingle(uv,i*4)==expected[i],"UV barycentric affine correspondence");
var color=RecordedVertexChannels.InterpolateScalars(VertexAttributeFormat.UNorm8,4,new byte[]{0,20,40,255, 100,60,80,255, 200,100,120,255},corners,weights);
Check(color.Skip(4).Take(4).SequenceEqual(new byte[]{50,40,60,255}),"authored normalized color interpolation");
Check(color.Skip(8).Take(4).SequenceEqual(new byte[]{125,70,90,255}),"color third triangle corner weight");
var half=new[]{0f,0,8,0,0,4}.SelectMany(x=>BitConverter.GetBytes((ushort)math.f32tof16(x))).ToArray();
var halfOut=RecordedVertexChannels.InterpolateScalars(VertexAttributeFormat.Float16,2,half,corners,weights);
for(int i=0;i<expected.Length;i++)Check(math.f16tof32(BitConverter.ToUInt16(halfOut,i*2))==expected[i],"half UV values");
var raw=new byte[]{0,0,128,128, 17,31,65,255, 4,5,6,7};
var exact=RecordedVertexChannels.InterpolateScalars(VertexAttributeFormat.UInt32,1,raw,new[]{0,1,2},new[]{0f,1,0});
Check(exact.SequenceEqual(raw.Skip(4).Take(4)),"untouched packed attribute exact bits");
var triangles=new[]{0,1,2, 2,1,3, 4,5,6, 6,5,7};var faces=new[]{0,0,2,3};
var slot0=new List<int>();var slot1=new List<int>();
RecordedVertexChannels.AppendTriangles(slot0,triangles,faces,0,2);RecordedVertexChannels.AppendTriangles(slot1,triangles,faces,2,4);
Check(slot0.SequenceEqual(triangles.Take(6)),"split source face retains first material slot");
Check(slot1.SequenceEqual(triangles.Skip(6)),"remaining source faces retain second material slot");
Check(slot0.Count+slot1.Count==triangles.Length,"every explicit output face retained once");
var tangent=RecordedVertexChannels.NormalizeTangent(new float4(2,2,0,-.5f));
Check(math.abs(math.length(tangent.xyz)-1)<1e-6f && tangent.w==-1f,"interpolated tangent unit direction and negative handedness");
Check(RecordedVertexChannels.NormalizeTangent(new float4(1,0,0,0)).w==1f,"zero crossing produces valid positive tangent handedness");
var atlasUv=new[]{new UnityEngine.Vector2(.25f,.99951171875f),new UnityEngine.Vector2(.5f,0),new UnityEngine.Vector2(.75f,.9990234375f),new UnityEngine.Vector2(.125f,-.25f),new UnityEngine.Vector2(.875f,2.25f)};
foreach(var format in new[]{VertexAttributeFormat.Float16,VertexAttributeFormat.Float32})
{
    int width=format==VertexAttributeFormat.Float16?2:4;
    var channels=new byte[atlasUv.Length*4*width]; Array.Fill(channels,(byte)0x5a);
    var unchanged=(byte[])channels.Clone();
    RecordedVertexChannels.WriteUv0(channels,format,4,atlasUv);
    for(int i=0;i<atlasUv.Length;i++)
    {
        float v=width==2?math.f16tof32(BitConverter.ToUInt16(channels,(i*4+1)*width)):BitConverter.ToSingle(channels,(i*4+1)*width);
        Check(v==atlasUv[i].y,"explicit atlas UV value including negative/repeat preserved");
        Check(channels.Skip((i*4+2)*width).Take(2*width).SequenceEqual(unchanged.Skip((i*4+2)*width).Take(2*width)),"UV zw remains byte-identical");
    }
}
float NativeAtlasV(float v){float packed=math.f16tof32(math.f32tof16(v-1));return packed>=0?packed:packed-math.floor(packed);}
Check(NativeAtlasV(1)==0,"native shader original atlas upper edge wraps to zero");
Check(NativeAtlasV(.99951171875f)==.99951171875f,"half predecessor retains top atlas row");
Check(math.abs((NativeAtlasV(.95f)+NativeAtlasV(.99951171875f))*.5f-(.95f+1)*.5f)<.001f,"triangle interpolation stays inside top atlas region");
Check(math.abs((NativeAtlasV(.95f)+NativeAtlasV(1))*.5f-(.95f+1)*.5f)>.49f,"old triangle sweeps unrelated atlas region");
Console.WriteLine($"PASS {assertions} production clipping channel/provenance assertions; native packed frame decoding requires game runtime.");
