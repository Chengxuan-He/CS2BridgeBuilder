// Offline only: original finite panel evidence becomes one finite native asset.
// Runtime receives only final UV bounds and never the original face partitions.
int FiniteWarpEnabled;
float4 FiniteWarpShape; // width at q.y=0/1, native z at q.y=0, length
float4 FiniteWarpSeam;
float4 FiniteWarpU0, FiniteWarpV0, FiniteWarpU1, FiniteWarpV1;
void Lift01FiniteWarp(float2 q, out float2 uv, out float lod)
{
    float halfWidth=lerp(FiniteWarpShape.x,FiniteWarpShape.y,q.y);
    float3 p=float3((2*q.x-1)*halfWidth,FiniteWarpShape.z-FiniteWarpShape.w*q.y,1);
    bool first=dot(FiniteWarpSeam.xyz,p)<=0;
    float3 u=first?FiniteWarpU0.xyz:FiniteWarpU1.xyz;
    float3 v=first?FiniteWarpV0.xyz:FiniteWarpV1.xyz;
    uv=float2(dot(u,p),dot(v,p));
    float2 dx=float2(2*halfWidth,0);
    float2 dy=float2((2*q.x-1)*(FiniteWarpShape.y-FiniteWarpShape.x),-FiniteWarpShape.w);
    uint width,height;
    Source.GetDimensions(width,height);
    float2 sourceSize=float2(width,height);
    float2 gx=float2(dot(u.xy,dx),dot(v.xy,dx))*sourceSize*2/Extent.x;
    float2 gy=float2(dot(u.xy,dy),dot(v.xy,dy))*sourceSize*2/Extent.y;
    lod=log2(max(length(gx),length(gy)));
}
