using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Exact source render/mesh keys refer to offline measured UV domains.
internal sealed class MovableUvDomains
{
    internal readonly struct Domain
    {
        internal readonly int A, B, C;
        private readonly int[] _data;
        private readonly int _start, _count;
        internal ReadOnlySpan<int> Faces => new(_data,_start,_count);
        internal readonly Vector4 Bounds;
        internal readonly bool Triangle;
        internal Domain(int a, int b, int c, int[] data, int start, int count, Vector4 bounds, bool triangle)
        { A=a; B=b; C=c; _data=data; _start=start; _count=count; Bounds=bounds; Triangle=triangle; }
    }
    internal readonly Domain[] Items;
    private static readonly Dictionary<string, MovableUvDomains?> Loaded = new();
    private MovableUvDomains(HardcodedNumbers data)
    {
        if(data.I32.Length>=4 && data.I32[0]==-3)
        {
            // Exact measured columns; decode only this demanded render/mesh.
            var values=data.I32;
            int n=values[1],faceCount=values[2],boundsCount=values[3];
            if(n<0 || faceCount<0 || boundsCount<0 || 4L+4L*n>values.Length || 4L*boundsCount!=data.F32.Length)
                throw new InvalidOperationException("Invalid measured UV recipe header.");
            int rectangular=0;long totalFaces=0;
            for(int i=0;i<n;i++)
            {
                int count=values[4+3*n+i];
                if(count==0 || count==int.MinValue)throw new InvalidOperationException("Invalid measured UV face count.");
                if(count>0)rectangular++;
                totalFaces+=Math.Abs(count);
            }
            if(totalFaces!=faceCount || 4L+4L*n+rectangular+faceCount!=values.Length)
                throw new InvalidOperationException("Invalid measured UV recipe lengths.");
            var faces=new int[faceCount];int faceStart=4+4*n+rectangular,previous=0;
            for(int i=0;i<faceCount;i++)faces[i]=previous=checked(previous+values[faceStart+i]);
            Items=new Domain[n];int anchor=0,offset=0,boundAt=4+4*n;
            for(int i=0;i<n;i++)
            {
                anchor=checked(anchor+values[4+i]);
                int b=checked(anchor+values[4+n+i]),c=checked(anchor+values[4+2*n+i]),count=values[4+3*n+i];
                bool triangle=count<0;count=Math.Abs(count);Vector4 bounds=default;
                if(!triangle)
                {
                    int index=values[boundAt++];
                    if((uint)index>=(uint)boundsCount)throw new InvalidOperationException("Invalid measured UV bounds reference.");
                    index*=4;bounds=new Vector4(data.F32[index],data.F32[index+1],data.F32[index+2],data.F32[index+3]);
                }
                Items[i]=new Domain(anchor,b,c,faces,offset,count,bounds,triangle);offset+=count;
            }
            return;
        }
        if(data.I32[0]!=-2)throw new InvalidOperationException("Unsupported measured UV domain schema.");
        Items = new Domain[data.I32[1]];
        int at = 2, coordinate=0;
        for (int i=0; i<Items.Length; i++)
        {
            int a=data.I32[at++], b=data.I32[at++], c=data.I32[at++], count=data.I32[at++];
            bool triangle=count<0;count=Math.Abs(count);
            var bounds=triangle?default:new Vector4(data.F32[coordinate++],data.F32[coordinate++],data.F32[coordinate++],data.F32[coordinate++]);
            Items[i]=new Domain(a,b,c,data.I32,at,count,bounds,triangle);at+=count;
        }
        if(at!=data.I32.Length || data.F32.Length!=coordinate)
            throw new InvalidOperationException("Invalid embedded structural UV domain lengths.");
    }
    internal static MovableUvDomains? For(string render, int mesh)
    {
        string key="UvDomain_"+render.Replace(' ','_')+"_"+mesh;
        if(!Loaded.TryGetValue(key,out var value))
        {
            var data=MovableUvCatalogue.Load(key);
            value=data==null?null:new MovableUvDomains(data);
            Loaded.Add(key,value);
        }
        return value;
    }
}
