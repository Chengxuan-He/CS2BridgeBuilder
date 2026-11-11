// Current-skeleton projection adapter. Its paired fragment program preserves
// the inspected native DBuffer sampling, blending, clipping and distance rules.
#ifndef BB_GLOBAL_REGISTER
#define BB_GLOBAL_REGISTER b0
#endif
cbuffer BBGlobals:register(BB_GLOBAL_REGISTER){float4 g[183];};
#if defined(BB_NON_DOTS)
cbuffer BBDraw:register(b1){float4 drawValues[39];};
cbuffer BBMaterial:register(b2){uint4 m[17];};
ByteAddressBuffer bones:register(t0);
#else
cbuffer BBVisible:register(BB_VISIBLE_REGISTER){uint4 visible[256];};
cbuffer BBBuiltin:register(BB_BUILTIN_REGISTER){uint4 builtin[2];};
cbuffer BBMetadata:register(BB_MATERIAL_REGISTER){uint4 metadata[3];};
ByteAddressBuffer instances:register(t0);
ByteAddressBuffer bones:register(t1);
// Projection bias is material-owned, not an animated instance property.
cbuffer BBMaterialValues:register(BB_MATERIAL_VALUE_REGISTER){uint4 m[17];};
#endif
static float4 d[8];
struct Input {float3 position:POSITION;float4 tangent:TANGENT;float4 x:TEXCOORD0;float4 y:TEXCOORD1;float4 z:TEXCOORD2;float4 area:TEXCOORD3;uint bone:BLENDINDICES;
#if !defined(BB_NON_DOTS)
 uint instance:SV_InstanceID;
#endif
};
struct Output {
 float4 position:SV_POSITION;float4 tangent:INTERP0;float4 uv:INTERP1;float3 world:INTERP2;float3 normal:INTERP3;
 nointerpolation float4 p0:TEXCOORD8;nointerpolation float4 p1:TEXCOORD9;nointerpolation float4 p2:TEXCOORD10;nointerpolation float4 p3:TEXCOORD11;
 nointerpolation float4 p4:TEXCOORD12;nointerpolation float4 p5:TEXCOORD13;nointerpolation float4 p6:TEXCOORD14;nointerpolation float4 p7:TEXCOORD15;nointerpolation float4 p8:TEXCOORD16;
};
float3 RootVector(float3 v){return d[0].xyz*v.x+d[1].xyz*v.y+d[2].xyz*v.z;}
Output BBDecalVertex(Input v){
#if defined(BB_NON_DOTS)
 for(uint i=0;i<8;i++)d[i]=drawValues[i];
 uint2 range=m[16].xy;
#else
 uint instance=visible[v.instance].x;
 uint address=BBInstanceAddress(builtin[0].x,instance,48u);
 float4 oa=asfloat(instances.Load4(address)),ob=asfloat(instances.Load4(address+16)),oc=asfloat(instances.Load4(address+32));
 d[0]=float4(oa.xyz,0);d[1]=float4(oa.w,ob.xy,0);d[2]=float4(ob.zw,oc.x,0);d[3]=float4(oc.yzw,1);
 uint2 range=BBInstanceBoneRange(instances,metadata[2].w,instance);
#endif
 uint offset=min(range.x+(v.bone&255u),range.y)*64u;
 float4 a=asfloat(bones.Load4(offset)),b=asfloat(bones.Load4(offset+16)),c=asfloat(bones.Load4(offset+32)),t=asfloat(bones.Load4(offset+48));
 float3 x=RootVector(a.xyz*v.x.x+b.xyz*v.x.y+c.xyz*v.x.z);
 float3 y=RootVector(a.xyz*v.y.x+b.xyz*v.y.y+c.xyz*v.y.z);
 float3 z=RootVector(a.xyz*v.z.x+b.xyz*v.z.y+c.xyz*v.z.z);
 float3 center=RootVector(a.xyz*v.x.w+b.xyz*v.y.w+c.xyz*v.z.w+t.xyz)+d[3].xyz;
 float determinant=dot(x,cross(y,z));
 float3 ix=cross(y,z)/determinant,iy=cross(z,x)/determinant,iz=cross(x,y)/determinant;
 Output o;
 o.p0=float4(x,0);o.p1=float4(y,0);o.p2=float4(z,0);o.p3=float4(center,1);
 o.p4=float4(ix.x,iy.x,iz.x,0);o.p5=float4(ix.y,iy.y,iz.y,0);o.p6=float4(ix.z,iy.z,iz.z,0);
 o.p7=float4(-dot(ix,center),-dot(iy,center),-dot(iz,center),1);o.p8=float4(0,0,0,determinant<0?-1:1);
 float3 world=RootVector(a.xyz*v.position.x+b.xyz*v.position.y+c.xyz*v.position.z+t.xyz)+d[3].xyz-g[44].xyz;
 float4 material12=asfloat(m[12]),material13=asfloat(m[13]);
 float3 direction=g[55].w==0?-world:float3(g[0].z,g[1].z,g[2].z);
 if(material12.w==1)world+=normalize(direction)*material13.y;
 o.position=g[20]*world.x+g[21]*world.y+g[22]*world.z+g[23];
 if(material12.w==0)o.position.z-=material13.x;
 o.world=world;o.tangent=float4(normalize(x),v.tangent.w);o.normal=normalize(iy);o.uv=v.area;
 return o;
}
