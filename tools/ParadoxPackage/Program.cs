using System.Reflection;
using System.Runtime.Loader;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

if(args.Length!=3)throw new ArgumentException("Usage: ParadoxPackage <official publisher directory> <postprocessed content directory> <new archive.zip>");
var sdkDirectory = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => {
    var path=Path.Combine(sdkDirectory,name.Name+".dll");
    return File.Exists(path)?AssemblyLoadContext.Default.LoadFromAssemblyPath(path):null;
};
var sdk=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(sdkDirectory,"PDX.SDK.dll"));
var contextType=sdk.GetType("PDX.SDK.Context",true)!;
var ctor=contextType.GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
var platform=Enum.Parse(ctor.GetParameters()[0].ParameterType,"Windows");
// Constructor only registers services. Do not call Create/Init, login or publication.
var context=ctor.Invoke(new object?[]{platform,"cities_skylines_2",null});
var zipperType=sdk.GetType("PDX.SDK.Internal.Util.IO.ZippingService",true)!;
var zipper=Activator.CreateInstance(zipperType,new[]{context})!;
var root=Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);
var zip=Path.GetFullPath(args[2]);
if(File.Exists(zip))throw new IOException("Archive already exists: "+zip);
var files=Directory.GetFiles(root,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
var task=(Task)zipperType.GetMethod("CreateZip")!.Invoke(zipper,new object?[]{zip,root,files,null})!;
await task;
var result=task.GetType().GetProperty("Result")!.GetValue(task)!;
var resultInterface=result.GetType().GetInterfaces().Single(t=>t.FullName=="PDX.SDK.Contracts.Results.IResult");
if(!(bool)resultInterface.GetProperty("Success")!.GetValue(result)!)throw new Exception("SDK packaging failed");
using var archive=ZipFile.OpenRead(zip);
foreach(var entry in archive.Entries) {
    var path=Path.Combine(root,entry.FullName);
    using var input=entry.Open(); using var original=File.OpenRead(path);
    using var sha=SHA256.Create();
    if(!sha.ComputeHash(input).SequenceEqual(sha.ComputeHash(original)))throw new Exception("Archive mismatch: "+entry.FullName);
}
Console.WriteLine(JsonSerializer.Serialize(new { archive=zip, bytes=new FileInfo(zip).Length,
    sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(zip))),
    files=archive.Entries.Count, sdkSHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sdk.Location))),
    runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    entries=archive.Entries.Select(e=>new{e.FullName,e.Length,e.CompressedLength}) },new JsonSerializerOptions{WriteIndented=true}));
