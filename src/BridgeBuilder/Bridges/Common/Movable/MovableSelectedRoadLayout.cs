using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Evaluated selected-road dimensions in mechanism space; never prototype dimensions.
internal sealed class MovableSelectedRoadLayout
{
    internal readonly bool[] SidewalkOccurrences;
    internal readonly string Prototype;
    internal readonly float[] CarLaneCenters;
    internal readonly MovableRoadLaneLayout.Lane[] Lanes;
    internal readonly float[] LaneStations;
    internal MovableRoadLaneLayout.Boundary[] LaneBoundariesAt(float objectZ)
        => MovableRoadLaneLayout.BoundariesAt(Lanes, objectZ);
    internal readonly Vector2 SurfaceSpan;
    internal readonly Vector2 LeftSidewalk, RightSidewalk;

    internal MovableSelectedRoadLayout(MovableClosedRoad.Result road, Pose roadToObject, string prototype)
    {
        Prototype = prototype;
        var lanes = new System.Collections.Generic.List<MovableRoadLaneLayout.Lane>();
        foreach (var lane in road.RoadLanes)
        {
            var start = roadToObject.rotation * lane.Start + roadToObject.position;
            var control1 = roadToObject.rotation * lane.Control1 + roadToObject.position;
            var control2 = roadToObject.rotation * lane.Control2 + roadToObject.position;
            var end = roadToObject.rotation * lane.End + roadToObject.position;
            var transverse = roadToObject.rotation * Vector3.right;
            lanes.Add(new MovableRoadLaneLayout.Lane(start.x,start.z,control1.x,control1.z,
                control2.x,control2.z,end.x,end.z,lane.Width,lane.StartWidth,lane.EndWidth,
                transverse.x,lane.Type,(uint)lane.Flags));
        }
        lanes.Sort((a,b) => a.Left.CompareTo(b.Left));
        Lanes = lanes.ToArray();
        var stations = new System.Collections.Generic.SortedSet<float>();
        foreach(var lane in Lanes) { stations.Add(lane.StartZ); stations.Add(lane.EndZ); }
        LaneStations = new float[stations.Count]; stations.CopyTo(LaneStations);
        if (prototype == "Draw02") ReportNativeLanes(road, roadToObject);
        var carCenters = new System.Collections.Generic.List<float>();
        foreach(var center in road.CarLaneCenters)
        {
            var x=(roadToObject.rotation*center+roadToObject.position).x;
            if(!carCenters.Exists(value => Mathf.Abs(value-x)<.01f)) carCenters.Add(x);
        }
        carCenters.Sort();
        CarLaneCenters=carCenters.ToArray();
        SidewalkOccurrences = new bool[road.Occurrences.Length];
        var spans = new Vector2[road.Occurrences.Length];
        for (var i = 0; i < spans.Length; i++)
        {
            var section = road.Occurrences[i].RootSection
                ?? throw new InvalidOperationException("Moving road layout requires source section provenance.");
            SidewalkOccurrences[i] = section.name == "Sidewalk" ||
                section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            spans[i] = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        }
        foreach (var triangle in road.Triangles)
        {
            Add(triangle.Occurrence, triangle.A.Position);
            Add(triangle.Occurrence, triangle.B.Position);
            Add(triangle.Occurrence, triangle.C.Position);
        }
        var surfaceSpan = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        foreach(var span in spans) if(span.y>span.x) Extend(ref surfaceSpan,span);
        SurfaceSpan=surfaceSpan;
        var left = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        var right = left;
        for (var i = 0; i < spans.Length; i++)
        {
            var span = spans[i];
            if (!SidewalkOccurrences[i] || !(span.y > span.x)) continue;
            if (span.x + span.y < 0f) Extend(ref left, span);
            else Extend(ref right, span);
        }
        LeftSidewalk = left.y > left.x ? left : Vector2.zero;
        RightSidewalk = right.y > right.x ? right : Vector2.zero;

        void Add(int occurrence, Vector3 position)
        {
            var x = (roadToObject.rotation * position + roadToObject.position).x;
            spans[occurrence].x = Mathf.Min(spans[occurrence].x, x);
            spans[occurrence].y = Mathf.Max(spans[occurrence].y, x);
        }
    }

    private static void Extend(ref Vector2 target, Vector2 span)
    { target.x = Mathf.Min(target.x, span.x); target.y = Mathf.Max(target.y, span.y); }

    private void ReportNativeLanes(MovableClosedRoad.Result road, Pose pose)
    {
        static string V(Vector3 v) => FormattableString.Invariant($"({v.x:R},{v.y:R},{v.z:R})");
        Mod.Log.Info(FormattableString.Invariant($"Native lane snapshot: style={Prototype}; {road.LaneContext}; source=evaluated elevated context; lanes={road.RoadLanes.Length}; poseP={V(pose.position)}; poseQ=({pose.rotation.x:R},{pose.rotation.y:R},{pose.rotation.z:R},{pose.rotation.w:R}); stations={string.Join(",",Array.ConvertAll(LaneStations,z=>z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))}."));
        for(var i=0;i<road.RoadLanes.Length;i++)
        {
            var lane=road.RoadLanes[i];
            Mod.Log.Info(FormattableString.Invariant($"Native lane snapshot item: {road.LaneContext}; index={i}; prefab={lane.Prefab}; flags={(uint)lane.Flags}; kind={lane.Type}; width={lane.Width:R}; startWidth={lane.StartWidth:R}; endWidth={lane.EndWidth:R}; a={V(lane.Start)}; b={V(lane.Control1)}; c={V(lane.Control2)}; d={V(lane.End)}."));
        }
    }
}
