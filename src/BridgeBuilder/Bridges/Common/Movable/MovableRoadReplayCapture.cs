using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Explicit opt-in diagnostics only. Never requests construction or writes player assets.
internal sealed class MovableRoadReplayCapture
{
    private readonly string _folder;
    private long _bytes;
    private const long Limit=512L*1024*1024;
    private MovableRoadReplayCapture(string folder){_folder=folder;Directory.CreateDirectory(folder);}
    internal static MovableRoadReplayCapture? Begin(string style,string graph,bool preview)
    {
        if(preview)return null;
        var root=Path.Combine(Application.persistentDataPath,"ModsData","BridgeBuilder","RoadReplay");
        var marker=Path.Combine(root,"capture.once");
        try
        {
            if(!File.Exists(marker)||File.ReadAllText(marker).Trim()!=style)return null;
            var claimed=Path.Combine(root,"capture."+Guid.NewGuid().ToString("N")+".claimed");
            File.Move(marker,claimed);File.Delete(claimed);
            var capture=new MovableRoadReplayCapture(Path.Combine(root,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")));
            File.WriteAllText(Path.Combine(capture._folder,"request.txt"),"style="+style+"\ngraph="+graph+"\nassembly="+typeof(MovableRoadReplayCapture).Assembly.Location+"\n");
            Mod.Log.Info("Road replay capture armed for this request: "+capture._folder);
            return capture;
        }
        catch(Exception e){Mod.Log.Warn("Road replay capture unavailable: "+e.Message);return null;}
    }
    internal void Write(int lod,string role,MovableRoadLodCache.Key key,
        IReadOnlyList<MovableRoadGeometry.BoundTriangle> expected)
    {
        try
        {
            long bytes=(long)key.Triangles.Count*Marshal.SizeOf<MovableRoadGeometry.Triangle>();
            if(bytes+_bytes>Limit){Mod.Log.Warn("Road replay capture exceeded its 512 MiB request limit; incomplete diagnostic only.");return;}
            _bytes+=bytes;
            string path=Path.Combine(_folder,role+"-lod"+lod+".bin");
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(0x42525231);writer.Write(Marshal.SizeOf<MovableRoadGeometry.Triangle>());
                writer.Write(key.Triangles.Count);
                var raw=key.Triangles as MovableRoadGeometry.Triangle[]??key.Triangles.ToArray();
                writer.Write(MemoryMarshal.AsBytes(raw.AsSpan()).ToArray());
                writer.Write(key.Pose.position.x);writer.Write(key.Pose.position.y);writer.Write(key.Pose.position.z);
                writer.Write(key.Pose.rotation.x);writer.Write(key.Pose.rotation.y);writer.Write(key.Pose.rotation.z);writer.Write(key.Pose.rotation.w);
                writer.Write(key.Skeleton.m_Bones.Length);writer.Write(key.Surfaces.Length);
                writer.Write(key.Mappings.Length);
                foreach(var m in key.Mappings){writer.Write(m.Planar);writer.Write(m.Scale);}
                var nodes=new List<MovableRoadGeometry.Partition>();var ids=new Dictionary<MovableRoadGeometry.Partition,int>();
                int Id(MovableRoadGeometry.Partition p)
                {if(ids.TryGetValue(p,out int existing))return existing;int n=nodes.Count;nodes.Add(p);ids.Add(p,n);if(p.Negative!=null)Id(p.Negative);if(p.Positive!=null)Id(p.Positive);return n;}
                int root=Id(key.Partition);
                if(key.Occurrences!=null)foreach(var p in key.Occurrences)Id(p);
                if(key.Faces!=null)foreach(var p in key.Faces.Values)Id(p);
                writer.Write(nodes.Count);writer.Write(root);
                foreach(var p in nodes)
                {writer.Write(p.Bone);writer.Write(p.IsGap);writer.Write(p.Negative==null?-1:ids[p.Negative]);writer.Write(p.Positive==null?-1:ids[p.Positive]);for(int k=0;k<16;k++)writer.Write(p.ObjectToSeam[k]);}
                writer.Write(key.Occurrences?.Count??-1);
                if(key.Occurrences!=null)foreach(var p in key.Occurrences)writer.Write(ids[p]);
                writer.Write(key.Faces?.Count??-1);
                if(key.Faces!=null)foreach(var p in key.Faces.OrderBy(x=>x.Key.Occurrence).ThenBy(x=>x.Key.SourceTriangle))
                {writer.Write(p.Key.Occurrence);writer.Write(p.Key.SourceTriangle);writer.Write(ids[p.Value]);}
                writer.Write(expected.Count);
                var bound=expected as MovableRoadGeometry.BoundTriangle[]??expected.ToArray();
                using(var sha=SHA256.Create())writer.Write(sha.ComputeHash(MemoryMarshal.AsBytes(bound.AsSpan()).ToArray()));
            }
            Mod.Log.Info("Road replay captured: "+path+"; input="+key.Triangles.Count+"; output="+expected.Count);
        }
        catch(Exception e){Mod.Log.Warn("Road replay capture failed (generation unchanged): "+e.Message);}
    }
}
