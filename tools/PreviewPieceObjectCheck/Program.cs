using BridgeBuilder.Bridges;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Unity.Mathematics;
using Random=Unity.Mathematics.Random;
static class Program {
 static int checks;
 static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static Bezier4x3 Line(float x,float a,float b)=>new(new float3(x,30,a),new float3(x,30,math.lerp(a,b,1f/3)),new float3(x,30,math.lerp(a,b,2f/3)),new float3(x,30,b));
 static Segment Segment(float a,float b)=>new(){m_Left=Line(-6,a,b),m_Right=Line(6,a,b),m_Length=new float2(b-a)};
 static void Main(){
 var widths=new[]{2f,4f,4f,2.5f,4f,4f,2f};
 var raw=new NetCompositionPiece[widths.Length];
 var data=new Dictionary<Unity.Entities.Entity,NetPieceData>();
 for(int i=0;i<widths.Length;i++) {
  var id=new Unity.Entities.Entity{Index=i+1,Version=1};
  raw[i]=new NetCompositionPiece{m_Piece=id,m_SectionIndex=i,m_SectionFlags=NetSectionFlags.AlignCenter};
  data[id]=new NetPieceData{m_Width=widths[i],m_Length=64,m_HeightRange=new Bounds1(0,1)};
 }
 var preview=(NetCompositionPiece[])raw.Clone();var packedData=new NetCompositionData();
 NativePacking.Pack(ref packedData,preview,data);
 var initialized=(NetCompositionPiece[])raw.Clone();NativePacking.Pack(ref packedData,initialized,data);
 Check(math.abs(packedData.m_Width-22.5f)<.00001f,"Selected road full width");
 var expected=new[]{-10.25f,-7.25f,-3.25f,0,3.25f,7.25f,10.25f};
 for(int i=0;i<raw.Length;i++){
  Check(raw[i].m_Offset.x==0,"Raw offsets remain owned unchanged");
  Check(initialized[i].m_Offset.x==expected[i],"Native single packing exact offsets");
  Check(initialized[i].m_Offset.x==preview[i].m_Offset.x,"Mesh and initialized lane composition agree");
 }
 NativePacking.Pack(ref packedData,preview,data);
 Check(preview[1].m_Offset.x==-14.5f&&preview[2].m_Offset.x==-6.5f,"Reproduces captured double-pack regression");
 Check(initialized[2].m_Offset.x-initialized[1].m_Offset.x==4,"Adjacent four metre lanes remain adjacent");
 var edge=new EdgeGeometry{m_Start=Segment(0,32),m_End=Segment(32,64)};
 var comp=new NetCompositionData{m_Width=12};
 var net=new NetGeometryData{m_EdgeLengthRange=new Bounds1(38.4f,89.6f)};
 var rule=new MovableRoadObjects.Rule(new NetCompositionObject{m_Position=new float2(0),m_Rotation=quaternion.identity,m_Spacing=14,m_Probability=100,m_UseCurveRotation=new float2(1)});
 var hits=new List<MovableRoadObjectPlacement.Candidate>(); var random=new Random(1);
 void Collect(MovableRoadObjectPlacement.Candidate c,ref Random r)=>hits.Add(c);
 MovableRoadObjectPlacement.Visit(new[]{rule},edge,comp,net,default,default,default,default,ref random,Collect);
 Check(hits.Count==5,"64m native spacing14 must yield5");
 for(int i=0;i<5;i++){Check(math.abs(hits[i].Object.m_Position.z-i*12.8f)<.002f,"native equal interval");Check(math.abs(hits[i].Object.m_Position.y-30)<.001f,"elevation once");Check(math.all(math.isfinite(hits[i].Object.m_Rotation.value)),"finite rotation");}
 foreach(var domain in new[]{new[]{new float2(0,64)},new[]{new float2(64,0)},new[]{new float2(0,64),new float2(64,128)}}){
  float end=domain.Length==1?64:128;var anchors=new List<float>();
  for(float start=-64;start<=end;start+=64){
   var testEdge=new EdgeGeometry{m_Start=Segment(start,start+32),m_End=Segment(start+32,start+64)};
   var phase=new Random(1);
   void Include(MovableRoadObjectPlacement.Candidate c,ref Random r){
    if(BridgeBuilder.Runtime.BridgePreviewCourseDomain.Contains(domain,c.Object.m_Position.z))anchors.Add(c.Object.m_Position.z);
   }
   MovableRoadObjectPlacement.Visit(new[]{rule},testEdge,comp,net,default,default,default,default,ref phase,Include);
  }
  Check(anchors.Count==(domain.Length==1?6:11),"Visible spans plus next native boundary occurrence");
  Check(anchors.Distinct().Count()==anchors.Count,"Shared boundary occurrence emitted once");
  Check(anchors.Min()==0&&anchors.Max()==end,"Both presentation endpoints anchored");
  Check(anchors.Min()-10==-10&&anchors.Max()+10==end+10,"Whole symmetric support extents retained");
 }
 var even=rule.Placement;even.m_Flags|=SubObjectFlags.EvenSpacing;
 var endpoint=new EdgeNodeGeometry{m_Left=Segment(-4,0),m_Right=Segment(-4,0),m_Middle=Line(0,-4,0)};
 hits.Clear();random=new Random(1);
 MovableRoadObjectPlacement.Visit(new[]{new MovableRoadObjects.Rule(even)},edge,comp,net,default,default,endpoint,endpoint,ref random,Collect);
 Check(hits.Count==4,"native even spacing uses node extensions then count-1");
 Check(hits.All(h=>math.all(math.isfinite(h.Object.m_Position))),"even placement finite");
 var shortRule=rule.Placement;shortRule.m_MinLength=65;hits.Clear();
 MovableRoadObjectPlacement.Visit(new[]{new MovableRoadObjects.Rule(shortRule)},edge,comp,net,default,default,default,default,ref random,Collect);
 Check(hits.Count==0,"native minimum length eligibility");
 foreach(float length in new[]{109.2f,182f,254.8f,400f})
 {
  var goEdge=new EdgeGeometry{m_Start=Segment(0,length/2),m_End=Segment(length/2,length)};
  var endpoints=new[]{new MovableRoadObjects.Rule(new NetCompositionObject{m_Rotation=quaternion.identity,m_Probability=100,m_UseCurveRotation=new float2(1),m_CurveOffsetRange=new float2(0)}),
   new MovableRoadObjects.Rule(new NetCompositionObject{m_Rotation=quaternion.identity,m_Probability=100,m_UseCurveRotation=new float2(1),m_CurveOffsetRange=new float2(1)})};
  hits.Clear();random=new Random(1);MovableRoadObjectPlacement.Visit(endpoints,goEdge,comp,net,default,default,default,default,ref random,Collect);
  Check(hits.Count==2,"Native rigid half-supports occur once at each edge endpoint");
  Check(math.abs(hits[0].Object.m_Position.z)<.002f && math.abs(hits[1].Object.m_Position.z-length)<.002f,"Native support anchors follow actual trimmed edge length");
  Check(hits.All(h=>math.abs(h.Object.m_Position.y-30)<.001f),"Support elevation once");
  Game.Rendering.BatchDataHelpers.CalculateEdgeParameters(goEdge,false,out var parameters);
  var p0=(parameters.m_CompositionMatrix0.c0.xyz+parameters.m_CompositionMatrix2.c0.xyz)*.5f;
  var p1=(parameters.m_CompositionMatrix1.c3.xyz+parameters.m_CompositionMatrix3.c3.xyz)*.5f;
  p0=math.mul(parameters.m_TransformMatrix,new float4(p0,1));p1=math.mul(parameters.m_TransformMatrix,new float4(p1,1));
  Check(math.distance(p0,hits[0].Object.m_Position)<.002f && math.distance(p1,hits[1].Object.m_Position)<.002f,"Native composition matrices and rigid endpoint placement share exact joins");
 }
 Console.WriteLine($"PASS {checks} placement checks; real production enumeration; synthetic straight-course inputs; ECS course remains separate.");
 }
}
namespace BridgeBuilder.Bridges { internal static class MovableRoadObjects { internal readonly struct Rule {
 internal readonly ObjectPrefab Prefab; internal readonly NetCompositionObject Placement;
 internal Rule(NetCompositionObject p){Prefab=null!;Placement=p;}
}}}

