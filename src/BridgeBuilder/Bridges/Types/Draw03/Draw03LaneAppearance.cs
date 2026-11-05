using System;
using System.Collections.Generic;
using UnityEngine;
using BridgeBuilder.Runtime;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Actual lane appearance boundaries. Texture U stays continuous across the span.
internal static class Draw03LaneAppearance
{
    private static float[] Data => Numeric_Types_Draw03_Draw03LaneAppearance_cs.Values.F32;
    internal readonly struct Band
    {
        internal readonly float Start, End, Scale, Offset;
        internal readonly bool Wear;
        internal Band(float start, float end, float scale, float offset, bool wear)
        { Start=start; End=end; Scale=scale; Offset=offset; Wear=wear; }
    }

    internal static Band[] Bands(float[] centers, Vector2 span, bool right)
    {
        var selected=new List<float>();
        foreach(float center in centers)if(center>=span.x&&center<=span.y)selected.Add(center);
        centers=selected.ToArray();
        float sign=right?1:-1;
        var result=new List<Band>(); float cursor=span.x;
        void Background(float end)
        {
            if(end<=cursor)return;
            // Approved atlas-derived background; width changes coverage only.
            result.Add(new Band(cursor,end,1,0,false));
            cursor=end;
        }
        var lanes=new List<Band>();
        for(int i=0;i<centers.Length;i++)
        {
            // Every actual lane uses the same complete middle-lane appearance,
            // retaining its measured lane-relative offset and physical width.
            bool positive=centers[i]>=0;
            float laneSign=positive?1:-1;
            float reference=Data[2],minV=Data[3],maxV=Data[4];
            float a=(Data[1]-maxV)/Data[0],b=(Data[1]-minV)/Data[0];
            float first=Mathf.Min(laneSign*(a-reference),laneSign*(b-reference));
            float last=Mathf.Max(laneSign*(a-reference),laneSign*(b-reference));
            float low=Mathf.Max(span.x,centers[i]+first), high=Mathf.Min(span.y,centers[i]+last);
            float laneScale=sign*laneSign;
            lanes.Add(new Band(low,high,laneScale,sign*reference-laneScale*centers[i],true));
        }
        for(int i=0;i<lanes.Count;i++)
        {
            var lane=lanes[i]; float low=lane.Start,high=lane.End;
            if(i>0&&lanes[i-1].End>low)
                low=Mathf.Clamp((centers[i-1]+centers[i])*0.5f,low,lanes[i-1].End);
            if(i+1<lanes.Count&&lanes[i+1].Start<high)
                high=Mathf.Clamp((centers[i]+centers[i+1])*0.5f,lanes[i+1].Start,high);
            low=Mathf.Max(low,cursor);
            if(high<=low)continue;
            Background(low);
            result.Add(new Band(low,high,lane.Scale,lane.Offset,true));
            cursor=high;
        }
        Background(span.y);
        return result.ToArray();
    }

    internal static Patch[] Apply(Patch[] source, ref Partition carriageway,
        MovableSelectedRoadLayout layout, Pose pose, Vector2 span,
        Func<int,Band,Patch> map, Func<int,bool> right, out bool[] wear)
    {
        var patches=new List<Patch>(source); var flags=new List<bool>();
        for(int i=0;i<source.Length;i++)flags.Add(false);
        var cache=new Dictionary<int,Partition>();
        var frame=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one).transpose;
        Partition Expand(Partition node)
        {
            if(node.IsGap)return node;
            if(node.Bone<0)return new Partition(node.ObjectToSeam,Expand(node.Negative!),Expand(node.Positive!));
            if(cache.TryGetValue(node.Bone,out var existing))return existing;
            var bands=Bands(layout.CarLaneCenters,span,right(node.Bone));
            // A collapsed or reversed selected span has no additional appearance
            // boundaries. Keep its existing route; geometry quality is the player's choice.
            if(bands.Length==0)return node;
            int first=patches.Count;
            foreach(var band in bands){patches.Add(map(node.Bone,band));flags.Add(band.Wear);}
            Partition Tree(int lo,int hi)
            {
                if(hi-lo==1)return new Partition(first+lo);
                int middle=(lo+hi)/2;
                var seam=Matrix4x4.identity;
                seam.SetRow(0,frame*new Vector4(0,0,1,0));
                seam.SetRow(1,frame*new Vector4(0,1,0,0));
                seam.SetRow(2,frame*new Vector4(1,0,0,-bands[middle].Start));
                return new Partition(seam,Tree(lo,middle),Tree(middle,hi));
            }
            var result=Tree(0,bands.Length);cache.Add(node.Bone,result);return result;
        }
        carriageway=Expand(carriageway);wear=flags.ToArray();return patches.ToArray();
    }

    internal static Vector4 NearV(Band band,bool right,Matrix4x4 frame)
    {
        float slope=(right?-1:1)*Data[0];
        return frame*new Vector4(slope*band.Scale/(Data[9]-Data[8]),0,0,
            (Data[1]+slope*band.Offset-Data[8])/(Data[9]-Data[8]));
    }

    internal static Patch Background(Matrix4x4 frame,Vector4 u)
        =>new Patch(Vector2.zero,Vector2.one,u,
            frame*new Vector4(Data[0]/Data[13],0,0,0),BridgePreparedMaterials.RepeatUnit("Draw03","DerivedRoadBackground"),repeat:true,
            preparedUnit:true,repeatU:true,repeatV:true,projectTangent:true);
}

// Embedded numerical arrays: Types_Draw03_Draw03LaneAppearance_cs
internal static class Numeric_Types_Draw03_Draw03LaneAppearance_cs
{
    static Numeric_Types_Draw03_Draw03LaneAppearance_cs() {}
    internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Draw03_Draw03LaneAppearance_cs");
}
