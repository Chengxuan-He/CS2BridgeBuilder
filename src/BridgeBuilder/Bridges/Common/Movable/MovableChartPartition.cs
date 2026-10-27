using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Partition existing receiver faces, never cut them. Bounds remain in the same
// physical projection frame; only the destination image/material changes.
internal static class MovableChartPartition
{
    internal readonly struct Bounds
    {
        internal readonly float MinX, MinY, MaxX, MaxY;
        internal Bounds(float minX, float minY, float maxX, float maxY)
        { MinX=minX; MinY=minY; MaxX=maxX; MaxY=maxY; }
        internal Bounds Union(Bounds other) => new(Math.Min(MinX,other.MinX),Math.Min(MinY,other.MinY),
            Math.Max(MaxX,other.MaxX),Math.Max(MaxY,other.MaxY));
        internal bool Fits(float dx,float dy,int capacity) =>
            MaxX>=MinX && MaxY>=MinY &&
            (MaxX-MinX)*dx<=capacity && (MaxY-MinY)*dy<=capacity;
        internal double Area => (double)(MaxX-MinX)*(MaxY-MinY);
    }

    internal sealed class Group
    {
        internal Bounds Extent;
        internal readonly List<int> Faces=new();
    }

    internal static List<Group> Build(IReadOnlyList<Bounds> faces,float dx,float dy,int capacity)
    {
        if(!float.IsFinite(dx) || !float.IsFinite(dy) || dx<=0 || dy<=0 || capacity<1)
            throw new ArgumentOutOfRangeException(nameof(capacity),"Invalid native chart density or image capacity.");
        var groups=new List<Group>();
        foreach(int id in Enumerable.Range(0,faces.Count).OrderBy(i=>faces[i].MinY).ThenBy(i=>faces[i].MinX))
        {
            var face=faces[id];
            if(!face.Fits(dx,dy,capacity))
                throw new InvalidOperationException($"Native receiver face {id} exceeds texture capacity at source density; shrinking or cutting is not permitted.");
            Group? selected=null;
            double cost=double.PositiveInfinity;
            foreach(var group in groups)
            {
                var combined=group.Extent.Union(face);
                if(!combined.Fits(dx,dy,capacity))continue;
                double growth=combined.Area-group.Extent.Area;
                if(growth>=cost)continue;
                selected=group; cost=growth;
            }
            if(selected==null) {selected=new Group {Extent=face};groups.Add(selected);}
            else selected.Extent=selected.Extent.Union(face);
            selected.Faces.Add(id);
        }
        return groups;
    }
}
