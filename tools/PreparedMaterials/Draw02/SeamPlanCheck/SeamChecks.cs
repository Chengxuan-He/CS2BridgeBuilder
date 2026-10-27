using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
internal static class SeamChecks
{
    static double[][] Matrix(JsonElement e,string name)=>e.GetProperty(name).EnumerateArray().Select(a=>a.EnumerateArray().Select(n=>n.GetDouble()).ToArray()).ToArray();
    static double[] Array(JsonElement e,string name)=>e.GetProperty(name).EnumerateArray().Select(n=>n.GetDouble()).ToArray();
    internal static void Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));
        var groups=new Dictionary<(string,int,int),List<int>>();
        var examples=new List<object>();
        int checkedFaces=0,splitTriangles=0,phaseFailures=0,areaFailures=0;double worstPhase=0,worstArea=0,worstFloat32Pixels=0;
        var heavy=new List<object>();
        foreach(var row in doc.RootElement.EnumerateArray())
        {
            var q=Matrix(row,"q");bool triangle=row.GetProperty("triangle").GetBoolean();
            var a=Array(row,"a");var b=Array(row,"b");var c=Array(row,"c");var bounds=Array(row,"bounds");
            double[] Atlas(double x,double y)=>triangle
              ? new[]{a[0]+(b[0]-a[0])*x+(c[0]-a[0])*y,a[1]+(b[1]-a[1])*x+(c[1]-a[1])*y}
              : new[]{bounds[0]+(bounds[2]-bounds[0])*x,bounds[1]+(bounds[3]-bounds[1])*y};
            var pieces=NativeSeam.Split(q,triangle,0);double covered=0;
            foreach(var p in pieces)
            {
                covered+=Math.Abs((p.B.B-p.A.B)*(p.C.C-p.A.C)-(p.B.C-p.A.C)*(p.C.B-p.A.B));
                var v=new[]{p.A,p.B,p.C};var local=v.Select(z=>p.Local(z)).ToArray();
                var mapped=local.Select(z=>Atlas(z.X,z.Y)).ToArray();
                foreach(var weights in new[]{new[]{.173,.317,.51},new[]{.793,.107,.1}})
                {
                    double x=0,y=0;
                    for(int i=0;i<3;i++){x+=weights[i]*v[i].X;y+=weights[i]*v[i].Y;}
                    x-=Math.Floor(x);y-=Math.Floor(y);
                    if(triangle && x+y>1){x=1-x;y=1-y;}
                    var expected=Atlas(x,y);double error=0,quantized=0;
                    for(int axis=0;axis<2;axis++)
                    {
                        double actual=0,stored=0;
                        for(int i=0;i<3;i++){actual+=weights[i]*mapped[i][axis];stored+=weights[i]*(float)mapped[i][axis];}
                        error=Math.Max(error,Math.Abs(actual-expected[axis]));
                        quantized=Math.Max(quantized,4096*Math.Abs(stored-expected[axis]));
                    }
                    worstPhase=Math.Max(worstPhase,error);worstFloat32Pixels=Math.Max(worstFloat32Pixels,quantized);
                    if(error>1e-10)phaseFailures++;
                }
            }
            double areaError=Math.Abs(covered-1);worstArea=Math.Max(worstArea,areaError);if(areaError>1e-8)areaFailures++;
            var render=row.GetProperty("render").GetString()!;int lod=row.GetProperty("lod").GetInt32(),delta=row.GetProperty("delta").GetInt32();
            var key=(render,lod,delta);if(!groups.TryGetValue(key,out var counts))groups.Add(key,counts=new());counts.Add(pieces.Count);
            if(pieces.Count>64 && heavy.Count<10)heavy.Add(new{render,lod,delta,triangle,q,outputTriangles=pieces.Count});
            if(examples.Count<2 && pieces.Count>=3 && pieces.Count<=8 && (examples.Count==0 || triangle))
                examples.Add(new {input=row.Clone(),surfaceReference="retain original Surface and its native texture graph",pieces=pieces.Select(p=>new {
                    p.CellX,p.CellY,p.Reflected,corners=new[]{p.A,p.B,p.C}.Select(v=>new {
                        barycentric=new[]{v.A,v.B,v.C},unit=new[]{v.X,v.Y},atlasUv=Atlas(p.Local(v).X,p.Local(v).Y)})})});
            checkedFaces++;splitTriangles+=pieces.Count;
        }
        var report=new{checkedFaces,splitTriangles,phaseFailures,areaFailures,worstPhase,worstArea,worstFloat32Pixels,
            sampling="Interior piecewise atlas UV equivalence; native region edge filtering and reflected normal frame NOT proven",
            newTexturePayloads=0,productionIntegration=false,
            totals=groups.GroupBy(g=>g.Key.Item3).Select(g=>new {delta=g.Key,inputs=g.Sum(x=>x.Value.Count),outputs=g.Sum(x=>x.Value.Sum()),over64=g.Sum(x=>x.Value.Count(n=>n>64)),maximum=g.Max(x=>x.Value.Max())}),
            rows=groups.Select(g=>new{render=g.Key.Item1,lod=g.Key.Item2,delta=g.Key.Item3,inputs=g.Value.Count,outputs=g.Value.Sum(),over64=g.Value.Count(n=>n>64),maximum=g.Value.Max()}),heavy,examples};
        File.WriteAllText(path+".cuts.json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(new{checkedFaces,splitTriangles,phaseFailures,areaFailures,worstPhase,worstArea,worstFloat32Pixels}));
        if(phaseFailures!=0 || areaFailures!=0)throw new Exception("Native seam CPU coverage or interpolation validation failed.");
    }
}
