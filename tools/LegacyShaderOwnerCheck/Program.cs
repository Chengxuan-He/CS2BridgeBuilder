using System.Text;
using BridgeBuilder.Runtime;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
static byte[] Surface(int template,float? order=null,int? queue=null){
 using var data=new MemoryStream();using var w=new BinaryWriter(data,Encoding.UTF8,true);
 w.Write((ushort)1);w.Write(template);w.Write(false);
 w.Write(order.HasValue?(byte)255:(byte)0);if(order.HasValue){w.Write(1);w.Write("_DrawOrder");w.Write(order.Value);}
 w.Write(queue.HasValue?(byte)255:(byte)0);if(queue.HasValue){w.Write(1);w.Write(BridgeNativeMaterialProgram.QueueProperty);w.Write(queue.Value);}
 return data.ToArray();
}
foreach(var v in new[]{1,2})Check(BridgeLegacyShaderOwners.ProgramEvidence(Encoding.UTF8.GetBytes($"BridgeBuilder.MaterialProgram/{v}\r\n17\r\nQkJTMQ=="),".txt"),"Published envelope version");
Check(!BridgeLegacyShaderOwners.ProgramEvidence(Encoding.UTF8.GetBytes("BridgeBuilder.MaterialProgram/20\n"),".txt"),"Unknown header preserved");
Check(BridgeLegacyShaderOwners.ProgramEvidence(Encoding.ASCII.GetBytes("UnityFS\0rest"),".bundle"),"Development graph marker");
Check(!BridgeLegacyShaderOwners.ProgramEvidence(Encoding.ASCII.GetBytes("BBS1rest"),".bundle"),"Unrelated bundle content");
Check(BridgeLegacyShaderOwners.SurfaceEvidence(Surface(BridgeLegacyShaderOwners.HistoricalTemplate(BridgeLegacyShaderOwners.PublishedWorld,17,null,true))),"Missing published world program");
foreach(var priority in new[]{-1999,0,7,12345,-4567})foreach(var published in new[]{true,false}){
 var digest=published?BridgeLegacyShaderOwners.PublishedDecal:BridgeLegacyShaderOwners.DevelopmentDecal;
 int template=BridgeLegacyShaderOwners.HistoricalTemplate(digest,18,2000+priority,published);
 Check(BridgeLegacyShaderOwners.SurfaceEvidence(Surface(template,priority)),"Exact old decal queue");
 Check(!BridgeLegacyShaderOwners.SurfaceEvidence(Surface(template,priority+1)),"Different queue not guessed");
 Check(!BridgeLegacyShaderOwners.SurfaceEvidence(Surface(template)),"Missing priority not guessed");
}
foreach(var priority in new[]{16777216f,float.NaN,float.PositiveInfinity,1.5f})Check(!BridgeLegacyShaderOwners.SurfaceEvidence(Surface(0x40001234,priority)),"Imprecise/nonintegral priority preserved");
foreach(var template in new[]{0,17,18,33,0x40001234,0x7fffffff})Check(!BridgeLegacyShaderOwners.SurfaceEvidence(Surface(template,0)),"Native/unknown templates preserved");
foreach(var queue in new[]{int.MinValue,-999,0,2000,2225,int.MaxValue})foreach(var family in new[]{17,18})
 Check(!BridgeLegacyShaderOwners.SurfaceEvidence(Surface(BridgeNativeMaterialProgram.TemplateId(family,queue),0,queue)),"Current stable template preserved");
Check(!BridgeLegacyShaderOwners.SurfaceEvidence(new byte[]{1,0,0}),"Truncated surface not old shader evidence");
BridgeFileAccess.Files["root/owner/Appearance/x.Surface"]=Surface(BridgeLegacyShaderOwners.HistoricalTemplate(BridgeLegacyShaderOwners.PublishedWorld,17,null,true));
Check(BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Owner-specific Surface evidence");
Check(!BridgeLegacyShaderOwners.Identify("other",BridgeFileAccess.Files.Keys,out _),"Foreign owner not selected");
BridgeFileAccess.Files.Clear();BridgeFileAccess.Files["root/owner/Dependencies/x.Surface"]=Surface(BridgeLegacyShaderOwners.HistoricalTemplate(BridgeLegacyShaderOwners.PublishedWorld,17,null,true));
Check(!BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Borrowed dependency template not owner evidence");
BridgeFileAccess.Files.Clear();BridgeFileAccess.Files["root/owner/MaterialPrograms/program.txt"]=Encoding.UTF8.GetBytes("BridgeBuilder.MaterialProgram/2\n18\n");
Check(BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Owned program selects owner");
BridgeFileAccess.Files.Clear();BridgeFileAccess.Files["root/owner/MaterialPrograms/Program 18 "+BridgeLegacyShaderOwners.PublishedDecal+".txt.cid"]=Array.Empty<byte>();
Check(BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Exact published orphan program filename");
BridgeFileAccess.Files.Clear();BridgeFileAccess.Files["root/owner/MaterialPrograms/unrelated.txt.cid"]=Array.Empty<byte>();
Check(!BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Unrelated orphan sidecar retained");
BridgeFileAccess.Files.Clear();var graph=Encoding.ASCII.GetBytes("UnityFS\0native graph fixture");
var graphHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(graph));
BridgeFileAccess.Files["root/owner/MaterialPrograms/Program "+graphHash+".bundle"]=graph;
Check(BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Exact development graph identity filename");
BridgeFileAccess.Files.Clear();BridgeFileAccess.Files["root/owner/MaterialPrograms/unrelated.bundle"]=graph;
Check(!BridgeLegacyShaderOwners.Identify("owner",BridgeFileAccess.Files.Keys,out _),"Unrelated UnityFS file not legacy graph evidence");
Console.WriteLine("PASS legacy program classification, exact Surface IDs/queues, missing metadata, current IDs, foreign owners and unrelated templates");
namespace BridgeBuilder.Runtime {
 internal static class BridgeFileAccess {internal static Dictionary<string,byte[]> Files=new();internal static byte[] ReadBytes(string p)=>Files[p];}
 internal static class BridgeAssetInfo {internal static bool MatchesOwner(string p,string o)=>p.Contains("/"+o+"/");}
 internal static class BridgeInspectionPaths {internal static bool IsDependencyPath(string p,string o)=>p.Contains("/Dependencies/");}
}
