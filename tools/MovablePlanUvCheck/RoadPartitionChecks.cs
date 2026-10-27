using BridgeBuilder.Bridges;

internal static class RoadPartitionChecks
{
    internal static void Run()
    {
        var random=new Random(241024);
        int cases=0,faces=0,probes=0;
        void Check(MovableChartPartition.Bounds[] input,float dx,float dy,int capacity,int minimumGroups=1)
        {
            var groups=MovableChartPartition.Build(input,dx,dy,capacity);
            if(groups.Count<minimumGroups)throw new Exception("Oversized receiver was not partitioned.");
            var membership=groups.SelectMany(g=>g.Faces).OrderBy(i=>i).ToArray();
            if(!membership.SequenceEqual(Enumerable.Range(0,input.Length)))throw new Exception("Face lost or duplicated.");
            foreach(var group in groups)
            {
                var b=group.Extent;
                if(!b.Fits(dx,dy,capacity))throw new Exception("Image capacity exceeded.");
                // Reproduce the production atlas rounding and Float32 UV inverse.
                int w=Math.Max(1,(int)Math.Ceiling((b.MaxX-b.MinX)*dx));
                int h=Math.Max(1,(int)Math.Ceiling((b.MaxY-b.MinY)*dy));
                float rx=b.MaxX>b.MinX?w/(b.MaxX-b.MinX):dx;
                float ry=b.MaxY>b.MinY?h/(b.MaxY-b.MinY):dy;
                if(rx<dx*.999999f || ry<dy*.999999f)throw new Exception("Texture density reduced.");
                foreach(int id in group.Faces)
                {
                    var f=input[id];
                    if(f.MinX<b.MinX || f.MinY<b.MinY || f.MaxX>b.MaxX || f.MaxY>b.MaxY)
                        throw new Exception("Face escaped image bounds.");
                    foreach(float t in new[]{0f,.37f,1f})
                    {
                        float x=f.MinX+(f.MaxX-f.MinX)*t,y=f.MinY+(f.MaxY-f.MinY)*t;
                        float u=(8+(x-b.MinX)*rx)/(w+16),v=(8+(y-b.MinY)*ry)/(h+16);
                        float px=(u*(w+16)-8)/rx+b.MinX,py=(v*(h+16)-8)/ry+b.MinY;
                        if(Math.Abs(px-x)*dx>.01 || Math.Abs(py-y)*dy>.01)
                            throw new Exception("Source phase changed by more than .01 pixel.");
                        probes++;
                    }
                }
            }
            faces+=input.Length;cases++;
        }
        // Two coincident LOD faces share a chart; separated original triangles
        // exceed a 16K image as a union but each fits at 256 texels/metre.
        Check(new[]{new MovableChartPartition.Bounds(-2,-80,2,-40),new(-2,40,2,80),new(-2,-80,2,-40)},256,256,16368,2);
        Check(new[]{new MovableChartPartition.Bounds(0,0,16368,0)},1,1,16368);
        Check(new[]{new MovableChartPartition.Bounds(0,0,0,16368)},1,1,16368);
        for(int test=0;test<1000;test++)
        {
            float dx=16+random.Next(512),dy=16+random.Next(512);
            var input=Enumerable.Range(0,32).Select(_=>
            {
                float x=(float)(random.NextDouble()*160-80),y=(float)(random.NextDouble()*160-80);
                return new MovableChartPartition.Bounds(x,y,x+(float)random.NextDouble()*8,y+(float)random.NextDouble()*8);
            }).ToArray();
            Check(input,dx,dy,8192);
        }
        bool rejected=false;
        try {MovableChartPartition.Build(new[]{new MovableChartPartition.Bounds(0,0,16369,1)},1,1,16368);}
        catch(InvalidOperationException) {rejected=true;}
        if(!rejected)throw new Exception("An oversized original face was silently accepted.");
        Console.WriteLine($"PASS road partition: cases={cases}; faces={faces}; phase probes={probes}; oversized original face rejected; topology and source density retained.");
    }
}
