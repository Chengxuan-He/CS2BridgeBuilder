using System.Text.Json;
using BridgeBuilder.Bridges;
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));
var charts=doc.RootElement.GetProperty("charts");int domains=0,refs=0;
foreach(var mesh in doc.RootElement.GetProperty("meshes").EnumerateArray())
{
    var render=mesh.GetProperty("render").GetString()!;int index=mesh.GetProperty("mesh").GetInt32();int domain=0;
    foreach(var expected in mesh.GetProperty("domains").EnumerateArray())
    {
        var loaded=Draw02UvCharts.For(render,index,domain++);
        if(loaded.Length!=expected.GetArrayLength())throw new Exception("Chart count mismatch.");
        int ci=0;
        foreach(var id in expected.EnumerateArray())
        {
            var loops=charts[id.GetInt32()];var actual=loaded[ci++].Loops;
            if(actual.Length!=loops.GetArrayLength())throw new Exception("Loop count mismatch.");
            for(int i=0;i<actual.Length;i++)
            {
                var points=loops[i];if(actual[i].Length!=points.GetArrayLength()*2)throw new Exception("Point count mismatch.");
                for(int j=0;j<points.GetArrayLength();j++)for(int axis=0;axis<2;axis++)
                    if(actual[i][j*2+axis]!=points[j][axis].GetDouble())throw new Exception("F64 coordinate mismatch.");
            }
            refs++;
        }
        if(!ReferenceEquals(loaded,Draw02UvCharts.For(render,index,domain-1)))throw new Exception("Expected lazy identity reuse.");
        domains++;
    }
}
if(Draw02UvCharts.For("Unknown",0,0).Length!=0)throw new Exception("Unexpected unknown key.");
Console.WriteLine(JsonSerializer.Serialize(new{domains,chartReferences=refs,exactF64=true,lazyIdentity=true,nativeRenderingVerified=false}));
namespace BridgeBuilder.Bridges
{
    // API/schema stand-in only. Actual native containment is owned by the public implementation.
    internal sealed class MovableUvChart
    {
        internal readonly double[][] Loops;
        private MovableUvChart(double[][] loops){Loops=loops;}
        internal static MovableUvChart Read(int[] integers,double[] values,int offset)
        {
            var loops=new double[integers[offset]][];
            for(int i=0;i<loops.Length;i++)
            {
                int start=integers[offset+1+i*2],count=integers[offset+2+i*2];
                loops[i]=values.Skip(start).Take(count*2).ToArray();
                if(loops[i].Length!=count*2)throw new Exception("Truncated chart.");
            }
            return new MovableUvChart(loops);
        }
    }
}
