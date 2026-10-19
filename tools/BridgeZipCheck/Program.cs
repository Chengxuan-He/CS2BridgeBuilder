using System.IO.Compression;
using BridgeBuilder.Runtime;
var root = Path.Combine(Path.GetTempPath(), "BridgeZipCheck-" + Guid.NewGuid().ToString("N"));
var source = Path.Combine(root, "b12345678-1234-1234-1234-123456789012");
Directory.CreateDirectory(Path.Combine(source, "Prefabs"));
Directory.CreateDirectory(Path.Combine(source, "Dependencies", "nested"));
Directory.CreateDirectory(Path.Combine(source, "Empty"));
var payload = new byte[] {0, 1, 2, 255};
File.WriteAllBytes(Path.Combine(source, "Prefabs", "Bridge.Prefab"), payload);
File.WriteAllText(Path.Combine(source, "Prefabs", "Bridge.Prefab.cid"), "unchanged CID");
File.WriteAllBytes(Path.Combine(source, "Dependencies", "nested", "asset.Texture"), payload);
var folder = Path.GetFileName(source);
var target = Path.Combine(root, "exports", "桥梁.zip");
try {
 BridgeZipExport.Write(source, target, folder);
 using (var zip = ZipFile.OpenRead(target)) {
  var expected = new[] {"Dependencies/nested/asset.Texture", "Prefabs/Bridge.Prefab", "Prefabs/Bridge.Prefab.cid"};
  if (!zip.Entries.Select(e => e.FullName).Order().SequenceEqual(expected.Select(name => folder + "/" + name))) throw new Exception("Incorrect ZIP root/entries");
  foreach (var entry in zip.Entries) {
   using var data = new MemoryStream(); using var input = entry.Open(); input.CopyTo(data);
   if (!data.ToArray().SequenceEqual(File.ReadAllBytes(Path.Combine(source,entry.FullName.Substring(folder.Length + 1))))) throw new Exception("Payload changed");
  }
 }
 void Fails(Action action) { try {action();} catch (IOException) {return;} throw new Exception("Expected failure"); }
 var custom = Path.Combine(root,"custom.zip");
 BridgeZipExport.Write(source, custom, "自定义桥梁");
 using (var zip = ZipFile.OpenRead(custom))
  if (zip.Entries.Any(e => !e.FullName.StartsWith("自定义桥梁/"))) throw new Exception("Custom folder not applied");
 foreach (var invalid in new[] {"", " ", ".", "..", "../escape", "a/b", "a\\b", "C:drive", "bad.", "bad ", "CON", "nul.txt"})
  Fails(() => BridgeZipExport.Write(source,Path.Combine(root,"invalid.zip"),invalid));
 if (File.Exists(Path.Combine(root,"invalid.zip"))) throw new Exception("Invalid folder created archive");
 var saved = File.ReadAllBytes(target);
 Fails(() => BridgeZipExport.Write(source,target,folder));
 if (!saved.SequenceEqual(File.ReadAllBytes(target))) throw new Exception("Replaced existing destination");
 Fails(() => BridgeZipExport.Write(source,Path.Combine(source,"bad.zip"),folder));
 Fails(() => BridgeZipExport.Write(source,Path.Combine(root,"bad.txt"),folder));
 using (var locked = new FileStream(Path.Combine(source,"Prefabs","Bridge.Prefab"),FileMode.Open,FileAccess.Read,FileShare.None))
  Fails(() => BridgeZipExport.Write(source,Path.Combine(root,"failed.zip"),folder));
 if (File.Exists(Path.Combine(root,"failed.zip")) || Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories).Length != 0) throw new Exception("Partial export remained");
 Console.WriteLine("PASS: default UUID and custom ZIP roots, invalid folder rejection, nested dependencies, byte-identical assets/CIDs, empty directories omitted, collision protection, invalid paths and locked-file cleanup.");
} finally { Directory.Delete(root,true); }
