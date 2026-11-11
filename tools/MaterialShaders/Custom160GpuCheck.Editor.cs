using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class Custom160GpuCheck
{
 const string Root="E:/CS2BridgeBuilder";
 public static void Run()
 {
  try
  {
   var shared=File.ReadAllText(Root+"/tools/MaterialShaders/MaterialFunctions.hlsl");
   CheckLayer(shared);
   shared += File.ReadAllText(Root+"/tools/MaterialShaders/NativeInstanceSkeleton.hlsl");
   var decal=File.ReadAllText(Root+"/tools/MaterialShaders/SkeletonDecal.hlsl");
   var source="Shader \"Hidden/BridgeBuilder/SkeletonDecalCheck\" { SubShader { Pass { ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha HLSLPROGRAM\n#pragma target 5.0\n#pragma vertex vert\n#pragma fragment frag\n"+shared+decal+@"
ByteAddressBuffer _Bones;
ByteAddressBuffer _InstanceData;
struct Input {float3 position:POSITION;float2 uv:TEXCOORD0;uint bone:BLENDINDICES;};
struct Varying {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 moved:TEXCOORD1;};
Varying vert(Input v){Varying o;o.position=float4(BBNativeSkinnedPoint(_InstanceData,_Bones,0x80000090u,0x80000000u,2u,v.bone,v.position),1);o.uv=v.uv;o.moved=o.position.xyz;o.position.z=.5;return o;}
float4 frag(Varying i):SV_Target{float3 restored=BBBoneInversePosition(_Bones,BBInstanceBoneRange(_InstanceData,0x80000090u,2u),1u,i.moved);
if(any(abs(restored-float3(i.uv*.4-.2,0))>0.0001))return float4(1,0,1,1);
BBMaterialSample s=BBEvaluateDecal(i.uv,float4(1,1,1,1));return float4(s.albedo,s.alpha);}
ENDHLSL } } }";
   var shader=ShaderUtil.CreateShaderAsset(source);if(ShaderUtil.ShaderHasError(shader))throw new Exception("generated shader compilation failed");
   var mat=new Material(shader);var bones=new ComputeBuffer(3*16,4,ComputeBufferType.Raw);
   var instance=new ComputeBuffer(64,4,ComputeBufferType.Raw);var words=new uint[64];
   // Third visible instance: packed float3x4 identity at byte 96. Bone range at byte 160.
   words[24]=words[28]=words[32]=0x3f800000u;words[40]=0;words[41]=2;instance.SetData(words);
   mat.SetBuffer("_InstanceData",instance);
   var tex=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);tex.SetPixel(0,0,new Color(.8f,.4f,.2f,.25f));tex.Apply();
   var normal=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);normal.SetPixel(0,0,new Color(.5f,.5f,0,1));normal.Apply();
   mat.SetTexture("_Base",tex);mat.SetTexture("_Normal",normal);mat.SetTexture("_Mask",Texture2D.whiteTexture);mat.SetBuffer("_Bones",bones);
   var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.2f,-.2f,0),new Vector3(.2f,-.2f,0),new Vector3(.2f,.2f,0),new Vector3(-.2f,.2f,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.boneWeights=new[]{Weight(),Weight(),Weight(),Weight()};
   var target=RenderTexture.GetTemporary(128,128,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
   var output=new System.Text.StringBuilder();
   foreach(float offset in new[]{-.5f,0f,.5f})
   {
    var values=new float[48];for(int b=0;b<3;b++){var m=Matrix4x4.TRS(new Vector3(b==1?offset:0,0,0),Quaternion.Euler(0,b==1?offset*90:0,0),Vector3.one);for(int n=0;n<16;n++)values[b*16+n]=m[n];}bones.SetData(values);
    RenderTexture.active=target;GL.Clear(false,true,Color.clear);if(!mat.SetPass(0))throw new Exception("pass unavailable");Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
    var read=new Texture2D(128,128,TextureFormat.RGBAFloat,false,true);read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();
    var point=read.GetPixel(Mathf.RoundToInt((offset*.5f+.5f)*128),64);if(Mathf.Abs(point.r-.2f)>.0001f||Mathf.Abs(point.g-.1f)>.0001f)throw new Exception("pose/blend mismatch "+offset+" "+point);
    int covered=0;foreach(var c in read.GetPixels())if(c.r>.001f)covered++;output.AppendLine("PASS pose="+offset+" alpha=.25 blended="+point+" pixels="+covered);UnityEngine.Object.DestroyImmediate(read);
   }
   File.WriteAllText(Root+"/work/custom160/gpu-decal-result.txt",output.ToString());Debug.Log(output);RenderTexture.active=null;RenderTexture.ReleaseTemporary(target);bones.Dispose();instance.Dispose();UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(normal);UnityEngine.Object.DestroyImmediate(shader);EditorApplication.Exit(0);
  }
  catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
 static void CheckLayer(string shared)
 {
  var layer=File.ReadAllText(Root+"/tools/MaterialShaders/WorldLayeredPbr.hlsl");
  var shader=ShaderUtil.CreateShaderAsset("Shader \"Hidden/BridgeBuilder/LayerCheck\" { SubShader { Pass { ZTest Always ZWrite Off Cull Off HLSLPROGRAM\n#pragma target 5.0\n#pragma vertex vert\n#pragma fragment frag\n#include \"UnityCG.cginc\"\n"+shared+layer+@"
float4 vert(float4 p:POSITION):SV_POSITION{return UnityObjectToClipPos(p);}
float4 frag(float4 p:SV_POSITION):SV_Target {BBMaterialSample s=BBEvaluateLayered(float2(.25,.5),float2(.75,.5),float4(1,1,1,1));return float4(s.albedo,s.smoothness);}
ENDHLSL } } }");
  if(ShaderUtil.ShaderHasError(shader))throw new Exception("layer shader compilation failed");
  var mat=new Material(shader);var owned=new System.Collections.Generic.List<Texture2D>();
  Texture2D Texture(Color a,Color b){var t=new Texture2D(2,1,TextureFormat.RGBAFloat,false,true);t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels(new[]{a,b});t.Apply();owned.Add(t);return t;}
  mat.SetTexture("_Base",Texture(new Color(.8f,.4f,.2f,1),Color.magenta));
  mat.SetTexture("_WorldBase",Texture(Color.green,new Color(.2f,.8f,.6f,1)));
  mat.SetTexture("_Normal",Texture(new Color(.5f,.5f,0,1),new Color(.5f,.5f,0,1)));
  mat.SetTexture("_WorldNormal",owned[2]);
  mat.SetTexture("_Mask",Texture(new Color(.2f,.8f,.25f,.4f),Color.clear));
  mat.SetTexture("_WorldMask",Texture(Color.clear,new Color(.6f,.2f,0,.8f)));
  var rt=RenderTexture.GetTemporary(8,8,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);Graphics.Blit(Texture2D.whiteTexture,rt,mat);RenderTexture.active=rt;
  var read=new Texture2D(8,8,TextureFormat.RGBAFloat,false,true);read.ReadPixels(new Rect(0,0,8,8),0,0);read.Apply();var c=read.GetPixel(4,4);
  if(Mathf.Abs(c.r-.65f)>.0001f||Mathf.Abs(c.g-.5f)>.0001f||Mathf.Abs(c.b-.3f)>.0001f||Mathf.Abs(c.a-.5f)>.0001f)throw new Exception("independent-coordinate layer mismatch "+c);
  File.WriteAllText(Root+"/work/custom160/gpu-layer-result.txt","PASS baseUV=(.25,.5) worldUV=(.75,.5) mask=.25; albedo/smoothness="+c);Debug.Log("PASS independent-coordinate layered PBR "+c);
  RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(read);foreach(var t in owned)UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(shader);
 }
 static BoneWeight Weight()=>new BoneWeight{boneIndex0=1,weight0=1};
}
