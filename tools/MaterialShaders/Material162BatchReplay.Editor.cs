// Actual saved-input GPU replay. Run each public Run method in an isolated Editor host.
// BB_MATERIAL_REPLAY contains mesh0/mesh4, original native texture payloads and both program containers.
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;


public sealed class Material162PipelineAsset : RenderPipelineAsset
{
    public override string renderPipelineShaderTag => "HDRenderPipeline";
    protected override RenderPipeline CreatePipeline() => new Material162Pipeline();
}
public sealed class Material162Pipeline : RenderPipeline
{
    public static RenderTexture[] Targets;
    public static string Pass;
    protected override bool IsRenderRequestSupported<T>(Camera camera, T request) => true;
    protected override void ProcessRenderRequests<T>(ScriptableRenderContext context, Camera camera, T request) => Render(context,new[]{camera});
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(var camera in cameras)
        {
            context.SetupCameraProperties(camera);
            if(!camera.TryGetCullingParameters(out var parameters)) throw new Exception("No culling parameters");
            var culled=context.Cull(ref parameters);
            var command=new CommandBuffer();
            var colors=new RenderTargetIdentifier[Targets.Length];for(int i=0;i<colors.Length;i++)colors[i]=Targets[i];
            command.SetRenderTarget(colors,Targets[0]);command.SetViewport(new Rect(0,0,512,1024));command.ClearRenderTarget(true,false,Color.clear,0);
            var vp=new Matrix4x4(new Vector4(1f/18,0,0,0),new Vector4(0,0,-1f/20,0),new Vector4(0,1f/100,0,0),new Vector4(0,0,1,1));command.SetGlobalMatrix("_ViewProjMatrix",vp);command.SetGlobalMatrix("_InvViewProjMatrix",vp.inverse);command.SetGlobalMatrix("_ViewMatrix",Matrix4x4.identity);command.SetGlobalVector("_WorldSpaceCameraPos_Internal",Vector4.zero);command.SetGlobalVector("_ScreenSize",new Vector4(512,1024,1f/512,1f/1024));context.ExecuteCommandBuffer(command);command.Release();
            var drawing=new DrawingSettings(new ShaderTagId(Pass),new SortingSettings(camera)){enableInstancing=true,perObjectData=PerObjectData.None};
            var filtering=new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(culled,ref drawing,ref filtering);context.Submit();
        }
    }
}
public static unsafe class Material162Brg
{
    static BatchID batch;static BatchMaterialID materialId;static BatchMeshID meshId;
    static int cullCount;
    static JobHandle Cull(BatchRendererGroup group,BatchCullingContext context,BatchCullingOutput output,IntPtr user)
    {
        cullCount++;
        var result=(BatchCullingOutputDrawCommands*)output.drawCommands.GetUnsafePtr();
        result->drawCommands=(BatchDrawCommand*)UnsafeUtility.Malloc(sizeof(BatchDrawCommand),16,Allocator.TempJob);
        result->drawRanges=(BatchDrawRange*)UnsafeUtility.Malloc(sizeof(BatchDrawRange),16,Allocator.TempJob);
        result->visibleInstances=(int*)UnsafeUtility.Malloc(4,16,Allocator.TempJob);
        result->drawCommandCount=1;result->drawRangeCount=1;result->visibleInstanceCount=1;
        result->visibleInstances[0]=0;
        result->drawCommands[0]=new BatchDrawCommand{batchID=batch,materialID=materialId,meshID=meshId,visibleCount=1,splitVisibilityMask=0xff};
        result->drawRanges[0]=new BatchDrawRange{drawCommandsCount=1,filterSettings=new BatchFilterSettings{renderingLayerMask=uint.MaxValue,layer=0,shadowCastingMode=ShadowCastingMode.Off}};
        return default;
    }
    public static void Draw(Mesh mesh,Material material,RenderTexture[] targets,string pass)
    {
        var old=GraphicsSettings.renderPipelineAsset;var asset=ScriptableObject.CreateInstance<Material162PipelineAsset>();
        var cameraObject=new GameObject("Isolated BRG camera");var camera=cameraObject.AddComponent<Camera>();
        camera.transform.position=new Vector3(0,30,0);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.orthographic=true;camera.orthographicSize=100;camera.aspect=18f/100;camera.nearClipPlane=.1f;camera.farClipPlane=60;camera.targetTexture=targets[0];
        var data=new uint[128];var floats=new float[data.Length];
        // Unity DOTS packed float3x4, identity, at byte 64; inverse at 112.
        for(int b=16;b<=28;b+=12){floats[b]=1;floats[b+4]=1;floats[b+8]=1;}
        Buffer.BlockCopy(floats,0,data,0,data.Length*4);
        data[40]=0;data[41]=3;
        GraphicsSettings.renderPipelineAsset=asset;GraphicsSettings.useScriptableRenderPipelineBatching=true;
        using(var buffer=new GraphicsBuffer(GraphicsBuffer.Target.Raw,data.Length,4))
        using(var group=new BatchRendererGroup(Cull,IntPtr.Zero))
        {
            buffer.SetData(data);material.EnableKeyword("DOTS_INSTANCING_ON");material.enableInstancing=true;
            var meta=new NativeArray<MetadataValue>(4,Allocator.Temp);
            meta[0]=new MetadataValue{NameID=Shader.PropertyToID("unity_ObjectToWorld"),Value=0x80000000u|64u};
            meta[1]=new MetadataValue{NameID=Shader.PropertyToID("unity_WorldToObject"),Value=0x80000000u|112u};
            meta[2]=new MetadataValue{NameID=Shader.PropertyToID("colossal_BoneParameters"),Value=0x80000000u|160u};
            meta[3]=new MetadataValue{NameID=Shader.PropertyToID("colossal_LodFade"),Value=0};
            batch=group.AddBatch(meta,buffer.bufferHandle);meta.Dispose();meshId=group.RegisterMesh(mesh);materialId=group.RegisterMaterial(material);group.SetGlobalBounds(mesh.bounds);
            Material162Pipeline.Targets=targets;Material162Pipeline.Pass=pass;GraphicsSettings.renderPipelineAsset=asset;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=targets[0]});
            if(cullCount==0)throw new Exception("BRG culling never executed");
            Debug.Log("BRG actual culling callbacks="+cullCount);
            camera.targetTexture=null;GraphicsSettings.renderPipelineAsset=old;
        }
        UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(asset);
    }
}



public static class Material162Pixels
{
    static string Root => (Environment.GetEnvironmentVariable("BB_MATERIAL_REPLAY") ?? throw new InvalidOperationException("Set BB_MATERIAL_REPLAY to the captured replay directory.")).TrimEnd('/', '\\') + "/";
    public static void Run()
    {
        var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();int status=1;
        try
        {
            var bundle=AssetBundle.LoadFromFile(Root+"layered.bundle");owned.Add(bundle);
            var material=new Material(bundle.LoadAllAssets<Shader>()[0]);owned.Add(material);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL");material.EnableKeyword("_TANGENTSPACE_OCTO");material.EnableKeyword("DECALS_4RT");
            material.DisableKeyword("DOTS_INSTANCING_ON");material.SetVector("colossal_BoneParameters",Vector4.zero);
            material.SetFloat("_CullMode",0);material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
            material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_CoatStrength",1);
            material.SetFloat("_Snow",0);material.SetFloat("_Wetness",0);material.SetFloat("_Porosity",0);
            foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap","_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})
            {
                using(var reader=new BinaryReader(File.OpenRead(Root+name+".Texture2D")))
                {
                    int version=reader.ReadUInt16(),w=reader.ReadUInt16(),h=reader.ReadUInt16(),mips=reader.ReadByte();
                    string format=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    int filter=reader.ReadByte(),wrap=reader.ReadByte(),aniso=reader.ReadByte();
                    var texture=new Texture2D(w,h,TextureFormat.BC7,mips,!format.EndsWith("SRGB"));owned.Add(texture);
                    texture.LoadRawTextureData(reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position)));texture.Apply(false,false);
                    texture.filterMode=(FilterMode)filter;texture.wrapMode=(TextureWrapMode)wrap;texture.anisoLevel=aniso;material.SetTexture(name,texture);
                }
            }
            var empty=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);empty.SetPixel(0,0,Color.clear);empty.Apply();owned.Add(empty);material.SetTexture("_EmissiveColorMap",empty);
            var mesh=new Mesh();owned.Add(mesh);mesh.indexFormat=IndexFormat.UInt32;
            using(var r=new BinaryReader(File.OpenRead(Root+"mesh0.bin")))
            {
                int count=r.ReadInt32(),indexCount=r.ReadInt32(),channels=r.ReadInt32();var index=new int[indexCount];for(int i=0;i<index.Length;i++)index[i]=r.ReadInt32();
                var desc=new VertexAttributeDescriptor[channels];var values=new byte[channels][];
                for(int i=0;i<channels;i++){int attr=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();desc[i]=new VertexAttributeDescriptor((VertexAttribute)attr,(VertexAttributeFormat)fmt,dim,0);values[i]=r.ReadBytes(size);}
                int stride=values.Sum(v=>v.Length/count);var packed=new byte[count*stride];int offset=0;for(int i=0;i<channels;i++){int size=values[i].Length/count;for(int v=0;v<count;v++)Buffer.BlockCopy(values[i],v*size,packed,v*stride+offset,size);offset+=size;} mesh.SetVertexBufferParams(count,desc);mesh.SetVertexBufferData(packed,0,0,packed.Length,0);
                mesh.SetIndices(index,MeshTopology.Triangles,0);mesh.bounds=new Bounds(new Vector3(0,10,0),new Vector3(35,2,200));
            }
            var g=new Vector4[183];g[20]=new Vector4(1f/18,0,0,0);g[21]=new Vector4(0,0,-1f/20,0);g[22]=new Vector4(0,1f/100,0,0);g[23]=new Vector4(0,0,1,1);
            var globals=new ComputeBuffer(g.Length,16,ComputeBufferType.Constant);buffers.Add(globals);globals.SetData(g);Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,g.Length*16);
            var bones=new ComputeBuffer(48,4,ComputeBufferType.Raw);buffers.Add(bones);var matrix=new float[48];for(int b=0;b<3;b++)for(int i=0;i<4;i++)matrix[b*16+i*5]=1;bones.SetData(matrix);material.SetBuffer("_BoneTransforms",bones);
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(512,1024,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);RenderTexture.active=targets[i];GL.Clear(true,true,Color.clear);}
            Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
            if(!material.SetPass(material.FindPass("GBuffer")))throw new Exception("Actual generated GBuffer failed to bind");Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
            var read=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(read);RenderTexture.active=targets[0];read.ReadPixels(new Rect(0,0,512,1024),0,0);read.Apply();
            var hashes=new List<string>();foreach(var target in targets){RenderTexture.active=target;var check=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(check);check.ReadPixels(new Rect(0,0,512,1024),0,0);check.Apply();using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(BitConverter.ToString(sha.ComputeHash(check.GetRawTextureData())).Replace("-",""));}File.WriteAllLines(Root+"Material162Pixels-mrt-hashes.txt",hashes);
            var pixels=read.GetPixels();var live=pixels.Where(p=>p.r+p.g+p.b>.00001f).ToArray();if(live.Length==0)throw new Exception("No actual-source GBuffer pixels");
            var png=new Texture2D(512,1024,TextureFormat.RGBA32,false);owned.Add(png);png.SetPixels(pixels.Select(p=>new Color(Mathf.LinearToGammaSpace(p.r),Mathf.LinearToGammaSpace(p.g),Mathf.LinearToGammaSpace(p.b),1)).ToArray());png.Apply();File.WriteAllBytes(Root+"actual-original-gbuffer.png",png.EncodeToPNG());
            string result="PASS actual saved Original submesh, original six native BC7 payloads, generated native GBuffer PS; pixels="+live.Length+" meanLinear="+live.Average(p=>p.r)+","+live.Average(p=>p.g)+","+live.Average(p=>p.b)+" maxLinear="+live.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b)))+"; non-DOTS standalone draw, not game scene acceptance";
            File.WriteAllText(Root+"pixels-result.txt",result);Debug.Log(result);status=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorApplication.Exit(status);}
    }
}



public static class Material162DecalPixels
{
    static string Root => (Environment.GetEnvironmentVariable("BB_MATERIAL_REPLAY") ?? throw new InvalidOperationException("Set BB_MATERIAL_REPLAY to the captured replay directory.")).TrimEnd('/', '\\') + "/";
    public static void Run()
    {
        var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();int status=1;
        try
        {
            var bundle=AssetBundle.LoadFromFile(Root+"decal.bundle");owned.Add(bundle);
            var material=new Material(bundle.LoadAllAssets<Shader>()[0]);owned.Add(material);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL");material.EnableKeyword("_TANGENTSPACE_OCTO");material.EnableKeyword("DECALS_4RT");
            foreach(var key in new[]{"_MATERIAL_AFFECTS_ALBEDO","_MATERIAL_AFFECTS_NORMAL","_MATERIAL_AFFECTS_MASKMAP"})material.EnableKeyword(key); material.DisableKeyword("DOTS_INSTANCING_ON");material.SetVector("colossal_BoneParameters",Vector4.zero);
            material.SetFloat("_CullMode",0);material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
            material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_CoatStrength",1);
            material.SetVector("colossal_MeshSize",Vector4.one);material.SetVector("colossal_TextureArea",new Vector4(0,0,1,1));material.SetFloat("colossal_LodDistanceFactor",1.7817975f);material.SetFloat("colossal_DecalLayerMask",32);material.SetFloat("_NormalOpacity",1);material.SetFloat("_MetallicOpacity",1);for(int i=0;i<4;i++)material.SetInt("_DecalColorMask"+i,i==3?3:15);material.SetFloat("_Snow",0);material.SetFloat("_Wetness",0);material.SetFloat("_Porosity",0);
            foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
            {
                using(var reader=new BinaryReader(File.OpenRead(Root+"decal"+name+".Texture2D")))
                {
                    int version=reader.ReadUInt16(),w=reader.ReadUInt16(),h=reader.ReadUInt16(),mips=reader.ReadByte();
                    string format=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    int filter=reader.ReadByte(),wrap=reader.ReadByte(),aniso=reader.ReadByte();
                    var texture=new Texture2D(w,h,TextureFormat.BC7,mips,!format.EndsWith("SRGB"));owned.Add(texture);
                    texture.LoadRawTextureData(reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position)));texture.Apply(false,false);
                    texture.filterMode=(FilterMode)filter;texture.wrapMode=(TextureWrapMode)wrap;texture.anisoLevel=aniso;material.SetTexture(name,texture);
                }
            }
            var empty=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);empty.SetPixel(0,0,Color.clear);empty.Apply();owned.Add(empty);material.SetTexture("_EmissiveColorMap",empty);
            var mesh=new Mesh();owned.Add(mesh);mesh.indexFormat=IndexFormat.UInt32;
            using(var r=new BinaryReader(File.OpenRead(Root+"mesh4.bin")))
            {
                int count=r.ReadInt32(),indexCount=r.ReadInt32(),channels=r.ReadInt32();var index=new int[indexCount];for(int i=0;i<index.Length;i++)index[i]=r.ReadInt32();
                var desc=new VertexAttributeDescriptor[channels];var values=new byte[channels][];
                for(int i=0;i<channels;i++){int attr=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();desc[i]=new VertexAttributeDescriptor((VertexAttribute)attr,(VertexAttributeFormat)fmt,dim,0);values[i]=r.ReadBytes(size);}
                int stride=values.Sum(v=>v.Length/count);var packed=new byte[count*stride];int offset=0;for(int i=0;i<channels;i++){int size=values[i].Length/count;for(int v=0;v<count;v++)Buffer.BlockCopy(values[i],v*size,packed,v*stride+offset,size);offset+=size;} mesh.SetVertexBufferParams(count,desc);mesh.SetVertexBufferData(packed,0,0,packed.Length,0);
                mesh.SetIndices(index,MeshTopology.Triangles,0);mesh.bounds=new Bounds(new Vector3(0,10,0),new Vector3(35,2,200));
            }
            var g=new Vector4[183];g[20]=new Vector4(1f/18,0,0,0);g[21]=new Vector4(0,0,-1f/20,0);g[22]=new Vector4(0,1f/100,0,0);g[23]=new Vector4(0,0,1,1);
            var vp=new Matrix4x4(g[20],g[21],g[22],g[23]);for(int i=0;i<4;i++){g[i]=Matrix4x4.identity.GetColumn(i);g[28+i]=vp.inverse.GetColumn(i);}g[46]=new Vector4(512,1024,1f/512,1f/1024);var lod=new ComputeBuffer(1,16,ComputeBufferType.Constant);buffers.Add(lod);lod.SetData(new[]{new Vector4(10000,0,0,0)});Shader.SetGlobalConstantBuffer("$Globals",lod,0,16);Shader.SetGlobalVector("colossal_LodParameters",new Vector4(10000,0,0,0));var depth=new Texture2DArray(512,1024,1,TextureFormat.RFloat,false,true);owned.Add(depth);depth.SetPixels(Enumerable.Repeat(new Color(.5f,0,0,0),512*1024).ToArray(),0);depth.Apply();Shader.SetGlobalTexture("_CameraDepthTexture",depth);var globals=new ComputeBuffer(g.Length,16,ComputeBufferType.Constant);buffers.Add(globals);globals.SetData(g);Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,g.Length*16);
            var bones=new ComputeBuffer(48,4,ComputeBufferType.Raw);buffers.Add(bones);var matrix=new float[48];for(int b=0;b<3;b++)for(int i=0;i<4;i++)matrix[b*16+i*5]=1;bones.SetData(matrix);material.SetBuffer("_BoneTransforms",bones);
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(512,1024,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);RenderTexture.active=targets[i];GL.Clear(true,true,new Color(0,0,0,1),0);}
            Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
            if(!material.SetPass(material.FindPass("DBufferMesh")))throw new Exception("Actual generated GBuffer failed to bind");Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
            var read=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(read);RenderTexture.active=targets[0];read.ReadPixels(new Rect(0,0,512,1024),0,0);read.Apply();
            var hashes=new List<string>();foreach(var target in targets){RenderTexture.active=target;var check=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(check);check.ReadPixels(new Rect(0,0,512,1024),0,0);check.Apply();using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(BitConverter.ToString(sha.ComputeHash(check.GetRawTextureData())).Replace("-",""));}File.WriteAllLines(Root+"Material162DecalPixels-mrt-hashes.txt",hashes);
            var pixels=read.GetPixels();var live=pixels.Where(p=>p.r+p.g+p.b>.00001f).ToArray();if(live.Length==0)throw new Exception("No actual-source GBuffer pixels");
            var png=new Texture2D(512,1024,TextureFormat.RGBA32,false);owned.Add(png);png.SetPixels(pixels.Select(p=>new Color(Mathf.LinearToGammaSpace(p.r),Mathf.LinearToGammaSpace(p.g),Mathf.LinearToGammaSpace(p.b),1)).ToArray());png.Apply();File.WriteAllBytes(Root+"actual-decal.png",png.EncodeToPNG());
            string result="PASS actual saved decal projection mesh, original three native BC7 payloads, current generated DBuffer PS; pixels="+live.Length+" meanLinear="+live.Average(p=>p.r)+","+live.Average(p=>p.g)+","+live.Average(p=>p.b)+" maxLinear="+live.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b)))+"; non-DOTS standalone draw, not game scene acceptance";
            File.WriteAllText(Root+"decal-result.txt",result);Debug.Log(result);status=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorApplication.Exit(status);}
    }
}



public static class Material162LayeredDots
{
    static string Root => (Environment.GetEnvironmentVariable("BB_MATERIAL_REPLAY") ?? throw new InvalidOperationException("Set BB_MATERIAL_REPLAY to the captured replay directory.")).TrimEnd('/', '\\') + "/";
    public static void Run()
    {
        var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();int status=1;
        try
        {
            var bundle=AssetBundle.LoadFromFile(Root+"candidate/native-layered-all-dots.bundle");owned.Add(bundle);
            var material=new Material(bundle.LoadAllAssets<Shader>()[0]);owned.Add(material);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL");material.EnableKeyword("_TANGENTSPACE_OCTO");material.EnableKeyword("DECALS_4RT");
            material.DisableKeyword("DOTS_INSTANCING_ON");material.SetVector("colossal_BoneParameters",Vector4.zero);
            material.SetFloat("_CullMode",0);material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
            material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_CoatStrength",1);
            material.SetFloat("_Snow",0);material.SetFloat("_Wetness",0);material.SetFloat("_Porosity",0);
            foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap","_WorldspaceAlbedo","_WorldspaceNormalMap","_WorldspaceMaskMap"})
            {
                using(var reader=new BinaryReader(File.OpenRead(Root+name+".Texture2D")))
                {
                    int version=reader.ReadUInt16(),w=reader.ReadUInt16(),h=reader.ReadUInt16(),mips=reader.ReadByte();
                    string format=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    int filter=reader.ReadByte(),wrap=reader.ReadByte(),aniso=reader.ReadByte();
                    var texture=new Texture2D(w,h,TextureFormat.BC7,mips,!format.EndsWith("SRGB"));owned.Add(texture);
                    texture.LoadRawTextureData(reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position)));texture.Apply(false,false);
                    texture.filterMode=(FilterMode)filter;texture.wrapMode=(TextureWrapMode)wrap;texture.anisoLevel=aniso;material.SetTexture(name,texture);
                }
            }
            var empty=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);empty.SetPixel(0,0,Color.clear);empty.Apply();owned.Add(empty);material.SetTexture("_EmissiveColorMap",empty);
            var mesh=new Mesh();owned.Add(mesh);mesh.indexFormat=IndexFormat.UInt32;
            using(var r=new BinaryReader(File.OpenRead(Root+"mesh0.bin")))
            {
                int count=r.ReadInt32(),indexCount=r.ReadInt32(),channels=r.ReadInt32();var index=new int[indexCount];for(int i=0;i<index.Length;i++)index[i]=r.ReadInt32();
                var desc=new VertexAttributeDescriptor[channels];var values=new byte[channels][];
                for(int i=0;i<channels;i++){int attr=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();desc[i]=new VertexAttributeDescriptor((VertexAttribute)attr,(VertexAttributeFormat)fmt,dim,0);values[i]=r.ReadBytes(size);}
                int stride=values.Sum(v=>v.Length/count);var packed=new byte[count*stride];int offset=0;for(int i=0;i<channels;i++){int size=values[i].Length/count;for(int v=0;v<count;v++)Buffer.BlockCopy(values[i],v*size,packed,v*stride+offset,size);offset+=size;} mesh.SetVertexBufferParams(count,desc);mesh.SetVertexBufferData(packed,0,0,packed.Length,0);
                mesh.SetIndices(index,MeshTopology.Triangles,0);mesh.bounds=new Bounds(new Vector3(0,10,0),new Vector3(35,2,200));
            }
            var g=new Vector4[183];g[20]=new Vector4(1f/18,0,0,0);g[21]=new Vector4(0,0,-1f/20,0);g[22]=new Vector4(0,1f/100,0,0);g[23]=new Vector4(0,0,1,1);
            var globals=new ComputeBuffer(g.Length,16,ComputeBufferType.Constant);buffers.Add(globals);globals.SetData(g);Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,g.Length*16);
            var bones=new ComputeBuffer(48,4,ComputeBufferType.Raw);buffers.Add(bones);var matrix=new float[48];for(int b=0;b<3;b++)for(int i=0;i<4;i++)matrix[b*16+i*5]=1;bones.SetData(matrix);material.SetBuffer("_BoneTransforms",bones);Shader.SetGlobalBuffer("_BoneTransforms",bones);
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(512,1024,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);RenderTexture.active=targets[i];GL.Clear(true,true,Color.clear);}
            Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
            Material162Brg.Draw(mesh,material,targets,"GBuffer");
            var read=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(read);RenderTexture.active=targets[0];read.ReadPixels(new Rect(0,0,512,1024),0,0);read.Apply();
            var hashes=new List<string>();foreach(var target in targets){RenderTexture.active=target;var check=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(check);check.ReadPixels(new Rect(0,0,512,1024),0,0);check.Apply();using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(BitConverter.ToString(sha.ComputeHash(check.GetRawTextureData())).Replace("-",""));}File.WriteAllLines(Root+"Material162LayeredDots-mrt-hashes.txt",hashes);
            var pixels=read.GetPixels();var live=pixels.Where(p=>p.r+p.g+p.b>.00001f).ToArray();if(live.Length==0)throw new Exception("No actual-source GBuffer pixels");
            var png=new Texture2D(512,1024,TextureFormat.RGBA32,false);owned.Add(png);png.SetPixels(pixels.Select(p=>new Color(Mathf.LinearToGammaSpace(p.r),Mathf.LinearToGammaSpace(p.g),Mathf.LinearToGammaSpace(p.b),1)).ToArray());png.Apply();File.WriteAllBytes(Root+"actual-layered-dots.png",png.EncodeToPNG());
            string result="PASS actual saved Original submesh, original six native BC7 payloads, generated native GBuffer PS; pixels="+live.Length+" meanLinear="+live.Average(p=>p.r)+","+live.Average(p=>p.g)+","+live.Average(p=>p.b)+" maxLinear="+live.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b)))+"; real BatchRendererGroup DOTS submission, not game scene acceptance";
            File.WriteAllText(Root+"layered-dots-result.txt",result);Debug.Log(result);status=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorApplication.Exit(status);}
    }
}







public static class Material162DotsPixels
{
    static string Root => (Environment.GetEnvironmentVariable("BB_MATERIAL_REPLAY") ?? throw new InvalidOperationException("Set BB_MATERIAL_REPLAY to the captured replay directory.")).TrimEnd('/', '\\') + "/";
    public static void Run()
    {
        var owned=new List<UnityEngine.Object>();var buffers=new List<ComputeBuffer>();int status=1;
        try
        {
            var bundle=AssetBundle.LoadFromFile(Root+"candidate/BoneDecal.bundle");owned.Add(bundle);
            var material=new Material(bundle.LoadAllAssets<Shader>()[0]);owned.Add(material);
            material.EnableKeyword("_GPU_ANIMATION_PROCEDURAL");material.EnableKeyword("_TANGENTSPACE_OCTO");material.EnableKeyword("DECALS_4RT");
            foreach(var key in new[]{"_MATERIAL_AFFECTS_ALBEDO","_MATERIAL_AFFECTS_NORMAL","_MATERIAL_AFFECTS_MASKMAP"})material.EnableKeyword(key); material.DisableKeyword("DOTS_INSTANCING_ON");material.SetVector("colossal_BoneParameters",Vector4.zero);
            material.SetFloat("_CullMode",0);material.SetFloat("_ZTestGBuffer",(float)CompareFunction.Always);
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_WorldspaceUVScale",.4f);
            material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_CoatStrength",1);
            material.SetVector("colossal_MeshSize",Vector4.one);material.SetVector("colossal_TextureArea",new Vector4(0,0,1,1));material.SetFloat("colossal_LodDistanceFactor",1.7817975f);material.SetFloat("colossal_DecalLayerMask",32);material.SetFloat("_NormalOpacity",1);material.SetFloat("_MetallicOpacity",1);for(int i=0;i<4;i++)material.SetInt("_DecalColorMask"+i,i==3?3:15);material.SetFloat("_Snow",0);material.SetFloat("_Wetness",0);material.SetFloat("_Porosity",0);
            foreach(var name in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
            {
                using(var reader=new BinaryReader(File.OpenRead(Root+"decal"+name+".Texture2D")))
                {
                    int version=reader.ReadUInt16(),w=reader.ReadUInt16(),h=reader.ReadUInt16(),mips=reader.ReadByte();
                    string format=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                    int filter=reader.ReadByte(),wrap=reader.ReadByte(),aniso=reader.ReadByte();
                    var texture=new Texture2D(w,h,TextureFormat.BC7,mips,!format.EndsWith("SRGB"));owned.Add(texture);
                    texture.LoadRawTextureData(reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position)));texture.Apply(false,false);
                    texture.filterMode=(FilterMode)filter;texture.wrapMode=(TextureWrapMode)wrap;texture.anisoLevel=aniso;material.SetTexture(name,texture);
                }
            }
            var empty=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);empty.SetPixel(0,0,Color.clear);empty.Apply();owned.Add(empty);material.SetTexture("_EmissiveColorMap",empty);
            var mesh=new Mesh();owned.Add(mesh);mesh.indexFormat=IndexFormat.UInt32;
            using(var r=new BinaryReader(File.OpenRead(Root+"mesh4.bin")))
            {
                int count=r.ReadInt32(),indexCount=r.ReadInt32(),channels=r.ReadInt32();var index=new int[indexCount];for(int i=0;i<index.Length;i++)index[i]=r.ReadInt32();
                var desc=new VertexAttributeDescriptor[channels];var values=new byte[channels][];
                for(int i=0;i<channels;i++){int attr=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();desc[i]=new VertexAttributeDescriptor((VertexAttribute)attr,(VertexAttributeFormat)fmt,dim,0);values[i]=r.ReadBytes(size);}
                int stride=values.Sum(v=>v.Length/count);var packed=new byte[count*stride];int offset=0;for(int i=0;i<channels;i++){int size=values[i].Length/count;for(int v=0;v<count;v++)Buffer.BlockCopy(values[i],v*size,packed,v*stride+offset,size);offset+=size;} mesh.SetVertexBufferParams(count,desc);mesh.SetVertexBufferData(packed,0,0,packed.Length,0);
                mesh.SetIndices(index,MeshTopology.Triangles,0);mesh.bounds=new Bounds(new Vector3(0,10,0),new Vector3(35,2,200));
            }
            var g=new Vector4[183];g[20]=new Vector4(1f/18,0,0,0);g[21]=new Vector4(0,0,-1f/20,0);g[22]=new Vector4(0,1f/100,0,0);g[23]=new Vector4(0,0,1,1);
            var vp=new Matrix4x4(g[20],g[21],g[22],g[23]);for(int i=0;i<4;i++){g[i]=Matrix4x4.identity.GetColumn(i);g[28+i]=vp.inverse.GetColumn(i);}g[46]=new Vector4(512,1024,1f/512,1f/1024);var lod=new ComputeBuffer(1,16,ComputeBufferType.Constant);buffers.Add(lod);lod.SetData(new[]{new Vector4(10000,0,0,0)});Shader.SetGlobalConstantBuffer("$Globals",lod,0,16);Shader.SetGlobalVector("colossal_LodParameters",new Vector4(10000,0,0,0));var depth=new Texture2DArray(512,1024,1,TextureFormat.RFloat,false,true);owned.Add(depth);depth.SetPixels(Enumerable.Repeat(new Color(.5f,0,0,0),512*1024).ToArray(),0);depth.Apply();Shader.SetGlobalTexture("_CameraDepthTexture",depth);var globals=new ComputeBuffer(g.Length,16,ComputeBufferType.Constant);buffers.Add(globals);globals.SetData(g);Shader.SetGlobalConstantBuffer("ShaderVariablesGlobal",globals,0,g.Length*16);
            var bones=new ComputeBuffer(48,4,ComputeBufferType.Raw);buffers.Add(bones);var matrix=new float[48];for(int b=0;b<3;b++)for(int i=0;i<4;i++)matrix[b*16+i*5]=1;bones.SetData(matrix);material.SetBuffer("_BoneTransforms",bones);Shader.SetGlobalBuffer("_BoneTransforms",bones);
            var targets=new RenderTexture[4];for(int i=0;i<4;i++){targets[i]=new RenderTexture(512,1024,i==0?24:0,RenderTextureFormat.ARGBFloat);targets[i].Create();owned.Add(targets[i]);RenderTexture.active=targets[i];GL.Clear(true,true,new Color(0,0,0,1),0);}
            Graphics.SetRenderTarget(targets.Select(t=>t.colorBuffer).ToArray(),targets[0].depthBuffer);
            Material162Brg.Draw(mesh,material,targets,"DBufferMesh");
            var read=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(read);RenderTexture.active=targets[0];read.ReadPixels(new Rect(0,0,512,1024),0,0);read.Apply();
            var hashes=new List<string>();foreach(var target in targets){RenderTexture.active=target;var check=new Texture2D(512,1024,TextureFormat.RGBAFloat,false,true);owned.Add(check);check.ReadPixels(new Rect(0,0,512,1024),0,0);check.Apply();using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(BitConverter.ToString(sha.ComputeHash(check.GetRawTextureData())).Replace("-",""));}File.WriteAllLines(Root+"Material162DotsPixels-mrt-hashes.txt",hashes);
            var pixels=read.GetPixels();var live=pixels.Where(p=>p.r+p.g+p.b>.00001f).ToArray();if(live.Length==0)throw new Exception("No actual-source GBuffer pixels");
            var png=new Texture2D(512,1024,TextureFormat.RGBA32,false);owned.Add(png);png.SetPixels(pixels.Select(p=>new Color(Mathf.LinearToGammaSpace(p.r),Mathf.LinearToGammaSpace(p.g),Mathf.LinearToGammaSpace(p.b),1)).ToArray());png.Apply();File.WriteAllBytes(Root+"actual-dots-decal.png",png.EncodeToPNG());
            string result="PASS actual saved decal projection mesh, original three native BC7 payloads, current generated DBuffer PS; pixels="+live.Length+" meanLinear="+live.Average(p=>p.r)+","+live.Average(p=>p.g)+","+live.Average(p=>p.b)+" maxLinear="+live.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b)))+"; real BatchRendererGroup DOTS submission, not game scene acceptance";
            File.WriteAllText(Root+"dots-result.txt",result);Debug.Log(result);status=0;
        }
        catch(Exception e){Debug.LogException(e);}
        finally{RenderTexture.active=null;foreach(var b in buffers)b.Dispose();foreach(var item in owned.AsEnumerable().Reverse())if(item!=null)UnityEngine.Object.DestroyImmediate(item);EditorApplication.Exit(status);}
    }
}
