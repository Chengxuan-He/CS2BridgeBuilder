using System.Collections.Generic;
namespace BridgeBuilder.Bridges;
// Measured whole indexed crossbeam charts; no receiver-dependent unit bounds.
// Embedded numerical arrays: Types_Lift02_Lift02StructureRepeatUnits_cs
internal static partial class MovableRepeatUnits
{
    static partial void Lift02Regions(string sourceDigest,List<Region> regions)
    {
        if(sourceDigest!="4dc9b63a77eabc2730b1ec0313cdacb913f74ad5d445b88498607e933030bf91")return;
        var d=Lift02StructureRepeatData.Values.F64;
        regions.Add(new Region("7bb6fcf80a7fa3daf565fb2768db12caa257f1e723f208d095a836db8b7ac4fc",d[0],d[1],d[2],d[3]));
        regions.Add(new Region("4845a5048ecda69e50165c288f69d8d7009d96117e3d854df6d8cf7901743404",d[4],d[5],d[6],d[7]));
        regions.Add(new Region("adc5d2f8f9f2a646981a6064fb79beadb6320ab2615636eb3e439b2f581f43d8",d[8],d[9],d[10],d[11]));
    }
    private static class Lift02StructureRepeatData
    {
        internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift02_Lift02StructureRepeatUnits_cs");
    }
}
