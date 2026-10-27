using BridgeBuilder.Bridges;

internal static class RoadUvChecks
{
    internal static void Run(string managed)
    {
        var codec=new NativeUvCodec(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll"));
        // The old Float16x4 branch collapses this otherwise valid UV triangle.
        var triangle=new[]{.5f,.5f,0,0, .50001f,.5f,0,0, .5f,.50001f,0,0};
        if((Half)triangle[0]!=(Half)triangle[4])throw new Exception("Regression fixture does not reproduce half-float collapse.");
        int cases=0;
        foreach(int extra in new[]{-1,2,3})
        foreach(float scale in new[]{1f,17f,131072f})
        {
            var source=(float[])triangle.Clone();
            for(int i=0;i<source.Length;i+=4){source[i]*=scale;source[i+1]*=scale;}
            if(extra>=0)source[4+extra]=.0000001f; // One vertex changes the whole batch layout.
            var encoded=MovableRoadUv.Encode(source,out int dimension);
            if(dimension!=(extra<0?2:4))throw new Exception("Native lane UV components were discarded.");
            for(int i=0;i<3;i++)for(int axis=0;axis<dimension;axis++)
                if(BitConverter.SingleToInt32Bits(encoded[i*dimension+axis])!=BitConverter.SingleToInt32Bits(source[i*4+axis]))
                    throw new Exception("Road UV encoding changed source precision.");
            float area=(encoded[dimension]-encoded[0])*(encoded[2*dimension+1]-encoded[1])-
                (encoded[dimension+1]-encoded[1])*(encoded[2*dimension]-encoded[0]);
            if(!(area>0))throw new Exception("Road UV triangle collapsed.");
            var bytes=new byte[encoded.Length*4];Buffer.BlockCopy(encoded,0,bytes,0,bytes.Length);
            codec.Check(bytes,3,dimension*4);cases++;
        }
        Console.WriteLine($"PASS road UV serialization: {cases} cases; Float32x2/x4; Z/W retained; thin UV triangles remain nondegenerate; values above Float16 range retained; native Meshopt/ZSTD round trips bit-exact.");
    }
}
