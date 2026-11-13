using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Runtime;

// Prepared once before owner checks, shared for the inspection. Never persisted or used by creation.
internal sealed class BridgeInspectionCaches
{
    internal sealed class Entry
    {
        internal string Cid="", Path="";
        internal AssetData? Asset;
    }
    private readonly Func<IEnumerable<MidMipCacheAsset>> _registered;
    private readonly Func<IEnumerable<string>> _paths;
    private readonly Dictionary<string,List<Entry>> _members=new(StringComparer.OrdinalIgnoreCase);
    private bool _built;
    private string? _error;
    internal int Traversals { get; private set; }
    internal int Headers { get; private set; }
    internal BridgeInspectionCaches(Func<IEnumerable<MidMipCacheAsset>> registered,Func<IEnumerable<string>> paths)
    { _registered=registered;_paths=paths; }

    internal bool Prepare(out string? error)
    { if(!_built)Build();error=_error;return error==null; }

    internal bool TryFind(string surface,string owner,out Entry[] entries,out string? error)
    {
        entries=Array.Empty<Entry>();error=null;
        if(_error!=null){error=_error;return false;}
        if(_members.TryGetValue(surface,out var found))
        {
            entries=found.GroupBy(x=>x.Cid,StringComparer.OrdinalIgnoreCase)
                .Select(g=>g.OrderByDescending(x=>BridgeAssetInfo.MatchesOwner(x.Path,owner)).First()).ToArray();
            return true;
        }
        error=_error;return error==null;
    }

    private void Build()
    {
        _built=true;Traversals++;
        var readPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Read(Entry entry,Func<Stream> open)
        {
            try
            {
                using var stream=open();Headers++;
                if(!BridgeMidMipReferences.TryRead(stream,out var surfaces,out var failure))
                { _error??="Native cache membership unavailable: "+entry.Cid+"; "+failure;return; }
                foreach(string surface in surfaces)
                {
                    if(!_members.TryGetValue(surface,out var list))_members.Add(surface,list=new List<Entry>());
                    list.Add(entry);
                }
            }
            catch(Exception exception){_error??="Native cache inspection: "+entry.Cid+"; "+exception.Message;}
        }
        try
        {
            // One typed native-cache enumeration, not an all-asset index.
            foreach(var asset in _registered())
            {
                var meta=asset.GetMeta();string path=meta.packaged?"":meta.path;
                if(!string.IsNullOrEmpty(path))
                {
                    if(!BridgeFileAccess.Exists(path))continue; // Stale registration, not a usable native source.
                    if(!readPaths.Add(Path.GetFullPath(BridgeFileAccess.Logical(path))))continue;
                }
                Read(new Entry{Cid=asset.id.guid.ToString(),Asset=asset,Path=path},asset.GetReadStream);
            }
        }
        catch(Exception exception){_error??="Native cache registrations unavailable: "+exception.Message;}
        // Reuse the existing inspection path inventory; no extra directory enumeration.
        foreach(string path in _paths().Where(p=>p.EndsWith(".MidMips",StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if(!readPaths.Add(Path.GetFullPath(BridgeFileAccess.Logical(path))) || !BridgeFileAccess.Exists(path))continue;
                if(!BridgeFileAccess.Exists(path+".cid"))
                {
                    // A registered original can repair the canonical cache-CID snapshot sidecar.
                    string parent=Path.GetFileName(Path.GetDirectoryName(path));
                    if(_members.Values.SelectMany(x=>x).Any(x=>x.Cid.Equals(parent,StringComparison.OrdinalIgnoreCase)))continue;
                    _error??="Native cache sidecar unavailable: "+path;continue;
                }
                string cid=BridgeFileAccess.ReadText(path+".cid").Trim();
                if(!System.Text.RegularExpressions.Regex.IsMatch(cid,@"\A[a-fA-F0-9]{32}\z"))
                {_error??="Invalid native cache sidecar: "+path;continue;}
                Read(new Entry{Cid=cid,Path=path},()=>BridgeFileAccess.OpenRead(path));
            }
            catch(Exception exception){_error??="Owner cache inspection: "+path+"; "+exception.Message;}
        }
    }
}
