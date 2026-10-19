using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Share bounded receiver images only when source phase and every paint footprint match.
// Native persisted VT uses Clamp; source repetition is evaluated by the GPU baker.
internal static class MovableAppearanceUnit
{
    internal static string Key(Vector3 origin,Vector3 u,Vector3 v,Vector2 extent,
        MovableExistingRoadSurface.Patch patch,IReadOnlyList<MovableNativeDecals.Region> paint)
    {
        var values=new List<int>();
        void Add(float value)=>values.Add(Mathf.RoundToInt(value*10000));
        void Vector(Vector3 value){Add(value.x);Add(value.y);Add(value.z);}
        var normal=Vector3.Cross(u,v).normalized;
        Add(extent.x);Add(extent.y);Vector(u);Vector(v);
        Add(patch.Min.x);Add(patch.Min.y);Add(patch.Max.x);Add(patch.Max.y);
        Vector(patch.U);Vector(patch.V);
        Add(Mathf.Repeat(Vector3.Dot(origin,(Vector3)patch.U)+patch.U.w,1));
        Add(Mathf.Repeat(Vector3.Dot(origin,(Vector3)patch.V)+patch.V.w,1));
        var strokes=new List<string>();
        if(Vector3.Dot(normal,Vector3.up)>=.5f)
        foreach(var stroke in paint)
        {
            var d=stroke.Center-origin;
            float Radius(Vector3 axis)=>Mathf.Abs(Vector3.Dot(stroke.Right,axis))*stroke.Half.x+
                Mathf.Abs(Vector3.Dot(stroke.Up,axis))*stroke.Half.y+Mathf.Abs(Vector3.Dot(stroke.Forward,axis))*stroke.Half.z;
            float x=Vector3.Dot(d,u),y=Vector3.Dot(d,v),z=Vector3.Dot(d,normal);
            if(x+Radius(u)<=0 || x-Radius(u)>=extent.x || y+Radius(v)<=0 || y-Radius(v)>=extent.y || Mathf.Abs(z)>Radius(normal))continue;
            int start=values.Count;
            Add(x);Add(y);Add(z);
            foreach(var axis in new[]{stroke.Right,stroke.Up,stroke.Forward})
            {Add(Vector3.Dot(axis,u));Add(Vector3.Dot(axis,v));Add(Vector3.Dot(axis,normal));}
            Vector(stroke.Half);Add(stroke.Yellow?1:0);
            strokes.Add(string.Join(",",values.Skip(start)));
            values.RemoveRange(start,values.Count-start);
        }
        strokes.Sort(StringComparer.Ordinal);
        return string.Join(",",values)+"|"+string.Join(";",strokes);
    }

}
