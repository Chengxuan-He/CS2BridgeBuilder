using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
namespace BridgeBuilder.Bridges;

// Compares only consumed immutable input values. Asset handles are identities, never traversed.
internal static class MovableAppearanceReuse
{
    internal static bool Same(MovableSurfaceAtlas.Input a,MovableSurfaceAtlas.Input b)
    {
        if(a.SelectedInput!=b.SelectedInput || a.PrototypeRoad!=b.PrototypeRoad ||
            !ReferenceEquals(a.Sidewalks,b.Sidewalks) || !ReferenceEquals(a.Native,b.Native))return false;
        if(!Value(a.Materials,b.Materials)||!Value(a.DomainNatives,b.DomainNatives)||
            !Value(a.SupportedTypes,b.SupportedTypes)||!Value(a.Patches,b.Patches)||
            !Value(a.SourceFrame,b.SourceFrame))return false;
        return ReferenceEquals(a.Faces,b.Faces)||MemoryMarshal.AsBytes(a.Faces.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(b.Faces.AsSpan()));
    }
    internal static bool SameLevel(MovableRoadAppearance.Level a,MovableRoadAppearance.Level b)=>
        ReferenceEquals(a.Native,b.Native)&&a.Error==b.Error&&Value(a.Patches,b.Patches)&&
        Value(a.SupportedTypes,b.SupportedTypes)&&MovableRoadLodCache.Tree(a.Carriageway,b.Carriageway)&&
        MovableRoadLodCache.Tree(a.Sidewalk,b.Sidewalk);
    private static bool Value(object? a,object? b)
    {
        if(ReferenceEquals(a,b))return true;
        if(a==null||b==null||a.GetType()!=b.GetType())return false;
        if(a is float x)return BitConverter.SingleToInt32Bits(x)==BitConverter.SingleToInt32Bits((float)b);
        if(a is Array aa)
        {
            var bb=(Array)b;if(aa.Length!=bb.Length)return false;
            for(int i=0;i<aa.Length;i++)if(!Value(aa.GetValue(i),bb.GetValue(i)))return false;
            return true;
        }
        var type=a.GetType();
        if(type.IsPrimitive||type.IsEnum||a is string)return a.Equals(b);
        if(!type.IsValueType)return false;
        var fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        foreach(var field in fields)if(!Value(field.GetValue(a),field.GetValue(b)))return false;
        return true;
    }
}
