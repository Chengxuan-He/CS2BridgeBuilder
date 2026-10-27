using System;

namespace BridgeBuilder.Bridges;

internal static class MovableRoadUv
{
    // Extra lane data in Z/W must not reduce the precision of the atlas XY.
    // GeometryAsset.CreateMeshes preserves Float32x2/x4 object attributes.
    // This is the object-road encoder, not the NetMeshHelpers UV cache (x2 only).
    internal static float[] Encode(float[] values,out int dimension)
    {
        if(values.Length%4!=0)throw new ArgumentException("Road UV input requires four components per vertex.");
        dimension=4;
        for(int i=0;i<values.Length;i+=4)
            if(values[i+2]!=0f || values[i+3]!=0f)return values;
        dimension=2;
        var planar=new float[values.Length/2];
        for(int i=0;i<values.Length;i+=4)
        {planar[i/2]=values[i];planar[i/2+1]=values[i+1];}
        return planar;
    }
}
