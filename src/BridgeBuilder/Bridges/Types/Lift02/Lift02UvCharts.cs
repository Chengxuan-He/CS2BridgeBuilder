// Generated from native indexed UV charts by ExportNativeCharts.py.
// Embedded numerical arrays: Types_Lift02_Lift02UvCharts_cs
using System;
using System.Collections.Concurrent;
namespace BridgeBuilder.Bridges;
internal static class Lift02UvCharts
{
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        int offset=(renderName,meshIndex) switch
        {
            ("LiftBridge02_LOD1 Mesh", 0) => 25988,
            ("LiftBridge02_LOD1 Mesh", 1) => 98810,
            ("LiftBridge02_LOD2 Mesh", 0) => 105093,
            ("LiftBridge02 Mesh", 0) => 110847,
            ("LiftBridge02 Mesh", 1) => 228381,
            ("LiftBridge02NetPillar_LOD1 Mesh", 0) => 228389,
            ("LiftBridge02NetPillar_LOD2 Mesh", 0) => 228579,
            ("LiftBridge02NetPillar Mesh", 0) => 228634,
            ("LiftBridge02NetPillarBase_LOD1 Mesh", 0) => 228727,
            ("LiftBridge02NetPillarBase_LOD2 Mesh", 0) => 229255,
            ("LiftBridge02NetPillarBase Mesh", 0) => 229375,
            ("LiftBridge02NetPillarTop_LOD1 Mesh", 0) => 230497,
            ("LiftBridge02NetPillarTop_LOD2 Mesh", 0) => 230990,
            ("LiftBridge02NetPillarTop Mesh", 0) => 231110,
            ("LiftBridge02Pillar_LOD1 Mesh", 0) => 231765,
            ("LiftBridge02Pillar_LOD2 Mesh", 0) => 232382,
            ("LiftBridge02Pillar Mesh", 0) => 232542,
            ("LiftBridge02PillarBase_LOD1 Mesh", 0) => 233220,
            ("LiftBridge02PillarBase_LOD2 Mesh", 0) => 234222,
            ("LiftBridge02PillarBase Mesh", 0) => 234332,
            _ => throw new InvalidOperationException("Unrecorded Lift02 UV chart source: "+renderName+"/"+meshIndex)
        };
        return Data.Sets.GetOrAdd(Data.Values.I32[offset+domain], ReadSet);
    }
    private static MovableUvChart[] ReadSet(int offset)
    {
        var values=Data.Values;
        var result=new MovableUvChart[values.I32[offset]];
        for(int i=0;i<result.Length;i++)
            result[i]=Data.Charts.GetOrAdd(values.I32[offset+i+1], o=>MovableUvChart.Read(values.I32,values.F64,o));
        return result;
    }
    private static class Data
    {
        internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift02_Lift02UvCharts_cs");
        internal static readonly ConcurrentDictionary<int,MovableUvChart> Charts=new();
        internal static readonly ConcurrentDictionary<int,MovableUvChart[]> Sets=new();
    }
}
