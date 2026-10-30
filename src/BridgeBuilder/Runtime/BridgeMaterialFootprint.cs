using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BridgeBuilder.Runtime;

// Observes the actual serialized save graph. No prepared-directory estimates or
// additional source reads; physical owner files are checked only after save succeeds.
internal sealed class BridgeMaterialFootprint
{
    private sealed class Node
    {
        internal bool Builtin,Owned;
        internal string Extension="",Path="",NativeFileName="";
        internal string[] References=Array.Empty<string>();
    }
    private readonly Dictionary<string,Node> _nodes=new(StringComparer.OrdinalIgnoreCase);
    internal void Observe(string cid,BridgeDependencyCopies.Source source)
        =>_nodes.Add(cid,new Node{Builtin=source.Builtin,Owned=source.Owned,
            Extension=source.Extension,Path=source.PhysicalPath,NativeFileName=source.NativeFileName,
            References=source.Builtin?Array.Empty<string>():BridgeDependencyCopies.References(source).ToArray()});

    internal string Write(string gameRoot,string owner,string generatorBuild)
    {
        var roots=_nodes.Where(n=>IsSurface(n.Value.Extension)).Select(n=>n.Key).ToArray();
        var pending=new Stack<string>(roots);var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var builtins=new List<string>();
        var issues=new List<string>();var rows=new List<string>();long bytes=0;
        while(pending.Count!=0)
        {
            string cid=pending.Pop();if(!visited.Add(cid))continue;
            if(!_nodes.TryGetValue(cid,out var node)){issues.Add("Unobserved dependency: "+cid);continue;}
            if(node.Builtin){builtins.Add(cid+"\t"+node.Extension);continue;}
            string path=node.Owned?node.Path:BridgeDependencyCopies.Destination(gameRoot,owner,cid,
                new BridgeDependencyCopies.Source{Extension=node.Extension,NativeFileName=node.NativeFileName});
            if(string.IsNullOrEmpty(path)){issues.Add("Missing owned physical path: "+cid);continue;}
            path=Path.GetFullPath(path);
            if(!BridgeAssetInfo.MatchesOwner(path,owner)){issues.Add("Material outside current owner: "+cid);continue;}
            try
            {
                if(!string.Equals(BridgeFileAccess.ReadText(path+".cid").Trim(),cid,StringComparison.OrdinalIgnoreCase))
                {issues.Add("Owner sidecar mismatch: "+cid);continue;}
                foreach(string file in new[]{path,path+".cid"})if(files.Add(file))
                {
                    using var stream=BridgeFileAccess.OpenRead(file);bytes=checked(bytes+stream.Length);
                    rows.Add(cid+"\t"+stream.Length+"\t"+Clean(file));
                }
            }
            catch(IOException e){issues.Add(cid+": "+Clean(e.Message));}
            // Native external VT cache dependencies need explicit graph edges.
            // Do not certify a terminal unparsed VT payload as a complete closure.
            if((node.Extension.Equals(".VTTexture",StringComparison.OrdinalIgnoreCase)||
                node.Extension.Equals(".VTSurface",StringComparison.OrdinalIgnoreCase))&&node.References.Length==0)
                issues.Add("External VT graph not expanded: "+cid);
            foreach(string dependency in node.References)pending.Push(dependency);
        }
        if(roots.Length==0)issues.Add("No serialized material roots observed.");
        var text=new StringBuilder();
        text.AppendLine("Schema\tBridgeBuilder/SavedOwnerMaterialFootprint/1");
        text.AppendLine("Owner\t"+owner);text.AppendLine("UTC\t"+DateTime.UtcNow.ToString("O"));
        text.AppendLine("GeneratorBuild\t"+generatorBuild);
        text.AppendLine("Stage\tDependency save succeeded; native publication/gameplay acceptance separate");
        text.AppendLine("CompleteMaterialGraph\t"+(issues.Count==0));
        text.AppendLine("MaterialFiles\t"+files.Count);text.AppendLine("MaterialBytes\t"+bytes);
        text.AppendLine("MaterialLimitBytes\t10000000");
        text.AppendLine("BudgetStatus\t"+(issues.Count!=0?"UNVERIFIED":bytes<=10000000?"PASS":"OVER_BUDGET"));
        text.AppendLine("BuiltinReferences\t"+builtins.Count);
        text.AppendLine("ObservedSerializedAssets\t"+_nodes.Count);
        text.AppendLine("MaterialRootCIDs\t"+string.Join(",",roots));
        text.AppendLine("Files\tCID\tBytes\tPath");foreach(string row in rows)text.AppendLine("File\t"+row);
        foreach(string row in builtins)text.AppendLine("Builtin\t"+row);
        foreach(string issue in issues)text.AppendLine("Issue\t"+Clean(issue));
        string folder=Path.Combine(gameRoot,"ModsData","BridgeBuilder","material-footprints");
        Directory.CreateDirectory(folder);string report=Path.Combine(folder,owner+".tsv");
        File.WriteAllText(report,text.ToString(),new UTF8Encoding(false));return report;
    }
    private static bool IsSurface(string extension)=>extension.Equals(".Surface",StringComparison.OrdinalIgnoreCase)||extension.Equals(".Material",StringComparison.OrdinalIgnoreCase);
    private static string Clean(string value)=>value.Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
}
